using Avalonia.Controls;
using DialogHostAvalonia;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench.Dialogs;

public partial class SettingsDialog : UserControl
{
    public bool IsCancelled { get; private set; }

    public SettingsDialog()
    {
        this.DataContext = new SettingsViewModel();

        InitializeComponent();
        this.cancel.Click += Cancel_Click;
        this.save.Click += Save_Click;
    }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsCancelled = false;
        DialogHost.Close(null);
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsCancelled = true;
        DialogHost.Close(null);
    }
}
