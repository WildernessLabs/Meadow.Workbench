﻿using CliFx.Attributes;
using Microsoft.Extensions.Logging;

namespace Meadow.CLI.Commands.DeviceManagement;

[Command("runtime disable", Description = "Sets the runtime to NOT run on the Meadow board then resets it")]
public class RuntimeDisableCommand : BaseDeviceCommand<RuntimeEnableCommand>
{
    public RuntimeDisableCommand(MeadowConnectionManager connectionManager, ILoggerFactory loggerFactory)
        : base(connectionManager, loggerFactory)
    {
        Logger.LogInformation($"Disabling runtime...");
    }

    protected override async ValueTask ExecuteCommand(Hcom.IMeadowDevice device, CancellationToken cancellationToken)
    {
        await device.RuntimeDisable(cancellationToken);
    }
}