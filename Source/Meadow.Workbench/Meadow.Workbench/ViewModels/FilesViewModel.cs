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
    private IMeadowConnection? _activeConnection;
    private DeviceInformation? _activeDevice;

    public ObservableCollection<string> AvailableRemoteRoutes { get; } = new();

    public IReactiveCommand RemoteDirTapped { get; }

    public FilesViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();

        LocalDirectory = "c:\\SomeFolder";
        RemoteDirectory = "/meadow0/";
        _localFiles = new MeadowDirectory(LocalDirectory);
        _remoteFiles = new MeadowDirectory(RemoteDirectory);

        RemoteDirTapped = ReactiveCommand.CreateFromTask(Foo);
        AvailableRemoteRoutes.AddRange(_deviceService.KnownDevices.Select(d => d.LastRoute));
    }

    private Task Foo()
    {
        return Task.CompletedTask;
    }

    public string? SelectedRemoteRoute
    {
        get => _selectedRoute;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedRoute, value);
            UpdateRemoteSource(value, RemoteDirectory);
        }
    }


    public string LocalDirectory
    {
        get => _localDirectory;
        set => this.RaiseAndSetIfChanged(ref _localDirectory, value);
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

    public void UpdateRemoteSource(string route, string? folder = null)
    {
        SelectedRemoteItem = null;

        _remoteFiles = null;
        this.RaisePropertyChanged(nameof(RemoteFiles));

        _ = Task.Run(async () =>
        {
            _remoteFiles = await _deviceService.GetFiles(route, folder);
            this.RaisePropertyChanged(nameof(RemoteFiles));
        });
    }
}
