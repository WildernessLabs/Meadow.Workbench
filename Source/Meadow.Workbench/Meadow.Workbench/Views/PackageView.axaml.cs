using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Meadow.Workbench.ViewModels;
using System.IO;
using System.Linq;

namespace Meadow.Workbench;

public partial class PackageView : UserControl
{
    public PackageView()
    {
        InitializeComponent();
        
        // Wire up drag and drop event handlers
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Only allow file drops
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is PackageViewModel viewModel && e.Data.GetFileNames() is { } fileNames)
        {
            foreach (var fileName in fileNames)
            {
                if (File.Exists(fileName))
                {
                    // For now, add files to the "app" folder
                    // In a more sophisticated version, this could be determined by drop target
                    viewModel.AddFileToPackage(fileName, "app");
                }
            }
        }
    }
}