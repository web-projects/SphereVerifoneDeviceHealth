using Common.XO.Device;
using Common.XO.Requests;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.Helpers;
using Devices.Core.State.Enums;
using System;
using System.Threading.Tasks;
using static Devices.Core.State.Enums.DeviceSubWorkflowState;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal class DeviceVIPAVersionsSubStateAction : DeviceBaseSubStateAction
    {
        public override DeviceSubWorkflowState WorkflowStateType => VIPAVersions;

        public DeviceVIPAVersionsSubStateAction(IDeviceSubStateController _) : base(_) { }

        public override SubStateActionLaunchRules LaunchRules => new SubStateActionLaunchRules
        {
            RequestCancellationToken = true
        };

        public override async Task DoWork()
        {
            if (StateObject is null)
            {
                //_ = Controller.LoggingClient.LogErrorAsync("Unable to find a state object while attempting to obtain VIPA versions.");
                Console.WriteLine("Unable to find a state object while attempting to obtain VIPA versions.");
                _ = Error(this);
            }
            else
            {
                LinkRequest linkRequest = StateObject as LinkRequest;

                foreach (LinkActionRequest linkActionRequest in linkRequest.Actions)
                {
                    LinkDeviceIdentifier deviceIdentifier = linkActionRequest.DALRequest.DeviceIdentifier;
                    IDeviceCancellationBroker cancellationBroker = Controller.GetDeviceCancellationBroker();

                    ICardDevice cardDevice = FindTargetDevice(deviceIdentifier);
                    if (cardDevice != null)
                    {
                        var timeoutPolicy = await cancellationBroker.ExecuteWithTimeoutAsync<LinkActionRequest>(
                            _ => cardDevice.VIPAVersions(linkActionRequest),
                            DeviceConstants.CardCaptureTimeout,
                            this.CancellationToken);

                        if (timeoutPolicy.Outcome == Polly.OutcomeType.Failure)
                        {
                            //_ = Controller.LoggingClient.LogErrorAsync($"Unable to process VIPA versions request from device - '{Controller.DeviceEvent}'.");
                            Console.WriteLine($"Unable to process VIPA versions request from device - '{Controller.DeviceEvent}'.");
                            BuildSubworkflowErrorResponse(linkRequest, cardDevice.DeviceInformation, Controller.DeviceEvent);
                        }
                    }
                    else
                    {
                        UpdateRequestDeviceNotFound(linkRequest, deviceIdentifier);
                    }
                }

                Controller.SaveState(linkRequest);

                _ = Complete(this);
            }
        }
    }
}
