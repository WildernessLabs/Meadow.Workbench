using DynamicData;
using Meadow.Hcom;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class SerialPortMonitor
{
    public event EventHandler<string> NewPortDetected;

    private List<string> _knownSerialPorts = new();

    public SerialPortMonitor()
    {
        foreach (var p in System.IO.Ports.SerialPort.GetPortNames().Distinct())
        {
            _knownSerialPorts.Add(p);
        }

        _ = Task.Run(MonitorProc);
    }

    public IEnumerable<string> KnownPorts
    {
        get
        {
            lock (_knownSerialPorts)
            {
                return _knownSerialPorts.ToArray();
            }
        }
    }

    private async void MonitorProc()
    {
        while (true)
        {
            lock (_knownSerialPorts)
            {
                foreach (var p in System.IO.Ports.SerialPort.GetPortNames().Distinct().Except(_knownSerialPorts))
                {
                    _knownSerialPorts.Add(p);
                    NewPortDetected?.Invoke(this, p);
                }
            }

            await Task.Delay(2000);
        }
    }
}

internal class DeviceService
{
    public event EventHandler<DeviceInformation> DeviceAdded;

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

        _portMonitor.NewPortDetected += OnNewSerialPortDetected;
    }

    private async void OnNewSerialPortDetected(object? sender, string e)
    {
        await CheckForDeviceAtLocation(e);
    }

    private async Task CheckForDeviceAtLocation(string route)
    {
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
            }
        }
        catch (Exception ex)
        {
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
