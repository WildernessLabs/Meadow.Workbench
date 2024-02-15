using Avalonia.Controls;
using Meadow.Workbench.Services;
using Meadow.Workbench.ViewModels;
using ReactiveUI;
using System;

namespace Meadow.Workbench.Models;

internal class Feature<TView, TViewModel> : ReactiveObject, IFeature
    where TView : UserControl, new()
    where TViewModel : FeatureViewModel, new()
{
    private bool _isActive;
    private bool _isVisible = true;

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

    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
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
