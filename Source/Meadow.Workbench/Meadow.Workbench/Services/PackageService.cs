using Meadow.Workbench.Models;
using OpenNETCF.ORM;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class PackageService
{
    private readonly StorageService? _storageService;
    private readonly FirmwareService? _firmwareService;

    private readonly DirectoryInfo _packageRoot;

    public PackageService()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.None);
        root = Path.Combine(root, "WildernessLabs", "Packages");
        _packageRoot = new DirectoryInfo(root);
        if (!_packageRoot.Exists) _packageRoot.Create();

        _storageService = Locator.Current.GetService<StorageService>() ?? throw new Exception();
        _firmwareService = Locator.Current.GetService<FirmwareService>() ?? throw new Exception();
    }

    public IEnumerable<Package> GetAllPackages()
    {
        var knownPackages = _storageService!.GetAllPackages().ToList();

        foreach (var package in knownPackages)
        {
            try
            {
                var packagePath = new FileInfo(Path.Combine(_packageRoot.FullName, package.FileName));

                // Populate runtime properties from actual file system state
                if (!packagePath.Exists)
                {
                    package.FileFound = false;
                    package.FileSize = 0;
                }
                else
                {
                    package.FileFound = true;
                    package.FileSize = packagePath.Length;
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with other packages
                System.Diagnostics.Debug.WriteLine($"Error checking package {package.FileName}: {ex.Message}");
                package.FileFound = false;
                package.FileSize = 0;
            }
        }

        return knownPackages;
    }

    public async Task RefreshPackages()
    {
        // For backward compatibility - just wrap GetAllPackages in a task
        await Task.Run(() => GetAllPackages().ToList());
    }
}
