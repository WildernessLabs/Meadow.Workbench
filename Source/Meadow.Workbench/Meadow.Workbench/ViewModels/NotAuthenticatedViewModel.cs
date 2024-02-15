using DialogHostAvalonia;
using Meadow.Cloud.Client.Identity;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System;

namespace Meadow.Workbench.ViewModels;

public class NotAuthenticatedViewModel : ViewModelBase
{
    public enum AuthReason
    {
        FirmwareDownload,
        DeviceProvision
    }

    private IdentityManager _identityManager;
    private SettingsService _settingsService;
    private string _buttonText = Strings.BeforeLaunchButtonText;
    private bool _launchClicked = false;

    public IReactiveCommand LoginCommand { get; }

    public NotAuthenticatedViewModel(AuthReason reason)
    {
        _identityManager = Locator.Current.GetService<IdentityManager>();
        _settingsService = Locator.Current.GetService<SettingsService>();

        InstructionText = reason switch
        {
            AuthReason.FirmwareDownload => Strings.AuthForDownloadInstruction,
            AuthReason.DeviceProvision => Strings.AuthForProvisioningInstruction,
            _ => throw new ArgumentException(nameof(reason)),
        };

        LoginCommand = ReactiveCommand.Create(LaunchLogin);
    }

    public string InstructionText { get; }

    public string LaunchButtonText
    {
        get => _buttonText;
        private set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }

    public void LaunchLogin()
    {
        if (!_launchClicked)
        {
            _ = _identityManager.Login(_settingsService.CloudHostName);

            LaunchButtonText = Strings.AfterLaunchButtonText;
            _launchClicked = true;
        }
        else
        {
            DialogHost.Close(null);
        }
    }
}
