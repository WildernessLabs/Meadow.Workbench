using Meadow.Cloud.Client.Identity;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;

namespace Meadow.Workbench.ViewModels;

public class NotAuthenticatedViewModel : ViewModelBase
{
    private IdentityManager _identityManager;
    private SettingsService _settingsService;

    public IReactiveCommand LoginCommand { get; }

    public NotAuthenticatedViewModel()
    {
        _identityManager = Locator.Current.GetService<IdentityManager>();
        _settingsService = Locator.Current.GetService<SettingsService>();

        LoginCommand = ReactiveCommand.Create(LaunchLogin);
    }

    public void LaunchLogin()
    {
        _identityManager.Login(_settingsService.CloudHostName);
    }
}
