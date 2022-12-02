namespace ORDU;

public partial class ServerOperator : ContentPage
{
	private readonly string Server;
	public ServerOperator(string Server)
	{
		InitializeComponent();
		Title = "���������" + Server;
		this.Server = Server;
        foreach (Server p in CoreService.ServersList)
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
        bool answer = await DisplayAlert("�Ƿ�ȷ��","��ȷ��Ҫ�رմ˶˿�", "��", "��");
        if (answer)
        {
            CoreService.Send("StopServer:" + Server);
            await Navigation.PopAsync();
        }
    }

    private async void ShowLog(object sender, EventArgs e)
    {
        foreach (Server p in CoreService.ServersList)
        {
            if (p.Port == Server)
            {
                await Navigation.PushAsync(new LogView(Server));
                break;
            }
        }
        
    }

    private void UIRefreshListClick(object sender, EventArgs e)
    {
        CoreService.Send("GetServers");
    }
}