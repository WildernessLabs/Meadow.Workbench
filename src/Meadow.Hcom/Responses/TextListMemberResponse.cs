using System.Text;

namespace Meadow.Hcom;

internal class TextListMemberResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextListMemberResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}
