namespace Meadow.Workbench.Controls;

public partial class FirmwareView : ContentView
{
	public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(FirmwareView), propertyChanged: (bindable, oldval, newval) =>
	{
		var control = (FirmwareView)bindable;
	});

	public FirmwareView()
	{
		InitializeComponent();
	}

	public string Title
	{
		get => GetValue(TitleProperty) as string;
		set => SetValue(TitleProperty, value);
	}
}