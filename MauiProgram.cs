using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
namespace ORDU;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
        AppCenter.Start("android=d97ecf82-6261-4eb1-89c8-82000ea09ef5;" +
                      "windowsdesktop=1a058cbd-d729-47d3-a886-1f6f4f3df211;",
                      typeof(Analytics), typeof(Crashes));
        CoreService.Start();
        return builder.Build();
	}
}
