using MQTTnet;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

public enum UpdateType
{
    Bootloader,
    OS,
    Application
}

internal class UpdateMessage : UpdateInfo
{
    public string MpakID
    {
        get => ID;
        set => ID = value;
    }
    public string MpakDownloadUrl { get; set; }
    public string[] TargetDevices { get; set; }
}

public class UpdateInfo
{
    public DateTime PublishedOn { get; internal set; }
    public string ID { get; protected set; }
    public UpdateType UpdateType { get; internal set; }
    public string Version { get; internal set; }
    public long DownloadSize { get; internal set; }
    public string? Summary { get; internal set; }
    public string? Detail { get; internal set; }
    public bool Retrieved { get; internal set; }
    public bool Applied { get; internal set; }
    public string DownloadHash { get; internal set; }
}

internal class OtAPublisher
{
    private readonly IMqttClient _client;

    public string SourceFolder { get; }

    public OtAPublisher()
    {
        var factory = new MqttClientFactory();

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

    public async Task MakeUpdateAvailable()
    {
        var options = new MqttClientOptionsBuilder()
            .WithClientId("workbench")
            .WithTcpServer("localhost", 1883)
            .Build();

        await _client.ConnectAsync(options);

        var update = GenerateMessageForUpdate("0.6.7.13");

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
            MpakDownloadUrl = $"http://192.168.1.133:5000/update/{updateName}",
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
