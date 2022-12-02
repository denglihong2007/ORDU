using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Collections.ObjectModel;

namespace ORDU
{
    internal class CoreService
    {
        public static readonly string Version = "1.3.0";
        private static Socket socket;
        private static byte[] buffer;
        public static ServerManger serverManger;
        public static ObservableCollection<Server> ServersList = new();
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
            ip = IPAddress.Parse("120.48.72.37");
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
                    try
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
                                try
                                {
                                    serverManger.UICoder.Text = "v" + Version + " 最新版本\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
                                }
                                catch
                                {
                                    safeWrite = delegate
                                    {
                                        serverManger.UICoder.Text = "v" + Version + " 最新版本\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
                                    };
                                }
                            }
                            else
                            {
                                try
                                {
                                    serverManger.UICoder.Text = "v" + Version + " 有新版本：v" + GetVersion + "\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
                                }
                                catch
                                {
                                    safeWrite = delegate
                                    {
                                        serverManger.UICoder.Text = "v" + Version + " 有新版本：v" + GetVersion + "\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
                                    };
                                }
                            }
                        }
                        else if (message.Contains("ServersList"))
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
                                ServersList = JsonConvert.DeserializeObject<ObservableCollection<Server>>(message[(message.IndexOf(":") + 1)..]);
                                safeWrite = delegate
                                {
                                    serverManger.UIServersList.ItemsSource = ServersList;
                                    serverManger.UIServersList.IsVisible = true;
                                };
                            }
                        }
                        else if (message.Contains("ServerAlreadyExists"))
                        {
                            safeWrite = delegate
                            {
                                serverManger.UIInfo.Text = "服务器" + message[(message.IndexOf(":") + 1)..] + "已经存在";
                            };
                        }
                        else if (message.Contains("ServerHasBeenStopped"))
                        {
                            safeWrite = delegate
                            {
                                serverManger.UIInfo.Text = "服务器" + message[(message.IndexOf(":") + 1)..] + "关闭成功";
                            };
                        }
                        else if (message.Contains("SuccessfullyStartedTheNewServer"))
                        {
                            safeWrite = delegate
                            {
                                serverManger.UIInfo.Text = "成功打开服务器" + message[(message.IndexOf(":") + 1)..];
                            };
                        }
                        else if (message.Contains("FailedToStartNewServer"))
                        {
                            safeWrite = delegate
                            {
                                serverManger.UIInfo.Text = "服务器" + message[(message.IndexOf(":") + 1)..] + "打开失败";
                            };
                        }
                        else if (message.Contains("InsufficientPermissions"))
                        {
                            safeWrite = delegate
                            {
                                serverManger.UIInfo.Text = "权限不足";
                            };
                        }
                        MainThread.BeginInvokeOnMainThread(safeWrite);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
class Server
{
    public string IP 
    {
        get
        {
            return "120.48.72.37:" + Port;
        } 
        set 
        {
        }
    }
    public string Port { get; set; }
    public ObservableCollection<string> Players { get { return players; } set { } }
    private readonly ObservableCollection<string> players = new();
}