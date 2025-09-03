using Meadow.Workbench.Models;
using Splat;
using System;
using System.IO;
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

    public async Task RefreshPackages()
    {
        try
        {
            await Task.Run(() =>
            {
                var knownPackages = _storageService.GetAllPackages();

                foreach (var package in knownPackages)
                {
                    try
                    {
                        var packagePath = new FileInfo(Path.Combine(_packageRoot.FullName, package.FileName));
                        var fileFoundBefore = package.FileFound;
                        var fileSizeBefore = package.FileSize;
                        
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
                        
                        // Update database if values changed
                        if (package.FileFound != fileFoundBefore || package.FileSize != fileSizeBefore)
                        {
                            _storageService.UpdatePackage(package);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other packages
                        System.Diagnostics.Debug.WriteLine($"Error refreshing package {package.FileName}: {ex.Message}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Error in RefreshPackages: {ex.Message}");
        }
    }
}
