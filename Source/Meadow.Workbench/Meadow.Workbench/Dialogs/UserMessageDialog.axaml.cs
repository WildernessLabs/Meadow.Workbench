using Avalonia.Controls;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench.Dialogs;

public partial class UserMessageDialog : UserControl
{
    public UserMessageDialog()
    {
        InitializeComponent();
    }

    public UserMessageDialog(UserMessageViewModel vm)
        : this()
    {
        this.DataContext = vm;
    }
}