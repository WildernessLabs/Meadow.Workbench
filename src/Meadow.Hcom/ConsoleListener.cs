namespace Meadow.Hcom
{
    public class ConsoleListener : IConnectionListener
    {
        public void OnInformationMessageReceived(string message)
        {
            Console.WriteLine(message);
        }

        public void OnDeviceInformationMessageReceived(Dictionary<string, string> deviceInfo)
        {
            Console.WriteLine($"Device Info{Environment.NewLine}-----------");
            foreach (var i in deviceInfo)
            {
                Console.WriteLine($" {i.Key}: {i.Value}");
            }
        }

        public void OnTextListReceived(string[] list)
        {
        }
    }
}