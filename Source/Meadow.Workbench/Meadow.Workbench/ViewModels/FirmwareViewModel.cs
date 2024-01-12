using Meadow.Software;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class FirmwareViewModel : FeatureViewModel
{
    private const string CurrentStoreName = "Meadow F7";

    private readonly FileManager _manager;
    private IFirmwarePackageCollection? _store;
    private FirmwarePackageViewModel? _selectedFirmware;
    private string? _latestAvailable;

    public ObservableCollection<FirmwarePackageViewModel> FirmwareVersions { get; } = new();

    public FirmwareViewModel()
    {
        _manager = new FileManager();
        _ = RefreshCurrentStore();
    }

    public bool UpdateIsAvailable
    {
        get
        {
            if (LatestAvailableVersion == null || _store == null) return false;

            return !_store.Any(f => f.Version == LatestAvailableVersion);
        }
    }

    public string? LatestAvailableVersion
    {
        get => _latestAvailable;
        private set => this.RaiseAndSetIfChanged(ref _latestAvailable, value);
    }

    private async Task CheckForUpdate()
    {
        var latest = await _store?.GetLatestAvailableVersion();

        if (latest != null)
        {
            LatestAvailableVersion = latest;
            this.RaisePropertyChanged(nameof(UpdateIsAvailable));
        }
    }

    public FirmwarePackageViewModel? SelectedFirmwareVersion
    {
        get => _selectedFirmware;
        set => this.RaiseAndSetIfChanged(ref _selectedFirmware, value);
    }

    private async Task RefreshCurrentStore()
    {
        FirmwareVersions.Clear();
        await _manager.Refresh();
        if (_store == null)
        {
            _store = _manager.Firmware[CurrentStoreName];
        }
        foreach (var f in _store)
        {
            FirmwareVersions.Add(new FirmwarePackageViewModel(f, f.Version == _store.DefaultPackage?.Version));
        }
        _ = CheckForUpdate();
    }
}
