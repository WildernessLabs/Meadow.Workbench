using DynamicData;
using Meadow.Update;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows.Input;

namespace Meadow.Workbench.ViewModels;

public class UpdateServerModel : ViewModelBase
{
    private UpdateServer _updateServer;
    private ContentServer _contentServer;
    private UpdatePublisher _publisher;
    private ObservableCollection<string> _availableUpdates = new();
    private string? _selectedUpdate;
    private string _serverAddress;
    private int _updatePort;
    private string[] _addressList;

    public UpdateServerModel()
    {
        _updateServer = new UpdateServer();
        _contentServer = new ContentServer();
        _publisher = new UpdatePublisher();

        UpdateServerPort = 1883;

        _updateServer.StateChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(ServerIsRunning));
        };

        AvailableAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(i => i.GetIPProperties().UnicastAddresses
            .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && a.IsDnsEligible))
            .Select(a2 => a2.Address.ToString())
            .ToArray();

        RefreshAvailableUpdatesCommand.Execute(null);
    }

    public int UpdateServerPort
    {
        get => _updatePort;
        set => this.RaiseAndSetIfChanged(ref _updatePort, value);
    }

    public int ContentServerPort
    {
        get => _contentServer.ServerPort;
        set
        {
            _contentServer.ServerPort = value;
            this.RaisePropertyChanged();
        }
    }

    public string[] AvailableAddresses
    {
        get => _addressList;
        set => this.RaiseAndSetIfChanged(ref _addressList, value);
    }

    public string SelectedServerAddress
    {
        get => _serverAddress;
        set => this.RaiseAndSetIfChanged(ref _serverAddress, value);
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
            if (SelectedUpdate == null) return;

            var fi = new FileInfo(Path.Combine(UpdateBinaryFolder, SelectedUpdate));

            await _publisher.PublishPackage(
                fi,
                SelectedServerAddress,
                _updateServer.ServerPort,
                SelectedServerAddress,
                _contentServer.ServerPort);
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
