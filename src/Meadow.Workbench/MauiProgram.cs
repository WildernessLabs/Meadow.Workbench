﻿using CommunityToolkit.Maui;
using Meadow.CLI.Core;
using Meadow.Workbench.ViewModels;
using Meadow.Workbench.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;

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
			.ConfigureLifecycleEvents(events =>
			{
#if WINDOWS
				events.AddWindows(windows => windows
                        .OnWindowCreated(window =>
                        {
							window.Closed += OnWindowClosed;
                        }));
#endif
			})
			;

		_settingsService = new UserSettingsService();

		builder.Services
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
#if WINDOWS
    static void OnWindowClosed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
		var w = sender as MauiWinUIWindow;
        
		_settingsService.Settings.ShellSize = new Size(w.Bounds.Width, w.Bounds.Height);
		_settingsService.Settings.ShellPosition = new Point(w.Bounds.Left, w.Bounds.Top);
		_settingsService.SaveCurrentSettings();

        Debug.WriteLine($"{w.Bounds}");
    }
#endif
}
