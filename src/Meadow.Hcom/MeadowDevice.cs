namespace Meadow.Hcom
{
    internal class ResponseListener : IConnectionListener
    {
        public List<string> Messages { get; } = new List<string>();
        public Dictionary<string, string> DeviceInfo { get; private set; } = new Dictionary<string, string>();
        public List<string> TextList { get; } = new List<string>();

        public void OnInformationMessageReceived(string message)
        {
            Messages.Add(message);
        }

        public void OnDeviceInformationMessageReceived(Dictionary<string, string> deviceInfo)
        {
            DeviceInfo = deviceInfo;
        }

        public void OnTextListReceived(string[] list)
        {
            TextList.Clear();
            TextList.AddRange(list);
        }
    }

    public class MeadowDevice : IMeadowDevice
    {
        private IHcomConnection _connection;
        private ResponseListener _listener;

        public int CommandTimeoutSeconds { get; set; } = 30;

        internal MeadowDevice(IHcomConnection connection)
        {
            _connection = connection;
            _connection.AddListener(_listener = new ResponseListener());
        }

        public async Task Reset()
        {
            var command = CommandBuilder.Build<ResetDeviceRequest>();

            _connection.SendRequest(command);

            // TODO: find a way to determine reset complete - specific text output?  Any text output?  TEst mono enabled, disabled, no OS, etc.
        }

        private async Task<bool> CheckForResult(Func<bool> checkAction, CancellationToken? cancellationToken)
        {
            var timeout = CommandTimeoutSeconds * 2;

            while (timeout-- > 0)
            {
                if (cancellationToken?.IsCancellationRequested ?? false) return false;
                if (timeout <= 0) throw new TimeoutException();

                if (checkAction())
                {
                    break;
                }

                await Task.Delay(500);
            }

            return true;
        }

        public async Task<Dictionary<string, string>?> GetDeviceInfo(CancellationToken? cancellationToken = null)
        {
            var command = CommandBuilder.Build<GetDeviceInfoRequest>();

            _listener.DeviceInfo.Clear();

            _connection.SendRequest(command);

            if (!await CheckForResult(
                () => _listener.DeviceInfo.Count > 0,
                cancellationToken))
            {
                return null;
            }

            return _listener.DeviceInfo;
        }

        public async Task<MeadowFileInfo[]?> GetFileList(bool includeCrcs, CancellationToken? cancellationToken = null)
        {
            var command = CommandBuilder.Build<GetFileListRequest>();
            command.IncludeCrcs = includeCrcs;

            _listener.DeviceInfo.Clear();

            _connection.SendRequest(command);

            if (!await CheckForResult(
                () => _listener.TextList.Count > 0,
                cancellationToken))
            {
                return null;
            }

            var list = new List<MeadowFileInfo>();

            foreach (var candidate in _listener.TextList)
            {
                // TODO: should this be part of the connection?  A serial response might be different than a future response (TCP or whatever)
                var fi = MeadowFileInfo.Parse(candidate);
                if (fi != null)
                {
                    list.Add(fi);
                }
            }

            return list.ToArray();

        }
    }
}