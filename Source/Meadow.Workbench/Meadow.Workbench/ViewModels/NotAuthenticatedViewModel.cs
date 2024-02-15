using DialogHostAvalonia;
using Meadow.Cloud.Client.Identity;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;

namespace Meadow.Workbench.ViewModels;

public class NotAuthenticatedViewModel : ViewModelBase
{
    public const string BeforeLaunchButtonText = "Launch Browser";
    public const string AfterLaunchButtonText = "Close";

    private IdentityManager _identityManager;
    private SettingsService _settingsService;
    private string _buttonText = BeforeLaunchButtonText;
    private bool _launchClicked = false;

    public IReactiveCommand LoginCommand { get; }

    public NotAuthenticatedViewModel()
    {
        _identityManager = Locator.Current.GetService<IdentityManager>();
        _settingsService = Locator.Current.GetService<SettingsService>();

        LoginCommand = ReactiveCommand.Create(LaunchLogin);
    }

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

            LaunchButtonText = AfterLaunchButtonText;
            _launchClicked = true;
        }
        else
        {
            DialogHost.Close(null);
        }
    }
}
