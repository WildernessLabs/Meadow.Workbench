using Meadow.Hcom;
using System.Diagnostics;

namespace Meadow.HCom.Integration.Tests
{
    public class ConnectionManagerTests
    {
        public string ValidPortName { get; } = "COM9";

        [Fact]
        public async Task TestInvalidPortName()
        {
            var c = ConnectionManager.GetConnection<SerialConnection>(ValidPortName);
            var deviceExists = await c.TryAttach();
            Debug.WriteLine($"Device exists? {deviceExists}");

            await c.Device.Reset();
            var info = await c.Device?.GetDeviceInfo();
            //var files = await c.Device?.GetFileList(false);
            //var files2 = await c.Device?.GetFileList(true);
            //var result = await c.Device?.ReadFile("app.config.yaml", "c:\\temp\\app.comfig.yml");
        }
    }
}