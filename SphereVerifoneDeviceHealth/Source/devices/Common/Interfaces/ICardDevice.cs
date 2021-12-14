using Common.XO.Requests;
using Common.XO.Responses;
using Devices.Common.AppConfig;
using Execution;
using System;
using System.Collections.Generic;
using System.Threading;
using static Common.XO.Responses.LinkEventResponse;

namespace Devices.Common.Interfaces
{
    public delegate void PublishEvent(EventTypeType eventType, EventCodeType eventCode,
            List<LinkDeviceResponse> devices, object request, string message);

    public interface ICardDevice : ICloneable, IDisposable
    {
        event PublishEvent PublishEvent;
        event DeviceEventHandler DeviceEventOccured;

        string Name { get; }

        string ManufacturerConfigID { get; }

        int SortOrder { get; set; }

        AppExecConfig AppExecConfig { get; set; }

        DeviceInformation DeviceInformation { get; }

        bool IsConnected(object request);

        void SetDeviceSectionConfig(DeviceSection config, AppExecConfig appConfig, bool displayOutput);

        List<DeviceInformation> DiscoverDevices();

        List<LinkErrorValue> Probe(DeviceConfig config, DeviceInformation deviceInfo, out bool dalActive);

        void DeviceSetIdle();

        bool DeviceRecovery();

        void Disconnect();

        List<LinkRequest> GetDeviceResponse(LinkRequest deviceInfo);

        // ------------------------------------------------------------------------
        // Methods that are mapped for usage in their respective sub-workflows.
        // ------------------------------------------------------------------------
        LinkRequest GetStatus(LinkRequest linkRequest);
        LinkRequest GetActiveKeySlot(LinkRequest linkRequest);
        LinkRequest GetEMVKernelChecksum(LinkRequest linkRequest);
        LinkRequest GetSecurityConfiguration(LinkRequest linkRequest);
        LinkRequest AbortCommand(LinkRequest linkRequest);
        LinkRequest VIPARestart(LinkRequest linkRequest);
        LinkRequest ResetDevice(LinkRequest linkRequest);
        LinkRequest RebootDevice(LinkRequest linkRequest);
        LinkRequest DeviceExtendedReset(LinkRequest linkRequest);
        LinkRequest Configuration(LinkRequest linkRequest);
        LinkRequest FeatureEnablementToken(LinkRequest linkRequest);
        LinkRequest LockDeviceConfiguration0(LinkRequest linkRequest);
        LinkRequest LockDeviceConfiguration8(LinkRequest linkRequest);
        LinkRequest UnlockDeviceConfiguration(LinkRequest linkRequest);
        LinkRequest UpdateHMACKeys(LinkRequest linkRequest);
        LinkRequest GenerateHMAC(LinkRequest linkRequest);
        LinkRequest UpdateIdleScreen(LinkRequest linkRequest);
        LinkRequest DisplayCustomScreen(LinkRequest linkRequest);
        LinkRequest Reboot24Hour(LinkRequest linkRequest);
        LinkRequest SetTerminalDateTime(LinkRequest linkRequest);
        LinkActionRequest VIPAVersions(LinkActionRequest linkRequest);
        LinkRequest GetSphereHealthFile(LinkRequest linkRequest);
        LinkRequest ManualCardEntry(LinkRequest linkRequest, CancellationToken cancellationToken);
    }
}
