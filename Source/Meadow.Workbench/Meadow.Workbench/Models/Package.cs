using OpenNETCF.ORM;
using System;

namespace Meadow.Workbench.Models;

public enum PackageTarget
{
    MeadowF7,
    Linux,
    Windows
}

[Entity]
internal class Package
{
    [Field(IsPrimaryKey = true)]
    public string PackageID { get; set; }
    [Field]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Field]
    public string AppName { get; set; }
    [Field]
    public string AppVersion { get; set; }
    [Field]
    public string OSVersion { get; set; }
    [Field]
    public string Description { get; set; }
    [Field]
    public string FileName { get; set; }
    [Field]
    public PackageTarget Target { get; set; }

    public bool FileFound { get; set; }
    public long FileSize { get; set; }
}
