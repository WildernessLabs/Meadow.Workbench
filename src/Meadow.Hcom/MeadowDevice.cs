namespace Meadow.Hcom
{
    public class MeadowDevice : IMeadowDevice
    {
        private IHcomConnection _connection;

        internal MeadowDevice(IHcomConnection connection)
        {
            _connection = connection;
        }

        public async Task Reset()
        {
        }
    }
}