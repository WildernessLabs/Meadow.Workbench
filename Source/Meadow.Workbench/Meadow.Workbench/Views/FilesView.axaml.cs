using Avalonia.Controls;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench;

public partial class FilesView : UserControl
{
    public FilesView()
    {
        InitializeComponent();
    }

    private void LocalFolderDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is StackPanel s)
        {
            if (s.DataContext is MeadowFolderEntry mef)
            {
                if (this.DataContext is FilesViewModel fvm)
                {
                    var d = System.IO.Path.Combine(fvm.LocalDirectory, mef.Name);
                    fvm.UpdateLocalSource(d);
                }
            }
        }
    }

    private void RemoteFolderDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is StackPanel s)
        {
            if (s.DataContext is MeadowFolderEntry mef)
            {
                if (this.DataContext is FilesViewModel fvm)
                {
                    fvm.UpdateRemoteSource(
                        fvm.SelectedRemoteRoute,
                        $"{fvm.RemoteDirectory}{mef.Name}/");
                }
            }
        }
    }
}