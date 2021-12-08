using Common.XO.Device;
using Common.XO.Requests;
using Devices.Core.Cancellation;
using Devices.Core.Helpers;
using Devices.Core.State.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal class DeviceGetSecurityConfigurationSubStateAction : DeviceBaseSubStateAction
    {
        public override DeviceSubWorkflowState WorkflowStateType => DeviceSubWorkflowState.GetSecurityConfiguration;

        public DeviceGetSecurityConfigurationSubStateAction(IDeviceSubStateController _) : base(_) { }

        public override SubStateActionLaunchRules LaunchRules => new SubStateActionLaunchRules
        {
            RequestCancellationToken = true
        };

        public override async Task DoWork()
        {
            if (StateObject is null)
            {
                //_ = Controller.LoggingClient.LogErrorAsync("Unable to find a state object while attempting to get security configuration.");
                Console.WriteLine("Unable to find a state object while attempting to get security configuration.");
                _ = Error(this);
            }
            else
            {
                LinkRequest linkRequest = StateObject as LinkRequest;
                IDeviceCancellationBroker cancellationBroker = Controller.GetDeviceCancellationBroker();

                List<LinkRequest> devicesRequest = new List<LinkRequest>();

                foreach (Common.Interfaces.ICardDevice device in Controller.TargetDevices)
                {
                    // Update device information
                    linkRequest.Actions[0].DeviceRequest.DeviceIdentifier
                        = new LinkDeviceIdentifier()
                        {
                            Manufacturer = device.DeviceInformation?.Manufacturer,
                            Model = device.DeviceInformation?.Model,
                            SerialNumber = device.DeviceInformation?.SerialNumber
                        };

                    devicesRequest.Add(JsonConvert.DeserializeObject<LinkRequest>(JsonConvert.SerializeObject(linkRequest)));

                    var timeoutPolicy = await cancellationBroker.ExecuteWithTimeoutAsync<LinkRequest>(
                        _ => device.GetSecurityConfiguration(devicesRequest.Last()),
                        DeviceConstants.CardCaptureTimeout,
                        System.Threading.CancellationToken.None);

                    if (timeoutPolicy.Outcome == Polly.OutcomeType.Failure)
                    {
                        //_ = Controller.LoggingClient.LogErrorAsync($"Unable to obtain security configuration - '{Controller.DeviceEvent}'.");
                        Console.WriteLine($"Unable to obtain security configuration - '{Controller.DeviceEvent}'.");
                        BuildSubworkflowErrorResponse(linkRequest, device.DeviceInformation, Controller.DeviceEvent);
                    }
                }

                /*if (linkRequest.LinkObjects.LinkActionResponseList[0].DALResponse == null)
                {
                    linkRequest.LinkObjects.LinkActionResponseList[0].DALResponse = new LinkDeviceResponse();
                }

                if (linkRequest.LinkObjects.LinkActionResponseList[0].DALResponse.Devices == null)
                {
                    linkRequest.LinkObjects.LinkActionResponseList[0].DALResponse.Devices = new List<LinkDeviceResponse>();
                }

                foreach (var response in devicesRequest)
                {
                    if (response.LinkObjects.LinkActionResponseList[0].DALResponse?.Devices != null)
                    {
                        linkRequest.LinkObjects.LinkActionResponseList[0].DALResponse.Devices.Add(new LinkDeviceResponse
                        {
                            Manufacturer = response.LinkObjects.LinkActionResponseList[0].DALResponse?.Devices[0].Manufacturer,
                            Model = response.LinkObjects.LinkActionResponseList[0].DALResponse?.Devices[0].Model,
                            SerialNumber = response.LinkObjects.LinkActionResponseList[0].DALResponse?.Devices[0].SerialNumber
                        });
                    }
                }*/

                Controller.SaveState(linkRequest);

                _ = Complete(this);
            }
        }
    }
}
