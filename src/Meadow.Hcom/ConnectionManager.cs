namespace Meadow.Hcom
{
    public class ConnectionManager
    {
        private static List<IHcomConnection> _connections = new List<IHcomConnection>();

        public static TConnection GetConnection<TConnection>(string connectionName)
            where TConnection : class, IHcomConnection
        {
            // see if it already is known
            var existing = _connections.FirstOrDefault(c => c.Name == connectionName) as TConnection;
            if (existing != null) return existing;

            // otherwise create
            switch (typeof(TConnection))
            {
                case Type t when t == typeof(SerialConnection):
                    return new SerialConnection(connectionName) as TConnection;
                default:
                    throw new NotImplementedException();
            };

        }
    }
}