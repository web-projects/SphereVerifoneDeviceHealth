using Devices.Common.Helpers;
using System.Threading.Tasks;

namespace Devices.Common
{
    public delegate void DeviceEventHandler(DeviceEvent deviceEvent, DeviceInformation deviceInformation);
    public delegate void DeviceLogHandler(DeviceEvent deviceEvent, DeviceInformation deviceInformation);
    public delegate Task ComPortEventHandler(PortEventType comPortEvent, string portNumber);
    public delegate void QueueEventHandler();
}
