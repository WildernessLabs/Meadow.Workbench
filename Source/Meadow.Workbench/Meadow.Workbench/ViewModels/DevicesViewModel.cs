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

    public IReactiveCommand AddDeviceCommand { get; }

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    public DevicesViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();
        _deviceService!.DeviceConnected += OnDeviceConnected;
        _deviceService!.DeviceDisconnected += OnDeviceDisconnected;

        _storageService = Locator.Current.GetService<StorageService>();

        foreach (var device in _deviceService.KnownDevices)
        {
            Devices.Add(new DeviceViewModel(device, _storageService));
        }

        AddDeviceCommand = ReactiveCommand.CreateFromTask(OnAddDevice);
    }

    public DeviceViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
    }

    private void OnDeviceConnected(object? sender, DeviceInformation e)
    {
        var dvm = Devices.FirstOrDefault(d => d.DeviceID == e.DeviceID);
        if (dvm == null)
        {
            dvm = new DeviceViewModel(e, _storageService);
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
