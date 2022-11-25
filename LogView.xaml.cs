namespace ORDU;

public partial class LogView : ContentPage
{
	public LogView(string Server)
	{
		InitializeComponent();
        Title = "查看日志" + Server;
        foreach (Server p in MainPage.servers_list)
        {
            if (p.Port == Server)
            {

                UILog.SetBinding(Label.TextProperty, "Log");
                UILog.BindingContext = p;
                break;
            }
        }
    }
}