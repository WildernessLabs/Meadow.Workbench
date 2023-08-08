using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Meadow.Cli;
using Meadow.Hcom;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Meadow.CLI.Commands.DeviceManagement;

public class MeadowConnectionManager
{
    private SettingsManager _settingsManager;

    public MeadowConnectionManager(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public IMeadowConnection? GetCurrentConnection()
    {
        var route = _settingsManager.GetSetting(SettingsManager.PublicSettings.Route);

        if (route == null)
        {
            throw new Exception("No 'route' configuration set");
        }

        // try to determine what the route is
        string? uri = null;
        if (route.StartsWith("http"))
        {
            uri = route;
        }
        else if (IPAddress.TryParse(route, out var ipAddress))
        {
            uri = $"http://{route}:5000";
        }
        else if (IPEndPoint.TryParse(route, out var endpoint))
        {
            uri = $"http://{route}";
        }

        if (uri != null)
        {
            return new TcpConnection(uri);
        }
        return new SerialConnection(route);
    }
}

[Command("config", Description = "Get the device info")]
public class ConfigCommand : ICommand
{
    private SettingsManager _settingsManager;
    private readonly ILogger<GetDeviceInfoCommand> _logger;

    [CommandOption("list", IsRequired = false)]
    public bool List { get; set; }

    [CommandParameter(0, Name = "Settings", IsRequired = false)]
    public string[] Settings { get; set; }

    public ConfigCommand(SettingsManager settingsManager, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GetDeviceInfoCommand>();
        _settingsManager = settingsManager;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (List)
        {
            _logger.LogInformation($"Current CLI configuration");

            // display all current config
            var settings = _settingsManager.GetPublicSettings();
            if (settings.Count == 0)
            {
                _logger.LogInformation($"  <no settings found>");
            }
            else
            {
                foreach (var kvp in _settingsManager.GetPublicSettings())
                {
                    _logger.LogInformation($"  {kvp.Key} = {kvp.Value}");
                }
            }
        }
        else
        {
            switch (Settings.Length)
            {
                case 0:
                    // not valid
                    throw new CommandException($"No setting provided");
                case 1:
                    // erase a setting
                    _logger.LogInformation($"Deleting Setting {Settings[0]}");
                    _settingsManager.DeleteSetting(Settings[0]);
                    break;
                case 2:
                    // set a setting
                    _logger.LogInformation($"Setting {Settings[0]}={Settings[1]}");
                    _settingsManager.SaveSetting(Settings[0], Settings[1]);
                    break;
                default:
                    // not valid;
                    throw new CommandException($"Too many parameters provided");
            }
        }
    }
}

[Command("device info", Description = "Get the device info")]
public class GetDeviceInfoCommand : ICommand
{
    private readonly ILogger<GetDeviceInfoCommand> _logger;
    private readonly MeadowConnectionManager _connectionManager;

    public GetDeviceInfoCommand(MeadowConnectionManager connectionManager, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GetDeviceInfoCommand>();
        _connectionManager = connectionManager;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        // TODO: call a base "before" for logging?

        //        await base.ExecuteAsync(console);
        var cancellationToken = console.RegisterCancellationHandler();
        var c = _connectionManager.GetCurrentConnection();

        if (c != null)
        {
            c.ConnectionError += (s, e) =>
            {
                _logger.LogError(e.Message);
            };

            try
            {
                await c.Attach(cancellationToken);


                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"Cancelled");
                    return;
                }

                var deviceInfo = await c.Device.GetDeviceInfo(cancellationToken);
                if (deviceInfo != null)
                {
                    _logger.LogInformation(deviceInfo.ToString());
                }
            }
            catch (TimeoutException)
            {
                _logger.LogError($"Timeout attempting to attach to device on {c.Name}");
            }
        }

        // TODO: call a base "after" for logging?
    }
}
