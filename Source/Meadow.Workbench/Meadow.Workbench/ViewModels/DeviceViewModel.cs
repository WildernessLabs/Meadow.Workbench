using DialogHostAvalonia;
using Meadow.Workbench.Services;
using ReactiveUI;
using System;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

internal class DeviceViewModel : ViewModelBase
{
    private bool _isConnected;

    public DeviceInformation RootInfo { get; private set; }

    private StorageService _storageService;

    public IReactiveCommand SetFriendlyNameCommand { get; }

    public DeviceViewModel(DeviceInformation info, StorageService storageService)
    {
        RootInfo = info;
        _storageService = storageService;

        IsConnected = info.IsConnected;

        SetFriendlyNameCommand = ReactiveCommand.CreateFromTask(OnSetFriendlyName);
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

    public bool HasFriendlyName
    {
        get => !string.IsNullOrWhiteSpace(FriendlyName);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
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
