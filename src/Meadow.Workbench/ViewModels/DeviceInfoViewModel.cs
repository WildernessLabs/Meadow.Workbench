using DynamicData;
using Meadow.CLI.Core;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace Meadow.Workbench.ViewModels;

public class DeviceInfoViewModel : ViewModelBase
{
    private CaptureLogger _logger;
    private MeadowConnectionManager _connectionManager;
    private UserSettingsService _settingsService;
    private IFolderPicker _folderPicker;

    public DeviceInfoViewModel(ILogger logger, MeadowConnectionManager connectionManager, UserSettingsService settingsService, IFolderPicker folderPicker)
    {
        _logger = logger as CaptureLogger;
        if (_logger == null)
        {
            _logger = new CaptureLogger();
        }

        _logger.OnLogInfo += (level, info) =>
        {
            lock (ConsoleOutput)
            {
                ConsoleOutput.Add(info);
            }
        };

        _folderPicker = folderPicker;
        _settingsService = settingsService;

        _connectionManager = connectionManager;
        _connectionManager.ConnectionAdded += OnConnectionAdded;

        RefreshLocalFirmwareVersionsCommand.Execute(null);
        RefreshKnownApps();
    }

    private ObservableCollection<string> _consoleOutput = new ObservableCollection<string>();
    public ObservableCollection<string> ConsoleOutput
    {
        get => _consoleOutput;
    }

    private bool _meadowConnected = false;
    public bool MeadowConnected
    {
        get => _meadowConnected;
        set
        {
            this.RaiseAndSetIfChanged(ref _meadowConnected, value);
            // this will force a device info refresh
            this.RaisePropertyChanged(nameof(SelectedConnection));
        }
    }

    private string? _selectedPort;
    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPort, value);

            SelectedConnection = _connectionManager[value];
        }
    }

    private ObservableCollection<string> _ports = new();
    public ObservableCollection<string> Ports
    {
        get => _ports;
    }

    private FirmwareInfo? _selectedLocalFirmware;
    public FirmwareInfo? SelectedLocalFirmware
    {
        get => _selectedLocalFirmware;
        set => this.RaiseAndSetIfChanged(ref _selectedLocalFirmware, value);
    }


    public ICommand GetFirmwareCommand
    {
        get => new Command(async () =>
        {
            string version = await App.Current.MainPage.DisplayPromptAsync("Firmware Download", "What is the version number?");

            if (string.IsNullOrEmpty(version))
            {
                // abort
                return;
            }

            var fi = await FirmwareManager.GetRemoteFirmwareInfo(version, _logger);
            if (fi == null)
            {
                await App.Current.MainPage.DisplayAlert("Firmware Download", $"Version {version} does not exist.", "OK");
            }
            else
            {
                // download
                await FirmwareManager.GetRemoteFirmware(version, _logger);
                RefreshLocalFirmwareVersionsCommand.Execute(null);
            }
        });
    }

    public ICommand RefreshLocalFirmwareVersionsCommand
    {
        get => new Command(() =>
        {
            // a file system monitor might be in order for this instead?
            var builds = FirmwareManager.GetAllLocalFirmwareBuilds();
            var newItems = builds.Except(LocalFirmwareVersions).ToArray();
            var removedItems = LocalFirmwareVersions.Except(builds).ToArray();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    LocalFirmwareVersions.AddRange(newItems);
                    LocalFirmwareVersions.RemoveMany(removedItems);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

        });
    }

    private ObservableCollection<FirmwareInfo> _localFirmwareVersions = new();
    public ObservableCollection<FirmwareInfo> LocalFirmwareVersions
    {
        get => _localFirmwareVersions;
    }

    private IMeadowConnection? _selectedConnection;
    public IMeadowConnection? SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            if (value != null)
            {
                MeadowConnected = value.IsConnected;

                // TODO: when we change connections, remove old handlers
                value.ConnectionStateChanged += (connection, newState) =>
                {
                    MeadowConnected = newState;
                };

                value.AutoReconnect = true;
            }

            this.RaiseAndSetIfChanged(ref _selectedConnection, value);
        }
    }

    private AppInfo? _selectedApp;
    public AppInfo? SelectedApp
    {
        get => _selectedApp;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedApp, value);
        }
    }

    private ObservableCollection<AppInfo> _knownApps = new();
    public ObservableCollection<AppInfo> KnownApps
    {
        get => _knownApps;
    }

    private void RefreshKnownApps()
    {
        KnownApps.Clear();
        var updatedList = false;
        foreach (var app in _settingsService.Settings.KnownApplications)
        {
            var di = new DirectoryInfo(app);
            if (di.Exists)
            {
                KnownApps.Add(new AppInfo(di));
            }
            else
            {
                // doesn't exist, so remove it
                _settingsService.Settings.KnownApplications.Remove(app);
                updatedList = true;
            }
        }

        if (updatedList)
        {
            _settingsService.SaveCurrentSettings();
        }
    }

    public ICommand ClearConsoleCommand
    {
        get => new Command(() =>
        {
            ConsoleOutput.Clear();
        });
    }

    public ICommand SendSelectedFirmwareCommand
    {
        get => new Command(async () =>
        {
            if (SelectedLocalFirmware == null || SelectedConnection == null) return;

            await UpdateFirmware(SelectedLocalFirmware, SelectedConnection);
        });
    }

    private async Task UpdateFirmware(FirmwareInfo version, IMeadowConnection connection)
    {
        if (connection == null || connection.Device == null || !connection.IsConnected) return;

        try
        {
            // TODO: tell user to power with boot button pressed?

            await FirmwareManager.PushFirmwareToDevice(connection, version.Version, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flashing OS to Meadow");
        }
        finally
        {
            connection.AutoReconnect = true;
        }

        // refresh the device info

        await connection.WaitForConnection(TimeSpan.FromSeconds(5));

        await connection.Device
                        .GetDeviceInfo(TimeSpan.FromSeconds(60));
    }

    public ICommand ResetDeviceCommand
    {
        get => new Command(() =>
        {
            SelectedConnection?.Device?.ResetMeadow();
        });
    }

    public ICommand DeployAppCommand
    {
        get => new Command(async () =>
        {
            // TODO: remember last selected location
            try
            {
                if (SelectedConnection == null) return;

                var pickedFolder = await _folderPicker.PickFolder();

                AppInfo info;

                try
                {
                    info = new AppInfo(new DirectoryInfo(pickedFolder));
                }
                catch
                {
                    await App.Current.MainPage.DisplayAlert("Invalid Location", $"Select a folder containing a compiled 'App.dll'.", "OK");
                    return;
                }

                _settingsService.Settings.KnownApplications.Add(pickedFolder);
                KnownApps.Add(info);
                SelectedApp = info;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        });
    }

    private void OnConnectionAdded(IMeadowConnection connection)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                Ports.Add(connection.Name);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        });
    }
}

public class AppInfo
{
    private FileInfo _fi;

    public AppInfo(DirectoryInfo di)
    {
        // see if there's an App.exe in the folder
        _fi = di.GetFiles("App.dll").FirstOrDefault();

        if (_fi == null)
        {
            throw new Exception("Invalid Location");
        }

        // app name is the project name - look in the folder above "bin"
        var projectFolder = di.FullName.Substring(0, di.FullName.IndexOf("\\bin"));
        var projectFile = Directory.GetFiles(projectFolder, "*proj").FirstOrDefault();

        Name = Path.GetFileNameWithoutExtension(projectFile);
    }

    public string Name { get; init; }
    public DateTime LastChanged => _fi.LastWriteTime;
}
