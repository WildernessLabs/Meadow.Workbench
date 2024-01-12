using Avalonia.Controls;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class MainViewModel : ViewModelBase
{
    private UserControl _activeContent;
    private IFeature? _activeFeature;

    internal FeatureService FeatureService { get; }
    private SettingsService SettingsService { get; }

    public ReactiveCommand<IFeature, Unit> FeatureSelectedCommand { get; }
    public IReactiveCommand SettingsCommand { get; }
    public IReactiveCommand UserCommand { get; }

    public UserControl Content { get => _activeContent; set => this.RaiseAndSetIfChanged(ref _activeContent, value); }

    public MainViewModel()
    {
        FeatureService = Locator.Current.GetService<FeatureService>();
        FeatureSelectedCommand = ReactiveCommand.Create<IFeature>(ActivateFeature);
        SettingsService = Locator.Current.GetService<SettingsService>();

        SettingsCommand = ReactiveCommand.CreateFromTask(ShowSettings);
        UserCommand = ReactiveCommand.CreateFromTask(ShowUserLogin);

        // select the first feature
        SettingsService = Locator.Current.GetService<SettingsService>();
        var lastFeature = SettingsService.LastFeature;

        var c = FeatureService!.Features.FirstOrDefault(f => f.Title == lastFeature)?.ViewInstance.Value;
        if (c == null) c = FeatureService!.Features.First().ViewInstance.Value;
        Content = c;
    }

    private async Task ShowUserLogin()
    {
    }

    private async Task ShowSettings()
    {
    }

    private void ActivateFeature(IFeature feature)
    {
        if (_activeFeature != null)
        {
            _activeFeature.IsActive = false;
        }

        _activeFeature = feature;
        _activeFeature.IsActive = true;
        Content = feature.ViewInstance.Value;
        SettingsService.LastFeature = feature.Title;
    }
}
