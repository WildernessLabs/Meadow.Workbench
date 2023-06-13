using Meadow.Hcom;
using System.Diagnostics;

namespace Meadow.HCom.Integration.Tests
{
    public class ConnectionManagerTests
    {
        public string ValidPortName { get; } = "COM3";

        [Fact]
        public async Task TestInvalidPortName()
        {
            var c = ConnectionManager.GetConnection<SerialConnection>(ValidPortName);
            var deviceExists = await c.TryAttach();

            if (deviceExists)
            {
                Debug.WriteLine($"Device exists? {deviceExists}");

                //                var result = await c.Device.Reset();
                //                var didReset = result.IsSuccess;

                //                var en = await c.Device.RuntimeEnable();
                //                var didEnable = en.IsSuccess;

                var t = await c.Device.IsRuntimeEnabled();
                Debug.WriteLine(
                    t.Match(
                        en => $"Runtime is {(en ? "enabled" : "disabled")}",
                        err => $"failed to connect: {err.Message}"
                    )
                    );

                var info = await c.Device?.GetDeviceInfo();
            }

            //var files = await c.Device?.GetFileList(false);
            //var files2 = await c.Device?.GetFileList(true);
            //var result = await c.Device?.ReadFile("app.config.yaml", "c:\\temp\\app.comfig.yml");
        }

        [Fact]
        public async Task TestRuntimeEnableAndDisable()
        {
            var c = ConnectionManager.GetConnection<SerialConnection>(ValidPortName);
            var deviceExists = await c.TryAttach();

            if (deviceExists)
            {
                // get the current runtime state
                var state = await c.Device?.IsRuntimeEnabled();

                var start = state.Match(
                    en => en,
                    err =>
                    {
                        Assert.Fail($"Unable to query state: {err}");
                        throw new Exception();
                    });

                if (start)
                {
                    Debug.WriteLine("*** Runtime started enabled.");
                    Debug.WriteLine("*** Disabling...");
                    Assert.True((await c.Device.RuntimeDisable()).IsSuccess);
                    Debug.WriteLine("*** Enabling...");
                    Assert.True((await c.Device.RuntimeEnable()).IsSuccess);
                }
                else
                {
                    Debug.WriteLine("*** Runtime started disabled.");
                    Debug.WriteLine("*** Enabling...");
                    Assert.True((await c.Device.RuntimeEnable()).IsSuccess);
                    Debug.WriteLine("*** Disabling...");
                    Assert.True((await c.Device.RuntimeDisable()).IsSuccess);
                }

            }
        }
    }
}