using System.Text;

namespace Meadow.Hcom;

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

/// <summary>
/// A text response to a solicited host request
/// </summary>
internal class TextRequestResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextRequestResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}

internal class DeviceInfoResponse : Response
{
    public Dictionary<string, string> Fields { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

    public string RawText => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal DeviceInfoResponse(byte[] data, int length)
        : base(data, length)
    {
        var rawFields = RawText.Split('~', StringSplitOptions.RemoveEmptyEntries);
        foreach (var f in rawFields)
        {
            var pair = f.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if ((pair.Length == 2) && !Fields.ContainsKey(pair[0]))
            {
                Fields.Add(pair[0], pair[1]);
            }
        }
    }
}
