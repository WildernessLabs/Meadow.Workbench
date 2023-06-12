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
            await Task.Delay(1000);
            var info = await c.Device?.GetDeviceInfo();
        }
    }
}