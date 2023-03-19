using Meadow.Hcom;

namespace Meadow.HCom.Integration.Tests
{
    public class SerialConnectionTests
    {
        public string ValidPortName { get; } = "COM3";

        [Fact]
        public void TestInvalidPortName()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var connection = new SerialConnection("COMxx");
            });
        }

        [Fact]
        public async void TestListen()
        {
            using (var connection = new SerialConnection(ValidPortName))
            {
                Assert.Equal(ConnectionState.Disconnected, connection.State);
                var connected = await connection.TryAttach(TimeSpan.FromSeconds(2));

                while (true)
                {
                    await Task.Delay(1000);
                }
            }
        }

        [Fact]
        public async void TestAttachPositive()
        {
            using (var connection = new SerialConnection(ValidPortName))
            {
                Assert.Equal(ConnectionState.Disconnected, connection.State);
                var connected = await connection.TryAttach(TimeSpan.FromSeconds(2));
                Assert.Equal(ConnectionState.Connected, connection.State);

                while (true)
                {
                    await Task.Delay(1000);
                }
            }
        }
    }
}