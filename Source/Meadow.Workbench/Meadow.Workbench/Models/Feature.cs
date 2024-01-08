using Avalonia.Controls;
using Meadow.Workbench.Services;
using Meadow.Workbench.ViewModels;
using System;

namespace Meadow.Workbench.Models;

internal class Feature<TView, TViewModel> : IFeature
    where TView : UserControl, new()
    where TViewModel : FeatureViewModel, new()
{
    public string Title { get; set; }
    public Type ViewType => typeof(TView);
    public Type ViewModelType => typeof(TView);
    public Lazy<UserControl> ViewInstance { get; }
    public Lazy<FeatureViewModel> ViewModelInstance { get; }

    public Feature()
    {
        ViewModelInstance = new Lazy<FeatureViewModel>(BuildViewModel);
        ViewInstance = new Lazy<UserControl>(BuildView);
    }

    private TViewModel BuildViewModel()
    {
        var vm = new TViewModel();

        return vm;
    }

    private TView BuildView()
    {
        var view = new TView();
        ViewModelInstance.Value.FeatureView = view;
        view.DataContext = ViewModelInstance.Value;
        return view;
    }
}
