using ReactiveUI;

namespace Meadow.Workbench.ViewModels;

public class RepoViewModel : ViewModelBase
{
    private string _name;
    private string? _currentBranch;
    private string _status;
    private bool _isBehind;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string? CurrentBranch
    {
        get => _currentBranch;
        set => this.RaiseAndSetIfChanged(ref _currentBranch, value);
    }

    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public bool IsBehind
    {
        get => _isBehind;
        set => this.RaiseAndSetIfChanged(ref _isBehind, value);
    }
}
