namespace Meadow.Hcom
{
    internal class GetDeviceInfoCommand : Command
    {
        public override RequestType RequestType => RequestType.HCOM_MDOW_REQUEST_GET_DEVICE_INFORMATION;

        public GetDeviceInfoCommand()
        {
        }
    }
}