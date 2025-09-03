using DialogHostAvalonia;
using Meadow.Workbench.Models;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

internal class PackageViewModel : FeatureViewModel
{
    private readonly PackageService _packageService;
    private readonly StorageService _storageService;
    private Package? _selectedPackage;

    public ObservableCollection<Package> Packages { get; } = new();
    public ObservableCollection<PackageContentItem> PackageContents { get; } = new();

    public IReactiveCommand CreatePackageCommand { get; }
    public IReactiveCommand DeletePackageCommand { get; }
    public IReactiveCommand RefreshPackagesCommand { get; }
    public IReactiveCommand GeneratePackageCommand { get; }

    public Package? SelectedPackage
    {
        get => _selectedPackage;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPackage, value);
            if (value != null)
            {
                _ = LoadPackageContents(value);
            }
            else
            {
                PackageContents.Clear();
            }
        }
    }

    public PackageViewModel()
    {
        _packageService = Locator.Current.GetService<PackageService>()!;
        _storageService = Locator.Current.GetService<StorageService>()!;

        CreatePackageCommand = ReactiveCommand.CreateFromTask(CreatePackage);
        DeletePackageCommand = ReactiveCommand.CreateFromTask(DeletePackage);
        RefreshPackagesCommand = ReactiveCommand.CreateFromTask(RefreshPackages);
        GeneratePackageCommand = ReactiveCommand.CreateFromTask(GeneratePackage);

        // Load packages async without blocking startup
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadPackages();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading packages during startup: {ex.Message}");
            }
        });
    }

    public override async void OnActivated()
    {
        try
        {
            await RefreshPackages();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during OnActivated: {ex.Message}");
        }
    }

    private async Task LoadPackages()
    {
        await RefreshPackages();
    }

    private async Task RefreshPackages()
    {
        await _packageService.RefreshPackages();
        var packages = _storageService.GetAllPackages();

        Packages.Clear();
        foreach (var package in packages)
        {
            Packages.Add(package);
        }
    }

    private async Task CreatePackage()
    {
        var dialogViewModel = new AddPackageViewModel();
        var dialog = new AddPackageDialog(dialogViewModel);

        await DialogHost.Show(dialog);

        if (!dialogViewModel.IsCancelled)
        {
            try
            {
                var newPackage = dialogViewModel.CreatePackage();

                // Save to database
                _storageService.InsertPackage(newPackage);

                // Create initial package structure with "app" and "os" folders
                CreateInitialPackageStructure(newPackage);

                await RefreshPackages();

                // Select the new package
                SelectedPackage = Packages.FirstOrDefault(p => p.PackageID == newPackage.PackageID);
            }
            catch (Exception ex)
            {
                // TODO: Show error message to user
                System.Diagnostics.Debug.WriteLine($"Error creating package: {ex.Message}");
            }
        }
    }

    private void CreateInitialPackageStructure(Package package)
    {
        PackageContents.Clear();
        CreateInitialPackageStructureInternal();
    }

    private void CreateInitialPackageStructureInternal()
    {
        // Create "app" folder
        var appFolder = new PackageContentItem
        {
            Name = "app",
            IsDirectory = true,
            Children = new ObservableCollection<PackageContentItem>()
        };

        // Create "os" folder  
        var osFolder = new PackageContentItem
        {
            Name = "os",
            IsDirectory = true,
            Children = new ObservableCollection<PackageContentItem>()
        };

        PackageContents.Add(appFolder);
        PackageContents.Add(osFolder);
    }

    private async Task DeletePackage()
    {
        if (SelectedPackage == null) return;

        // Delete the package file and database entry
        var packagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WildernessLabs", "Packages", SelectedPackage.FileName);

        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        _storageService.DeletePackage(SelectedPackage);
        await RefreshPackages();
        SelectedPackage = null;
    }

    private async Task LoadPackageContents(Package package)
    {
        await Task.Run(() =>
        {
            PackageContents.Clear();

            var packagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WildernessLabs", "Packages", package.FileName);

            if (!File.Exists(packagePath))
            {
                // No ZIP file yet, show default structure
                CreateInitialPackageStructureInternal();
                return;
            }

            try
            {
                using var archive = ZipFile.OpenRead(packagePath);
                var rootItems = new Dictionary<string, PackageContentItem>();

                foreach (var entry in archive.Entries.OrderBy(e => e.FullName))
                {
                    var parts = entry.FullName.Split('/');
                    var isDirectory = entry.FullName.EndsWith("/");

                    if (parts.Length == 1 && !isDirectory)
                    {
                        // Root file
                        rootItems[entry.Name] = new PackageContentItem
                        {
                            Name = entry.Name,
                            IsDirectory = false,
                            Size = entry.Length
                        };
                    }
                    else if (parts.Length >= 1)
                    {
                        // Directory or file in subdirectory
                        var rootName = parts[0];
                        if (!rootItems.ContainsKey(rootName))
                        {
                            rootItems[rootName] = new PackageContentItem
                            {
                                Name = rootName,
                                IsDirectory = true,
                                Children = new ObservableCollection<PackageContentItem>()
                            };
                        }

                        if (parts.Length > 1)
                        {
                            AddToTree(rootItems[rootName], parts.Skip(1).ToArray(), entry.Length, isDirectory);
                        }
                    }
                }

                foreach (var item in rootItems.Values.OrderBy(i => !i.IsDirectory).ThenBy(i => i.Name))
                {
                    PackageContents.Add(item);
                }
            }
            catch
            {
                // Handle zip reading errors
            }
        });
    }

    private void AddToTree(PackageContentItem parent, string[] remainingParts, long size, bool isDirectory)
    {
        if (remainingParts.Length == 0) return;

        var currentName = remainingParts[0];
        var existing = parent.Children?.FirstOrDefault(c => c.Name == currentName);

        if (existing == null)
        {
            existing = new PackageContentItem
            {
                Name = currentName,
                IsDirectory = remainingParts.Length > 1 || isDirectory,
                Size = remainingParts.Length == 1 && !isDirectory ? size : 0,
                Children = (remainingParts.Length > 1 || isDirectory) ? new ObservableCollection<PackageContentItem>() : null
            };
            parent.Children?.Add(existing);
        }

        if (remainingParts.Length > 1)
        {
            AddToTree(existing, remainingParts.Skip(1).ToArray(), size, isDirectory);
        }
    }

    private async Task GeneratePackage()
    {
        if (SelectedPackage == null) return;

        await Task.Run(() =>
        {
            var packagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WildernessLabs", "Packages", SelectedPackage.FileName);

            // Create the zip file from the package contents
            using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);

            // Add folders and files from PackageContents
            foreach (var rootItem in PackageContents)
            {
                if (HasContent(rootItem))
                {
                    AddItemToZip(archive, rootItem, rootItem.Name);
                }
            }
        });

        // Refresh to update file size
        await RefreshPackages();
    }

    private void AddItemToZip(ZipArchive archive, PackageContentItem item, string path)
    {
        if (item.IsDirectory)
        {
            // Create directory entry (ending with /)
            if (!string.IsNullOrEmpty(path))
            {
                archive.CreateEntry(path + "/");
            }

            // Add children
            if (item.Children != null)
            {
                foreach (var child in item.Children)
                {
                    AddItemToZip(archive, child, Path.Combine(path, child.Name).Replace('\\', '/'));
                }
            }
        }
        else if (item.FilePath != null && File.Exists(item.FilePath))
        {
            // Add file
            archive.CreateEntryFromFile(item.FilePath, path);
        }
    }

    private bool HasContent(PackageContentItem item)
    {
        // If it's a file, it has content
        if (!item.IsDirectory)
        {
            return true;
        }

        // If it's a directory, check if it has any children with content
        if (item.Children != null)
        {
            foreach (var child in item.Children)
            {
                if (HasContent(child))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void AddFileToPackage(string filePath, string targetFolder)
    {
        if (SelectedPackage == null) return;

        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);

        // Find the target folder in the package contents
        var targetFolderItem = PackageContents.FirstOrDefault(item =>
            item.IsDirectory && item.Name.Equals(targetFolder, StringComparison.OrdinalIgnoreCase));

        if (targetFolderItem?.Children != null)
        {
            // Check if file already exists
            var existingFile = targetFolderItem.Children.FirstOrDefault(f => f.Name == fileName);
            if (existingFile != null)
            {
                // Update existing file
                existingFile.FilePath = filePath;
                existingFile.Size = fileInfo.Length;
            }
            else
            {
                // Add new file
                var newFile = new PackageContentItem
                {
                    Name = fileName,
                    IsDirectory = false,
                    Size = fileInfo.Length,
                    FilePath = filePath
                };
                targetFolderItem.Children.Add(newFile);
            }
        }
    }
}

public class PackageContentItem
{
    public string Name { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public string? FilePath { get; set; }
    public ObservableCollection<PackageContentItem>? Children { get; set; }
}
