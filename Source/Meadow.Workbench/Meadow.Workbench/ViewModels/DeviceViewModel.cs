using DialogHostAvalonia;
using Meadow.Workbench.Services;
using ReactiveUI;
using System;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

internal class DeviceViewModel : ViewModelBase
{
    private bool _isConnected;

    private StorageService _storageService;
    private DeviceService _deviceService;
    private string? _deviceTime;
    private bool _isRuntimeEnabled;

    public DeviceInformation RootInfo { get; private set; }
    public IReactiveCommand SetFriendlyNameCommand { get; }
    public IReactiveCommand SetClockCommand { get; }
    public IReactiveCommand GetClockCommand { get; }
    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand DisableRuntimeCommand { get; }
    public IReactiveCommand EnableRuntimeCommand { get; }
    public IReactiveCommand DeleteDeviceCommand { get; }

    public DeviceViewModel(DeviceInformation info, DeviceService deviceService, StorageService storageService)
    {
        RootInfo = info;
        _storageService = storageService;
        _deviceService = deviceService;

        IsConnected = info.IsConnected;

        if (IsConnected)
        {
            _ = GetDeviceClockTime();
        }

        SetFriendlyNameCommand = ReactiveCommand.CreateFromTask(OnSetFriendlyName);
        SetClockCommand = ReactiveCommand.CreateFromTask(SendPcTimeToDevice);
        GetClockCommand = ReactiveCommand.CreateFromTask(GetDeviceClockTime);
        ResetCommand = ReactiveCommand.CreateFromTask(Reset);
        DisableRuntimeCommand = ReactiveCommand.CreateFromTask(DisableRuntime);
        EnableRuntimeCommand = ReactiveCommand.CreateFromTask(EnableRuntime);
        DeleteDeviceCommand = ReactiveCommand.CreateFromTask(DeleteDevice);

    }

    private async Task Reset()
    {
        if (IsConnected)
        {
            await _deviceService.ResetDevice(RootInfo.LastRoute);
            _ = RefreshRuntimeState();
        }
    }

    private async Task RefreshRuntimeState()
    {
        if (IsConnected)
        {
            IsRuntimeEnabled = await _deviceService.IsRuntimEnabled(RootInfo.LastRoute);
        }
    }

    private async Task DeleteDevice()
    {
        await _deviceService.RemoveDevice(RootInfo.DeviceID);
        await RefreshRuntimeState();
    }

    private async Task EnableRuntime()
    {
        if (IsConnected)
        {
            await _deviceService.EnableRuntime(RootInfo.LastRoute);
            await RefreshRuntimeState();
        }
    }

    private async Task DisableRuntime()
    {
        if (IsConnected)
        {
            await _deviceService.DisableRuntime(RootInfo.LastRoute);
            await RefreshRuntimeState();
        }
    }

    private async Task SendPcTimeToDevice()
    {
        if (IsConnected)
        {
            var utc = DateTime.UtcNow;
            await _deviceService.SetUtcTime(RootInfo.LastRoute, utc);
            await GetDeviceClockTime();
        }
    }

    private async Task GetDeviceClockTime()
    {
        if (IsConnected)
        {
            var utc = await _deviceService.GetUtcTime(RootInfo.LastRoute);
            if (utc != null)
            {
                DeviceTime = utc.Value.ToLocalTime().ToString();
            }

            _ = RefreshRuntimeState();
        }
    }

    private async Task OnSetFriendlyName()
    {
        var dialog = new InputBoxDialog("Set Friendly Name", string.Empty, "Enter a name");
        var result = await DialogHost.Show(dialog, closingEventHandler: (s, e) =>
        {
            if (!dialog.IsCancelled && dialog.Text != null)
            {
                FriendlyName = dialog.Text;
                _storageService.UpdateDeviceInfo(RootInfo);
                this.RaisePropertyChanged(nameof(FriendlyName));
            }
        });
    }

    public string? DeviceTime
    {
        get => _deviceTime;
        private set => this.RaiseAndSetIfChanged(ref _deviceTime, value);
    }

    public bool HasFriendlyName
    {
        get => !string.IsNullOrWhiteSpace(FriendlyName);
    }

    public bool IsRuntimeEnabled
    {
        get => _isRuntimeEnabled;
        private set => this.RaiseAndSetIfChanged(ref _isRuntimeEnabled, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isConnected, value);
            _ = RefreshRuntimeState();
        }
    }

    public string? FriendlyName
    {
        get => RootInfo.FriendlyName;
        set
        {
            RootInfo.FriendlyName = value;
            this.RaisePropertyChanged(nameof(HasFriendlyName));
            this.RaisePropertyChanged(nameof(FriendlyName));
        }
    }

    public string DeviceID
    {
        get => RootInfo.DeviceID;
        set
        {
            RootInfo.DeviceID = value;
            this.RaisePropertyChanged(nameof(DeviceID));
        }
    }

    public string? Version
    {
        get => RootInfo.OsVersion;
        set
        {
            RootInfo.OsVersion = value;
            this.RaisePropertyChanged(nameof(Version));
        }
    }

    public DateTime LastSeen
    {
        get => RootInfo.LastSeen;
        set
        {
            RootInfo.LastSeen = value;
            this.RaisePropertyChanged(nameof(LastSeen));
        }
    }
}
