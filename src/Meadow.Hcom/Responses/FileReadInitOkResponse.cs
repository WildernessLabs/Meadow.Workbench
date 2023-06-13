namespace Meadow.Hcom;

internal class FileReadInitOkResponse : Response
{
    internal FileReadInitOkResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}
