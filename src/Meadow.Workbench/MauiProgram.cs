using CommunityToolkit.Maui;
using Meadow.CLI.Core;
using Meadow.Workbench.ViewModels;
using Meadow.Workbench.Views;
using Microsoft.Extensions.Logging;

namespace Meadow.Workbench;

public static partial class MauiProgram
{
    private static UserSettingsService _settingsService;

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
            })
            ;

        _settingsService = new UserSettingsService();

        builder.Services
            .AddTransient<MyWindow>()
            .AddTransient<AppShell>()
            .AddSingleton<ILogger, CaptureLogger>()
            .AddSingleton<UserSettingsService>(_settingsService)
            .AddSingleton<MeadowConnectionManager>()
            .AddTransient<DeviceInfoViewModel>()
            .AddTransient<DeviceInfoPage>()
#if WINDOWS
			.AddTransient<IFolderPicker, Platforms.Windows.FolderPicker>();
#elif MACCATALYST
            .AddTransient<IFolderPicker, Platforms.MacCatalyst.FolderPicker>();
#endif
        ;

        return builder.Build();
    }
}
