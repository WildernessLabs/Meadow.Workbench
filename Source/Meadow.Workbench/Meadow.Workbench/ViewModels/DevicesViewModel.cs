using DialogHostAvalonia;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

internal class DevicesViewModel : FeatureViewModel
{
    private DeviceService _deviceService;
    private FirmwareService _firmwareService;
    private StorageService _storageService;
    private DeviceViewModel? _selectedDevice;
    private bool _flashOS;
    private bool _flashAll;
    private bool _flashRuntime;
    private bool _flashCoprocessor;
    private bool _showInfo;

    public IReactiveCommand AddDeviceCommand { get; }
    public IReactiveCommand FlashDeviceCommand { get; }
    public IReactiveCommand ShowInfoCommand { get; }
    public IReactiveCommand ShowOutputCommand { get; }

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    public DevicesViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();
        _deviceService!.DeviceConnected += OnDeviceConnected;
        _deviceService!.DeviceDisconnected += OnDeviceDisconnected;
        _deviceService!.DeviceRemoved += OnDeviceRemoved;

        _storageService = Locator.Current.GetService<StorageService>();

        _firmwareService = Locator.Current.GetService<FirmwareService>();

        foreach (var device in _deviceService.KnownDevices)
        {
            Devices.Add(new DeviceViewModel(device, _deviceService, _storageService));
        }

        AddDeviceCommand = ReactiveCommand.CreateFromTask(OnAddDevice);
        FlashDeviceCommand = ReactiveCommand.CreateFromTask(FlashSelectedDevice);
        ShowInfoCommand = ReactiveCommand.Create(ShowDeviceInfo);
        ShowOutputCommand = ReactiveCommand.Create(ShowDeviceOutput);
        ShowInfo = true;
    }

    private void ShowDeviceInfo()
    {
        ShowInfo = true;
    }

    private void ShowDeviceOutput()
    {
        ShowInfo = false;
    }

    private void OnDeviceRemoved(object? sender, string e)
    {
        var existing = Devices.FirstOrDefault(d => d.DeviceID == e);
        if (existing != null)
        {
            Devices.Remove(existing);
        }
    }

    public override void OnActivated()
    {
        // force a refresh of default firmware (in case the user downloaded on another tab)
        this.RaisePropertyChanged(nameof(DefaultFirmwareVersion));
    }

    public string DefaultFirmwareVersion
    {
        get => _firmwareService.CurrentStore?.DefaultPackage?.Version ?? "unknown";
    }

    private async Task FlashSelectedDevice()
    {
        if (_selectedDevice == null) return;
        if (!_selectedDevice.IsConnected) return;

        // DFU
        await _deviceService.FlashFirmwareWithDfu(
            _selectedDevice.RootInfo.LastRoute,
            FlashOS,
            FlashRuntime,
            FlashCoprocessor,
            DefaultFirmwareVersion);

        // NON_DFU
        //var vm = new OtAFirmwareFlashViewModel(
        //    _deviceService,
        //    _selectedDevice.RootInfo.LastRoute,
        //    FlashOS,
        //    FlashCoprocessor);

        //var dialog = new OtAFirmwareFlashDialog(vm);

        //var result = await DialogHost.Show(dialog);
    }

    public DeviceViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedDevice, value);
            this.RaisePropertyChanged(nameof(DefaultFirmwareVersion));
        }
    }

    public bool ShowInfo
    {
        get => _showInfo;
        set => this.RaiseAndSetIfChanged(ref _showInfo, value);
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

    private void OnDeviceConnected(object? sender, DeviceInformation e)
    {
        var dvm = Devices.FirstOrDefault(d => d.DeviceID == e.DeviceID);
        if (dvm == null)
        {
            dvm = new DeviceViewModel(e, _deviceService, _storageService);
            Devices.Add(dvm);
        }
        else
        {
            dvm.Update(e);
        }

        dvm.IsConnected = true;
    }

    private void OnDeviceDisconnected(object? sender, DeviceInformation e)
    {
        var dvm = Devices.FirstOrDefault(d => d.DeviceID == e.DeviceID);
        if (dvm != null)
        {
            dvm.IsConnected = false;
        }
    }

    private async Task OnAddDevice()
    {
        var result = await DialogHost.Show(new AddDeviceDialog());
    }
}
