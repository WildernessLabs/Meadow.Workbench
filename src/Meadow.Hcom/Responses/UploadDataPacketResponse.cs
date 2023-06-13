namespace Meadow.Hcom;

internal class UploadDataPacketResponse : Response
{
    internal UploadDataPacketResponse(byte[] data, int length)
        : base(data, length)
    {
    }

    public byte[] FileData => _data[RESPONSE_PAYLOAD_OFFSET..];
}
