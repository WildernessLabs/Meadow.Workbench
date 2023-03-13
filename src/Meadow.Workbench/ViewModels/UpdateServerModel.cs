using DynamicData;
using Meadow.Update;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace Meadow.Workbench.ViewModels;

public class UpdateServerModel : ViewModelBase
{
    private UpdateServer _updateServer;
    private UpdatePublisher _publisher;
    private ObservableCollection<string> _availableUpdates = new();
    private string? _selectedUpdate;

    public UpdateServerModel()
    {
        _updateServer = new UpdateServer();
        _publisher = new UpdatePublisher();

        _updateServer.StateChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(ServerIsRunning));
        };

        RefreshAvailableUpdatesCommand.Execute(null);
    }

    public ICommand StartServerCommand
    {
        get => new Command(async () =>
        {
            await _updateServer.Start();
            this.RaisePropertyChanged(nameof(ServerIsRunning));
        });
    }

    public ICommand StopServerCommand
    {
        get => new Command(async () =>
        {
            await _updateServer.Stop();
            this.RaisePropertyChanged(nameof(ServerIsRunning));
        });
    }

    public ICommand PublishUpdateCommand
    {
        get => new Command(async () =>
        {
            await _publisher.MakeUpdateAvailable(SelectedUpdate);
        });
    }

    public ICommand RefreshAvailableUpdatesCommand
    {
        get => new Command(() =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var updates = _publisher.GetAvailableUpdates();

                var newItems = updates.Except(AvailableUpdates).ToArray();
                var removedItems = AvailableUpdates.Except(updates).ToArray();

                try
                {
                    AvailableUpdates.AddRange(newItems);
                    AvailableUpdates.RemoveMany(removedItems);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        });
    }

    public bool ServerIsRunning
    {
        get => _updateServer.IsRunning;
    }

    public ObservableCollection<string> AvailableUpdates
    {
        get => _availableUpdates;
        set => this.RaiseAndSetIfChanged(ref _availableUpdates, value);
    }

    public string UpdateBinaryFolder
    {
        get => _publisher.SourceFolder;
    }

    public string? SelectedUpdate
    {
        get => _selectedUpdate;
        set => this.RaiseAndSetIfChanged(ref _selectedUpdate, value);
    }
}
