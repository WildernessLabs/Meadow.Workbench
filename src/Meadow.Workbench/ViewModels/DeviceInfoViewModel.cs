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
            //            if (SelectedDevice == null || SelectedLocalFirmware == null) return;

            //            var m = new MeadowDeviceHelper(SelectedDevice, null);
            //            await m.FlashOsAsync(osVersion: SelectedLocalFirmware.Version);
        });
    }

    public ICommand ResetDeviceCommand
    {
        get => new Command(() =>
        {
            SelectedConnection?.Device?.ResetMeadowAsync();
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

                var result = await FilePicker.Default.PickAsync(PickOptions.Default);
                if (result != null)
                {
                    var path = result.FullPath;
                    //                    var m = new MeadowDeviceHelper(SelectedDevice, null);
                    //                    await m.DeployAppAsync(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        });
    }

    public DeviceInfoViewModel(ILogger logger, MeadowConnectionManager connectionManager)
    {
        _logger = logger as CaptureLogger;
        if (_logger == null)
        {
            _logger = new CaptureLogger();
        }

        _logger.OnLogInfo += (level, info) =>
        {
            ConsoleOutput.Add(info);
        };

        _connectionManager = connectionManager;
        _connectionManager.ConnectionAdded += OnConnectionAdded;

        RefreshLocalFirmwareVersionsCommand.Execute(null);
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
