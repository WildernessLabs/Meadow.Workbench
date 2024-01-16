using Meadow.Software;
using ReactiveUI;

namespace Meadow.Workbench.ViewModels;

public class FirmwarePackageViewModel : ViewModelBase
{
    private FirmwarePackage _package;
    private bool _isDefault;

    public FirmwarePackageViewModel(FirmwarePackage package, bool isDefault)
    {
        _package = package;
        IsDefault = isDefault;
    }

    public string Version => _package.Version;
    public bool HasOsFiles => !string.IsNullOrEmpty(_package.OSWithBootloader);
    public bool HasRuntimeFiles => !string.IsNullOrEmpty(_package.Runtime);
    public bool HasBclFiles => !string.IsNullOrEmpty(_package.BclFolder);
    public bool HasCoprocessorFiles => !string.IsNullOrEmpty(_package.CoprocApplication);

    public bool IsDefault
    {
        get => _isDefault;
        set => this.RaiseAndSetIfChanged(ref _isDefault, value);
    }
}
