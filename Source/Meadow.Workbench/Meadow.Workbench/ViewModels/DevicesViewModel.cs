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
    private StorageService _storageService;
    private DeviceViewModel? _selectedDevice;
    private bool _flashOS;
    private bool _flashAll;
    private bool _flashRuntime;
    private bool _flashCoprocessor;

    public IReactiveCommand AddDeviceCommand { get; }
    public IReactiveCommand FlashDeviceCommand { get; }

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    public DevicesViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();
        _deviceService!.DeviceConnected += OnDeviceConnected;
        _deviceService!.DeviceDisconnected += OnDeviceDisconnected;

        _storageService = Locator.Current.GetService<StorageService>();

        foreach (var device in _deviceService.KnownDevices)
        {
            Devices.Add(new DeviceViewModel(device, _deviceService, _storageService));
        }

        AddDeviceCommand = ReactiveCommand.CreateFromTask(OnAddDevice);
        FlashDeviceCommand = ReactiveCommand.CreateFromTask(FlashSelectedDevice);
    }

    public string DefaultFirmwareVersion
    {
        get => _deviceService.GetDefaultFirmwareVersionForDevice(_selectedDevice?.RootInfo.LastRoute ?? string.Empty);
    }

    private async Task FlashSelectedDevice()
    {
        if (_selectedDevice == null) return;
        if (!_selectedDevice.IsConnected) return;

        await _deviceService.FlashFirmware(
            _selectedDevice.RootInfo.LastRoute,
            FlashOS,
            FlashRuntime,
            FlashCoprocessor);
    }

    public DeviceViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
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
