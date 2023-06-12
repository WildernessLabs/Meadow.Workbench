public class MeadowFileInfo
{
    public string Name { get; private set; } = default!;
    public long? Size { get; private set; }
    public string? Crc { get; private set; }

    public static MeadowFileInfo? Parse(string info)
    {
        throw new NotImplementedException();
    }
}