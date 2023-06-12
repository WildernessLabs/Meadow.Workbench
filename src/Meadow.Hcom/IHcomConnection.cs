namespace Meadow.Hcom
{
    public interface IHcomConnection
    {
        string Name { get; }
        IMeadowDevice? Device { get; }
        Task<bool> TryAttach(int timeoutSeconds, CancellationToken? cancellationToken = null);

        // internal stuff that probably needs to get moved to anotehr interface
        void AddListener(IConnectionListener listener);
        void RemoveListener(IConnectionListener listener);
        internal void SendRequest(Request command);
    }
}