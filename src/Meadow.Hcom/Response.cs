using System.Text;

namespace Meadow.Hcom;

internal class TextInformationResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextInformationResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}

internal class Response
{
    private const int HCOM_PROTOCOL_REQUEST_HEADER_SEQ_OFFSET = 0;
    private const int HCOM_PROTOCOL_REQUEST_HEADER_VERSION_OFFSET = 2;
    private const int HCOM_PROTOCOL_REQUEST_HEADER_RQST_TYPE_OFFSET = 4;
    private const int HCOM_PROTOCOL_REQUEST_HEADER_EXTRA_DATA_OFFSET = 6;
    private const int HCOM_PROTOCOL_REQUEST_HEADER_USER_DATA_OFFSET = 8;
    protected const int RESPONSE_PAYLOAD_OFFSET = 12;

    protected byte[] _data;

    public ushort SequenceNumber => BitConverter.ToUInt16(_data, HCOM_PROTOCOL_REQUEST_HEADER_SEQ_OFFSET);
    public ushort ProtocolVersion => BitConverter.ToUInt16(_data, HCOM_PROTOCOL_REQUEST_HEADER_VERSION_OFFSET);
    public ResponseType RequestType => (ResponseType)BitConverter.ToUInt16(_data, HCOM_PROTOCOL_REQUEST_HEADER_RQST_TYPE_OFFSET);
    public ushort ExtraData => BitConverter.ToUInt16(_data, HCOM_PROTOCOL_REQUEST_HEADER_EXTRA_DATA_OFFSET);
    public uint UserData => BitConverter.ToUInt32(_data, HCOM_PROTOCOL_REQUEST_HEADER_USER_DATA_OFFSET);
    protected int PayloadLength => _data.Length - RESPONSE_PAYLOAD_OFFSET;

    public static Response Parse(byte[] data, int length)
    {
        var type = (ResponseType)BitConverter.ToUInt16(data, HCOM_PROTOCOL_REQUEST_HEADER_RQST_TYPE_OFFSET);

        switch (type)
        {
            case ResponseType.HCOM_HOST_REQUEST_TEXT_INFORMATION:
                return new TextInformationResponse(data, length);
            default:
                return new Response(data, length);
        }
    }

    protected Response(byte[] data, int length)
    {
        _data = new byte[length];
        Array.Copy(data, 0, _data, 0, length);
    }
}
