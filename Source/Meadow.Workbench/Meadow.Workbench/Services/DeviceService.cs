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
            existing.IsConnected = false;
            DeviceDisconnected?.Invoke(this, existing);
        }
    }

    private async void OnSerialPortConnected(object? sender, string e)
    {
        await CheckForDeviceAtLocation(e);
    }

    private async Task CheckForDeviceAtLocation(string route)
    {
        var connection = new Hcom.SerialConnection(route);
        await connection.Attach();
        try
        {
            var info = await connection.GetDeviceInfo();
            Debug.WriteLine($"Device detected at {route}");

            if (info != null)
            {
                var device = KnownDevices.FirstOrDefault(d => d.DeviceID == info.ProcessorId);
                var d = _storageService.UpdateDeviceInfo(info, route);

                if (device == null)
                {
                    device = d;
                    KnownDevices.Add(device);
                }

                device.Connection = connection;
                device.IsConnected = true;

                DeviceAdded?.Invoke(this, device);
                DeviceConnected?.Invoke(this, device);

            }
            else
            {
                connection.Detach();
            }
        }
        catch (Exception)
        {
        }
    }

    public async Task<MeadowDirectory> GetFiles(string route, string directory)
    {
        var d = KnownDevices.FirstOrDefault(d => d.LastRoute == route);
        if (d == null)
        {
            // TODO: need to do:
            // CheckForDeviceAtLocation(route);
            throw new NotImplementedException();
        }
        else
        {
            if (d.Connection == null)
            {
                // TODO: need to implement creating connection
                //var connection = new SerialConnection(route);
                //await connection.Attach();
                //d.Connection = connection;
                throw new NotImplementedException();
            }
            var list = await d.Connection.GetFileList(directory, false);
            return new MeadowDirectory(directory, list);
        }
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
