using Common.XO.Private;
using Common.XO.Requests;
using Devices.Core.State.Enums;
using Newtonsoft.Json;
using Servicer.Core.Action.Payment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal class DeviceRequestCompleteSubStateAction : DeviceBaseSubStateAction
    {
        public override bool WorkflowCutoff => true;

        public override DeviceSubWorkflowState WorkflowStateType => DeviceSubWorkflowState.RequestComplete;

        public DeviceRequestCompleteSubStateAction(IDeviceSubStateController _) : base(_) { }

        private void ReportEMVKernelVersions(LinkRequestIPA5Object linkResponse)
        {
            string EMVL2KernelVersion = linkResponse?.LinkActionResponseList[0]?.DALResponse?.Devices[0]?.EMVL2KernelVersion;
            string ContactlessKernelInformation = linkResponse?.LinkActionResponseList[0]?.DALResponse?.Devices[0]?.ContactlessKernelInformation;

            PaymentActionProcessor paymentActionProcessor = new PaymentActionProcessor(EMVL2KernelVersion, ContactlessKernelInformation);

            List<(string name, string aid)> cardBrands = new List<(string name, string aid)>
            {
                ("AMEX", "A00000002501"),
                ("MC C", "A0000000041010"),
                ("VISA", "A0000000031010"),
                ("MC D", "A0000000043060"),
                ("MC M", "A0000000042203"),
                ("V US", "A0000000980840"),
                ("V CR", "A0000000032010"),
                ("VS D", "A0000000033010"),
                ("DISC", "A0000001523010"),
                ("DISC", "A0000003241010"),
                ("JCB ", "A0000000651010")
            };

            Console.WriteLine("");

            foreach ((string name, string aid) cardbrand in cardBrands)
            {
                string kernelVersion = paymentActionProcessor.GetPaymentContactlessEMVKernelVersion(cardbrand.aid);
                Console.WriteLine($"{cardbrand.name} : [{cardbrand.aid.PadRight(14)}]: EMV KERNEL VERSION='{kernelVersion}'");
            }
        }

        private void ProcessCompletionActions(LinkRequest linkResponse)
        {
            if (linkResponse.Actions[0].DeviceActionRequest.DeviceAction == LinkDeviceActionType.ReportEMVKernelVersions)
            {
                ReportEMVKernelVersions(linkResponse.LinkObjects);
            }
        }

        private void DeviceCompleteAction(LinkRequest linkResponse)
        {
            string serializedResponse = JsonConvert.SerializeObject(linkResponse);

            //_ = Controller.LoggingClient.LogInfoAsync($"Request completed. Sending to Listener.");
            Debug.WriteLine("Request completed. Sending to Listener.");
            //_ = Controller.Connector.Publish(serializedResponse, new TopicOption[] { TopicOption.Servicer }).ConfigureAwait(false);

            // special case to report emv kernel versions: normally handled in Servicer
            ProcessCompletionActions(linkResponse);
        }

        public override Task DoWork()
        {
            if (StateObject != null)
            {
                LinkRequest linkResponse = StateObject as LinkRequest;

                if (linkResponse is { })
                {
                    DeviceCompleteAction(linkResponse);
                }
                else
                {
                    List<LinkRequest> linkRequests = StateObject as List<LinkRequest>;

                    foreach (LinkRequest request in linkRequests)
                    {
                        DeviceCompleteAction(request);
                    }
                }
            }
            else
            {
                // TODO: What should you do here in the event that you have no response?
                // TODO: Furthermore, this is a cutoff state action which means that you have
                // nowhere else to go when this completes.. This is the one point where you
                // get to decide whether or not we throw a Completion (Manage Path) or an 
                // Error (Device Recovery Path). I'll leave it to you to decide what you'll expect
                // to receive in the state object to make that determination.
                //
                // Perhaps, the state object can be different in order to illuminate the type
                // of flow you'll take going forward.
            }

            Complete(this);

            return Task.CompletedTask;
        }

        public override void RequestReceived(LinkRequest request)
        {
            base.RequestReceived(request);
        }
    }
}
