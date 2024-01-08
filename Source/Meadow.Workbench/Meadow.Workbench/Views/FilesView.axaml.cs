using Avalonia.Controls;
using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench;

public partial class FilesView : UserControl
{
    public FilesView()
    {
        InitializeComponent();
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