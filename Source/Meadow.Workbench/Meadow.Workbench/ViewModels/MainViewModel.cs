using Avalonia.Controls;
using DialogHostAvalonia;
using Meadow.Cloud.Client;
using Meadow.Cloud.Client.Identity;
using Meadow.Workbench.Dialogs;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class MainViewModel : ViewModelBase
{
    private UserControl _activeContent;
    private bool _isAuthenticated;
    private User? _activeUser;
    private IdentityManager _identityManager;
    private UserService _userService;

    internal FeatureService FeatureService { get; }
    private SettingsService SettingsService { get; }

    public ReactiveCommand<IFeature, Unit> FeatureSelectedCommand { get; }
    public IReactiveCommand SettingsCommand { get; }
    public IReactiveCommand UserCommand { get; }

    public UserControl Content { get => _activeContent; set => this.RaiseAndSetIfChanged(ref _activeContent, value); }

    public IEnumerable<IFeature> VisibleFeatures => FeatureService.Features.Where(f => f.IsVisible);

    public MainViewModel()
    {
        _identityManager = Locator.Current.GetService<IdentityManager>();
        _userService = Locator.Current.GetService<UserService>();
        FeatureService = Locator.Current.GetService<FeatureService>();
        FeatureSelectedCommand = ReactiveCommand.Create<IFeature>(ActivateFeature);
        SettingsService = Locator.Current.GetService<SettingsService>();

        SettingsCommand = ReactiveCommand.CreateFromTask(ShowSettings);
        UserCommand = ReactiveCommand.CreateFromTask(ChangeAuthentication);

        // select the first feature
        SettingsService = Locator.Current.GetService<SettingsService>();
        var lastFeature = SettingsService.LastFeature;

        var c = FeatureService!.Features.FirstOrDefault(f => f.Title == lastFeature)?.ViewInstance.Value;
        if (c == null) c = FeatureService!.Features.First().ViewInstance.Value;
        Content = c;

        IsAuthenticated = false;
        _ = RefreshUserInfo();
    }

    private async Task ChangeAuthentication()
    {
        var identityManager = Locator.Current.GetService<IdentityManager>();

        if (IsAuthenticated)
        {
            identityManager.Logout();
            IsAuthenticated = false;
        }
        else
        {
            if (await identityManager.Login())
            {
                _ = RefreshUserInfo();
            }
        }
    }

    private async Task ShowSettings()
    {
        var dialog = new SettingsDialog();
        var result = await DialogHost.Show(dialog, closingEventHandler: (s, e) =>
        {
            /*
            if (!dialog.IsCancelled)
            {
            }
            */
        });

    }

    private void ActivateFeature(IFeature feature)
    {
        Content = FeatureService.Activate(feature);
        SettingsService.LastFeature = feature.Title;
    }

    public string UserName => _activeUser?.FullName ?? string.Empty;

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set => this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
    }

    public async Task<User?> RefreshUserInfo()
    {
        _activeUser = await _userService.GetMe("https://staging.meadowcloud.dev");

        IsAuthenticated = _activeUser != null;

        return _activeUser;
    }

}
