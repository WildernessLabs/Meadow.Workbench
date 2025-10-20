using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class DeviceLoginRequest
{
    public string Id { get; set; } = string.Empty;
}

internal class HttpUpdateServer
{
    public event EventHandler<string> ActivityLogged = delegate { };

    private WebApplication? _app;
    private readonly string _updatesFolder;
    private readonly int _port = 5000;
    private readonly string _localIpAddress;

    public string BaseUrl => $"http://{_localIpAddress}:{_port}";
    public bool IsRunning => _app != null;

    public HttpUpdateServer()
    {
        _updatesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WildernessLabs", "Updates");

        if (!Directory.Exists(_updatesFolder))
        {
            Directory.CreateDirectory(_updatesFolder);
        }

        // Auto-detect local IP for display purposes
        _localIpAddress = GetLocalIpAddress();
    }

    public async Task Start()
    {
        if (_app != null)
        {
            return; // Already running
        }

        var builder = WebApplication.CreateBuilder();

        // Configure Kestrel to listen on all interfaces
        // Note: On Windows, binding to 0.0.0.0 doesn't require admin rights (unlike *)
        builder.WebHost.UseUrls($"http://0.0.0.0:{_port}");

        // Suppress logging noise
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

        _app = builder.Build();

        // Mock authentication endpoint for testing
        _app.MapPost("/api/devices/login", HandleDeviceLogin);

        // Mock cloud data endpoints
        _app.MapPost("/api/logs", HandleLogPost);
        _app.MapPost("/api/events", HandleEventPost);

        // Update download endpoints
        _app.MapGet("/update/{id}", HandleUpdateDownload);
        _app.MapGet("/update/{id}/{filename}", HandleUpdateFileDownload);
        _app.MapGet("/update-os/{id}", HandleUpdateWithOsDownload);

        await _app.StartAsync();
    }

    private IResult HandleDeviceLogin(DeviceLoginRequest request)
    {
        System.Diagnostics.Debug.WriteLine($"Mock auth request from device: {request.Id}");

        // Return mock encrypted values - clients just need successful 200 response
        // These are base64-encoded placeholder strings for testing
        return Results.Ok(new
        {
            encryptedKey = "bW9jayBlbmNyeXB0ZWQga2V5",
            encryptedToken = "bW9jayBlbmNyeXB0ZWQgdG9rZW4=",
            iv = "bW9jayBpdg=="
        });
    }

    private IResult HandleLogPost(HttpContext context)
    {
        ActivityLogged?.Invoke(this, "HTTP: Log received from device");
        return Results.Ok();
    }

    private IResult HandleEventPost(HttpContext context)
    {
        ActivityLogged?.Invoke(this, "HTTP: Event received from device");
        return Results.Ok();
    }

    private IResult HandleUpdateDownload(string id, HttpContext context)
    {
        // Try both .mpak and .zip extensions for compatibility
        var mpakPath = Path.Combine(_updatesFolder, id, "update.mpak");
        var zipPath = Path.Combine(_updatesFolder, id, "update.zip");

        var filePath = File.Exists(mpakPath) ? mpakPath
                     : File.Exists(zipPath) ? zipPath
                     : null;

        if (filePath == null)
        {
            ActivityLogged?.Invoke(this, $"HTTP: Update file not found: {id}");
            return Results.NotFound();
        }

        var fileName = Path.GetFileName(filePath);
        var fileSize = new FileInfo(filePath).Length / 1024.0 / 1024.0; // MB
        ActivityLogged?.Invoke(this, $"HTTP: Serving update '{id}' ({fileName}, {fileSize:F2} MB)");

        // Support HTTP Range headers for resumable downloads (F7 compatibility)
        return Results.File(
            filePath,
            contentType: "application/zip",
            enableRangeProcessing: true);
    }

    private IResult HandleUpdateFileDownload(string id, string filename, HttpContext context)
    {
        System.Diagnostics.Debug.WriteLine($"Update file download request: {id}/{filename}");

        var filePath = Path.Combine(_updatesFolder, id, filename);

        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"Update file not found: {filePath}");
            return Results.NotFound();
        }

        System.Diagnostics.Debug.WriteLine($"Serving update file: {filePath}");

        // Support HTTP Range headers for resumable downloads
        return Results.File(
            filePath,
            contentType: "application/zip",
            enableRangeProcessing: true);
    }

    private IResult HandleUpdateWithOsDownload(string id, HttpContext context)
    {
        System.Diagnostics.Debug.WriteLine($"Update with OS download request: {id}");

        // For OS updates, look for update-with-os.mpak or update-with-os.zip
        var mpakPath = Path.Combine(_updatesFolder, id, "update-with-os.mpak");
        var zipPath = Path.Combine(_updatesFolder, id, "update-with-os.zip");

        var filePath = File.Exists(mpakPath) ? mpakPath
                     : File.Exists(zipPath) ? zipPath
                     : null;

        if (filePath == null)
        {
            System.Diagnostics.Debug.WriteLine($"Update with OS file not found: {id}");
            return Results.NotFound();
        }

        System.Diagnostics.Debug.WriteLine($"Serving update with OS file: {filePath}");

        return Results.File(
            filePath,
            contentType: "application/zip",
            enableRangeProcessing: true);
    }

    private string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            // Prefer 192.168.x.x or 10.x.x.x addresses (local network)
            var localIp = host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .FirstOrDefault(ip =>
                {
                    var bytes = ip.GetAddressBytes();
                    return (bytes[0] == 192 && bytes[1] == 168) || bytes[0] == 10;
                });

            if (localIp != null)
            {
                return localIp.ToString();
            }

            // Fallback to any IPv4 address
            var anyIpv4 = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            return anyIpv4?.ToString() ?? "127.0.0.1";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get local IP address: {ex.Message}");
            return "127.0.0.1";
        }
    }

    public async Task Stop()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
    }
}
