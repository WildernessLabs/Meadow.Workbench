﻿using DialogHostAvalonia;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

internal class DevicesViewModel : FeatureViewModel
{
    private DeviceService _deviceService;
    private StorageService _storageService;

    public IReactiveCommand AddDeviceCommand { get; }

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    public DevicesViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();
        _deviceService!.DeviceAdded += OnDeviceAppeared;

        _storageService = Locator.Current.GetService<StorageService>();

        foreach (var device in _deviceService.KnownDevices)
        {
            Devices.Add(new DeviceViewModel(device, _storageService));
        }

        AddDeviceCommand = ReactiveCommand.CreateFromTask(OnAddDevice);
    }

    private void OnDeviceAppeared(object? sender, DeviceInformation e)
    {
    }

    private async Task OnAddDevice()
    {
        var result = await DialogHost.Show(new AddDeviceDialog());
    }
}
