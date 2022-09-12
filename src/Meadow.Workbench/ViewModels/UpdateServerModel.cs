using DynamicData;
using Meadow.Foundation.Web.Maple;
using Meadow.Update;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Input;

namespace Meadow.Workbench.ViewModels;

public class UpdateServerModel : ViewModelBase
{
    private MapleServer _maple;
    private UpdateServer _updateServer;
    private UpdatePublisher _publisher;
    private ObservableCollection<string> _availableUpdates = new();
    private string? _selectedUpdate;

    public UpdateServerModel()
    {
        _updateServer = new UpdateServer();
        _publisher = new UpdatePublisher();
        _maple = new MapleServer(IPAddress.Any, 5000);

        RefreshAvailableUpdatesCommand.Execute(null);
    }

    public ICommand StartServerCommand
    {
        get => new Command(async () =>
        {
            await _updateServer.Start();
            _maple.Start();
        });
    }

    public ICommand StopServerCommand
    {
        get => new Command(async () =>
        {
            await _updateServer.Stop();
        });
    }

    public ICommand PublishUpdateCommand
    {
        get => new Command(async () =>
        {
            await _publisher.MakeUpdateAvailable();
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

    public ObservableCollection<string> AvailableUpdates
    {
        get => _availableUpdates;
        set => this.RaiseAndSetIfChanged(ref _availableUpdates, value);
    }

    public string UpdateBinaryFolder
    {
        // TODO: remember this across app runs
        get => _publisher.SourceFolder;
        set
        {
            // TODO: verify this is really a folder (folder picker, maybe?)
            _publisher.SourceFolder = value;
            this.RaisePropertyChanged();
        }
    }

    public string? SelectedUpdate
    {
        get => _selectedUpdate;
        set => this.RaiseAndSetIfChanged(ref _selectedUpdate, value);
    }
}
