using DynamicData;
using Meadow.CLI.Core;
using Meadow.CLI.Core.DeviceManagement;
using Meadow.CLI.Core.Devices;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace Meadow.Workbench.ViewModels;

public class DeviceInfoModel : ViewModelBase
{
    private ObservableCollection<string> _ports = new();
    private string? _selectedPort;
    private ObservableCollection<FirmwareInfo> _localFirmwareVersions = new();
    private FirmwareInfo? _selectedLocalFirmware;
    private CLI.Core.Devices.IMeadowDevice? _selectedDevice;
    private string _consoleText = string.Empty;
    private ILogger _logger;

    public DeviceInfoModel()
    {
        var l = new CaptureLogger();
        l.OnLogInfo += (level, info) =>
        {
            ConsoleText += $"{info}\r\n";
        };

        _logger = l;

        Task.Run(PortWatcherProc);

        RefreshLocalFirmwareVersionsCommand.Execute(null);
    }

    private async Task PortWatcherProc()
    {
        while (true)
        {
            await Task.Delay(5000);

            var ports = (await MeadowDeviceManager.GetSerialPorts()).ToArray();

            var newItems = ports.Except(Ports).ToArray();
            var removedItems = Ports.Except(ports).ToArray();

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        Ports.AddRange(newItems);
                        Ports.RemoveMany(removedItems);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }

    public string ConsoleText
    {
        get => _consoleText;
        set => this.RaiseAndSetIfChanged(ref _consoleText, value);
    }

    public string? SelectedPort
    {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    public ObservableCollection<string> Ports
    {
        get => _ports;
    }

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

    public ObservableCollection<FirmwareInfo> LocalFirmwareVersions
    {
        get => _localFirmwareVersions;
    }

    public CLI.Core.Devices.IMeadowDevice? SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
    }

    public ICommand ClearConsoleCommand
    {
        get => new Command(() =>
        {
            ConsoleText = string.Empty;
        });
    }

    public ICommand SelectDeviceCommand
    {
        get => new Command(() =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (SelectedPort != null)
                {
                    SelectedDevice = await MeadowDeviceManager.GetMeadowForSerialPort(SelectedPort, logger: _logger);
                }
                else
                {
                    SelectedDevice = null;
                }
            });
        });
    }

    public ICommand SendSelectedFirmwareCommand
    {
        get => new Command(async () =>
        {
            if (SelectedDevice == null || SelectedLocalFirmware == null) return;

            var m = new MeadowDeviceHelper(SelectedDevice, null);
            await m.FlashOsAsync(osVersion: SelectedLocalFirmware.Version);
        });
    }

    public ICommand ResetDeviceCommand
    {
        get => new Command(() =>
        {
            _ = SelectedDevice?.ResetMeadowAsync();
            SelectedDevice = null;
            SelectedPort = null;
            _ports.Clear();
        });
    }

    public ICommand DeployAppCommand
    {
        get => new Command(async () =>
        {
            // TODO: remember last selected location
            try
            {
                if (SelectedDevice == null) return;

                var result = await FilePicker.Default.PickAsync(PickOptions.Default);
                if (result != null)
                {
                    var path = result.FullPath;
                    var m = new MeadowDeviceHelper(SelectedDevice, null);
                    await m.DeployAppAsync(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        });
    }
}
