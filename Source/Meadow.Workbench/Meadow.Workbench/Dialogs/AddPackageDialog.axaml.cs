using Avalonia.Controls;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench;

internal partial class AddPackageDialog : UserControl
{
    public AddPackageDialog()
    {
        InitializeComponent();
    }

    public AddPackageDialog(AddPackageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}