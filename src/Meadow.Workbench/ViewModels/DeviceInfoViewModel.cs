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
                try
                {
                    ConsoleOutput.Add(info);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // feels like a bug in the MAUI control here - just ignore it
                }
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

    private bool _useDFU = false;
    public bool UseDfuMode
    {
        get => _useDFU;
        set
        {
            this.RaiseAndSetIfChanged(ref _useDFU, value);
            this.RaisePropertyChanged(nameof(UseDfuMode));
        }
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
            if (!UseDfuMode)
            {
                if (SelectedLocalFirmware == null || SelectedConnection == null) return;
            }

            await UpdateFirmware(SelectedLocalFirmware, SelectedConnection);
        });
    }

    private async Task UpdateFirmware(FirmwareInfo version, IMeadowConnection connection)
    {
        if (!UseDfuMode)
        {
            if (connection == null || connection.Device == null || !connection.IsConnected) return;
        }

        try
        {
            // TODO: tell user to power with boot button pressed?

            await FirmwareManager.PushFirmwareToDevice(_connectionManager, connection, version.Version, _logger);
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

    public ICommand BrowseAppCommand
    {
        get => new Command(async () =>
        {
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
                _settingsService.SaveCurrentSettings();
                KnownApps.Add(info);
                SelectedApp = info;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        });
    }

    public ICommand DeployAppCommand
    {
        get => new Command(async () =>
        {
            if (SelectedConnection == null || SelectedApp == null) return;

            try
            {
                var di = new DirectoryInfo(Path.GetDirectoryName(SelectedApp.FullName));
                if (!di.Exists)
                {
                    await App.Current.MainPage.DisplayAlert("Invalid Location", $"Select a valid folder containing 'App.dll'.", "OK");
                    return;
                }

                await FirmwareManager.PushApplicationToDevice(SelectedConnection, di, _logger);
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