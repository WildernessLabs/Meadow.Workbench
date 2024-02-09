using Meadow.Cloud.Client;
using Meadow.Cloud.Client.Identity;
using Meadow.Software;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;


internal class FirmwareService
{
    private const string Meadow_F7 = "Meadow F7";

    private MeadowCloudClient _cloudClient;

    private readonly FileManager _manager;

    public IFirmwarePackageCollection? CurrentStore { get; private set; }

    public FirmwareService()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
            BaseAddress = new Uri("https://staging.meadowcloud.dev")
        };
        var identiyManager = new IdentityManager();

        _cloudClient = new MeadowCloudClient(
            client,
            identiyManager,
            MeadowCloudUserAgent.Workbench);

        _manager = new FileManager(_cloudClient);
    }

    public async Task SelectStore(string storeName = Meadow_F7)
    {
        await _manager.Refresh();
        if (CurrentStore == null)
        {
            CurrentStore = _manager.Firmware[storeName];
        }
    }

    public async Task RefreshCurrentStore()
    {
        await _manager.Refresh();
        if (CurrentStore == null)
        {
            CurrentStore = _manager.Firmware[Meadow_F7];
        }
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