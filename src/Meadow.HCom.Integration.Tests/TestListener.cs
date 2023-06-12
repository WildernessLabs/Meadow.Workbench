using Meadow.Hcom;

namespace Meadow.HCom.Integration.Tests
{
    public class TestListener : IConnectionListener
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
}