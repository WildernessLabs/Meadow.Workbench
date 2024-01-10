using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DynamicData;
using Meadow.Hcom;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class FilesViewModel : FeatureViewModel
{
    private string _localDirectory;
    private string? _remoteDirectory;
    private MeadowDirectory _localFiles;
    private MeadowDirectory _remoteFiles;
    private MeadowFolderEntry? _selectedLocalItem;
    private MeadowFolderEntry? _selectedRemoteItem;
    private string? _selectedRoute;
    private DeviceService? _deviceService;
    private SettingsService? _settingsService;
    private IMeadowConnection? _activeConnection;
    private DeviceInformation? _activeDevice;
    private bool _isLoadingRemoteFiles;

    public ObservableCollection<string> AvailableRemoteRoutes { get; } = new();

    public IReactiveCommand SelectLocalFolderCommand { get; }

    public FilesViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();
        _settingsService = Locator.Current.GetService<SettingsService>();

        _localDirectory = _settingsService!.LocalFilesFolder;

        RemoteDirectory = "/meadow0/";
        _localFiles = new MeadowDirectory(LocalDirectory);
        _remoteFiles = new MeadowDirectory(RemoteDirectory);

        AvailableRemoteRoutes.AddRange(_deviceService.KnownDevices.Select(d => d.LastRoute));
        SelectLocalFolderCommand = ReactiveCommand.CreateFromTask(OnSelectLocalFolder);
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
                UpdateRemoteSource(value, RemoteDirectory);
            }
        }
    }


    public string LocalDirectory
    {
        get => _localDirectory;
        set
        {
            _settingsService.LocalFilesFolder = value;
            this.RaiseAndSetIfChanged(ref _localDirectory, value);
        }
    }

    public string RemoteDirectory
    {
        get => _remoteDirectory;
        set => this.RaiseAndSetIfChanged(ref _remoteDirectory, value);
    }

    public MeadowDirectory LocalFiles
    {
        get => _localFiles;
    }

    public MeadowDirectory RemoteFiles
    {
        get => _remoteFiles;
    }

    public MeadowFolderEntry? SelectedLocalItem
    {
        get => _selectedLocalItem;
        set => this.RaiseAndSetIfChanged(ref _selectedLocalItem, value);
    }

    public MeadowFolderEntry? SelectedRemoteItem
    {
        get => _selectedRemoteItem;
        set => this.RaiseAndSetIfChanged(ref _selectedRemoteItem, value);
    }

    public bool IsLoadingRemoteFiles
    {
        get => _isLoadingRemoteFiles;
        set => this.RaiseAndSetIfChanged(ref _isLoadingRemoteFiles, value);
    }

    public void UpdateRemoteSource(string route, string? folder = null)
    {
        SelectedRemoteItem = null;

        _remoteFiles = null;
        this.RaisePropertyChanged(nameof(RemoteFiles));

        _ = Task.Run(async () =>
        {
            IsLoadingRemoteFiles = true;
            _remoteDirectory = folder;
            _remoteFiles = await _deviceService.GetFiles(route, folder);
            this.RaisePropertyChanged(nameof(RemoteDirectory));
            this.RaisePropertyChanged(nameof(RemoteFiles));
            IsLoadingRemoteFiles = false;
        });
    }
}
