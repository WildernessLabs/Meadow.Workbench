using Avalonia.Controls;
using DialogHostAvalonia;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench;

public partial class HcomFileWriteDialog : UserControl
{
    public HcomFileWriteDialog()
    {
        InitializeComponent();
        close.Click += Close_Click;
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }

    internal HcomFileWriteDialog(HcomFileWriteViewModel viewModel)
        : this()
    {
        this.DataContext = viewModel;
    }
}