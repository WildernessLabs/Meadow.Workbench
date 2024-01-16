using Avalonia.Controls;
using System.Collections.ObjectModel;

namespace Meadow.Workbench.Services;

internal class FeatureService
{
    private IFeature? _activeFeature;

    public ObservableCollection<IFeature> Features { get; } = new ObservableCollection<IFeature>();

    public UserControl Activate(IFeature feature)
    {
        if (_activeFeature != null)
        {
            _activeFeature.ViewModelInstance.Value.OnDeactivated();
            _activeFeature.IsActive = false;
        }

        _activeFeature = feature;
        _activeFeature.IsActive = true;

        _activeFeature.ViewModelInstance.Value.OnActivated();

        return feature.ViewInstance.Value;
    }
}
