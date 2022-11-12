using Meadow.Workbench.ViewModels;

namespace Meadow.Workbench.Views;

public partial class DeviceInfoPage : ContentPage
{
	public DeviceInfoPage(DeviceInfoViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
	}
}