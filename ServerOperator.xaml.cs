using System.Collections.ObjectModel;

namespace ORDU;

public partial class ServerOperator : ContentPage
{
	private readonly string Server;
    public ObservableCollection<string> PlayersList = new();
    private readonly ManualResetEvent ManualResetEvent = new(false);
    public ServerOperator(string Server)
	{
		InitializeComponent();
		Title = "管理服务器" + Server;
		this.Server = Server;
        Thread t = new(GetServerDetails)
        {
            IsBackground = true
        };
        t.Start();
        NetworkService.serverOperator = this;
    }
	private async void StopServer(object sender, EventArgs e)
	{
        bool answer = await DisplayAlert("是否确认","您确定要关闭此服务器", "是", "否");
        if (answer)
        {
            NetworkService.Send("StopServer:" + Server);
            await Navigation.PopAsync();
        }
    }
    private async void ShowLog(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LogView(Server));
    }
    private void ChangeDispatcher(object sender, EventArgs e)
    {
        if (UIPlayersList.SelectedItem != null)
        {
            NetworkService.Send("ChangeDispatcher:" + Server + UIPlayersList.SelectedItem);
        }
    }
    private void GetServerDetails()
    {
        while (true)
        {
            NetworkService.Send("GetServerDetails:" + Server);
            ManualResetEvent.WaitOne(Timeout.Infinite);
            Thread.Sleep(3500);
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