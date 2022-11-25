using Orts.MultiPlayerServer;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace ORDU;

public partial class MainPage : ContentPage
{
    public readonly static ObservableCollection<Server> servers_list = new();
    public static TcpClient TcpClient = new();
    readonly string version = "1.2.3";
    private Socket socket;
    private byte[] buffer;
    public MainPage()
    {
        InitializeComponent();
        ui_ports_list.ItemsSource = servers_list;
        Coder.Text = "v" + version + " 正在连接服务器\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
        Thread t = new(Recieve);
        t.Start();
        Send("GetVersion");
    }
    private async void New_port(object sender, EventArgs e)
    {
        string port = "";
        try
        {
            port = await DisplayPromptAsync("端口请求", "请输入0到65535之间的整数");
            foreach (Server p in servers_list)
            {
                if (p.Port == port)
                {
                    Info.Text = "端口已经存在";
                    break;
                }
            }
            if (Info.Text != "端口已经存在")
            {
                if (int.Parse(port) < 0 || int.Parse(port) > 65535)
                {
                    throw new Exception();
                }
                Host server = new(int.Parse(port), Info);
                Task serverTask = server.Run();
                if (servers_list.Count == 0)
                {
                    ui_ports_list.IsVisible = false;
                }
                else
                {
                    ui_ports_list.IsVisible = true;
                }
            }
        }
        catch
        {
            if (port != null)
            {
                Info.Text = "错误：请检查端口输入";
            }
        }
    }
    bool SendAvailable = false;
    bool FailedToConnectToServer = false;
    public void Recieve()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        buffer = new byte[1024];
        IPAddress ip = IPAddress.Parse("120.48.72.37");
        IPEndPoint point = new(ip, 86);
        IAsyncResult connResult = socket.BeginConnect(point, null, null);
        connResult.AsyncWaitHandle.WaitOne(1000, true);  //等待2秒
        if (!connResult.IsCompleted)
        {
            socket.Close();
            FailedToConnectToServer = true;
        }
        else
        {
            SendAvailable = true;
            while (true)
            {
                int length = socket.Receive(buffer);
                string message = Encoding.UTF8.GetString(buffer, 0, length);//字节转换为字符串
                MessageDeal(message);//消息处理
            }
        }
    }
    public void Send(string message)
    {
        while (true)
        {
            if (SendAvailable)
            {
                socket.Send(Encoding.UTF8.GetBytes(message));
                break;
            }
            else if (FailedToConnectToServer)
            {
                Coder.Text = "v" + version + " 连接服务器失败\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
                break;
            }
        }
    }
    public void MessageDeal(string message)
    {
        if (message.Contains("Version"))
        {
            string GetVersion = message[(message.IndexOf(":") + 1)..];
            if (GetVersion == version)
            {
                Coder.Text = "v" + version + " 最新版本\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
            }
            else
            {
                Coder.Text = "v" + version + " 有新版本\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
            }
        }
    }
    private async void Ui_ports_list_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (ui_ports_list.SelectedItem != null)
        {
            foreach (Server p in servers_list)
            {
                if (p.Port == ((Server)ui_ports_list.SelectedItem).Port)
                {
                    await Navigation.PushAsync(new PortManger(((Server)ui_ports_list.SelectedItem).Port, ui_ports_list));
                    ui_ports_list.SelectedItem = null;
                    break;
                }
            }
        }
    }
}

public class Server
{
    public string Port { get; set; }
    public ObservableCollection<string> Players { get { return players; } set { } }
    public string Log { get; set; }
    public CancellationTokenSource Cancellation { get; set; }
    private readonly ObservableCollection<string> players = new();
}