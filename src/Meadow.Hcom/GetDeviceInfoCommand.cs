namespace Meadow.Hcom
{
    internal class GetDeviceInfoCommand : Request
    {
        public override RequestType RequestType => RequestType.HCOM_MDOW_REQUEST_GET_DEVICE_INFORMATION;

        public GetDeviceInfoCommand()
        {
        }
    }
}