using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Meadow.Workbench.Models;
using Meadow.Workbench.Services;
using Meadow.Workbench.ViewModels;
using Meadow.Workbench.Views;
using Splat;

namespace Meadow.Workbench;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // TODO: load configuration for what features to show

        var fs = new FeatureService();
        fs.Features.Add(new Feature<DevicesView, DevicesViewModel>
        {
            Title = "Devices"
        });
        fs.Features.Add(new Feature<FilesView, FilesViewModel>
        {
            Title = "Files"
        });
        fs.Features.Add(new Feature<FirmwareView, FirmwareViewModel>
        {
            Title = "Firmware"
        });
        fs.Features.Add(new Feature<CodeView, CodeViewModel>
        {
            Title = "Code"
        });
        Locator.CurrentMutable.RegisterConstant(fs);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
