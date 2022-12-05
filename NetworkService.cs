﻿using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace ORDU
{
    internal class NetworkService
    {
        public static readonly string Version = "1.3.1";
        private static Socket socket;
        private static byte[] buffer;
        public static ServerManger serverManger;
        public static ServerOperator serverOperator;
        public static TcpClient TcpClient = new();
        public static void Start()
        {
            Thread t = new(Recieve)
            {
                IsBackground = true
            };
            t.Start();
            Thread.Sleep(350);
        }
        public static void Send(string message)
        {
            try
            {
                socket.Send(Encoding.UTF8.GetBytes(message));
            }
            catch
            {
            }
        }
        public static void Recieve()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            buffer = new byte[1024];
            IPAddress ip;
#if DEBUG
            ip = IPAddress.Parse("127.0.0.1");
#else
            ip = IPAddress.Parse("120.48.72.37");
#endif
            IPEndPoint point = new(ip, 86);
            IAsyncResult connResult = socket.BeginConnect(point, null, null);
            connResult.AsyncWaitHandle.WaitOne(300, true);
            if (!connResult.IsCompleted)
            {
                socket.Close();
            }
            else
            {
                while (true)
                {
                    Action safeWrite = delegate 
                    {
                        ;
                    };
                    int length = socket.Receive(buffer);
                    string message = Encoding.UTF8.GetString(buffer, 0, length);//字节转换为字符串
                    if (message.Contains("Version"))
                    {
                        string GetVersion = message[(message.IndexOf(":") + 1)..];
                        if (GetVersion == Version)
                        {
                            serverManger.UICoder.Text = "v" + Version + " 最新版本\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
                        }
                        else
                        {
                            serverManger.UICoder.Text = "v" + Version + " 有新版本：v" + GetVersion + "\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
                        }
                    }
                    else if (message.Contains("ServersList"))
                    {
                        if(serverManger.ServersList.Count != JsonConvert.DeserializeObject<ObservableCollection<string>>(message[(message.IndexOf(":") + 1)..]).Count)
                        {
                            if (message == "ServersList:[]")
                            {
                                safeWrite = delegate
                                {
                                    serverManger.UIServersList.IsVisible = false;
                                };
                            }
                            else
                            {
                                serverManger.ServersList = JsonConvert.DeserializeObject<ObservableCollection<string>>(message[(message.IndexOf(":") + 1)..]);
                                safeWrite = delegate
                                {
                                    serverManger.UIServersList.ItemsSource = serverManger.ServersList;
                                    serverManger.UIServersList.IsVisible = true;
                                };
                            }
                            MainThread.BeginInvokeOnMainThread(safeWrite);
                        }
                    }
                    else if (message.Contains("ServerAlreadyExists"))
                    {
                        safeWrite = delegate
                        {
                            serverManger.UIInfo.Text = "服务器" + message[(message.IndexOf(":") + 1)..] + "已经存在";
                        };
                        MainThread.BeginInvokeOnMainThread(safeWrite);
                    }
                    else if (message.Contains("ServerHasBeenStopped"))
                    {
                        safeWrite = delegate
                        {
                            serverManger.UIInfo.Text = "服务器" + message[(message.IndexOf(":") + 1)..] + "关闭成功";
                        };
                        MainThread.BeginInvokeOnMainThread(safeWrite);
                    }
                    else if (message.Contains("SuccessfullyStartedTheNewServer"))
                    {
                        safeWrite = delegate
                        {
                            serverManger.UIInfo.Text = "成功打开服务器" + message[(message.IndexOf(":") + 1)..];
                        };
                        MainThread.BeginInvokeOnMainThread(safeWrite);
                    }
                    else if (message.Contains("FailedToStartNewServer"))
                    {
                        safeWrite = delegate
                        {
                            serverManger.UIInfo.Text = "服务器" + message[(message.IndexOf(":") + 1)..] + "打开失败";
                        };
                        MainThread.BeginInvokeOnMainThread(safeWrite);
                    }
                    else if (message.Contains("InsufficientPermissions"))
                    {
                        safeWrite = delegate
                        {
                            serverManger.UIInfo.Text = "权限不足";
                        };
                        MainThread.BeginInvokeOnMainThread(safeWrite);
                    }
                    else if (message.Contains("ServerDetails"))
                    {
                        JObject obj = JObject.Parse(message[(message.IndexOf(":") + 1)..]);
                        if (serverOperator.PlayersList.Count != ((JArray)obj["Players"]).ToObject<ObservableCollection<string>>().Count)
                        {
                            if (((JArray)obj["Players"]).ToObject<ObservableCollection<string>>().Count == 0)
                            {
                                safeWrite = delegate
                                {
                                    serverOperator.UIPlayersList.IsVisible = false;
                                };
                            }
                            else
                            {
                                serverOperator.PlayersList = ((JArray)obj["Players"]).ToObject<ObservableCollection<string>>();
                                safeWrite = delegate
                                {
                                    serverOperator.UIPlayersList.ItemsSource = serverOperator.PlayersList;
                                    serverOperator.UIPlayersList.IsVisible = true;
                                };
                            }
                            MainThread.BeginInvokeOnMainThread(safeWrite);
                        }
                        safeWrite = delegate
                        {

                            string Dispatcher = obj["Dispatcher"].ToString();
                            if (Dispatcher != "" && Dispatcher.Length <= 5)
                            { 
                                serverOperator.UIDispatcher.Text = "当前调度：" + Dispatcher;
                            }
                            else
                            {
                                serverOperator.UIDispatcher.Text = "等待玩家加入";
                            }
                        };
                        MainThread.BeginInvokeOnMainThread(safeWrite);
                    }
                }
            }
        }
    }
}
