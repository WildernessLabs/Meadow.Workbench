using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;

namespace Meadow.Workbench.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _developerMode;
    private bool _betaFeatures;
    private SettingsService? _settingsService;

    public SettingsViewModel()
    {
        _settingsService = Locator.Current.GetService<SettingsService>();
    }

    public bool DeveloperModeEnabled
    {
        get => _developerMode;
        set => this.RaiseAndSetIfChanged(ref _developerMode, value);
    }

    public bool BetaFeaturesEnabled
    {
        get => _betaFeatures;
        set => this.RaiseAndSetIfChanged(ref _betaFeatures, value);
    }

    public bool UseDfuForFlashing
    {
        get => _settingsService?.UseDfu ?? false;
        set
        {
            _settingsService!.UseDfu = value;
            this.RaisePropertyChanged(nameof(UseDfuForFlashing));
        }
    }
}
