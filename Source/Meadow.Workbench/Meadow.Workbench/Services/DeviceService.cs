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
        // do we already know about this device?
        var existing = KnownDevices.FirstOrDefault(d => d.LastRoute == route && d.IsConnected);
        if (existing != null)
        {
            Debug.WriteLine($"Already known device at {route}");
            // TODO: should we pull info and verify ID?
            return;
        }

        Debug.WriteLine($"Looking for a device at {route}");

        var connection = new Hcom.SerialConnection(route);
        await connection.Attach();
        try
        {
            var info = await connection.GetDeviceInfo();

            if (info != null)
            {
                Debug.WriteLine($"Device detected at {route}");

                var device = KnownDevices.FirstOrDefault(d => d.DeviceID == info.ProcessorId);
                var d = _storageService.UpdateDeviceInfo(info, route);

                if (device == null)
                {
                    Debug.WriteLine($"Device at {route} is a new device");

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
                Debug.WriteLine($"No device detected at {route} (no info returned)");
                connection.Detach();
            }
        }
        catch (Exception)
        {
        }
    }

    private IMeadowConnection GetConnectionForRoute(string route)
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
            return d.Connection;
        }
    }

    public async Task SetUtcTime(string route, DateTimeOffset utcTime)
    {
        var connection = GetConnectionForRoute(route);
        await connection.SetRtcTime(utcTime);
    }

    public async Task<DateTimeOffset?> GetUtcTime(string route)
    {
        var connection = GetConnectionForRoute(route);
        return await connection.GetRtcTime();
    }

    public async Task<bool> IsRuntimEnabled(string route)
    {
        var connection = GetConnectionForRoute(route);
        return await connection.IsRuntimeEnabled();
    }

    public async Task DisableRuntime(string route)
    {
        var connection = GetConnectionForRoute(route);
        if (await connection.IsRuntimeEnabled())
        {
            await connection.RuntimeDisable();
        }
    }

    public async Task EnableRuntime(string route)
    {
        var connection = GetConnectionForRoute(route);
        if (!await connection.IsRuntimeEnabled())
        {
            await connection.RuntimeEnable();
        }
    }

    public async Task ResetDevice(string route)
    {
        var connection = GetConnectionForRoute(route);
        await connection.ResetDevice();
    }

    public async Task<bool> DeleteFile(string route, string remoteFile)
    {
        var connection = GetConnectionForRoute(route);
        await connection.DeleteFile(remoteFile);
        return true;
    }

    public async Task<bool> DownloadFile(string route, string remoteSource, string localDestination)
    {
        var connection = GetConnectionForRoute(route);
        return await connection.ReadFile(remoteSource, localDestination);
    }

    public async Task<bool> UploadFile(string route, string localSource, string remoteDestination)
    {
        var connection = GetConnectionForRoute(route);
        return await connection.WriteFile(localSource, remoteDestination);
    }

    public async Task<MeadowDirectory> GetFileList(string route, string directory)
    {
        var connection = GetConnectionForRoute(route);
        var list = await connection.GetFileList(directory, false);
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
