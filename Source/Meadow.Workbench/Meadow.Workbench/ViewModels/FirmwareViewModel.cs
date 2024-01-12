using Meadow.Software;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class FirmwarePackageViewModel : ViewModelBase
{
    private FirmwarePackage _package;
    private bool _isDefault;

    public FirmwarePackageViewModel(FirmwarePackage package, bool isDefault)
    {
        _package = package;
        IsDefault = isDefault;
    }

    public string Version => _package.Version;
    public bool HasOsFiles => !string.IsNullOrEmpty(_package.OSWithBootloader);
    public bool HasRuntimeFiles => !string.IsNullOrEmpty(_package.Runtime);
    public bool HasBclFiles => !string.IsNullOrEmpty(_package.BclFolder);
    public bool HasCoprocessorFiles => !string.IsNullOrEmpty(_package.CoprocApplication);

    public bool IsDefault
    {
        get => _isDefault;
        set => this.RaiseAndSetIfChanged(ref _isDefault, value);
    }
}

public class FirmwareViewModel : FeatureViewModel
{
    private const string CurrentStoreName = "Meadow F7";
    private readonly FileManager _manager;
    private IFirmwarePackageCollection? _store;
    private FirmwarePackageViewModel? _selectedFirmware;

    public ObservableCollection<FirmwarePackageViewModel> FirmwareVersions { get; } = new();

    public FirmwareViewModel()
    {
        _manager = new FileManager();
        _ = RefreshCurrentStore();
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
    }
}
