using Orts.MultiPlayerServer;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Net;

namespace ORDU;

public partial class MainPage : ContentPage
{
    public static ObservableCollection<Server> servers_list = new();
    public MainPage()
	{
		InitializeComponent();
        ui_ports_list.ItemsSource = servers_list;
        string version = "1.2.2";
        string update;
        try
        {
            Root rb = JsonConvert.DeserializeObject<Root>(MainPage.GetHtml("http://120.48.72.37/ORDU.json"));
            if (rb.Version != version)
            {
                update = " 不是最新版本";
            }
            else
            {
                update = " 最新版本";
            }
        }
        catch
        {
            update = " 检查更新失败";
        }
        Coder.Text = "v" + version + update + "\n猪排骨联控小屋 @电排骨\n交流群:1143414240";
    }
    public static string GetHtml(string html)
    {
        string pageHtml = "";
#pragma warning disable SYSLIB0014
        //TMD安卓用不了HttpClient，你TM给我报SYSLIB0014
        WebClient MyWebClient = new();
        Byte[] pageData = MyWebClient.DownloadData(html);
        MemoryStream ms = new(pageData);
        using (StreamReader sr = new(ms))
        {
            pageHtml = sr.ReadToEnd();
        }
        return pageHtml;
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
                Host server = new(int.Parse(port),Info);
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
    public class Root
    {
        /// <summary>
        /// 
        /// </summary>
        public string Version { get; set; }

    }

    private async void Ui_ports_list_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (ui_ports_list.SelectedItem != null)
        {
            foreach (Server p in servers_list)
            {
                if (p.Port == ((Server)ui_ports_list.SelectedItem).Port)
                {
                    await Navigation.PushAsync(new PortManger(((Server)ui_ports_list.SelectedItem).Port,ui_ports_list));
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
    private ObservableCollection<string> players = new();
}