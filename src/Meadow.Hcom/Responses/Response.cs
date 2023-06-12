﻿namespace Meadow.Hcom;

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
            case ResponseType.HCOM_HOST_REQUEST_TEXT_MONO_STDERR:
                return new TextStdErrResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_MONO_STDOUT:
                return new TextStdOutResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_INFORMATION:
                return new TextInformationResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_ACCEPTED:
                return new TextRequestResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_DEVICE_INFO:
                return new DeviceInfoResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_CONCLUDED:
                return new TextConcludedResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_LIST_HEADER:
                return new TextListHeaderResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_LIST_MEMBER:
                return new TextListMemberResponse(data, length);
            case ResponseType.HCOM_HOST_REQUEST_TEXT_CRC_MEMBER:
                return new TextCrcMemberResponse(data, length);
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
