namespace Meadow.Hcom
{
    public interface IHcomConnection
    {
        event EventHandler<string> FileReadCompleted;
        event EventHandler<Exception> FileException;

        bool IsConnected { get; }
        string Name { get; }
        IMeadowDevice? Device { get; }
        Task<IMeadowDevice?> Attach(int timeoutSeconds = 30, CancellationToken? cancellationToken = null);
        Task WaitForMeadowAttach(CancellationToken? cancellationToken = null);

        // internal stuff that probably needs to get moved to anotehr interface
        void AddListener(IConnectionListener listener);
        void RemoveListener(IConnectionListener listener);
        internal void SendRequest(Request command);
    }
}