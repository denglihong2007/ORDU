using System.Collections.ObjectModel;

namespace ORDU;

public partial class ServerManger : ContentPage
{
    public ObservableCollection<string> ServersList = new();
    private readonly ManualResetEvent ManualResetEvent = new(false);
    public ServerManger()
    {
        InitializeComponent();
        NetworkService.serverManger = this;
        Thread.Sleep(350);
        NetworkService.Send("GetVersion");
        Thread t = new(GetServersList)
        {
            IsBackground = true
        };
        t.Start();
    }
    private async void UINewServerClick(object sender, EventArgs e)
    {
        string port = "";
        try
        {
            port = await DisplayPromptAsync("端口请求", "请输入30000到40000之间的整数");
            if (int.Parse(port) < 30000 || int.Parse(port) > 40000)
            {
                throw new Exception();
            }
            NetworkService.Send("NewServer:" + port);
        }
        catch
        {
            if (port != null)
            {
                UIInfo.Text = "错误：请检查端口输入";
            }
        }
    }
    private async void UIServersListItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (UIServersList.SelectedItem != null)
        {
            await Navigation.PushAsync(new ServerOperator((string)UIServersList.SelectedItem));
            UIServersList.SelectedItem = null;
        }
    }
    private void GetServersList()
    {
        while (true)
        {
            Thread.Sleep(3500);
            NetworkService.Send("GetServersList");
            ManualResetEvent.WaitOne(Timeout.Infinite);
        }
    }
    protected override void OnAppearing()
    {
        ManualResetEvent.Set();
        base.OnAppearing();
    }
    protected override void OnDisappearing()
    {
        ManualResetEvent.Reset();
        base.OnDisappearing();
    }
}