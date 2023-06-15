using DynamicData;
using Meadow.Hcom;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Input;

namespace Meadow.Workbench.ViewModels;

public partial class DeviceInfoViewModel : ViewModelBase
{
    private CaptureLogger _logger;
    private UserSettingsService _settingsService;
    private IFolderPicker _folderPicker;

    public LogLevel _logLevel;
    public LogLevel SelectedLogLevel
    {
        get => _logLevel;
        set
        {
            _logger.Level = value;
            this.RaiseAndSetIfChanged(ref _logLevel, value);
        }
    }

    public LogLevel[] LogLevels
    {
        get => Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>().ToArray();
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

            SelectedConnection = ConnectionManager.GetConnection<SerialConnection>(_selectedPort);

            Task.Run(async () =>
            {
                var device = await SelectedConnection.Attach();
                RefreshDeviceInfo.Execute(null);
            });
        }
    }

    private bool _isFirmwareTab;
    public bool IsFirmwareTab
    {
        get => _isFirmwareTab;
        set => this.RaiseAndSetIfChanged(ref _isFirmwareTab, value);
    }

    private bool _isApplicationTab;
    public bool IsApplicationTab
    {
        get => _isApplicationTab;
        set => this.RaiseAndSetIfChanged(ref _isApplicationTab, value);
    }

    private bool _isFileSystemTab;
    public bool IsFileSystemTab
    {
        get => _isFileSystemTab;
        set => this.RaiseAndSetIfChanged(ref _isFileSystemTab, value);
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

    private bool _firmwareUpdateAvailable;
    public bool FirmwareUpdateAvailable
    {
        get => _firmwareUpdateAvailable;
        set => this.RaiseAndSetIfChanged(ref _firmwareUpdateAvailable, value);
    }

    private bool _firmwareUpdateInProgress;
    public bool FirmwareUpdateInProgress
    {
        get => _firmwareUpdateInProgress;
        set => this.RaiseAndSetIfChanged(ref _firmwareUpdateInProgress, value);
    }

    private string _latestFirwareVersion;
    public string LatestFirwareVersion
    {
        get => _latestFirwareVersion;
        set => this.RaiseAndSetIfChanged(ref _latestFirwareVersion, value);
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

    private Hcom.DeviceInfo _info;
    public Hcom.DeviceInfo DeviceInfo
    {
        get => _info;
        set
        {
            this.RaiseAndSetIfChanged(ref _info, value);
            this.RaisePropertyChanged(nameof(DeviceInfo));
        }
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
                /*
                value.ConnectionStateChanged += (connection, newState) =>
                {
                    MeadowConnected = newState;
                };
                */
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

    private DateTimeOffset? _lastRtc;
    public DateTimeOffset? LastRtc
    {
        get => _lastRtc;
        set
        {
            this.RaiseAndSetIfChanged(ref _lastRtc, value);
        }
    }

    private bool _runtimeState;
    public bool RuntimeState
    {
        get => _runtimeState;
        set
        {
            this.RaiseAndSetIfChanged(ref _runtimeState, value);
        }
    }

    private ObservableCollection<AppInfo> _knownApps = new();
    public ObservableCollection<AppInfo> KnownApps
    {
        get => _knownApps;
    }

    public ICommand DownloadLatestFirmwareCommand
    {
        get => new Command(async () =>
        {
            // download
            await FirmwareManager.GetRemoteFirmware(LatestFirwareVersion, _logger);
            RefreshLocalFirmwareVersionsCommand.Execute(null);
        });
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

            var fi = await FirmwareManager.GetRemoteFirmwareInfo(version, null);
            if (fi == null)
            {
                await App.Current.MainPage.DisplayAlert("Firmware Download", $"Version {version} does not exist.", "OK");
            }
            else
            {
                // download
                await FirmwareManager.GetRemoteFirmware(version, null);
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
            var newItems = builds.Except(LocalFirmwareVersions).ToArray().Reverse();
            var removedItems = LocalFirmwareVersions.Except(builds).ToArray();

            Application.Current.Dispatcher.Dispatch(() =>
            {
                try
                {
                    LocalFirmwareVersions.AddRange(newItems);
                    LocalFirmwareVersions.RemoveMany(removedItems);
                    CheckForNewFirmware();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

        });
    }

    public ICommand ClearConsoleCommand
    {
        get => new Command(() =>
        {
            ConsoleOutput.Clear();
        });
    }

    public ICommand RefreshDeviceInfo
    {
        get => new Command(async () =>
        {
            if (SelectedConnection == null || SelectedConnection.Device == null) return;

            DeviceInfo = await SelectedConnection.Device.GetDeviceInfo();
            this.RaisePropertyChanged(nameof(SelectedConnection));

            _ = Task.Run(RefreshRuntimeState);

        });
    }

    public ICommand EnableRuntimeCommand
    {
        get => new Command(async () =>
        {
            await SelectedConnection.Device.RuntimeEnable();
            await RefreshRuntimeState();
        });
    }

    public ICommand DisableRuntimeCommand
    {
        get => new Command(async () =>
        {
            await SelectedConnection.Device.RuntimeDisable();
            await RefreshRuntimeState();
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

    public ICommand ResetDeviceCommand
    {
        get => new Command(() =>
        {
            SelectedConnection?.Device?.Reset();
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

    public ICommand TimeSyncCommand
    {
        get => new Command(async () =>
        {
            if (SelectedConnection == null || SelectedConnection.Device == null) return;

            try
            {
                await SelectedConnection.Device.SetRtcTime(DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        });
    }

    public ICommand CopyDeviceInfoCommand
    {
        get => new Command(async () =>
        {
            if (SelectedConnection == null || SelectedConnection.Device == null || DeviceInfo == null) return;

            try
            {
                var info = $"Name: {DeviceInfo.DeviceName}\r\n" +
                           $"Serial #: {DeviceInfo.SerialNumber}\r\n" +
                           $"Hardware: {DeviceInfo.HardwareVersion}\r\n" +
                           $"OS: {DeviceInfo.OsVersion}\r\n" +
                           $"Runtime: {DeviceInfo.RuntimeVersion}\r\n" +
                           $"Coprocessor: {DeviceInfo.CoprocessorOsVersion}\r\n";

                await Clipboard.SetTextAsync(info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        });
    }

    public ICommand FirmwareTabCommand
    {
        get => new Command(() =>
        {
            IsApplicationTab = IsFileSystemTab = false;
            IsFirmwareTab = true;
        });
    }

    public ICommand ApplicationTabCommand
    {
        get => new Command(() =>
        {
            IsFirmwareTab = IsFileSystemTab = false;
            IsApplicationTab = true;
        });
    }

    public ICommand FileSystemTabCommand
    {
        get => new Command(() =>
        {
            IsApplicationTab = IsFirmwareTab = false;
            IsFileSystemTab = true;
        });
    }

    public DeviceInfoViewModel(
        ILogger logger,
        UserSettingsService settingsService,
        IFolderPicker folderPicker)
    {
        _logger = logger as CaptureLogger;
        if (_logger == null)
        {
            _logger = new CaptureLogger();
        }

        SelectedLogLevel = LogLevel.Information;

        _logger.OnLogInfo += (level, info) =>
        {
            Application.Current.Dispatcher.Dispatch(() =>
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
            });
        };

        IsFirmwareTab = true;

        _folderPicker = folderPicker;
        _settingsService = settingsService;

        //_deviceManager = connectionManager;
        //_deviceManager.ConnectionAdded += OnConnectionAdded;

        RefreshAvailablePorts();

        RefreshLocalFirmwareVersionsCommand.Execute(null);
        RefreshKnownApps();
        Task.Run(() => RtcUpdater());
    }

    private async Task UpdateFirmware(FirmwareInfo version, IMeadowConnection? connection)
    {
        if (!UseDfuMode)
        {
            if (connection == null || connection.Device == null || !connection.IsConnected) return;
        }

        FirmwareUpdateInProgress = true;

        try
        {
            // TODO: tell user to power with boot button pressed?
            var updater = FirmwareManager.GetFirmwareUpdater(_selectedConnection);
            // TODO: watch the state to update the UI?
            await updater.Update(connection, version.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flashing OS to Meadow");
        }
        finally
        {
            FirmwareUpdateInProgress = false;
        }

        if (connection != null)
        {
            // refresh the device info
            try
            {
                await connection.WaitForMeadowAttach();

                DeviceInfo = await connection.Device.GetDeviceInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed getting device info");
            }
        }
    }

    private void OnConnectionAdded(IMeadowConnection connection)
    {
        Application.Current.Dispatcher.Dispatch(() =>
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

    private async void RtcUpdater()
    {
        while (true)
        {
            if (SelectedConnection != null && SelectedConnection.Device != null && !FirmwareUpdateInProgress)
            {
                try
                {
                    LastRtc = await SelectedConnection.Device.GetRtcTime();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private async Task RefreshRuntimeState()
    {
        RuntimeState = await SelectedConnection?.Device?.IsRuntimeEnabled();
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

    private void RefreshAvailablePorts()
    {
        _ports.Clear();

        _ports.AddRange(SerialPort.GetPortNames());
    }

    private void CheckForNewFirmware()
    {
        Task.Run(async () =>
        {
            LatestFirwareVersion = await FirmwareManager.GetCloudLatestFirmwareVersion();
            var match = LocalFirmwareVersions.FirstOrDefault(v => v.Version == LatestFirwareVersion);

            FirmwareUpdateAvailable = match == null;
        });
    }
}