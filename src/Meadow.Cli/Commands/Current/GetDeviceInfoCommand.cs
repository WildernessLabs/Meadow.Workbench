﻿using CliFx.Attributes;
using Microsoft.Extensions.Logging;

namespace Meadow.CLI.Commands.DeviceManagement;

[Command("device info", Description = "Get the device info")]
public class GetDeviceInfoCommand : BaseDeviceCommand<GetDeviceInfoCommand>
{
    public GetDeviceInfoCommand(MeadowConnectionManager connectionManager, ILoggerFactory loggerFactory)
        : base(connectionManager, loggerFactory)
    {
        Logger.LogInformation($"Getting device info...");
    }

    protected override async ValueTask ExecuteCommand(Hcom.IMeadowDevice device, CancellationToken cancellationToken)
    {
        var deviceInfo = await device.GetDeviceInfo(cancellationToken);
        if (deviceInfo != null)
        {
            Logger.LogInformation(deviceInfo.ToString());
        }
    }
}