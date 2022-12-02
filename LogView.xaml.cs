namespace ORDU;

public partial class LogView : ContentPage
{
    public LogView(string Server)
    {
        InitializeComponent();
        Title = "�鿴��־" + Server;
        HttpClient httpClient = new();
        try
        {
            UILog.Text = httpClient.GetStringAsync("http://120.48.72.37/" + Server + ".txt").Result;
        }
        catch
        {
            UILog.Text = "����ʼ";
        }
    }
}