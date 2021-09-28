using Common.XO.Requests;
using Devices.Core.State.Enums;
using System;
using static Common.Execution.Modes;

namespace Devices.Core.State.Management
{
    public interface IDeviceStateManager : IDisposable
    {
        void SetExecutionMode(Execution executionMode);
        void SetHealthCheckMode(string healthCheckValidationMode);
        void SetPluginPath(string pluginPath);
        void SetWorkflow(LinkDeviceActionType action);
        void LaunchWorkflow();
        DeviceWorkflowState GetCurrentWorkflow();
        void StopWorkflow();
        void DisplayDeviceStatus();
    }
}
