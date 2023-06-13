using LanguageExt;
using LanguageExt.Common;

namespace Meadow.Hcom
{
    public interface IMeadowDevice
    {
        Task<Result<Unit>> Reset(CancellationToken? cancellationToken = null);
        Task<Result<Unit>> RuntimeDisable(CancellationToken? cancellationToken = null);
        Task<Result<Unit>> RuntimeEnable(CancellationToken? cancellationToken = null);
        Task<Result<bool>> IsRuntimeEnabled(CancellationToken? cancellationToken = null);

        Task<Dictionary<string, string>?> GetDeviceInfo(CancellationToken? cancellationToken = null);
        Task<MeadowFileInfo[]?> GetFileList(bool includeCrcs, CancellationToken? cancellationToken = null);
        Task<bool> ReadFile(string meadowFileName, string? localFileName = null, CancellationToken? cancellationToken = null);
    }
}