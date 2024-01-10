using Avalonia.Controls;
using Meadow.Workbench.Services;
using Splat;

namespace Meadow.Workbench.Views;

public partial class MainWindow : Window
{
    private SettingsService? _settingsService;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = Locator.Current.GetService<SettingsService>();

        if (_settingsService?.StartupWindowInfo is { } info)
        {
            this.Width = info.Width;
            this.Height = info.Height;
            this.Position = new Avalonia.PixelPoint(info.Left, info.Top);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_settingsService != null)
        {
            var info = new WindowSettings
            {
                Width = this.Width,
                Height = this.Height,
                Left = this.Position.X,
                Top = this.Position.Y
            };
            _settingsService.StartupWindowInfo = info;
        }

        base.OnClosing(e);
    }
}
