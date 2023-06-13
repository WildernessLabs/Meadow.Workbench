namespace Meadow.Hcom
{
    public interface IMeadowDevice
    {
        Task Reset(CancellationToken? cancellationToken = null);
        Task RuntimeDisable(CancellationToken? cancellationToken = null);
        Task RuntimeEnable(CancellationToken? cancellationToken = null);
        Task<Dictionary<string, string>?> GetDeviceInfo(CancellationToken? cancellationToken = null);
        Task<MeadowFileInfo[]?> GetFileList(bool includeCrcs, CancellationToken? cancellationToken = null);
        Task<bool> ReadFile(string meadowFileName, string? localFileName = null, CancellationToken? cancellationToken = null);
    }
}