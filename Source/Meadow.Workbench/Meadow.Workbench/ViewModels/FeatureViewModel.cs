using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Meadow.Workbench.ViewModels;

public abstract class FeatureViewModel : ViewModelBase
{
    private Window? _parent;

    public UserControl FeatureView
    {
        get; set;
    }

    public virtual void OnActivated()
    {
    }

    public virtual void OnDeactivated()
    {
    }

    public Window ParentWindow
    {
        get
        {
            if (_parent == null)
            {
                if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    _parent = desktop.MainWindow;
                }
            }
            return _parent;
        }
    }
}
