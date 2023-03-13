using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Update
{
    public class UpdatePublisher
    {
        private IMqttClient _client;

        public string SourceFolder { get; }
        public string UpdateServer { get; } = "http://192.168.1.133:5000";
        public UpdatePublisher()
        {
            var factory = new MqttFactory();

            _client = factory.CreateMqttClient();

            SourceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WildernessLabs", "Updates");

            if (!Directory.Exists(SourceFolder))
            {
                Directory.CreateDirectory(SourceFolder);
            }
        }

        public string[] GetAvailableUpdates()
        {
            var di = new DirectoryInfo(SourceFolder);

            var list = new List<string>();

            foreach (var d in di.EnumerateDirectories())
            {
                var upd = d.GetFiles("update.zip").FirstOrDefault();
                if (upd != null)
                {
                    list.Add(d.Name);
                }
            }

            return list.ToArray();
        }

        public async Task MakeUpdateAvailable(string selectedUpdate)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId("workbench")
                .WithTcpServer("192.168.1.133", 1883)
                .Build();

            await _client.ConnectAsync(options);

            var update = GenerateMessageForUpdate(selectedUpdate);

            var json = JsonSerializer.Serialize(update);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("Meadow.OtA")
                .WithPayload(json)
            .Build();

            await _client.PublishAsync(message, CancellationToken.None);

            await _client.DisconnectAsync();
        }

        private UpdateMessage GenerateMessageForUpdate(string updateName)
        {
            var updateFolder = Path.Combine(SourceFolder, updateName);

            // make sure the file exists
            var fi = new FileInfo(Path.Combine(updateFolder, "update.zip"));

            if (!fi.Exists)
            {
                throw new FileNotFoundException();
            }

            // get the update info (hash, etc)
            var hash = GetFileHash(fi);


            var update = new UpdateMessage
            {
                MpakID = updateName,
                MpakDownloadUrl = $"{UpdateServer}/update/{updateName}",
                DownloadHash = hash,
                DownloadSize = fi.Length,
                PublishedOn = fi.CreationTimeUtc,
                Version = updateName,
                UpdateType = UpdateType.OS
            };

            return update;
        }

        public string GetFileHash(FileInfo file)
        {
            using (var sha = SHA256.Create())
            using (var stream = file.OpenRead())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            }
        }
    }
}
