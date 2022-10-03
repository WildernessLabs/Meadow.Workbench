using CommunityToolkit.Maui;
using Meadow.CLI.Core;
using Meadow.Workbench.ViewModels;
using Meadow.Workbench.Views;
using Microsoft.Extensions.Logging;

namespace Meadow.Workbench;

public static class MauiProgram
{

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});


		builder.Services
			.AddTransient<AppShell>()
			.AddSingleton<ILogger, CaptureLogger>()
			.AddSingleton<MeadowConnectionManager>()
			.AddTransient<DeviceInfoViewModel>()
			.AddTransient<DeviceInfoPage>()
			;

		return builder.Build();
	}
}
