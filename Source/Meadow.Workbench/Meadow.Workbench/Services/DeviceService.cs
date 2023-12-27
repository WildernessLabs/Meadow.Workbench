using Meadow.Hcom;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class DeviceInformation
{
}

internal class DeviceService
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private Dictionary<string, IMeadowConnection> _connections = new();

    public async Task<IMeadowConnection?> AddConnection(string route)
    {
        if (_semaphore.Wait(5000))
        {
            if (_connections.ContainsKey(route))
            {
                return _connections[route];
            }

            // determine connection type
            var c = new SerialConnection(route);
            _connections.Add(route, c);

            await c.GetDeviceInfo();

            _semaphore.Release();

            return c;
        }

        return null;
    }

    public IEnumerable<IMeadowConnection> Connections
    {
        get => _connections.Values;
    }
}
