using Avalonia.Controls;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench.Dialogs;

public partial class NotAuthenticatedDialog : UserControl
{
    public NotAuthenticatedDialog(NotAuthenticatedViewModel vm)
    {
        this.DataContext = vm;

        InitializeComponent();
    }

    public NotAuthenticatedDialog()
    {
        InitializeComponent();
    }
}