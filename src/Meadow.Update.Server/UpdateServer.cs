using MQTTnet;
using MQTTnet.Server;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Meadow.Update
{
    public class UpdateServer
    {
        private MqttServer _broker;

        public UpdateServer()
        {
            var factory = new MqttFactory();
            var options = new MqttServerOptionsBuilder()
                       .WithDefaultEndpoint()
                       .Build();

            _broker = factory.CreateMqttServer(options);
            _broker.ClientConnectedAsync += OnClientConnectedAsync;
            _broker.ClientSubscribedTopicAsync += OnClientSubscribedTopicAsync;
        }

        private async Task OnClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
        {
            Debug.WriteLine("Client subscribed");
            await Task.Delay(1000);
        }

        private async Task OnClientConnectedAsync(ClientConnectedEventArgs arg)
        {
            Debug.WriteLine($"Client connected: {arg.ClientId}");
            await Task.Delay(1000);
        }

        public async Task Start()
        {
            if (!_broker.IsStarted)
            {
                await _broker.StartAsync();

                Debug.WriteLine("Update broker started");
            }
        }

        public async Task Stop()
        {
            if (_broker.IsStarted)
            {
                await _broker.StopAsync();

                Debug.WriteLine("Update broker stopped");
            }
        }
    }
}
