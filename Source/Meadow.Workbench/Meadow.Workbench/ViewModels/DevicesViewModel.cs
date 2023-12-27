using DialogHostAvalonia;
using ReactiveUI;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class DevicesViewModel : FeatureViewModel
{
    public IReactiveCommand AddDeviceCommand { get; }

    public DevicesViewModel()
    {
        AddDeviceCommand = ReactiveCommand.CreateFromTask(OnAddDevice);
    }

    private async Task OnAddDevice()
    {
        var result = await DialogHost.Show(new AddDeviceDialog());
    }
}
