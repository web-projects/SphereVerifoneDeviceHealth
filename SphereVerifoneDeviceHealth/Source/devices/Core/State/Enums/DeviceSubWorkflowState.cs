using System;

namespace Devices.Core.State.Enums
{
    /// <summary>
    /// Represents a set of sub-workflow states that represent certain specific
    /// processes that need to be completed before a transition occurs to send us
    /// back to the Manage state (Idle).
    /// </summary>
    public enum DeviceSubWorkflowState
    {
        /// <summary>
        /// Default state for all SubWorkflows.
        /// </summary>
        Undefined,

        /// <summary>
        /// Represents a state when DAL starts getting status information from the device
        /// </summary>
        GetStatus,

        /// <summary>
        /// Represents a state when DAL gets the active ADE KEY SLOT from the device
        /// </summary>
        GetActiveKeySlot,

        /// <summary>
        /// Represents a state when DAL gets EMV kernel checksum from the device
        /// </summary>
        GetEMVKernelChecksum,

        /// <summary>
        /// Represents a state when DAL gets security status information from the device
        /// </summary>
        GetSecurityConfiguration,

        /// <summary>
        /// Represents a state when DAL aborts pending device commands
        /// </summary>
        AbortCommand,

        /// <summary>
        /// Represents a state when DAL resets the device
        /// </summary>
        ResetCommand,

        /// <summary>
        /// Represents a state when DAL requests device to restart VIPA
        /// </summary>
        VIPARestart,

        /// <summary>
        /// Represents a state when DAL updates HMAC keys to the device
        /// </summary>
        UpdateHMACKeys,

        /// <summary>
        /// Represents a state when DAL generates HMAC from the device
        /// </summary>
        GenerateHMAC,

        /// <summary>
        /// Represents a state when DAL reboots the device
        /// </summary>
        RebootDevice,

        /// <summary>
        /// Represents a state when DAL request an extended reset from the device
        /// </summary>
        DeviceExtendedReset,

        /// <summary>
        /// Represents a state when DAL updates Payment workflow configuration files on the device
        /// </summary>
        Configuration,

        /// <summary>
        /// Represents a state when DAL updates Feature Enablement Token to device
        /// </summary>
        FeatureEnablementToken,

        /// <summary>
        /// Represents a state when DAL unlocks key updates on the device
        /// </summary>
        UnlockDeviceConfig,

        /// <summary>
        /// Represents a state when DAL locks key updates on the device with SLOT-0
        /// </summary>
        LockDeviceConfig0,

        /// <summary>
        /// Represents a state when DAL locks key updates on the device with SLOT-8
        /// </summary>
        LockDeviceConfig8,

        /// <summary>
        /// Represents a state when DAL updates Idle Screen to device
        /// </summary>
        UpdateIdleScreen,

        /// <summary>
        /// Represents a state when DAL requests a custom screen be displayed on Device
        /// </summary>
        DisplayCustomScreen,

        /// <summary>
        /// Represents a state when DAL sets the 24-hour reboot time for the device
        /// </summary>
        Reboot24Hour,

        /// <summary>
        /// Represents a state when DAL sets the device date-time
        /// </summary>
        SetTerminalDateTime,

        /// <summary>
        /// Represents a state when DAL queries the device for VIPA bundle versions
        /// </summary>
        VIPAVersions,

        /// <summary>
        /// Represents a state when DAL queries the device for Sphere Device Health file
        /// </summary>
        GetSphereHealthFile,

        /// <summary>
        /// Represents a state where a sanity check is performed to ensure that the DAL
        /// is in an operational state ready to receive the next command before a response
        /// is sent back to the caller.
        /// </summary>
        SanityCheck,

        /// <summary>
        /// Represents a state when SubWorkflow Completes
        /// </summary>
        RequestComplete
    }
}
