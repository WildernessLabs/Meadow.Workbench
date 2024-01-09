using Meadow.Hcom;
using OpenNETCF.ORM;
using System;

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

    public IMeadowConnection? Connection { get; set; }
    public bool IsConnected { get; set; }
}
