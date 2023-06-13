namespace Meadow.Hcom;

internal class UploadCompletedResponse : Response
{
    internal UploadCompletedResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}
