using Meadow.Software;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
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
    private bool _flashCoprocessor;
    private bool _flashAll;
    private bool _flashOS;
    private bool _flashRuntime;
    private readonly DeviceService _deviceService;
    private string? _selectedRoute;
    private bool _useDfu;
    private bool _defuDeviceAvailable;

    public ObservableCollection<FirmwarePackageViewModel> FirmwareVersions { get; } = new();
    public ObservableCollection<string> ConnectedRoutes { get; } = new();

    public IReactiveCommand DownloadLatestCommand { get; }
    public IReactiveCommand MakeDefaultCommand { get; }
    public IReactiveCommand DeleteFirmwareCommand { get; }
    public IReactiveCommand FlashCommand { get; }

    public FirmwareViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();

        foreach (var d in _deviceService.KnownDevices)
        {
            if (d.IsConnected && d.LastRoute != null)
            {
                ConnectedRoutes.Add(d.LastRoute);
            }
        }

        _deviceService!.DeviceConnected += OnDeviceConnected;
        _deviceService!.DeviceDisconnected += OnDeviceDisconnected;
        _deviceService!.DeviceRemoved += OnDeviceRemoved;


        _manager = new FileManager(null); //ToDo
        _ = RefreshCurrentStore();

        DownloadLatestCommand = ReactiveCommand.CreateFromTask(DownloadLatest);
        MakeDefaultCommand = ReactiveCommand.CreateFromTask(MakeSelectedTheDefault);
        DeleteFirmwareCommand = ReactiveCommand.CreateFromTask(DeleteSelectedFirmware);
        FlashCommand = ReactiveCommand.CreateFromTask(FlashSelectedFirmware);
    }

    public bool UsingDfu
    {
        get => _useDfu;
        set
        {
            this.RaiseAndSetIfChanged(ref _useDfu, value);

            _ = CheckForDfuDevice();
        }
    }

    private Task CheckForDfuDevice()
    {
        return Task.Run(() =>
        {
            DfuDeviceAvailable = _deviceService.IsLibUsbDeviceConnected();
        });
    }

    private void OnDeviceConnected(object? sender, DeviceInformation e)
    {
        if (e.LastRoute != null)
        {
            ConnectedRoutes.Add(e.LastRoute);
        }
    }

    private void OnDeviceDisconnected(object? sender, DeviceInformation e)
    {
        if (e.LastRoute != null)
        {
            ConnectedRoutes.Remove(e.LastRoute);
        }
    }

    private void OnDeviceRemoved(object? sender, string e)
    {
        ConnectedRoutes.Remove(e);
    }

    public bool FlashAll
    {
        get => _flashAll;
        set
        {
            if (value == FlashAll) return;
            this.RaiseAndSetIfChanged(ref _flashAll, value);

            FlashOS = value;
            FlashRuntime = value;
            FlashCoprocessor = value;
        }
    }

    public bool FlashOS
    {
        get => _flashOS;
        set
        {
            this.RaiseAndSetIfChanged(ref _flashOS, value);
            if (!value)
            {
                _flashAll = false;
                this.RaisePropertyChanged(nameof(FlashAll));
            }
        }
    }

    public bool FlashRuntime
    {
        get => _flashRuntime;
        set
        {
            this.RaiseAndSetIfChanged(ref _flashRuntime, value);
            if (!value)
            {
                _flashAll = false;
                this.RaisePropertyChanged(nameof(FlashAll));
            }
        }
    }

    public bool FlashCoprocessor
    {
        get => _flashCoprocessor;
        set
        {
            this.RaiseAndSetIfChanged(ref _flashCoprocessor, value);
            if (!value)
            {
                _flashAll = false;
                this.RaisePropertyChanged(nameof(FlashAll));
            }
        }
    }

    public bool DfuDeviceAvailable
    {
        get => _defuDeviceAvailable;
        set
        {
            this.RaiseAndSetIfChanged(ref _defuDeviceAvailable, value);
        }
    }

    private async Task FlashSelectedFirmware()
    {
        if (SelectedFirmwareVersion == null) return;

        if (UsingDfu)
        {
            await _deviceService.FlashFirmwareWithDfu(SelectedRoute, FlashOS, FlashRuntime, FlashCoprocessor, SelectedFirmwareVersion.Version);
        }
        else
        {
            await _deviceService.FlashFirmwareWithOtA(SelectedRoute, FlashOS, FlashCoprocessor, SelectedFirmwareVersion.Version);
        }
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

    public string? SelectedRoute
    {
        get => _selectedRoute;
        private set
        {
            this.RaiseAndSetIfChanged(ref _selectedRoute, value);

            var device = _deviceService.KnownDevices.FirstOrDefault(d => d.LastRoute == _selectedRoute);
            if (device != null)
            {
                if (Version.TryParse(device.OsVersion, out Version? v))
                {
                    if (v != null)
                    {
                        if (v.Minor < 7)
                        {
                            UsingDfu = true;
                            return;
                        }
                    }
                }
            }
            UsingDfu = false;
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
        foreach (var f in _store.OrderByDescending(s => s.Version))
        {
            FirmwareVersions.Add(new FirmwarePackageViewModel(f, f.Version == _store.DefaultPackage?.Version));
        }
        _ = CheckForUpdate();
    }
}
