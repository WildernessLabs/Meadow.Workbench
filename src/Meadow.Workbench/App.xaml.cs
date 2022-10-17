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
            
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            appWindow.Resize(new Windows.Graphics.SizeInt32((int)settings.Settings.ShellSize.Width, (int)settings.Settings.ShellSize.Height));
            appWindow.Move(new Windows.Graphics.PointInt32((int)settings.Settings.ShellPosition.X, (int)settings.Settings.ShellPosition.Y));
#endif
        });

        MainPage = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return base.CreateWindow(activationState);
    }

}
