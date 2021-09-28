using Common.XO.Requests;
using Devices.Core.State.Enums;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal class DeviceRequestCompleteSubStateAction : DeviceBaseSubStateAction
    {
        public override bool WorkflowCutoff => true;

        public override DeviceSubWorkflowState WorkflowStateType => DeviceSubWorkflowState.RequestComplete;

        public DeviceRequestCompleteSubStateAction(IDeviceSubStateController _) : base(_) { }

        public override Task DoWork()
        {
            if (StateObject != null)
            {
                LinkRequest response = StateObject as LinkRequest;
                string serializedResponse = JsonConvert.SerializeObject(response);

                //_ = Controller.LoggingClient.LogInfoAsync($"Request completed. Sending to Listener.");
                Debug.WriteLine($"Request completed. Sending to Listener.");
                //_ = Controller.Connector.Publish(serializedResponse, new TopicOption[] { TopicOption.Servicer }).ConfigureAwait(false);
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
