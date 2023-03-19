namespace Meadow.Hcom
{
    internal static class CommandBuilder
    {
        private static uint _sequenceNumber;

        public static Request Build<T>(uint userData = 0, ushort extraData = 0, ushort protocol = Protocol.HCOM_PROTOCOL_HCOM_VERSION_NUMBER)
            where T : Request, new()
        {
            var sequence = Interlocked.Increment(ref _sequenceNumber);
            if (sequence > ushort.MaxValue)
            {
                sequence = _sequenceNumber = 0;
            }

            return new T
            {
                SequenceNumber = (ushort)sequence,
                ProtocolVersion = protocol,
                UserData = userData,
                ExtraData = extraData
            };
        }
    }
}