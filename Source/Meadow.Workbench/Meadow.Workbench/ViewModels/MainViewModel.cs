using Avalonia.Controls;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System.Linq;
using System.Reactive;

namespace Meadow.Workbench.ViewModels;

public class MainViewModel : ViewModelBase
{
    private UserControl _activeContent;

    internal FeatureService FeatureService { get; }
    private SettingsService SettingsService { get; }

    public ReactiveCommand<IFeature, Unit> FeatureSelectedCommand { get; }
    public UserControl Content { get => _activeContent; set => this.RaiseAndSetIfChanged(ref _activeContent, value); }

    public MainViewModel()
    {
        FeatureService = Locator.Current.GetService<FeatureService>();
        FeatureSelectedCommand = ReactiveCommand.Create<IFeature>(ActivateFeature);
        SettingsService = Locator.Current.GetService<SettingsService>();

        // select the first feature
        // TODO: remember the last feature selected and restore it on app run
        SettingsService = Locator.Current.GetService<SettingsService>();
        var lastFeature = SettingsService.LastFeature;

        var c = FeatureService!.Features.FirstOrDefault(f => f.Title == lastFeature)?.ViewInstance.Value;
        if (c == null) c = FeatureService!.Features.First().ViewInstance.Value;
        Content = c;
    }

    private void ActivateFeature(IFeature feature)
    {
        Content = feature.ViewInstance.Value;
        SettingsService.LastFeature = feature.Title;
    }
}
