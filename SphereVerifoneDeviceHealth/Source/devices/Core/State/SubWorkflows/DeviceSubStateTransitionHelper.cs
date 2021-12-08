using Devices.Core.State.Enums;

using static Devices.Core.State.Enums.DeviceSubWorkflowState;

namespace Devices.Core.State.SubWorkflows
{
    public static class DeviceSubStateTransitionHelper
    {
        private static DeviceSubWorkflowState ComputeGetStatusStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeGetActiveKeySlotStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeGetEMVKernelChecksumStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeGetSecurityConfigurationStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeDeviceAbortStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeDeviceResetStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeVIPARestartStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeDeviceRebootStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeDeviceExtendedResetStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeConfigurationStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeFeatureEnablementTokenStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeLockDeviceConfig0StateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeLockDeviceConfig8StateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeUpdateIdleScreenUpdateStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeDisplayCustomScreenUpdateStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeUnlockDeviceConfigStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeGetLoadHMACKeysStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeGetGenerateHMACStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeReboot24HourStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeSetTerminalDateTimeStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DeviceSubWorkflowState ComputeVIPAVersionsStateTransition(bool exception) =>
        exception switch
        {
            true => SanityCheck,
            false => SanityCheck
        };

        private static DeviceSubWorkflowState ComputeGetSphereDeviceHealthStateTransition(bool exception) =>
        exception switch
        {
            true => SanityCheck,
            false => SanityCheck
        };

        private static DeviceSubWorkflowState ComputeSanityCheckStateTransition(bool exception) =>
            exception switch
            {
                true => RequestComplete,
                false => RequestComplete
            };

        private static DeviceSubWorkflowState ComputeRequestCompletedStateTransition(bool exception) =>
            exception switch
            {
                true => Undefined,
                false => Undefined
            };

        public static DeviceSubWorkflowState GetNextState(DeviceSubWorkflowState state, bool exception) =>
            state switch
            {
                GetStatus => ComputeGetStatusStateTransition(exception),
                GetActiveKeySlot => ComputeGetActiveKeySlotStateTransition(exception),
                GetEMVKernelChecksum => ComputeGetEMVKernelChecksumStateTransition(exception),
                GetSecurityConfiguration => ComputeGetSecurityConfigurationStateTransition(exception),
                AbortCommand => ComputeDeviceAbortStateTransition(exception),
                ResetCommand => ComputeDeviceResetStateTransition(exception),
                VIPARestart => ComputeVIPARestartStateTransition(exception),
                RebootDevice => ComputeDeviceRebootStateTransition(exception),
                DeviceExtendedReset => ComputeDeviceExtendedResetStateTransition(exception),
                Configuration => ComputeConfigurationStateTransition(exception),
                FeatureEnablementToken => ComputeFeatureEnablementTokenStateTransition(exception),
                LockDeviceConfig0 => ComputeLockDeviceConfig0StateTransition(exception),
                LockDeviceConfig8 => ComputeLockDeviceConfig8StateTransition(exception),
                UnlockDeviceConfig => ComputeUnlockDeviceConfigStateTransition(exception),
                UpdateHMACKeys => ComputeGetLoadHMACKeysStateTransition(exception),
                GenerateHMAC => ComputeGetGenerateHMACStateTransition(exception),
                UpdateIdleScreen => ComputeUpdateIdleScreenUpdateStateTransition(exception),
                DisplayCustomScreen => ComputeDisplayCustomScreenUpdateStateTransition(exception),
                Reboot24Hour => ComputeReboot24HourStateTransition(exception),
                SetTerminalDateTime => ComputeSetTerminalDateTimeStateTransition(exception),
                VIPAVersions => ComputeVIPAVersionsStateTransition(exception),
                GetSphereHealthFile => ComputeGetSphereDeviceHealthStateTransition(exception),
                SanityCheck => ComputeSanityCheckStateTransition(exception),
                RequestComplete => ComputeRequestCompletedStateTransition(exception),
                _ => throw new StateException($"Invalid state transition '{state}' requested.")
            };
    }
}
