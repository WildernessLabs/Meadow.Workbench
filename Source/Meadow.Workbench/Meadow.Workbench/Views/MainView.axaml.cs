using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Meadow.Workbench.ViewModels;
using System.Threading;

namespace Meadow.Workbench.Views;

public partial class MainView : UserControl
{
    private FlyoutBase? _flyout;
    private Timer? _flyoutTimer;
    private MainViewModel? _vm;

    private MainViewModel? ViewModel => _vm ??= this.DataContext as MainViewModel;

    public MainView()
    {
        InitializeComponent();

        userButton.PointerEntered += UserButton_PointerEntered;
        userButton.PointerExited += UserButton_PointerExited;
    }

    private void UserButton_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!ViewModel?.IsAuthenticated ?? false) return;

        if (_flyout != null)
        {
            _flyoutTimer?.Change(2000, -1);
            return;
        }

        userFlyoutContent.Text = ViewModel?.UserName;

        var ctl = sender as Control;
        if (ctl != null)
        {
            _flyout = FlyoutBase.GetAttachedFlyout(ctl);
            _flyoutTimer = new Timer((o) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _flyout?.Hide();
                    _flyout = null;
                });
            },
            null,
            2000,
            -1);

            FlyoutBase.ShowAttachedFlyout(ctl);
        }
    }

    private void UserButton_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var ctl = sender as Control;
        if (ctl != null)
        {
            // FlyoutBase.GetAttachedFlyout(ctl)?.Hide();
        }
    }
}
