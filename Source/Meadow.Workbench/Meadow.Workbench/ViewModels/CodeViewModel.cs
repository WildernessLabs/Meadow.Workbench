using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class RepoStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RepoViewModel repo)
        {
            if (!repo.ExistsLocally) return Brushes.Red;
            if (repo.IsBehind) return Brushes.Yellow;
            if (repo.Status?.IsDirty ?? false) return Brushes.Green;
        }
        return Brushes.AntiqueWhite;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CodeViewModel : FeatureViewModel
{
    private SettingsService? _settingsService;
    private string? _meadowRootFolder;

    public ObservableCollection<RepoViewModel> MeadowRepos { get; } = new();

    public IReactiveCommand SelectRootFolderCommand { get; }

    private string[] RequiredRepos =
        [
            "Meadow.Contracts",
            "Meadow.Core",
            "Meadow.Foundation",
            "Meadow.Logging",
            "Meadow.Modbus",
            "Meadow.Units",
            "MQTTnet",

            "Clima",
            "Cultivar",
            "Juego",
            "Meadow.ProjectLab",
            "Meadow.Samples",
        ];

    public CodeViewModel()
    {
        _settingsService = Locator.Current.GetService<SettingsService>();
        RepoRootFolder = _settingsService!.MeadowRepoRootFolder;

        SelectRootFolderCommand = ReactiveCommand.CreateFromTask(OnSelectRootFolder);

        if (Directory.Exists(RepoRootFolder))
        {
            LoadRepos(_meadowRootFolder);
        }
    }

    public string? RepoRootFolder
    {
        get => _meadowRootFolder;
        set => this.RaiseAndSetIfChanged(ref _meadowRootFolder, value);
    }

    private void LoadRepos(string folder)
    {
        foreach (var repo in RequiredRepos)
        {
            var repoFolder = new DirectoryInfo(Path.Combine(folder, repo));
            var vm = new RepoViewModel(repoFolder);
            MeadowRepos.Add(vm);
        }

        var others = Directory.GetDirectories(folder).Except(RequiredRepos);

        foreach (var o in others)
        {
            try
            {
                var repoFolder = new DirectoryInfo(Path.Combine(folder, o));
                var vm = new RepoViewModel(repoFolder);
                MeadowRepos.Add(vm);
            }
            catch
            {
                // ignore
                Debug.WriteLine($"{o} doesn't seem to be a valid repo");
            }
        }
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

        if (result != null)
        {
            RepoRootFolder = result[0].Path.AbsolutePath;

            if (Directory.Exists(RepoRootFolder))
            {
                if (_settingsService != null)
                {
                    _settingsService.MeadowRepoRootFolder = RepoRootFolder;
                }
                LoadRepos(RepoRootFolder);
            }
        }
    }
}
