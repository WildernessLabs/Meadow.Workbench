namespace Meadow.Hcom
{
    public interface IMeadowDevice
    {
        Task Reset();
        Task<Dictionary<string, string>?> GetDeviceInfo(CancellationToken? cancellationToken = null);
        Task<MeadowFileInfo[]?> GetFileList(bool includeCrcs, CancellationToken? cancellationToken = null);
        Task<bool> ReadFile(string meadowFileName, string? localFileName = null, CancellationToken? cancellationToken = null);
    }
}