using Meadow.Hcom;
using Meadow.Workbench.Models;
using OpenNETCF.ORM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Meadow.Workbench.Services;

internal class StorageService
{
    private IDataStore _store;
    private static readonly object _dbLock = new object();

    public StorageService()
    {
        CreateDatabaseIfNecessary();
    }

    private void CreateDatabaseIfNecessary()
    {
        var storePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.None),
                "wildernesslabs",
                "workbench.sqlite");

        _store = new SQLiteDataStore(storePath);
        _store.CreateOrUpdateStore();
    }

    public IEnumerable<Package> GetAllPackages()
    {
        lock (_dbLock)
        {
            return _store.Select<Package>().ToList();
        }
    }

    public IEnumerable<DeviceInformation> GetAllDevices()
    {
        lock (_dbLock)
        {
            return _store.Select<DeviceInformation>().ToList();
        }
    }

    public void DeleteDeviceInfo(string deviceID)
    {
        lock (_dbLock)
        {
            _store.Delete<DeviceInformation>(deviceID);
        }
    }

    public DeviceInformation UpdateDeviceInfo(DeviceInformation info)
    {
        lock (_dbLock)
        {
            var existing = _store.Select<DeviceInformation>(info.DeviceID);

            if (existing == null)
            {
                _store.Insert(info);
                return info;
            }

            // TODO: only some fields are updateable?
            existing.LastSeen = info.LastSeen;
            existing.LastRoute = info.LastRoute ?? existing.LastRoute;
            existing.FriendlyName = info.FriendlyName ?? existing.FriendlyName;

            _store.Update(existing);
            return existing;
        }
    }

    public DeviceInformation UpdateDeviceInfo(DeviceInfo info, string route)
    {
        lock (_dbLock)
        {
            var existing = _store.Select<DeviceInformation>(info.ProcessorId);

            if (existing == null)
            {
                var di = new DeviceInformation
                {
                    DeviceID = info.ProcessorId,
                    OsVersion = info.OsVersion,
                    LastSeen = DateTime.UtcNow,
                    LastRoute = route,
                    DeviceName = info.DeviceName,
                    Model = info.Model,
                    SerialNumber = info.SerialNumber,
                    CoprocessorVersion = info.CoprocessorOsVersion,
                    RuntimeVersion = info.RuntimeVersion,
                    RawInfo = info.ToString()
                };

                _store.Insert(di);
                return di;
            }

            existing.LastSeen = DateTime.UtcNow;
            existing.LastRoute = route;
            existing.OsVersion = info.OsVersion;
            existing.Model = info.Model;
            existing.CoprocessorVersion = info.CoprocessorOsVersion;
            existing.RuntimeVersion = info.RuntimeVersion;
            existing.RawInfo = info.ToString();

            _store.Update(existing);
            return existing;
        }
    }

    public void DeletePackage(Package package)
    {
        lock (_dbLock)
        {
            _store.Delete<Package>(package.PackageID);
        }
    }

    public void InsertPackage(Package package)
    {
        lock (_dbLock)
        {
            _store.Insert(package);
        }
    }

    public void UpdatePackage(Package package)
    {
        lock (_dbLock)
        {
            _store.Update(package);
        }
    }
}
