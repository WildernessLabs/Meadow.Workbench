namespace Meadow.Hcom
{
    public class ConsoleListener : IConnectionListener
    {
        public void OnInformationMessageReceived(string message)
        {
            Console.WriteLine(message);
        }
    }
}