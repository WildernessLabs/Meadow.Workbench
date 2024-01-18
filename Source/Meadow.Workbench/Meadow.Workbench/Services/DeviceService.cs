using Meadow.Hcom;
using Meadow.Workbench.ViewModels;
using MeadowCLI;
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
    private const string F7OtAOsFolder = "/meadow0/update/os/";

    public event EventHandler<DeviceInformation> DeviceAdded;
    public event EventHandler<string> DeviceRemoved;
    public event EventHandler<DeviceInformation> DeviceConnected;
    public event EventHandler<DeviceInformation> DeviceDisconnected;

    private SerialPortMonitor? _portMonitor;
    private StorageService _storageService;
    private FirmwareService _firmwareService;
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private FirmwareWriter _firmwareWriter;

    public List<DeviceInformation> KnownDevices { get; } = new();
    private FirmwareWriter FirmwareWriter => _firmwareWriter ??= new FirmwareWriter();

    public DeviceService()
    {
        _storageService = Locator.Current.GetService<StorageService>() ?? throw new Exception();
        _firmwareService = Locator.Current.GetService<FirmwareService>() ?? throw new Exception();

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
        IMeadowConnection? connection = null;

        Debug.WriteLine($"Looking for a device at {route}");

        var existing = KnownDevices.FirstOrDefault(d => d.LastRoute == route);
        if (existing != null)
        {
            if (existing.IsConnected)
            {
                Debug.WriteLine($"Already known and connected device at {route}");
                // TODO: should we pull info and verify ID?
                return;
            }

            connection = existing.Connection;
        }

        if (connection == null)
        {

            connection = new Hcom.SerialConnection(route);
        }

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

    public bool IsLibUsbDeviceConnected()
    {
        return FirmwareWriter.GetLibUsbDevices().Count() > 0;
    }

    public async Task FlashFirmwareWithDfu(string route, bool writeOS, bool writeRuntime, bool writeCoprocessor, string version)
    {

        var devices = FirmwareWriter.GetLibUsbDevices();
    }

    public async Task FlashFirmwareWithOtA(string route, bool writeOS, bool writeCoprocessor, string? version = null)
    {
        var package =
            version == null
            ? _firmwareService.CurrentStore.DefaultPackage
            : _firmwareService.CurrentStore[version];

        if (package == null)
        {
            // TODO: error
            return;
        }

        var connection = GetConnectionForRoute(route);

        if (await connection.IsRuntimeEnabled())
        {
            await connection.RuntimeDisable();
        }

        await connection.WaitForMeadowAttach();

        if (writeOS)
        {
            // note: OtA *requires* both the OS and runtime pair
            var source = package.GetFullyQualifiedPath(package.OsWithoutBootloader);
            var dest = $"{F7OtAOsFolder}{package.OsWithoutBootloader}";
            await connection.WriteFile(source, dest);
            source = package.GetFullyQualifiedPath(package.Runtime);
            dest = $"{F7OtAOsFolder}{package.Runtime}";
            await connection.WriteFile(source, dest);
        }
        if (writeCoprocessor)
        {
            var source = package.GetFullyQualifiedPath(package.CoprocApplication);
            var dest = $"{F7OtAOsFolder}{package.CoprocApplication}";
            await connection.WriteFile(source, dest);
            source = package.GetFullyQualifiedPath(package.CoprocPartitionTable);
            dest = $"{F7OtAOsFolder}{package.CoprocPartitionTable}";
            await connection.WriteFile(source, dest);
            source = package.GetFullyQualifiedPath(package.CoprocBootloader);
            dest = $"{F7OtAOsFolder}{package.CoprocBootloader}";
            await connection.WriteFile(source, dest);
        }

        await connection.ResetDevice();
        await connection.WaitForMeadowAttach();
    }

    public string GetDefaultFirmwareVersionForDevice(string route)
    {
        return _firmwareService.CurrentStore?.DefaultPackage?.Version ?? "unknown";
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

    public Task RemoveDevice(string deviceID)
    {
        _storageService.DeleteDeviceInfo(deviceID);
        DeviceRemoved?.Invoke(this, deviceID);
        return Task.CompletedTask;
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
