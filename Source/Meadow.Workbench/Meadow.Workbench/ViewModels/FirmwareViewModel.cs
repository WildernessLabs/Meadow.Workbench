using Meadow.Software;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private bool _makeDownloadDefault = true;

    public ObservableCollection<FirmwarePackageViewModel> FirmwareVersions { get; } = new();
    public IReactiveCommand DownloadLatestCommand { get; }
    public IReactiveCommand MakeDefaultCommand { get; }
    public IReactiveCommand DeleteFirmwareCommand { get; }

    public FirmwareViewModel()
    {
        _manager = new FileManager();
        _ = RefreshCurrentStore();

        DownloadLatestCommand = ReactiveCommand.CreateFromTask(DownloadLatest);
        MakeDefaultCommand = ReactiveCommand.CreateFromTask(MakeSelectedTheDefault);
        DeleteFirmwareCommand = ReactiveCommand.CreateFromTask(DeleteSelectedFirmware);
    }

    private async Task DeleteSelectedFirmware()
    {
        if (SelectedFirmwareVersion == null) return;
        try
        {
            await _store!.DeletePackage(SelectedFirmwareVersion.Version);
            await RefreshCurrentStore();
        }
        catch (Exception ex)
        {
            // TODO: log this?
            Debug.WriteLine(ex.Message);
        }
    }

    private async Task MakeSelectedTheDefault()
    {
        if (SelectedFirmwareVersion == null) return;
        try
        {
            await _store!.SetDefaultPackage(SelectedFirmwareVersion.Version);
            await RefreshCurrentStore();
        }
        catch (Exception ex)
        {
            // TODO: log this?
            Debug.WriteLine(ex.Message);
        }
    }

    private async Task DownloadLatest()
    {
        if (LatestAvailableVersion == null) return;

        // TODO: progress indicator
        // _store.DownloadProgress += ....

        await _store?.RetrievePackage(LatestAvailableVersion, true);

        if (MakeDownloadDefault)
        {
            await _store.SetDefaultPackage(LatestAvailableVersion);
        }

        await RefreshCurrentStore();

    }

    public bool UpdateIsAvailable
    {
        get
        {
            if (LatestAvailableVersion == null || _store == null) return false;

            return !_store.Any(f => f.Version == LatestAvailableVersion);
        }
    }

    public bool MakeDownloadDefault
    {
        get => _makeDownloadDefault;
        private set => this.RaiseAndSetIfChanged(ref _makeDownloadDefault, value);
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
        foreach (var f in _store.OrderByDescending(s => s.Version))
        {
            FirmwareVersions.Add(new FirmwarePackageViewModel(f, f.Version == _store.DefaultPackage?.Version));
        }
        _ = CheckForUpdate();
    }
}
