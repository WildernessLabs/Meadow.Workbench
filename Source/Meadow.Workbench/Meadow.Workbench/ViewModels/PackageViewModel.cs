using DialogHostAvalonia;
using Meadow.Workbench.Dialogs;
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
    private bool _isRefreshingAfterGeneration;
    private bool _isGeneratingPackage;
    private PackageContentItem? _selectedTreeItem;
    private bool _skipLoadContentsOnSelection;

    public ObservableCollection<Package> Packages { get; } = new();
    public ObservableCollection<PackageContentItem> PackageContents { get; } = new();

    public IReactiveCommand CreatePackageCommand { get; }
    public IReactiveCommand DeletePackageCommand { get; }
    public IReactiveCommand RefreshPackagesCommand { get; }
    public IReactiveCommand GeneratePackageCommand { get; }
    public IReactiveCommand DeleteTreeItemCommand { get; }

    public Package? SelectedPackage
    {
        get => _selectedPackage;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPackage, value);
            if (value != null && !_isGeneratingPackage && !_skipLoadContentsOnSelection)
            {
                _ = LoadPackageContents(value);
            }
            else if (value == null && !_isGeneratingPackage)
            {
                PackageContents.Clear();
            }
            
            // Reset the skip flag after selection
            _skipLoadContentsOnSelection = false;
        }
    }

    public PackageContentItem? SelectedTreeItem
    {
        get => _selectedTreeItem;
        set => this.RaiseAndSetIfChanged(ref _selectedTreeItem, value);
    }

    public PackageViewModel()
    {
        _packageService = Locator.Current.GetService<PackageService>()!;
        _storageService = Locator.Current.GetService<StorageService>()!;

        CreatePackageCommand = ReactiveCommand.CreateFromTask(CreatePackage);
        DeletePackageCommand = ReactiveCommand.CreateFromTask(DeletePackage);
        RefreshPackagesCommand = ReactiveCommand.CreateFromTask(RefreshPackages);
        GeneratePackageCommand = ReactiveCommand.CreateFromTask(GeneratePackage);
        DeleteTreeItemCommand = ReactiveCommand.Create(DeleteTreeItem, 
            this.WhenAnyValue(x => x.SelectedTreeItem, item => 
                item != null && CanDeleteTreeItem(item)));

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
        var packages = await Task.Run(() => _packageService.GetAllPackages().ToList());

        // Remember the currently selected package ID to avoid reloading its contents during generation refresh
        var selectedPackageId = SelectedPackage?.PackageID;

        Packages.Clear();
        foreach (var package in packages)
        {
            Packages.Add(package);
        }

        // Restore the selected package
        if (selectedPackageId != null)
        {
            var packageToSelect = Packages.FirstOrDefault(p => p.PackageID == selectedPackageId);
            if (packageToSelect != null)
            {
                if (_isRefreshingAfterGeneration)
                {
                    // Update the selected package directly without triggering content reload
                    _selectedPackage = packageToSelect;
                    this.RaisePropertyChanged(nameof(SelectedPackage));
                    _isRefreshingAfterGeneration = false;
                }
                else
                {
                    // Normal refresh - allow content reload
                    SelectedPackage = packageToSelect;
                }
            }
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

                // Select the new package without triggering LoadPackageContents
                _skipLoadContentsOnSelection = true;
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
                    // Skip empty or invalid entries
                    if (string.IsNullOrWhiteSpace(entry.FullName)) continue;
                    
                    var parts = entry.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var isDirectory = entry.FullName.EndsWith("/");

                    // Skip entries with no meaningful parts
                    if (parts.Length == 0) continue;

                    // Debug output to understand what we're processing
                    System.Diagnostics.Debug.WriteLine($"Processing entry: '{entry.FullName}' -> Parts: [{string.Join(", ", parts)}], IsDir: {isDirectory}");

                    if (parts.Length == 1 && !isDirectory)
                    {
                        // Root file - only add if it has a valid name
                        if (!string.IsNullOrWhiteSpace(entry.Name))
                        {
                            rootItems[entry.Name] = new PackageContentItem
                            {
                                Name = entry.Name,
                                IsDirectory = false,
                                Size = entry.Length
                            };
                        }
                    }
                    else if (parts.Length >= 1)
                    {
                        // Directory or file in subdirectory
                        var rootName = parts[0];
                        
                        // Skip if root name is empty
                        if (string.IsNullOrWhiteSpace(rootName)) continue;
                        
                        // Only create root directory if it doesn't exist
                        if (!rootItems.ContainsKey(rootName))
                        {
                            rootItems[rootName] = new PackageContentItem
                            {
                                Name = rootName,
                                IsDirectory = true,
                                Children = new ObservableCollection<PackageContentItem>()
                            };
                        }

                        // Only process subdirectories/files if there are more parts
                        if (parts.Length > 1)
                        {
                            AddToTree(rootItems[rootName], parts.Skip(1).ToArray(), entry.Length, isDirectory);
                        }
                        // If parts.Length == 1 and isDirectory, we've already created the directory above
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
        
        // Skip empty or whitespace-only names
        if (string.IsNullOrWhiteSpace(currentName)) return;
        
        System.Diagnostics.Debug.WriteLine($"AddToTree: Parent='{parent.Name}', CurrentName='{currentName}', RemainingParts=[{string.Join(", ", remainingParts)}], IsDir={isDirectory}");
        
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
            System.Diagnostics.Debug.WriteLine($"Created new item: '{currentName}' under '{parent.Name}'");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Found existing item: '{currentName}' under '{parent.Name}'");
        }

        if (remainingParts.Length > 1)
        {
            AddToTree(existing, remainingParts.Skip(1).ToArray(), size, isDirectory);
        }
    }

    private async Task GeneratePackage()
    {
        if (SelectedPackage == null) return;

        // Set flag to prevent tree clearing during generation
        _isGeneratingPackage = true;

        try
        {
            var packagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WildernessLabs", "Packages", SelectedPackage.FileName);

            // Check if file already exists and prompt user
            if (File.Exists(packagePath))
            {
                var dialogViewModel = new UserMessageViewModel
                {
                    Title = "Overwrite Package",
                    Message = $"Package file '{SelectedPackage.FileName}' already exists. Do you want to overwrite it?",
                    IsQuestion = true
                };
                
                var dialog = new UserMessageDialog(dialogViewModel);
                var result = await DialogHost.Show(dialog);
                
                // If user didn't confirm, cancel the operation
                if (dialogViewModel.IsCancelled)
                {
                    return;
                }
            }

            await Task.Run(() =>
            {
                string? originalPackagePath = null;
                
                // If file exists, create a backup before regenerating
                if (File.Exists(packagePath))
                {
                    originalPackagePath = packagePath + ".backup";
                    File.Copy(packagePath, originalPackagePath, true);
                    File.Delete(packagePath);
                }

                try
                {
                    // Create the zip file from the package contents
                    using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);

                    // Add folders and files from PackageContents
                    foreach (var rootItem in PackageContents)
                    {
                        if (HasContent(rootItem))
                        {
                            AddItemToZip(archive, rootItem, rootItem.Name, originalPackagePath);
                        }
                    }
                }
                finally
                {
                    // Clean up backup file
                    if (originalPackagePath != null && File.Exists(originalPackagePath))
                    {
                        File.Delete(originalPackagePath);
                    }
                }
            });

            // Refresh to update file size without clearing tree
            _isRefreshingAfterGeneration = true;
            await RefreshPackages();
        }
        finally
        {
            // Always clear the generation flag
            _isGeneratingPackage = false;
        }
    }

    private void AddItemToZip(ZipArchive archive, PackageContentItem item, string path, string? originalPackagePath = null)
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
                    AddItemToZip(archive, child, Path.Combine(path, child.Name).Replace('\\', '/'), originalPackagePath);
                }
            }
        }
        else if (item.FilePath != null && File.Exists(item.FilePath))
        {
            // Add new file that was dragged in
            archive.CreateEntryFromFile(item.FilePath, path);
        }
        else if (!item.IsDirectory && item.Size > 0)
        {
            // This is an existing file from the original ZIP - copy it from the backup
            if (originalPackagePath != null && File.Exists(originalPackagePath))
            {
                using var originalArchive = ZipFile.OpenRead(originalPackagePath);
                var originalEntry = originalArchive.GetEntry(path);
                if (originalEntry != null)
                {
                    var newEntry = archive.CreateEntry(path);
                    using var originalStream = originalEntry.Open();
                    using var newStream = newEntry.Open();
                    originalStream.CopyTo(newStream);
                }
            }
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

    private bool CanDeleteTreeItem(PackageContentItem item)
    {
        // Don't allow deletion of root "app" and "os" folders
        return !(item.IsDirectory && PackageContents.Contains(item) && 
                (item.Name.Equals("app", StringComparison.OrdinalIgnoreCase) || 
                 item.Name.Equals("os", StringComparison.OrdinalIgnoreCase)));
    }

    private void DeleteTreeItem()
    {
        if (SelectedTreeItem == null) return;

        // Find and remove the item from its parent collection
        if (RemoveFromParent(PackageContents, SelectedTreeItem))
        {
            SelectedTreeItem = null;
        }
    }

    private bool RemoveFromParent(ObservableCollection<PackageContentItem> collection, PackageContentItem itemToRemove)
    {
        // Check if the item is in this collection
        if (collection.Contains(itemToRemove))
        {
            collection.Remove(itemToRemove);
            return true;
        }

        // Recursively search in children
        foreach (var item in collection)
        {
            if (item.Children != null && RemoveFromParent(item.Children, itemToRemove))
            {
                return true;
            }
        }

        return false;
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
