﻿using ORDU;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Orts.MultiPlayerServer
{
    public class Host
    {
        private readonly int port;
        private readonly Label Info;
        private static readonly Encoding encoding = Encoding.Unicode;
        private static readonly int charSize = encoding.GetByteCount("0");
        private readonly Dictionary<string, TcpClient> onlinePlayers = new();
        private static readonly byte[] initData = encoding.GetBytes("10: SERVER YOU");
        private static readonly byte[] serverChallenge = encoding.GetBytes(" 21: SERVER WhoCanBeServer");
        private static readonly byte[] blankToken = encoding.GetBytes(" ");
        private static readonly byte[] playerToken = encoding.GetBytes(": PLAYER ");
        private static readonly byte[] quitToken = encoding.GetBytes(": QUIT ");
        private string currentServer;

        public Host(int port, Label Info)
        {
            this.port = port;
            this.Info = Info;
        }

        public async Task Run()
        {
            try
            {
                TcpListener listener = new(IPAddress.Any, port);
                listener.Start();
                MainPage.servers_list.Add(new Server
                {
                    Port = port.ToString(),
                    Cancellation = new CancellationTokenSource(),
                    Log = "服务开始\n"
                });
                Info.Text = "启动成功";
                bool run = true;
                while (run)
                {
                    try
                    {
                        Pipe pipe = new();
                        foreach (Server p in MainPage.servers_list)
                        {
                            if (p.Port == port.ToString() && p.Cancellation.IsCancellationRequested)
                            {
                                listener.Stop();
                                run = false;
                            }
                        }
                        TcpClient tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                        _ = PipeFillAsync(tcpClient, pipe.Writer);
                        _ = PipeReadAsync(tcpClient, pipe.Reader);
                    }
                    catch (Exception ex)
                    {
                        Info.Text = ex.Message;
                    }
                }
            }
            catch (SocketException socketException)
            {
                Info.Text = socketException.Message;
                throw;
            }
        }
        private async Task PipeFillAsync(TcpClient tcpClient, PipeWriter writer)
        {
            const int minimumBufferSize = 1024;
            _ = currentServer;
            NetworkStream networkStream = tcpClient.GetStream();

            while (tcpClient.Connected)
            {
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);

                int bytesRead = await networkStream.ReadAsync(memory).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }
                writer.Advance(bytesRead);

                FlushResult result = await writer.FlushAsync().ConfigureAwait(false);

                if (result.IsCompleted)
                {
                    break;
                }
            }
            await writer.CompleteAsync().ConfigureAwait(false);
        }

        private bool ReadPlayerName(in ReadOnlySequence<byte> sequence, ref string playerName, out SequencePosition bytesProcessed)
        {
            Span<byte> playerSeparator = playerToken.AsSpan();
            Span<byte> blankSeparator = blankToken.AsSpan();

            SequenceReader<byte> reader = new(sequence);

            if (reader.TryReadTo(out ReadOnlySequence<byte> playerPreface, playerSeparator))
            {
                if (reader.TryReadTo(out ReadOnlySequence<byte> playerNameSequence, blankSeparator))
                {
                    int maxDigits = 4;
                    if (playerPreface.GetIntFromEnd(ref maxDigits, out int length, encoding))
                    {
                        ReadOnlySequence<byte> before = sequence.Slice(0, playerPreface.Length - maxDigits * charSize);
                        foreach (ReadOnlyMemory<byte> message in before)
                        {
                            if (message.Length > 0)
                                Broadcast(playerName,message);
                        }
                        reader.Rewind(playerSeparator.Length + playerNameSequence.Length + maxDigits * charSize);

                        if (reader.Remaining >= length * charSize)
                        {
                            string newPlayerName = playerNameSequence.GetString(encoding);
                            foreach (Server s in MainPage.servers_list)
                            {
                                if (int.Parse(s.Port) == port && newPlayerName.Length < 7)
                                {
                                    s.Players.Add(newPlayerName);
                                }
                            }
                            ReadOnlySequence<byte> playerMessage = reader.Sequence.Slice(before.Length, (length + maxDigits + 2) * charSize);
                            if (currentServer != playerName)
                            {
                                foreach (ReadOnlyMemory<byte> message in playerMessage)
                                {
                                    SendMessage(currentServer, message).Wait();
                                }
                            }
                            playerName = newPlayerName;
                            bytesProcessed = sequence.GetPosition(before.Length + playerMessage.Length);
                            return true;
                        }
                    }
                }
            }
            bytesProcessed = sequence.GetPosition(sequence.Length);
            return false;
        }

        private static string ReadQuitMessage(ReadOnlySequence<byte> sequence)
        {
            Span<byte> quitSeparator = quitToken.AsSpan();
            Span<byte> blankSeparator = blankToken.AsSpan();

            SequenceReader<byte> reader = new(sequence);

            if (reader.TryReadTo(out ReadOnlySequence<byte> _, quitSeparator))
            {
                if (reader.TryReadTo(out ReadOnlySequence<byte> playerName, blankSeparator))
                {
                    return playerName.GetString(encoding);
                }
            }
            return null;
        }
        private async Task PipeReadAsync(TcpClient tcpClient, PipeReader reader)
        {
            string playerName = Guid.NewGuid().ToString();
            bool playerNameSet = false;
            string quitPlayer;
            onlinePlayers.Add(playerName, tcpClient);
            if (onlinePlayers.Count == 1)
            {
                currentServer = playerName;
                await SendMessage(playerName, initData).ConfigureAwait(false);
            }

            while (tcpClient.Client.Connected)
            {
                ReadResult result = await reader.ReadAsync().ConfigureAwait(false);

                ReadOnlySequence<byte> buffer = result.Buffer;

                if (!playerNameSet)
                {
                    string player = playerName;
                    if (ReadPlayerName(buffer, ref player, out SequencePosition bytesProcessed))
                    {
                        onlinePlayers.Remove(playerName);
                        if (currentServer == playerName)
                            currentServer = playerName = player;
                        else
                            playerName = player;
                        onlinePlayers.Add(playerName, tcpClient);
                        playerNameSet = true;
                    }
                    reader.AdvanceTo(bytesProcessed);
                }
                else
                {
                    if (!string.IsNullOrEmpty(quitPlayer = ReadQuitMessage(buffer)) && playerName == quitPlayer)
                        break;

                    foreach (ReadOnlyMemory<byte> message in buffer)
                    {
                        Broadcast(playerName, message);
                    }
                    reader.AdvanceTo(buffer.End);
                }

                if (result.IsCompleted)
                {
                    break;
                }
            }
            await RemovePlayer(playerName).ConfigureAwait(false);

            await reader.CompleteAsync().ConfigureAwait(false);
        }

        private void Broadcast(string playerName, ReadOnlyMemory<byte> buffer)
        {
            foreach (Server s in MainPage.servers_list)
            {
                if (int.Parse(s.Port) == port)
                {
                    s.Log += encoding.GetString(buffer.Span).Replace("\r", Environment.NewLine, StringComparison.OrdinalIgnoreCase) + "\n";
                }
            }
            Parallel.ForEach(onlinePlayers.Keys, async player =>
            {
                if (player != playerName)
                {
                    try
                    {
                        TcpClient client = onlinePlayers[player];
                        NetworkStream clientStream = client.GetStream();
                        await clientStream.WriteAsync(buffer).ConfigureAwait(false);
                        await clientStream.FlushAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is System.IO.IOException || ex is SocketException || ex is InvalidOperationException)
                    {
                        if (playerName != null)
                            await RemovePlayer(playerName).ConfigureAwait(false);
                    }
                }
            });
        }

        private async Task SendMessage(string playerName, ReadOnlyMemory<byte> buffer)
        {
            foreach (Server s in MainPage.servers_list)
            {
                if (int.Parse(s.Port) == port)
                {
                    s.Log += encoding.GetString(buffer.Span).Replace("\r", Environment.NewLine, StringComparison.OrdinalIgnoreCase) + "\n";
                }
            }
            try
            {
                TcpClient client = onlinePlayers[playerName];
                NetworkStream clientStream = client.GetStream();
                await clientStream.WriteAsync(buffer).ConfigureAwait(false);
                await clientStream.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is SocketException || ex is InvalidOperationException)
            {
                if (playerName != null)
                    await RemovePlayer(playerName).ConfigureAwait(false);
            }
        }

        private async Task RemovePlayer(string playerName)
        {
            if (onlinePlayers.Remove(playerName))
            {
                string lostMessage = $"LOST { playerName}";
                foreach (Server s in MainPage.servers_list)
                {
                    if (int.Parse(s.Port) == port && s.Players != null && playerName.Length < 7)
                    {
                        s.Players.Remove(playerName);
                    }
                }
                byte[] lostPlayer = encoding.GetBytes($" {lostMessage.Length}: {lostMessage}");
                Broadcast(playerName, lostPlayer);
                if (currentServer == playerName)
                {
                    Broadcast(playerName, serverChallenge);
                    await Task.Delay(5000).ConfigureAwait(false);
                    if (onlinePlayers.Count > 0)
                    {
                        Broadcast(null, lostPlayer);
                        currentServer = onlinePlayers.Keys.First();
                        string appointmentMessage = $"SERVER {currentServer}";
                        lostPlayer = encoding.GetBytes($" {appointmentMessage.Length}: {appointmentMessage}");
                        Broadcast(null, lostPlayer);
                    }
                }
            }
        }
    }

    public static class ReadOnlySequenceExtensions
    {
        public static bool GetIntFromEnd(in this ReadOnlySequence<byte> payload, ref int maxDigits, out int result, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            int charSize = encoding.GetByteCount("0");

            if (maxDigits > 0)
            {
                if (maxDigits * charSize > payload.Length)
                    maxDigits = (int)payload.Length / charSize;
                SequencePosition position = payload.GetPosition(payload.Length - maxDigits * charSize);
                if (payload.TryGet(ref position, out ReadOnlyMemory<byte> lengthIndicator, false))
                {
                    if (int.TryParse(encoding.GetString(lengthIndicator.Span), out result))
                        return true;
                    else
                    {
                        maxDigits--;
                        return GetIntFromEnd(payload, ref maxDigits, out result, encoding);
                    }
                }
            }
            result = 0;
            return false;
        }

        public static string GetString(in this ReadOnlySequence<byte> payload, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            return payload.IsSingleSegment ? encoding.GetString(payload.FirstSpan)
                : GetStringInternal(payload, encoding);

            static string GetStringInternal(in ReadOnlySequence<byte> payload, Encoding encoding)
            {
                // linearize
                int length = checked((int)payload.Length);
                byte[] oversized = ArrayPool<byte>.Shared.Rent(length);
                try
                {
                    payload.CopyTo(oversized);
                    return encoding.GetString(oversized, 0, length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(oversized);
                }
            }
        }
    }

}
