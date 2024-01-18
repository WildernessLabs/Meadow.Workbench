using Avalonia.Controls;
using DialogHostAvalonia;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench.Dialogs;

public partial class OtAFirmwareFlashDialog : UserControl
{
    public OtAFirmwareFlashDialog()
    {
        InitializeComponent();
        close.Click += Close_Click;
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }

    internal OtAFirmwareFlashDialog(OtAFirmwareFlashViewModel viewModel)
        : this()
    {
        this.DataContext = viewModel;
    }
}
