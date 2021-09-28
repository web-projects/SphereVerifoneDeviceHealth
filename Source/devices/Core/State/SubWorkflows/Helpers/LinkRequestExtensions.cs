using Common.XO.Device;
using Common.XO.Requests;

namespace LinkRequestExtensions
{
    public static class LinkRequestDeviceIdentifier
    {
        public static LinkDeviceIdentifier GetDeviceIdentifier(this LinkRequest linkRequest)
         => linkRequest.Actions?[0]?.DeviceRequest?.DeviceIdentifier;
    }
}
