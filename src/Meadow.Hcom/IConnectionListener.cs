namespace Meadow.Hcom
{
    public interface IConnectionListener
    {
        void OnInformationMessageReceived(string message);
    }
}