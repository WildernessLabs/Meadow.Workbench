using Meadow.Foundation.Web.Maple;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Meadow.Update
{
    public class UpdateServer
    {
        public event EventHandler StateChanged = delegate { };

        private MqttServer _broker;
        private MapleServer _maple;

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

            _maple = new MapleServer(IPAddress.Any, 5000);

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

            if (!_maple.Running)
            {
                _maple.Start();

                Debug.WriteLine("Maple started");
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

            if (_maple.Running)
            {
                _maple.Stop();
                Debug.WriteLine("Maple stopped");
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsRunning
        {
            get => _broker.IsStarted && _maple.Running;
        }
    }
}
