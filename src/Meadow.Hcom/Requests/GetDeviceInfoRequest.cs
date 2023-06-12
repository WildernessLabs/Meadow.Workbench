namespace Meadow.Hcom
{
    internal class GetFileListRequest : Request
    {
        public override RequestType RequestType => IncludeCrcs
                ? RequestType.HCOM_MDOW_REQUEST_LIST_PART_FILES_AND_CRC
                : RequestType.HCOM_MDOW_REQUEST_LIST_PARTITION_FILES;

        public bool IncludeCrcs { get; set; }

        public GetFileListRequest()
        {
        }
    }

    internal class GetDeviceInfoRequest : Request
    {
        public override RequestType RequestType => RequestType.HCOM_MDOW_REQUEST_GET_DEVICE_INFORMATION;

        public GetDeviceInfoRequest()
        {
        }
        // Serialized example:
        // message
        // 01-00-07-00-12-01-00-00-00-00-00-00"
        // encoded
        // 00-02-2A-02-06-03-12-01-01-01-01-01-01-01-00
    }
}