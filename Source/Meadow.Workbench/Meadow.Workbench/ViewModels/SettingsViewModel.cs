using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _developerMode;
    private bool _betaFeatures;
    private string _packagesFolder;
    private SettingsService? _settingsService;

    public SettingsViewModel()
    {
        _settingsService = Locator.Current.GetService<SettingsService>();
        _packagesFolder = _settingsService?.PackagesFolder ?? string.Empty;
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

    public string PackagesFolder
    {
        get => _packagesFolder;
        set
        {
            this.RaiseAndSetIfChanged(ref _packagesFolder, value);
            if (_settingsService != null)
            {
                _settingsService.PackagesFolder = value;
            }
        }
    }

    public async Task BrowsePackagesFolderAsync(Visual? visual)
    {
        if (visual == null) return;

        var topLevel = TopLevel.GetTopLevel(visual);
        if (topLevel == null) return;

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select Packages Folder"
            });

        if (result != null && result.Count > 0)
        {
            PackagesFolder = result[0].Path.LocalPath;
        }
    }
}
