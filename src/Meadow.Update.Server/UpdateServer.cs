using MQTTnet;
using MQTTnet.Server;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Meadow.Update
{
    /// <summary>
    /// This server provides an MQTT endpoint to notify a Meadow device of available packages
    /// </summary>
    public class UpdateServer
    {
        public event EventHandler StateChanged = delegate { };

        private MqttServer _broker;

        public int ServerPort { get; set; } = 1883;

        public UpdateServer()
        {
            var factory = new MqttFactory();
            var options = new MqttServerOptionsBuilder()
                       .WithDefaultEndpoint()
                       .Build();

            _broker = factory.CreateMqttServer(options);
            _broker.ClientConnectedAsync += OnClientConnectedAsync;
            _broker.ClientSubscribedTopicAsync += OnClientSubscribedTopicAsync;
            _broker.ClientDisconnectedAsync += OnClientDisconnectedAsync;
            _broker.InterceptingPublishAsync += OnMessagePublished;
        }

        private Task OnMessagePublished(InterceptingPublishEventArgs arg)
        {
            Debug.WriteLine($"Update Message Has been published by '{arg.ClientId}' to '{arg.ApplicationMessage.Topic}'");
            return Task.CompletedTask;
        }

        private Task OnClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
        {
            Debug.WriteLine($"Client disconnected: {arg.ClientId}");

            return Task.CompletedTask;
        }

        private async Task OnClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
        {
            Debug.WriteLine("Client subscribed");
            await Task.Delay(1000);
        }

        private async Task OnClientConnectedAsync(ClientConnectedEventArgs arg)
        {
            Debug.WriteLine($"Client connected: {arg.ClientId}");

            StateChanged?.Invoke(this, EventArgs.Empty);

            await Task.Delay(1000);
        }

        public async Task Start()
        {
            if (!_broker.IsStarted)
            {
                await _broker.StartAsync();

                Debug.WriteLine("Update broker started");
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task Stop()
        {
            if (_broker.IsStarted)
            {
                await _broker.StopAsync();

                Debug.WriteLine("Update broker stopped");
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsRunning
        {
            get => _broker.IsStarted;
        }
    }
}
