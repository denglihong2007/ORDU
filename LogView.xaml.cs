namespace ORDU;

public partial class LogView : ContentPage
{
	public LogView(string Server)
	{
		InitializeComponent();
        Title = "�鿴��־" + Server;
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