using ReactiveUI;
using System.Collections.ObjectModel;

namespace Meadow.Workbench.ViewModels;

public class RepoMeta : ReactiveObject
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

public class CodeViewModel : FeatureViewModel
{
    public ObservableCollection<RepoMeta> MeadowRepos { get; } = new();

    public CodeViewModel()
    {
        MeadowRepos.Add(new RepoMeta
        {
            Name = "Meadow.Contracts",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = true
        });
        MeadowRepos.Add(new RepoMeta
        {
            Name = "Meadow.Logging",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = true
        });
        MeadowRepos.Add(new RepoMeta
        {
            Name = "Meadow.Core",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = false
        });
        MeadowRepos.Add(new RepoMeta
        {
            Name = "Meadow.Foundation",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = false
        });
    }
}
