using Meadow.Hcom;
using OpenNETCF.ORM;
using System;
using System.Collections.Generic;
using System.IO;

namespace Meadow.Workbench.Services;

[Entity]
internal class DeviceInformation
{
    [Field(IsPrimaryKey = true)]
    public string DeviceID { get; set; }
    [Field]
    public string? FriendlyName { get; set; }
    [Field]
    public string? Version { get; set; }
    [Field]
    public string? LastRoute { get; set; }
    [Field]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}

internal class StorageService
{
    private IDataStore _store;

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

    public IEnumerable<DeviceInformation> GetAllDevices()
    {
        return _store.Select<DeviceInformation>();
    }

    public DeviceInformation UpdateDeviceInfo(DeviceInformation info)
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

        return existing;
    }

    public DeviceInformation UpdateDeviceInfo(DeviceInfo info, string route)
    {
        var existing = _store.Select<DeviceInformation>(info.ProcessorId);

        if (existing == null)
        {
            var di = new DeviceInformation
            {
                DeviceID = info.ProcessorId,
                Version = info.OsVersion,
                LastSeen = DateTime.UtcNow,
                LastRoute = route
            };

            _store.Insert(di);

            return di;
        }

        existing.LastSeen = DateTime.UtcNow;
        existing.LastRoute = route;
        _store.Update(existing);
        return existing;
    }
}
