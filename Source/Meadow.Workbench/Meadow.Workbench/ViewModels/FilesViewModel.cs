using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DynamicData;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class FilesViewModel : FeatureViewModel
{
    private string _localDirectory;
    private string? _remoteDirectory;
    private MeadowDirectory _localFiles;
    private MeadowDirectory _remoteFiles;
    private MeadowFileSystemEntry? _selectedLocalItem;
    private MeadowFileSystemEntry? _selectedRemoteItem;
    private string? _selectedRoute;
    private DeviceService? _deviceService;
    private SettingsService? _settingsService;
    private bool _isLoadingRemoteFiles;
    private bool _deviceConnected;

    public ObservableCollection<string> AvailableRemoteRoutes { get; } = new();

    public IReactiveCommand SelectLocalFolderCommand { get; }
    public IReactiveCommand DownloadRemoteFileCommand { get; }
    public IReactiveCommand DeleteRemoteFileCommand { get; }
    public IReactiveCommand UploadFileToRemoteCommand { get; }

    public FilesViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();
        _settingsService = Locator.Current.GetService<SettingsService>();

        _localDirectory = _settingsService!.LocalFilesFolder;

        RemoteDirectory = "/meadow0/";
        _localFiles = MeadowDirectory.LoadFrom(LocalDirectory);
        _remoteFiles = new MeadowDirectory(RemoteDirectory);

        AvailableRemoteRoutes.AddRange(_deviceService.KnownDevices.Select(d => d.LastRoute));
        SelectLocalFolderCommand = ReactiveCommand.CreateFromTask(OnSelectLocalFolder);
        DownloadRemoteFileCommand = ReactiveCommand.CreateFromTask(OnDowloadRemoteFile);
        DeleteRemoteFileCommand = ReactiveCommand.CreateFromTask(OnDeleteRemoteFile);
        UploadFileToRemoteCommand = ReactiveCommand.CreateFromTask(OnUploadToRemoteFile);
    }

    private async Task OnUploadToRemoteFile()
    {
        if (IsDeviceConnected && SelectedLocalItem != null)
        {
            if (SelectedLocalItem is MeadowFileEntry mfe)
            {
                var localSource = Path.Combine(LocalDirectory, mfe.Name);
                var dest = $"{RemoteDirectory}{mfe.Name}";

                await _deviceService.UploadFile(SelectedRemoteRoute, localSource, dest);
                RefreshRemoteSource();
            }
            else if (SelectedLocalItem is MeadowFolderEntry)
            {
                // todo: allow folder pushes
            }
        }
    }

    private async Task OnDowloadRemoteFile()
    {
        if (SelectedRemoteItem != null)
        {
            if (SelectedRemoteItem is MeadowFileEntry)
            {
                IsLoadingRemoteFiles = true;
                var result = await _deviceService.DownloadFile(
                    SelectedRemoteRoute,
                    $"{RemoteDirectory}{SelectedRemoteItem.Name}",
                    Path.Combine(LocalDirectory, SelectedRemoteItem.Name));
                IsLoadingRemoteFiles = false;

                if (result)
                {
                    RefreshLocalSource();
                }
            }
            else if (SelectedRemoteItem is MeadowFolderEntry)
            {
                // todo: allow folder pulls
            }
        }
    }

    private async Task OnDeleteRemoteFile()
    {
        if (SelectedRemoteItem != null)
        {
            if (SelectedRemoteItem is MeadowFileEntry)
            {
                IsLoadingRemoteFiles = true;
                var result = await _deviceService.DeleteFile(
                    SelectedRemoteRoute,
                    $"{RemoteDirectory}{SelectedRemoteItem.Name}");
                IsLoadingRemoteFiles = false;

                if (result)
                {
                    RefreshRemoteSource();
                }

            }
            else if (SelectedRemoteItem is MeadowFolderEntry)
            {
                // todo: allow folder pulls
            }
        }
    }

    private async Task OnSelectLocalFolder()
    {
        var result = await TopLevel
            .GetTopLevel(this.FeatureView)
            !.StorageProvider
            .OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select Meadow Root"
            });

        if (result != null)
        {
            if (result.Count > 0)
            {
                LocalDirectory = result[0].Path.LocalPath;
            }
        }
    }


    public string? SelectedRemoteRoute
    {
        get => _selectedRoute;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedRoute, value);

            if (value != null)
            {
                // if it's an F7 (i.e. serial) we must disable the runtime
                Task.Run(async () =>
                {
                    await _deviceService!.DisableRuntime(value);

                    RefreshRemoteSource();
                });
            }
        }
    }

    public string LocalDirectory
    {
        get => _localDirectory;
        private set
        {
            _settingsService.LocalFilesFolder = value;
            this.RaiseAndSetIfChanged(ref _localDirectory, value);
        }
    }

    public bool IsDeviceConnected
    {
        get => _deviceConnected;
        set => this.RaiseAndSetIfChanged(ref _deviceConnected, value);
    }

    public string RemoteDirectory
    {
        get => _remoteDirectory;
        set => this.RaiseAndSetIfChanged(ref _remoteDirectory, value);
    }

    public MeadowDirectory LocalFiles
    {
        get => _localFiles;
        private set => this.RaiseAndSetIfChanged(ref _localFiles, value);
    }

    public MeadowDirectory RemoteFiles
    {
        get => _remoteFiles;
        private set => this.RaiseAndSetIfChanged(ref _remoteFiles, value);
    }

    public MeadowFileSystemEntry? SelectedLocalItem
    {
        get => _selectedLocalItem;
        set => this.RaiseAndSetIfChanged(ref _selectedLocalItem, value);
    }

    public MeadowFileSystemEntry? SelectedRemoteItem
    {
        get => _selectedRemoteItem;
        set => this.RaiseAndSetIfChanged(ref _selectedRemoteItem, value);
    }

    public bool IsLoadingRemoteFiles
    {
        get => _isLoadingRemoteFiles;
        set => this.RaiseAndSetIfChanged(ref _isLoadingRemoteFiles, value);
    }

    public void RefreshLocalSource()
    {
        LocalFiles = MeadowDirectory.LoadFrom(LocalDirectory);
    }

    public void UpdateLocalSource(string folder)
    {
        LocalDirectory = System.IO.Path.GetFullPath(folder);
        RefreshLocalSource();
    }

    public void RefreshRemoteSource()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                IsLoadingRemoteFiles = true;
                var files = await _deviceService.GetFileList(SelectedRemoteRoute, RemoteDirectory);
                RemoteFiles = files;
                IsLoadingRemoteFiles = false;
                IsDeviceConnected = true;
            }
            catch
            {
                IsDeviceConnected = false;
            }
        });
    }

    private string ConvertRemotePathToAbsolute(string path)
    {
        if (!path.Contains("..")) return path;

        // remove any "../" suffix, and then back up that many folders
        var count = 0;
        while (path.EndsWith("../"))
        {
            path = path.Substring(0, path.Length - 3);
            count++;
        }

        while (count > 0)
        {
            // the path will be terminated with a '/'.  We need to back up to the one before that
            var index = path
                .Substring(0, path.Length - 1)
                .LastIndexOf('/');
            path = path.Substring(0, index + 1);
            count--;
        }

        return path;
    }
}
