using Avalonia.Controls;
using DialogHostAvalonia;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench.Dialogs;

public partial class FirmwareFlashDialog : UserControl
{
    public FirmwareFlashDialog()
    {
        InitializeComponent();
        close.Click += Close_Click;
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }

    internal FirmwareFlashDialog(FirmwareFlashViewModel viewModel)
        : this()
    {
        this.DataContext = viewModel;
    }
}
