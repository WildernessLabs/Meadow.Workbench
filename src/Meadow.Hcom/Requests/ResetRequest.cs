namespace Meadow.Hcom;

internal class RuntimeEnableRequest : Request
{
    public override RequestType RequestType => RequestType.HCOM_MDOW_REQUEST_MONO_ENABLE;
}

internal class RuntimeDisableRequest : Request
{
    public override RequestType RequestType => RequestType.HCOM_MDOW_REQUEST_MONO_DISABLE;
}

internal class ResetRequest : Request
{
    public override RequestType RequestType => RequestType.HCOM_MDOW_REQUEST_RESTART_PRIMARY_MCU;
}
