using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class CodeViewModel : FeatureViewModel
{
    public ObservableCollection<RepoViewModel> MeadowRepos { get; } = new();

    public IReactiveCommand SelectRootFolderCommand { get; }

    public CodeViewModel()
    {
        MeadowRepos.Add(new RepoViewModel
        {
            Name = "Meadow.Contracts",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = true
        });
        MeadowRepos.Add(new RepoViewModel
        {
            Name = "Meadow.Logging",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = true
        });
        MeadowRepos.Add(new RepoViewModel
        {
            Name = "Meadow.Core",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = false
        });
        MeadowRepos.Add(new RepoViewModel
        {
            Name = "Meadow.Foundation",
            CurrentBranch = "develop",
            Status = "Unknown",
            IsBehind = false
        });

        SelectRootFolderCommand = ReactiveCommand.CreateFromTask(OnSelectRootFolder);
    }

    private async Task OnSelectRootFolder()
    {
        var result = await TopLevel
            .GetTopLevel(this.FeatureView)
            !.StorageProvider
            .OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select Meadow Root"
            });
    }
}
