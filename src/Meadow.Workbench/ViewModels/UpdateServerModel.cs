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
    private string _updateActionText;
    private string _contentActionText;

    public UpdateServerModel()
    {
        _updateServer = new UpdateServer();
        _contentServer = new ContentServer();
        _publisher = new UpdatePublisher();

        _updateServer.StateChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(ServerIsRunning));
        };

        AvailableAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(i => i.GetIPProperties().UnicastAddresses
            .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && a.IsDnsEligible))
            .Select(a2 => a2.Address.ToString())
            .ToArray();

        UpdateServerActionText = "Start";
        ContentServerActionText = "Start";

        RefreshAvailableUpdatesCommand.Execute(null);
    }

    public int UpdateServerPort
    {
        get => _updateServer.ServerPort;
        set
        {
            _updateServer.ServerPort = value;
            this.RaisePropertyChanged();
        }
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

    public string UpdateServerActionText
    {
        get => _updateActionText;
        set => this.RaiseAndSetIfChanged(ref _updateActionText, value);
    }

    public string ContentServerActionText
    {
        get => _contentActionText;
        set => this.RaiseAndSetIfChanged(ref _contentActionText, value);
    }

    public ICommand UpdateServerActionCommand
    {
        get => new Command(async () =>
        {
            if (!_updateServer.IsRunning)
            {
                await _updateServer.Start();
                UpdateServerActionText = "Stop";
            }
            else
            {
                await _updateServer.Stop();
                UpdateServerActionText = "Start";
            }
        });
    }

    public ICommand ContentServerActionCommand
    {
        get => new Command(() =>
        {
            if (!_contentServer.IsRunning)
            {
                _contentServer.Start();
                ContentServerActionText = "Stop";
            }
            else
            {
                _contentServer.Stop();
                ContentServerActionText = "Start";
            }
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
