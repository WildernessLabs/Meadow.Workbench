using Avalonia.Controls;
using DialogHostAvalonia;
using Meadow.Workbench.ViewModels;
using System;

namespace Meadow.Workbench.Dialogs;

public partial class SettingsDialog : UserControl
{
    private SettingsViewModel? _viewModel;
    public bool IsCancelled { get; private set; }

    public SettingsDialog()
    {
        _viewModel = new SettingsViewModel();
        this.DataContext = _viewModel;

        InitializeComponent();
        this.cancel.Click += Cancel_Click;
        this.save.Click += Save_Click;
        this.browsePackagesFolder.Click += BrowsePackagesFolder_Click;
    }

    private async void BrowsePackagesFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.BrowsePackagesFolderAsync(this);
        }
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
