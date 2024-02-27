using DialogHostAvalonia;
using Meadow.Cloud.Client.Devices;
using Meadow.Workbench.Dialogs;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

internal class DeviceViewModel : ViewModelBase
{
    private bool _isConnected;

    private StorageService _storageService;
    private DeviceService _deviceService;
    private string? _deviceTime;
    private bool _isRuntimeEnabled;
    private bool _outputConneted = false;
    private int? _selectedOutput;

    public DeviceInformation RootInfo { get; private set; }
    public IReactiveCommand SetFriendlyNameCommand { get; }
    public IReactiveCommand SetClockCommand { get; }
    public IReactiveCommand GetClockCommand { get; }
    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand DisableRuntimeCommand { get; }
    public IReactiveCommand EnableRuntimeCommand { get; }
    public IReactiveCommand DeleteDeviceCommand { get; }
    public IReactiveCommand ClearOutputCommand { get; }
    public IReactiveCommand RefreshInfoCommand { get; }
    public IReactiveCommand ProvisionCommand { get; }

    public ObservableCollection<String> Output { get; } = new();

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
        ClearOutputCommand = ReactiveCommand.Create(ClearOutput);
        RefreshInfoCommand = ReactiveCommand.CreateFromTask(RefreshDeviceInfo);
        ProvisionCommand = ReactiveCommand.CreateFromTask(ProvisionDevice);

        if (RootInfo.Connection != null)
        {
            this.RootInfo.Connection.DeviceMessageReceived += OnDeviceMessageReceived;
            _outputConneted = true;
        }
    }

    public int? SelectedOutput
    {
        get => _selectedOutput;
        set => this.RaiseAndSetIfChanged(ref _selectedOutput, value);
    }

    private void OnDeviceMessageReceived(object? sender, (string message, string? source) e)
    {
        Output.Add(e.message.TrimEnd());
        SelectedOutput = Output.Count - 1;
    }

    public async Task RefreshDeviceInfo()
    {
        var info = await _deviceService.GetDeviceInformationAtLocation(RootInfo.LastRoute);
        if (info != null)
        {
            RootInfo = info;
            this.RaisePropertyChanged(nameof(Version));
            this.RaisePropertyChanged(nameof(LastSeen));
            this.RaisePropertyChanged(nameof(RootInfo));
        }
    }

    public async Task ProvisionDevice()
    {
        var meadowCloudClient = Locator.Current.GetService<Cloud.Client.IMeadowCloudClient>();
        if (!await meadowCloudClient!.Authenticate())
        {
            var dialog = new NotAuthenticatedDialog(new NotAuthenticatedViewModel(NotAuthenticatedViewModel.AuthReason.DeviceProvision));

            // notify user to log in
            await DialogHost.Show(dialog);
            // get the auth token
            await meadowCloudClient!.Authenticate();
        }

        var settingsService = Locator.Current.GetService<SettingsService>();

        // show a message to the user
        var umvm = new UserMessageViewModel(Strings.UserMessageGettingUserOrgs);
        var messageDialog = new UserMessageDialog(umvm);
        _ = DialogHost.Show(messageDialog);

        // get the orgs
        var org = (await meadowCloudClient.User.GetOrganizations()).First();
        ;
        // change the user message
        umvm.UserMessage = Strings.UserMessageGettingPublicKey;
        var publicKey = await _deviceService.GetPublicKey(RootInfo.LastRoute);

        var provisioningID = RootInfo.DeviceID != null ? RootInfo.DeviceID : RootInfo?.SerialNumber;
        var provisioningName = !string.IsNullOrWhiteSpace(RootInfo.FriendlyName) ? RootInfo.FriendlyName : RootInfo.DeviceName;

        // change the user message
        umvm.UserMessage = Strings.UserMessageProvisioning;

        try
        {
            var request = new AddDeviceRequest(
                id: provisioningID,
                name: provisioningName,
                orgId: org.Id,
                collectionId: org.DefaultCollectionId,
                publicKey: publicKey);

            await meadowCloudClient.Device.AddDevice(request);

            umvm.UserMessage = Strings.DeviceProvisionedSuccessfully;
            await Task.Delay(2000);

            DialogHost.Close(null);
        }
        catch (Exception ex)
        {
            // change the user message
            umvm.UserMessage = string.Format(Strings.DeviceProvisionFailedForSpecifiedReason, ex.Message);
        }
    }

    public void ClearOutput()
    {
        Output.Clear();
    }

    public void Update(DeviceInformation info)
    {
        this.RootInfo = info;

        if (RootInfo.Connection != null && !_outputConneted)
        {
            this.RootInfo.Connection.DeviceMessageReceived += OnDeviceMessageReceived;
            _outputConneted = true;
        }

        this.RaisePropertyChanged(nameof(RootInfo));
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

            this.RaisePropertyChanged(nameof(Version));
            this.RaisePropertyChanged(nameof(LastSeen));
            this.RaisePropertyChanged(nameof(RootInfo));
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
