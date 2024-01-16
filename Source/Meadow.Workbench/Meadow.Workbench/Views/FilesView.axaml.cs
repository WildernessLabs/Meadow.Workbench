using Avalonia.Controls;
using Avalonia.Input;
using Meadow.Workbench.ViewModels;
using System.Diagnostics;

namespace Meadow.Workbench;

public partial class FilesView : UserControl
{
    public FilesView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private async void LocalFileListPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var dragData = new DataObject();
        dragData.Set("localFile", sender);
        var result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
        switch (result)
        {
            case DragDropEffects.Copy:
                Debug.WriteLine("Copy");
                break;
            case DragDropEffects.Link:
                Debug.WriteLine("Link");
                break;
            case DragDropEffects.None:
                Debug.WriteLine("Cancel");
                break;
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("Drag over");
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("Drop");
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
                    fvm.RemoteDirectory = $"{fvm.RemoteDirectory}{mef.Name}/";
                    fvm.RefreshRemoteSource();
                }
            }
        }
    }
}