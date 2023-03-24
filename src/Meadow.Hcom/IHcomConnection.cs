namespace Meadow.Hcom
{
    public interface IHcomConnection
    {
        void AddListener(IConnectionListener listener);
        void RemoveListener(IConnectionListener listener);
    }
}