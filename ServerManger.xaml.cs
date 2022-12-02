namespace ORDU;

public partial class ServerManger : ContentPage
{
    public ServerManger()
    {
        InitializeComponent();
        CoreService.serverManger = this;
        Thread.Sleep(350);
        CoreService.Send("GetVersion");
    }
    public async void UINewServerClick(object sender, EventArgs e)
    {
        string port = "";
        try
        {
            port = await DisplayPromptAsync("端口请求", "请输入30000到40000之间的整数");
            if (int.Parse(port) < 30000 || int.Parse(port) > 40000)
            {
                throw new Exception();
            }
            CoreService.Send("NewServer:" + port);
        }
        catch
        {
            if (port != null)
            {
                UIInfo.Text = "错误：请检查端口输入";
            }
        }
    }
    public async void UIServersListItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (UIServersList.SelectedItem != null)
        {
            foreach (Server p in CoreService.ServersList)
            {
                if (p.Port == ((Server)UIServersList.SelectedItem).Port)
                {
                    await Navigation.PushAsync(new ServerOperator(((Server)UIServersList.SelectedItem).Port));
                    UIServersList.SelectedItem = null;
                    break;
                }
            }
        }
    }
    private void UIRefreshListClick(object sender, EventArgs e)
    {
        CoreService.Send("GetServers");
    }
}