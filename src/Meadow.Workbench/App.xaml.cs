namespace Meadow.Workbench;

public partial class App : Application
{
    readonly MyWindow Window;

    public App(MyWindow window)
    {
        Window = window;

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => Window;

    /*
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
    */
}

public class MyWindow : Window
{
    public MyWindow(AppShell shell, UserSettingsService settings)
        : base(shell)
    {
        Settings = settings;
    }

    readonly UserSettingsService Settings;

    protected override void OnCreated()
    {
        base.OnCreated();

        Settings.LoadSettings();
        X = Settings.Settings.ShellPosition.X;
        Y = Settings.Settings.ShellPosition.Y;
        Width = Settings.Settings.ShellSize.Width;
        Height = Settings.Settings.ShellSize.Height;
    }

    protected override void OnDestroying()
    {
        Settings.Settings.ShellPosition = new Point(X, Y);
        Settings.Settings.ShellSize = new Size(Width, Height);
        Settings.SaveCurrentSettings();

        base.OnDestroying();
    }
}