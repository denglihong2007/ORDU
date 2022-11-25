namespace ORDU;

public partial class PortManger : ContentPage
{
	private readonly string Server;
    private readonly ListView ui_ports_list;
	public PortManger(string Server, ListView ui_ports_list)
	{
		InitializeComponent();
		Title = "管理端口" + Server;
		this.Server = Server;
        this.ui_ports_list = ui_ports_list;
        foreach (Server p in MainPage.servers_list)
        {
            if (p.Port == Server)
            {
                UI_Players_List.ItemsSource = p.Players;
                break;
            }
        }
    }

	private async void Remove_port(object sender, EventArgs e)
	{
        bool answer = await DisplayAlert("是否确认","您确定要关闭此端口", "是", "否");
        if (answer)
        {
            foreach (Server p in MainPage.servers_list)
            {
                if (p.Port == Server)
                {
                    p.Cancellation.Cancel();
                    MainPage.servers_list.Remove(p);
                    if (MainPage.servers_list.Count == 0)
                    {
                        ui_ports_list.IsVisible = false;
                    }
                    else
                    {
                        ui_ports_list.IsVisible = true;
                    }
                    break;
                }
            }
            await Navigation.PopAsync();
        }
    }

    private async void ShowLog(object sender, EventArgs e)
    {
        foreach (Server p in MainPage.servers_list)
        {
            if (p.Port == Server)
            {
                await Navigation.PushAsync(new LogView(Server));
                break;
            }
        }
        
    }
}