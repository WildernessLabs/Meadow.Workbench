namespace Meadow.Hcom;

internal class FileReadInitFailedResponse : Response
{
    internal FileReadInitFailedResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}
