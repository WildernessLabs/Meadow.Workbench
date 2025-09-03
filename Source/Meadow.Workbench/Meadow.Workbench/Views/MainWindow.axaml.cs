using Avalonia.Controls;
using Meadow.Workbench.Services;
using Splat;
using System;
using System.Linq;

namespace Meadow.Workbench.Views;

public partial class MainWindow : Window
{
    private readonly SettingsService? _settingsService;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = Locator.Current.GetService<SettingsService>();

        if (_settingsService?.StartupWindowInfo is { } info)
        {
            // Validate and set size with reasonable bounds
            var width = info.Width > 0 && info.Width <= 4000 ? info.Width : 1200;
            var height = info.Height > 0 && info.Height <= 3000 ? info.Height : 800;

            this.Width = Math.Max(400, width);   // Minimum width
            this.Height = Math.Max(300, height); // Minimum height

            var targetPosition = new Avalonia.PixelPoint(info.Left, info.Top);

            // Check if the saved position is on a valid screen
            var screens = Screens?.All?.ToList();
            if (screens != null && screens.Count > 0)
            {
                var isPositionValid = screens.Any(screen =>
                    screen.Bounds.Contains(targetPosition));

                if (isPositionValid)
                {
                    this.Position = targetPosition;
                }
                else
                {
                    // If saved position is not valid, center on primary screen
                    var primaryScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.First();
                    this.Position = new Avalonia.PixelPoint(
                        primaryScreen.Bounds.X + (primaryScreen.Bounds.Width - (int)this.Width) / 2,
                        primaryScreen.Bounds.Y + (primaryScreen.Bounds.Height - (int)this.Height) / 2);
                }
            }
            else
            {
                // No screen info available, use center screen startup
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        else
        {
            // First time startup - set reasonable defaults and center on current screen
            this.Width = 1200;
            this.Height = 800;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_settingsService != null)
        {
            // Validate values before saving to prevent bad data
            var width = this.Width > 0 && this.Width <= 4000 && !double.IsNaN(this.Width) ? this.Width : 1200;
            var height = this.Height > 0 && this.Height <= 3000 && !double.IsNaN(this.Height) ? this.Height : 800;
            var left = this.Position.X >= -1000 && this.Position.X <= 10000 ? this.Position.X : 0;
            var top = this.Position.Y >= -1000 && this.Position.Y <= 10000 ? this.Position.Y : 0;

            var info = new WindowSettings
            {
                Width = Math.Max(400, width),   // Ensure minimum size
                Height = Math.Max(300, height), // Ensure minimum size
                Left = left,
                Top = top
            };
            _settingsService.StartupWindowInfo = info;
        }

        base.OnClosing(e);
    }
}
