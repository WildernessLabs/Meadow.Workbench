using System.Text;

namespace Meadow.Hcom;

internal class TextStdErrResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextStdErrResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}

internal class TextStdOutResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextStdOutResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}

/// <summary>
/// An unsolicited text response sent by HCOM (i.e. typically a Console.Write)
/// </summary>
internal class TextInformationResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextInformationResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}
