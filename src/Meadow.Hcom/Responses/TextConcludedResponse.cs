﻿using System.Text;

namespace Meadow.Hcom;

internal class TextCrcMemberResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextCrcMemberResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}

internal class TextListMemberResponse : Response
{
    public string Text => Encoding.UTF8.GetString(_data, RESPONSE_PAYLOAD_OFFSET, PayloadLength);

    internal TextListMemberResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}

internal class TextListHeaderResponse : Response
{
    internal TextListHeaderResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}

internal class TextConcludedResponse : Response
{
    internal TextConcludedResponse(byte[] data, int length)
        : base(data, length)
    {
    }
}
