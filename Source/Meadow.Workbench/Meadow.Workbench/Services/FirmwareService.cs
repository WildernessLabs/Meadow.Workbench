using Meadow.Software;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class FirmwareService
{
    private const string CurrentStoreName = "Meadow F7";

    private readonly FileManager _manager;

    public IFirmwarePackageCollection CurrentStore { get; private set; }

    public FirmwareService()
    {
        _manager = new FileManager();
        _ = RefreshCurrentStore();
    }

    private async Task RefreshCurrentStore()
    {
        await _manager.Refresh();
        CurrentStore = _manager.Firmware[CurrentStoreName];
    }

}
