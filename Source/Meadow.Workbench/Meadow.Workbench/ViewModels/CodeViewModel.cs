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
    private bool _isLoading;

    public ObservableCollection<RepoViewModel> MeadowRepos { get; } = new();

    public IReactiveCommand SelectRootFolderCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

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
            _ = LoadReposAsync(_meadowRootFolder);
        }
    }

    public override async void OnActivated()
    {
        // Refresh repos when tab is activated
        if (MeadowRepos.Count > 0 && !IsLoading)
        {
            await RefreshAllReposAsync();
        }
    }

    public string? RepoRootFolder
    {
        get => _meadowRootFolder;
        set => this.RaiseAndSetIfChanged(ref _meadowRootFolder, value);
    }

    private async Task LoadReposAsync(string folder)
    {
        IsLoading = true;
        try
        {
            await Task.Run(async () =>
            {
                // Create RepoViewModels on background thread
                var repoVMs = new System.Collections.Generic.List<RepoViewModel>();

                foreach (var repo in RequiredRepos)
                {
                    var repoFolder = new DirectoryInfo(Path.Combine(folder, repo));
                    var vm = new RepoViewModel(repoFolder);
                    repoVMs.Add(vm);
                }

                var others = Directory.GetDirectories(folder).Except(RequiredRepos);

                foreach (var o in others)
                {
                    try
                    {
                        var repoFolder = new DirectoryInfo(Path.Combine(folder, o));
                        var vm = new RepoViewModel(repoFolder);
                        repoVMs.Add(vm);
                    }
                    catch
                    {
                        // ignore
                        Debug.WriteLine($"{o} doesn't seem to be a valid repo");
                    }
                }

                // Add to ObservableCollection on UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MeadowRepos.Clear();
                    foreach (var vm in repoVMs)
                    {
                        MeadowRepos.Add(vm);
                    }
                });

                // Refresh all repos in parallel
                var refreshTasks = repoVMs.Select(vm => vm.RefreshAsync()).ToArray();
                await Task.WhenAll(refreshTasks);
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAllReposAsync()
    {
        IsLoading = true;
        try
        {
            var refreshTasks = MeadowRepos.Select(vm => vm.RefreshAsync()).ToArray();
            await Task.WhenAll(refreshTasks);
        }
        finally
        {
            IsLoading = false;
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
                _ = LoadReposAsync(RepoRootFolder);
            }
        }
    }
}
