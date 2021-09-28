using Common.XO.Requests;
using Devices.Common;
using Devices.Common.Helpers;

namespace Devices.Core.State.Interfaces
{
    internal interface IActionReceiver
    {
        void RequestReceived(LinkRequest request);
        void DeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation);
        void ComportEventReceived(PortEventType comPortEvent, string portNumber);
    }
}
