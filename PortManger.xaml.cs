namespace ORDU;

public partial class PortManger : ContentPage
{
	private readonly string Server;
    private readonly ListView ui_ports_list;
	public PortManger(string Server, ListView ui_ports_list)
	{
		InitializeComponent();
		Title = "�����˿�" + Server;
		this.Server = Server;
        this.ui_ports_list = ui_ports_list;
        foreach (Server p in MainPage.servers_list)
        {
            if (p.Port == Server)
            {
                UI_Players_List.ItemsSource = p.Players;
                UI_Log.SetBinding(Label.TextProperty,"Log");
                UI_Log.BindingContext = p;
                break;
            }
        }
    }

	private async void Remove_port(object sender, EventArgs e)
	{
        bool answer = await DisplayAlert("�Ƿ�ȷ��","��ȷ��Ҫ�رմ˶˿�", "��", "��");
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
#pragma warning disable CS4014
            Navigation.PopAsync();
        }
    }
}