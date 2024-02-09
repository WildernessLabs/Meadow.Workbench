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
        _manager = new FileManager(null); //ToDo
        _ = RefreshCurrentStore();
    }

    private async Task RefreshCurrentStore()
    {
        await _manager.Refresh();
        CurrentStore = _manager.Firmware[CurrentStoreName];
    }

}

/*

F7 feather update script (OS and runtime)

At time  Activity               LED State
00:00    Reset                  Off
00:01    Transfer OS image      Solid blue
00:40    Transfer runtime       Solid blue
01:40    Reset                  Off
01:41    Validating binaries    Off
02:10    Flashing OS            Solid blue/flashing green
02:45    Finished OS flashing   Solid green
02:50    Flashing runtime       Solid blue/flashing green
02:56    Reset
03:50    Reset                  Off
03:51    Flash complete         Blue                   
*/