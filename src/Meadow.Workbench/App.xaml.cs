using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Meadow.Workbench;

public partial class App : Application
{
    public App(AppShell shell, UserSettingsService settings)
    {
        InitializeComponent();

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if WINDOWS
            var mauiWindow = handler.VirtualView;
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.Resize(new SizeInt32((int)settings.Settings.ShellSize.Width, (int)settings.Settings.ShellSize.Height));
            appWindow.Move(new PointInt32((int)settings.Settings.ShellPosition.X, (int)settings.Settings.ShellPosition.Y));
#endif
        });

        MainPage = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return base.CreateWindow(activationState);
    }

}
