using MQTTnet.Server;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class OtAServer
{
    public event EventHandler StateChanged = delegate { };
    public event EventHandler<string> ActivityLogged = delegate { };

    private readonly MqttServer _broker;
    private readonly HttpUpdateServer _httpServer;
    private int _connectedClientCount = 0;

    public OtAServer()
    {
        var factory = new MqttServerFactory();

        // Configure options with default endpoint on port 1883
        var options = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(1883)
            .Build();

        _broker = factory.CreateMqttServer(options);

        // MQTTnet v5 requires events to be registered before StartAsync
        _broker.ClientConnectedAsync += OnClientConnectedAsync;
        _broker.ClientSubscribedTopicAsync += OnClientSubscribedTopicAsync;
        _broker.ClientDisconnectedAsync += OnClientDisconnectedAsync;

        // Add a test handler to verify events work
        _broker.ValidatingConnectionAsync += args =>
        {
            Debug.WriteLine($"MQTT: Client validating connection: {args.ClientId}");
            ActivityLogged?.Invoke(this, $"MQTT: Validating connection from {args.ClientId}");
            return Task.CompletedTask;
        };

        _httpServer = new HttpUpdateServer();
        _httpServer.ActivityLogged += (s, message) => ActivityLogged?.Invoke(this, message);
    }

    private Task OnClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
    {
        _connectedClientCount--;
        Debug.WriteLine($"Client disconnected: {arg.ClientId} (Total: {_connectedClientCount})");
        ActivityLogged?.Invoke(this, $"Client disconnected: {arg.ClientId} (Total: {_connectedClientCount})");

        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private Task OnClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
    {
        Debug.WriteLine($"Client subscribed to topic: {arg.TopicFilter.Topic}");
        ActivityLogged?.Invoke(this, $"Client '{arg.ClientId}' subscribed to topic: {arg.TopicFilter.Topic}");
        return Task.CompletedTask;
    }

    private Task OnClientConnectedAsync(ClientConnectedEventArgs arg)
    {
        _connectedClientCount++;
        var message = $"Client connected: {arg.ClientId} (Total: {_connectedClientCount})";
        Debug.WriteLine(message);

        // Verify event has subscribers
        if (ActivityLogged != null)
        {
            Debug.WriteLine("ActivityLogged event has subscribers, invoking...");
            ActivityLogged.Invoke(this, message);
        }
        else
        {
            Debug.WriteLine("WARNING: ActivityLogged event has NO subscribers!");
        }

        StateChanged?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    public async Task Start()
    {
        if (!_broker.IsStarted)
        {
            // Check if port 1883 is already in use on localhost (where clients connect)
            if (IsPortInUse(1883, System.Net.IPAddress.Loopback))
            {
                var message = "ERROR: Port 1883 is already in use on localhost. Another MQTT broker (e.g., mosquitto) may be running. Please stop it and try again.";
                Debug.WriteLine(message);
                ActivityLogged?.Invoke(this, message);
                throw new InvalidOperationException(message);
            }

            await _broker.StartAsync();
            Debug.WriteLine("MQTT broker started on port 1883");
            ActivityLogged?.Invoke(this, "MQTT broker started on port 1883");

            // Verify we can actually connect to our own broker
            if (!await VerifyBrokerIsListening(1883))
            {
                var warning = "WARNING: MQTT broker may not be listening correctly. Check for port conflicts.";
                Debug.WriteLine(warning);
                ActivityLogged?.Invoke(this, warning);
            }

            // Test that events are wired up
            Debug.WriteLine($"ActivityLogged has {ActivityLogged?.GetInvocationList().Length ?? 0} subscribers");
        }

        if (!_httpServer.IsRunning)
        {
            await _httpServer.Start();
            Debug.WriteLine($"HTTP server started: {_httpServer.BaseUrl}");
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool IsPortInUse(int port, System.Net.IPAddress address)
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(address, port);
            listener.Start();
            listener.Stop();
            Debug.WriteLine($"Port {port} check on {address}: Available");
            return false;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            Debug.WriteLine($"Port {port} check on {address}: IN USE (SocketException: {ex.Message})");
            return true;
        }
    }

    private async Task<bool> VerifyBrokerIsListening(int port)
    {
        try
        {
            // Try to connect to the broker we just started
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync("localhost", port);
            Debug.WriteLine($"Successfully verified broker is listening on localhost:{port}");
            return true;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            Debug.WriteLine($"Failed to connect to broker on localhost:{port}: {ex.Message}");
            return false;
        }
    }

    public async Task Stop()
    {
        if (_broker.IsStarted)
        {
            await _broker.StopAsync();
            Debug.WriteLine("MQTT broker stopped");
        }

        if (_httpServer.IsRunning)
        {
            await _httpServer.Stop();
            Debug.WriteLine("HTTP server stopped");
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsRunning => _broker.IsStarted && _httpServer.IsRunning;

    public int ConnectedClients => _connectedClientCount;

    public string MqttUrl => "tcp://localhost:1883";

    public string HttpUrl => _httpServer.BaseUrl;
}
