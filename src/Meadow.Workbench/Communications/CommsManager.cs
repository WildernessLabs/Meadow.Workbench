using Microsoft.Extensions.Logging;

namespace Meadow.Cli
{
    public interface IMeadowDevice : IDisposable
    {
        public ILogger Logger { get; }
        public MeadowDeviceInfo? DeviceInfo { get; }
    }
}
