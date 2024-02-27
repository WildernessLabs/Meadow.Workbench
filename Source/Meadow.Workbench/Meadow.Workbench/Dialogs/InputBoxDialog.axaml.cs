using Avalonia.Controls;
using DialogHostAvalonia;

namespace Meadow.Workbench;

public partial class InputBoxDialog : UserControl
{
    public InputBoxDialog()
    {
        InitializeComponent();
    }

    public InputBoxDialog(string title, string text, string watermark)
        : this()
    {
        this.title.Content = title;
        this.text.Text = text;
        this.text.Watermark = watermark;

        this.cancel.Click += Cancel_Click;
        this.save.Click += Save_Click;

        this.text.Focus();
    }

    public bool IsCancelled { get; private set; }
    public string? Text { get; set; }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Text = text.Text;
        IsCancelled = false;
        DialogHost.Close(null);
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Text = null;
        IsCancelled = true;
        DialogHost.Close(null);
    }
}