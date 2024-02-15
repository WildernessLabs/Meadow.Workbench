using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Meadow.Cloud.Client;
using Meadow.Workbench.Models;
using Meadow.Workbench.Services;
using Meadow.Workbench.ViewModels;
using Meadow.Workbench.Views;
using Splat;
using DeviceService = Meadow.Workbench.Services.DeviceService;

namespace Meadow.Workbench;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settingsService = new SettingsService();

        Locator.CurrentMutable.RegisterConstant(settingsService);

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
            Title = "Code",
            IsVisible = settingsService.ShowDeveloperFeatures && settingsService.ShowBetaFeatures
        });
        fs.Features.Add(new Feature<SimulationView, SimulationViewModel>
        {
            Title = "Simulation",
            IsVisible = settingsService.ShowDeveloperFeatures && settingsService.ShowBetaFeatures
        });

        Locator.CurrentMutable.RegisterConstant(fs);
        Locator.CurrentMutable.RegisterConstant(new StorageService());

        var im = new Cloud.Client.Identity.IdentityManager();
        Locator.CurrentMutable.RegisterConstant(im);
        Locator.CurrentMutable.RegisterConstant(new UserService(im));

        var cloudClient = new MeadowCloudClient(
            new System.Net.Http.HttpClient(),
            im,
            MeadowCloudUserAgent.Workbench);
        Locator.CurrentMutable.RegisterConstant<IMeadowCloudClient>(cloudClient);

        Locator.CurrentMutable.RegisterConstant(new FirmwareService());
        Locator.CurrentMutable.RegisterConstant(new DeviceService());

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
