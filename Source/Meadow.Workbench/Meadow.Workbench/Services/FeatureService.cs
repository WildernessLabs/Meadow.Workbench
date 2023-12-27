using System.Collections.ObjectModel;

namespace Meadow.Workbench.Services;

internal class FeatureService
{
    public ObservableCollection<IFeature> Features { get; } = new ObservableCollection<IFeature>();
}
