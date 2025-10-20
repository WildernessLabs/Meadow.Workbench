using Avalonia.Threading;
using Meadow.Workbench.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class OtAServerViewModel : FeatureViewModel
{
    private readonly OtAServer _server;
    private readonly OtAPublisher _publisher;
    private string? _selectedUpdate;
    private string _deviceId = "";

    public ObservableCollection<string> AvailableUpdates { get; } = new();
    public ObservableCollection<string> ActivityLog { get; } = new();

    public OtAServerViewModel()
    {
        _server = new OtAServer();
        _publisher = new OtAPublisher();

        _server.ActivityLogged += (s, message) =>
        {
            AddLogEntry(message);
        };

        _server.StateChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(IsServerRunning));
            this.RaisePropertyChanged(nameof(ServerStatus));
            this.RaisePropertyChanged(nameof(ConnectedClients));
            this.RaisePropertyChanged(nameof(StartStopButtonText));
            this.RaisePropertyChanged(nameof(MqttBrokerUrl));
            this.RaisePropertyChanged(nameof(HttpServerUrl));
            this.RaisePropertyChanged(nameof(CanPublish));
        };

        ToggleServerCommand = ReactiveCommand.CreateFromTask(OnToggleServer);
        PublishUpdateCommand = ReactiveCommand.CreateFromTask(OnPublishUpdate);
        RefreshUpdatesCommand = ReactiveCommand.Create(RefreshAvailableUpdates);

        RefreshAvailableUpdates();

        // Test that logging is working
        AddLogEntry("OTA Server initialized");
    }

    // Properties
    public bool IsServerRunning => _server.IsRunning;

    public string ServerStatus => IsServerRunning ? "Running" : "Stopped";

    public string MqttBrokerUrl => _server.MqttUrl;

    public string HttpServerUrl => _server.HttpUrl;

    public int ConnectedClients => _server.ConnectedClients;

    public string StartStopButtonText => IsServerRunning ? "Stop Server" : "Start Server";

    public bool CanPublish => IsServerRunning && SelectedUpdate != null;

    public string? SelectedUpdate
    {
        get => _selectedUpdate;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedUpdate, value);
            this.RaisePropertyChanged(nameof(CanPublish));
        }
    }

    public string DeviceId
    {
        get => _deviceId;
        set => this.RaiseAndSetIfChanged(ref _deviceId, value);
    }

    // Commands
    public IReactiveCommand ToggleServerCommand { get; }
    public IReactiveCommand PublishUpdateCommand { get; }
    public IReactiveCommand RefreshUpdatesCommand { get; }

    private void AddLogEntry(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Debug.WriteLine(message);

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            ActivityLog.Insert(0, $"[{timestamp}] {message}");

            // Keep only last 100 entries
            while (ActivityLog.Count > 100)
            {
                ActivityLog.RemoveAt(ActivityLog.Count - 1);
            }
        });
    }

    private async Task OnToggleServer()
    {
        if (IsServerRunning)
        {
            AddLogEntry("Stopping server...");
            await _server.Stop();
            AddLogEntry("Server stopped");
        }
        else
        {
            AddLogEntry("Starting server...");
            await _server.Start();
            AddLogEntry($"Server started - MQTT: {MqttBrokerUrl}, HTTP: {HttpServerUrl}");
        }

        this.RaisePropertyChanged(nameof(IsServerRunning));
        this.RaisePropertyChanged(nameof(ServerStatus));
        this.RaisePropertyChanged(nameof(StartStopButtonText));
        this.RaisePropertyChanged(nameof(MqttBrokerUrl));
        this.RaisePropertyChanged(nameof(HttpServerUrl));
        this.RaisePropertyChanged(nameof(CanPublish));
    }

    private async Task OnPublishUpdate()
    {
        if (SelectedUpdate == null) return;

        try
        {
            // When authentication is disabled, clients subscribe to {OID}/ota/{ID}
            // Since {OID} doesn't get replaced, we publish to the literal topic
            var topic = string.IsNullOrWhiteSpace(DeviceId)
                ? "{OID}/ota"  // Broadcast to all devices (matches {OID}/ota/{ID} pattern)
                : $"{{OID}}/ota/{DeviceId}";  // Target specific device

            AddLogEntry($"Publishing update '{SelectedUpdate}' to topic '{topic}'...");
            await _publisher.PublishUpdate(SelectedUpdate, HttpServerUrl, topic);
            AddLogEntry($"Update '{SelectedUpdate}' published successfully");
        }
        catch (Exception ex)
        {
            AddLogEntry($"Failed to publish update: {ex.Message}");
        }
    }

    private void RefreshAvailableUpdates()
    {
        AvailableUpdates.Clear();
        var updates = _publisher.GetAvailableUpdates();
        foreach (var update in updates)
        {
            AvailableUpdates.Add(update);
        }

        System.Diagnostics.Debug.WriteLine($"Found {AvailableUpdates.Count} available updates");
    }
}
