using DialogHostAvalonia;
using Meadow.Workbench.Services;
using ReactiveUI;
using System;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

internal class DeviceViewModel : ViewModelBase
{
    private bool _isConnected;

    private DeviceInformation _info;
    private StorageService _storageService;

    public IReactiveCommand SetFriendlyNameCommand { get; }

    public DeviceViewModel(DeviceInformation info, StorageService storageService)
    {
        _info = info;
        _storageService = storageService;

        IsConnected = false;

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
                _storageService.UpdateDeviceInfo(_info);
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
        get => _info.FriendlyName;
        set
        {
            _info.FriendlyName = value;
            this.RaisePropertyChanged(nameof(HasFriendlyName));
            this.RaisePropertyChanged(nameof(FriendlyName));
        }
    }

    public string DeviceID
    {
        get => _info.DeviceID;
        set
        {
            _info.DeviceID = value;
            this.RaisePropertyChanged(nameof(DeviceID));
        }
    }

    public string? Version
    {
        get => _info.Version;
        set
        {
            _info.Version = value;
            this.RaisePropertyChanged(nameof(Version));
        }
    }

    public DateTime LastSeen
    {
        get => _info.LastSeen;
        set
        {
            _info.LastSeen = value;
            this.RaisePropertyChanged(nameof(LastSeen));
        }
    }
}
