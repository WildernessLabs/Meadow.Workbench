using Meadow.Workbench.Models;
using MQTTnet;
using MQTTnet.Server;
using Splat;
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
    private PackageService? _packageService;

    public string SourceFolder { get; }

    public OtAPublisher()
    {
        var factory = new MqttClientFactory();

        _client = factory.CreateMqttClient();

        SourceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WildernessLabs", "Packages");

        if (!Directory.Exists(SourceFolder))
        {
            Directory.CreateDirectory(SourceFolder);
        }
    }

    private PackageService GetPackageService()
    {
        // Lazy initialization - get from Locator when first needed
        if (_packageService == null)
        {
            System.Diagnostics.Debug.WriteLine("OtAPublisher: Retrieving PackageService from Locator...");
            _packageService = Locator.Current.GetService<PackageService>();
            if (_packageService == null)
            {
                System.Diagnostics.Debug.WriteLine("OtAPublisher: ERROR - PackageService not found in Locator!");
                throw new InvalidOperationException("PackageService not found in Locator. Make sure it's registered before using OtAPublisher.");
            }
            System.Diagnostics.Debug.WriteLine("OtAPublisher: PackageService retrieved successfully");
        }
        return _packageService;
    }

    public string[] GetAvailableUpdates()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"OtAPublisher: GetAvailableUpdates() called");

            var packageService = GetPackageService();
            var allPackages = packageService.GetAllPackages().ToList();
            System.Diagnostics.Debug.WriteLine($"OtAPublisher: Found {allPackages.Count} total packages");

            // PackageService already populates FileFound from actual file system state
            var availablePackages = allPackages.Where(p => p.FileFound).ToList();
            System.Diagnostics.Debug.WriteLine($"OtAPublisher: {availablePackages.Count} packages have files on disk");

            foreach (var pkg in availablePackages)
            {
                System.Diagnostics.Debug.WriteLine($"  Package: {pkg.AppName} {pkg.AppVersion} ({pkg.Target}), FileFound={pkg.FileFound}, FileName={pkg.FileName}");
            }

            var result = availablePackages.Select(p => $"{p.AppName} {p.AppVersion} ({p.Target})").ToArray();
            System.Diagnostics.Debug.WriteLine($"OtAPublisher: Returning {result.Length} package names: {string.Join(", ", result)}");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OtAPublisher: Error getting packages: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"OtAPublisher: Stack trace: {ex.StackTrace}");
            return Array.Empty<string>();
        }
    }

    public Package? GetPackageByDisplayName(string displayName)
    {
        var packageService = GetPackageService();
        var packages = packageService.GetAllPackages().ToList();

        foreach (var package in packages)
        {
            var name = $"{package.AppName} {package.AppVersion} ({package.Target})";
            if (name == displayName)
            {
                return package;
            }
        }

        return null;
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

    private UpdateMessage GenerateMessageForUpdate(string displayName, string serverUrl)
    {
        var package = GetPackageByDisplayName(displayName);
        if (package == null)
        {
            throw new InvalidOperationException($"Package '{displayName}' not found");
        }

        var packageFile = new FileInfo(Path.Combine(SourceFolder, package.FileName));

        if (!packageFile.Exists)
        {
            throw new FileNotFoundException($"Package file not found: {package.FileName}");
        }

        // Get the package hash
        var hash = GetFileHash(packageFile);

        var update = new UpdateMessage
        {
            MpakID = package.PackageID,
            MpakDownloadUrl = $"{serverUrl}/update/{package.FileName}",
            MpakWithOsDownloadUrl = $"{serverUrl}/update-os/{package.FileName}",  // For F7 OS updates
            OsVersion = package.OSVersion,
            Crc = hash,
            FileSize = packageFile.Length,
            PublishedOn = package.CreatedAt,
            Version = package.AppVersion,
            UpdateType = UpdateType.Application,
            Summary = package.AppName,
            Detail = package.Description
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
