using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.XO.Requests
{
    public partial class LinkDeviceActionRequest
    {
        public LinkDeviceActionType? DeviceAction { get; set; }
        public string? Reboot24Hour { get; set; }
        public string TerminalDateTime { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkDeviceActionType
    {
        Configuration,
        GetStatus,
        AbortCommand,
        ResetCommand,
        VIPARestart,
        RebootDevice,
        DeviceExtendedReset,
        GetIdentifier,
        GetActiveKeySlot,
        GetEMVKernelChecksum,
        GetSecurityConfiguration,
        FeatureEnablementToken,
        LockDeviceConfig0,
        LockDeviceConfig8,
        UnlockDeviceConfig,
        UpdateHMACKeys,
        GenerateHMAC,
        UpdateIdleScreen,
        DisplayCustomScreen,
        Reboot24Hour,
        SetTerminalDateTime,
        VIPAVersions,
        GetSphereHealthFile,
        ManualCardEntry
    }
}
