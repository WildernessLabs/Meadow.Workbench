namespace Meadow.Hcom
{
    public interface IConnectionListener
    {
        void OnInformationMessageReceived(string message);
        void OnDeviceInformationMessageReceived(Dictionary<string, string> deviceInfo);
        void OnTextListReceived(string[] list);
    }
}