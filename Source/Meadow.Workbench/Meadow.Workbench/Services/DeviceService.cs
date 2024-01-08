using Meadow.Hcom;
using Meadow.Workbench.ViewModels;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class DeviceService
{
    public event EventHandler<DeviceInformation> DeviceAdded;
    public event EventHandler<DeviceInformation> DeviceConnected;
    public event EventHandler<DeviceInformation> DeviceDisconnected;

    private SerialPortMonitor? _portMonitor;
    private StorageService _storageService;
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public List<DeviceInformation> KnownDevices { get; } = new();

    public DeviceService()
    {
        _storageService = Locator.Current.GetService<StorageService>() ?? throw new Exception();

        KnownDevices = _storageService.GetAllDevices().ToList();

        // TODO: make this configurable
        CreateSerialPortMonitor();
    }

    private void CreateSerialPortMonitor()
    {
        _portMonitor = new SerialPortMonitor();

        foreach (var port in _portMonitor.KnownPorts)
        {
            _ = CheckForDeviceAtLocation(port);
        }

        _portMonitor.PortConnected += OnSerialPortConnected;
        _portMonitor.PortDisconnected += OnSerialPortDisconnected;
    }

    private void OnSerialPortDisconnected(object? sender, string e)
    {
        var existing = KnownDevices.FirstOrDefault(d => d.LastRoute == e);

        if (existing != null)
        {
            DeviceDisconnected?.Invoke(this, existing);
        }
    }

    private async void OnSerialPortConnected(object? sender, string e)
    {
        await CheckForDeviceAtLocation(e);
    }

    private async Task CheckForDeviceAtLocation(string route)
    {
        return;
        using var connection = new Hcom.SerialConnection(route);
        await connection.Attach();
        try
        {
            var info = await connection.GetDeviceInfo();
            Debug.WriteLine($"Device detected at {route}");

            if (info != null)
            {
                var device = _storageService.UpdateDeviceInfo(info, route);
                KnownDevices.Add(device);
                DeviceAdded?.Invoke(this, device);
                DeviceConnected?.Invoke(this, device);
            }
            //            connection.Detach();
        }
        catch (Exception ex)
        {
        }
    }

    public async Task<MeadowDirectory> GetFiles(string route, string directory)
    {
        using var connection = new SerialConnection(route);
        await connection.Attach();
        var list = await connection.GetFileList(directory, false);
        connection.Detach();
        return new MeadowDirectory(directory, list);
    }

    public async Task<IMeadowConnection?> AddConnection(string route)
    {
        if (_semaphore.Wait(5000))
        {
            /*
            if (_connections.ContainsKey(route))
            {
                return _connections[route];
            }

            // determine connection type
            var c = new SerialConnection(route);
            _connections.Add(route, c);

            await c.GetDeviceInfo();

            _semaphore.Release();

            return c;
            */
        }

        return null;
    }
}
