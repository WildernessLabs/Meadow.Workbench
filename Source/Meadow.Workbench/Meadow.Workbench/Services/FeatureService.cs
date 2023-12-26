using Avalonia.Controls;
using Meadow.Workbench.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace Meadow.Workbench.Services;

internal class DeviceService
{
}

public interface IFeature
{
    public string Title { get; }
    public Type ViewType { get; }
    public Type ViewModelType { get; }
    public Lazy<UserControl> ViewInstance { get; }
    public Lazy<FeatureViewModel> ViewModelInstance { get; }
}

internal class FeatureService
{
    public ObservableCollection<IFeature> Features { get; } = new ObservableCollection<IFeature>();
}
