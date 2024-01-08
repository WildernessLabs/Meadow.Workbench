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

    public ReactiveCommand<IFeature, Unit> FeatureSelectedCommand { get; }
    public UserControl Content { get => _activeContent; set => this.RaiseAndSetIfChanged(ref _activeContent, value); }

    public MainViewModel()
    {
        FeatureService = Locator.Current.GetService<FeatureService>();
        FeatureSelectedCommand = ReactiveCommand.Create<IFeature>(ActivateFeature);

        // select the first feature
        // TODO: remember the last feature selected and restore it on app run
        Content = FeatureService!.Features.First().ViewInstance.Value;
    }

    private void ActivateFeature(IFeature feature)
    {
        Content = feature.ViewInstance.Value;
    }
}
