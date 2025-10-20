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
    public string MpakDownloadUrl { get; set; } = string.Empty;
    public string MpakWithOsDownloadUrl { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string[] TargetDevices { get; set; } = Array.Empty<string>();
}

public class UpdateInfo
{
    public DateTime PublishedOn { get; internal set; }
    public string ID { get; protected set; } = string.Empty;
    public UpdateType UpdateType { get; internal set; }
    public string Version { get; internal set; } = string.Empty;
    public long FileSize { get; internal set; }  // Changed from DownloadSize to match F7
    public string? Summary { get; internal set; }
    public string? Detail { get; internal set; }
    public bool Retrieved { get; internal set; }
    public bool Applied { get; internal set; }
    public string Crc { get; internal set; } = string.Empty;  // Changed from DownloadHash to match F7
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
            // Support both .mpak and .zip extensions
            var mpakFile = d.GetFiles("update.mpak").FirstOrDefault();
            var zipFile = d.GetFiles("update.zip").FirstOrDefault();

            if (mpakFile != null || zipFile != null)
            {
                list.Add(d.Name);
            }
        }

        return list.ToArray();
    }

    public async Task PublishUpdate(string updateName, string serverUrl, string topic)
    {
        var options = new MqttClientOptionsBuilder()
            .WithClientId("workbench-publisher")
            .WithTcpServer("localhost", 1883)
            .Build();

        await _client.ConnectAsync(options);

        var update = GenerateMessageForUpdate(updateName, serverUrl);

        var json = JsonSerializer.Serialize(update);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(json)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message, CancellationToken.None);

        await _client.DisconnectAsync();
    }

    private UpdateMessage GenerateMessageForUpdate(string updateName, string serverUrl)
    {
        var updateFolder = Path.Combine(SourceFolder, updateName);

        // Support both .mpak and .zip extensions
        var mpakFile = new FileInfo(Path.Combine(updateFolder, "update.mpak"));
        var zipFile = new FileInfo(Path.Combine(updateFolder, "update.zip"));

        var file = mpakFile.Exists ? mpakFile : zipFile;

        if (!file.Exists)
        {
            throw new FileNotFoundException($"No update.mpak or update.zip found in {updateFolder}");
        }

        // Get the update info (hash, etc)
        var hash = GetFileHash(file);

        var update = new UpdateMessage
        {
            MpakID = updateName,
            MpakDownloadUrl = $"{serverUrl}/update/{updateName}",
            MpakWithOsDownloadUrl = $"{serverUrl}/update-os/{updateName}",  // For F7 OS updates
            OsVersion = "",  // Empty = app-only update
            Crc = hash,  // Use Crc property instead of DownloadHash
            FileSize = file.Length,  // Use FileSize instead of DownloadSize
            PublishedOn = file.CreationTimeUtc,
            Version = updateName,
            UpdateType = UpdateType.Application,  // Application update
            Summary = $"Update {updateName}",
            Detail = $"Test update package {updateName}"
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
