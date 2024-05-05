using LibGit2Sharp;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;

namespace Meadow.Workbench.ViewModels;

public class RepoViewModel : ViewModelBase
{
    private DirectoryInfo _localFolder;
    private Repository? _repo;

    private string _name;
    private string? _currentBranch;
    private RepositoryStatus? _status;
    private bool _isBehind;
    private bool _hasRemote;
    private int _behindBy;

    public IReactiveCommand PullCommand { get; }
    public IReactiveCommand CloneCommand { get; }

    public RepoViewModel(DirectoryInfo localFolder)
    {
        PullCommand = ReactiveCommand.Create(Pull);
        CloneCommand = ReactiveCommand.Create(Clone);

        _localFolder = localFolder;

        if (_localFolder.Exists)
        {
            _repo = new Repository(localFolder.FullName);
        }

        Name = localFolder.Name;

        Refresh();
    }

    private void Pull()
    {
        Commands.Pull(
            _repo,
            new Signature("meadow", "foo@noname.com", DateTimeOffset.Now),
            new PullOptions()
            );
        Refresh();
    }

    private void Clone()
    {
        var options = new CloneOptions
        {
            Checkout = true,
            BranchName = "develop"
        };
        var url = $"https://github.com/WildernessLabs/{_localFolder.Name}.git";

        var path = Repository.Clone(url, _localFolder.FullName, options);
        _repo = new Repository(path);
        Refresh();
    }

    private void Refresh()
    {
        CurrentBranch = _repo?.Head.FriendlyName;
        BehindBy = _repo?.Head.TrackingDetails?.BehindBy ?? 0;
        IsBehind = BehindBy > 0;
        HasRemote = _repo?.Head?.IsTracking ?? false;
        this.RaisePropertyChanged(nameof(ExistsLocally));

        Status = _repo?.RetrieveStatus();
    }

    public RepositoryStatus? Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public string StatusString
    {
        get
        {
            if (!ExistsLocally) return "missing";

            if (Status == null) return string.Empty;
            if (Status.IsDirty)
            {
                return $"+{Status.Added.Count()} ~{Status.Modified.Count()} -{Status.Missing.Count()}";
            }

            if (IsBehind)
            {
                return "stale";
            }

            return string.Empty;
        }
    }

    public bool ExistsLocally
    {
        get => _repo != null;
    }

    public int BehindBy
    {
        get => _behindBy;
        set => this.RaiseAndSetIfChanged(ref _behindBy, value);
    }

    public bool HasRemote
    {
        get => _hasRemote;
        set => this.RaiseAndSetIfChanged(ref _hasRemote, value);
    }

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

    public bool IsBehind
    {
        get => _isBehind;
        set => this.RaiseAndSetIfChanged(ref _isBehind, value);
    }
}
