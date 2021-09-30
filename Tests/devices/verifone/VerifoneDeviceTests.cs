using Common.XO.Private;
using Common.XO.Requests;
using Common.XO.Responses;
using Devices.Common;
using Devices.Common.Helpers;
using Devices.Verifone.Connection;
using Devices.Verifone.Tests.Helpers;
using Devices.Verifone.VIPA.Interfaces;
using Moq;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TestHelper;
using Xunit;
using static Common.Execution.Modes;
using static Common.XO.Responses.LinkEventResponse;
using static Devices.Verifone.VIPA.VIPAImpl;
using static XO.ProtoBuf.LogMessage.Types;
using EventCodeType = Common.XO.Responses.LinkEventResponse.EventCodeType;

namespace Devices.Verifone.Tests
{
    public class VerifoneDeviceTests
    {
        readonly VerifoneDevice subject;

        readonly DeviceInformation deviceInformation;
        readonly SerialDeviceConfig serialConfig;

        Mock<IVipa> mockIVipa;

        List<EventCodeType> EventsSeen;
        List<string> MonitorMessagesSeen = new List<string>();
        List<LogLevel> DeviceLogEventSeen;

        public VerifoneDeviceTests()
        {
            mockIVipa = new Mock<IVipa>();

            serialConfig = new SerialDeviceConfig
            {
                CommPortName = "COM9"
            };

            deviceInformation = new DeviceInformation()
            {
                ComPort = "COM9",
                Manufacturer = "Simulator",
                Model = "SimCity",
                SerialNumber = "CEEEDEADBEEF",
                ProductIdentification = "SIMULATOR",
                VendorIdentifier = "BADDCACA"
            };

            subject = new VerifoneDevice();

            using (IKernel kernel = new StandardKernel())
            {
                kernel.Settings.InjectNonPublic = true;
                kernel.Settings.InjectParentPrivateProperties = true;

                kernel.Bind<IVipa>().ToConstant(mockIVipa.Object).WithConstructorArgument(deviceInformation);
                kernel.Inject(subject);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Probe_ReturnsProperActiveState_WhenCalled(bool expectedValue)
        {
            mockIVipa.Setup(e => e.Connect(It.IsAny<SerialConnection>(), It.IsAny<DeviceInformation>())).Returns(expectedValue);

            DeviceConfig deviceConfig = new DeviceConfig()
            {
                Valid = true
            };

            subject.Probe(deviceConfig, deviceInformation, out bool actualValue);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void GetStatus_ExpectedValue_WhenCalled()
        {
            LinkRequest expectedValue = new LinkRequest();
            var actualValue = subject.GetStatus(expectedValue);
            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [InlineData("1", true, EventCodeType.DEVICE_VERIFY_AMOUNT_APPROVED, 5000)]  //User selects "Yes"
        [InlineData("2", false, EventCodeType.DEVICE_VERIFY_AMOUNT_DECLINED, 5000)] //User selects "No"
        [InlineData("0", false, EventCodeType.REQUEST_TIMEOUT, 0)]                  //Request timeout
        public void SelectVerifyAmount_ShouldReturnUserSelection(string userSelection, bool expectedResult, EventCodeType expectedEvent, int timeout)
        {
            EventsSeen = new List<EventCodeType>();
            subject.PublishEvent += Subject_PublishEvent;

            LinkRequest linkRequest = RequestBuilder.LinkGetPaymentRequest();
            LinkActionRequest linkActionRequest = linkRequest.Actions.First();

            //(DeviceInfoObject deviceInfoObject, int VipaResult) linkDeviceResponseValue = SetDeviceVIPAInfo();
            //mockIVipa.Setup(e => e.GetDeviceInfo()).Returns(linkDeviceResponseValue);
            mockIVipa.Setup(e => e.DisplayMessage(It.IsAny<VIPADisplayMessageValue>(), true, It.IsAny<string>())).Returns(true);

            LinkDALRequestIPA5Object linkDALRequestIPA5Object = new LinkDALRequestIPA5Object()
            {
                DALResponseData = new LinkDALActionResponse()
                {
                    Value = userSelection
                }
            };

            //string requestedAmount = linkActionRequest.PaymentRequest.RequestedAmount.ToString();
            //var verifyAmountInfo = (linkDALRequestIPA5Object, (int)VipaSW1SW2Codes.Success);
            //mockIVipa.Setup(e => e.ProcessPaymentGetVerifyAmount(It.IsAny<LinkActionRequest>(), It.IsAny<string>())).Returns(verifyAmountInfo);

            //linkRequest.Actions[0].PaymentRequest.CardWorkflowControls.VerifyAmountEnabled = true;

            Helper.SetPropertyValueToInstance<IVipa>("VipaConnection", false, false, subject, mockIVipa.Object);
            //mockIVipa.Setup(e => e.Connect(It.IsAny<SerialConnection>(), currentDeviceInformation)).Returns(true);
            //mockIVipa.Setup(e => e.ResetDevice()).Returns(linkDeviceResponseValue);
            //Assert.Null(subject.Probe(deviceConfig, currentDeviceInformation, out bool active));
            //Assert.True(active);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout);
            LinkRequest linkResponse = subject.GetVerifyAmount(linkRequest, cancellationTokenSource.Token);

            if (timeout == 0)
            {
                Assert.NotNull(linkRequest.Actions[0].DALRequest.LinkObjects.DALResponseData.Errors);
                Assert.Equal(Enum.GetName(typeof(EventCodeType), EventCodeType.REQUEST_TIMEOUT), linkResponse.Actions.First().DALRequest.LinkObjects.DALResponseData.Errors[0].Code);
                Assert.Equal(StringValueAttribute.GetStringValue(DeviceEvent.RequestTimeout), linkResponse.Actions.First().DALRequest.LinkObjects.DALResponseData.Errors[0].Description);
            }
            else
            {
                Assert.True(EventWasHandled(expectedEvent));
                Assert.Null(linkRequest.LinkObjects.LinkActionResponseList[0].Errors);
                //Assert.Equal(1, linkRequest.LinkObjects.LinkActionResponseList[0].DALResponse.Devices?.Count);
            }

            bool actualResult = EventWasHandled(EventCodeType.DEVICE_VERIFY_AMOUNT_APPROVED);
            Assert.Equal(expectedResult, actualResult);

            if (timeout == 0)
            {
                Assert.NotNull(linkRequest.Actions[0].DALRequest.LinkObjects.DALResponseData.Errors);
                Assert.Equal(StringValueAttribute.GetStringValue(DeviceEvent.RequestTimeout), linkResponse.Actions.First().DALRequest.LinkObjects.DALResponseData.Errors[0].Description);
                return;
            }

            if (expectedResult)
            {
                Assert.Null(linkRequest.Actions[0].DALRequest.LinkObjects.DALResponseData);
            }
            else
            {
                Assert.NotNull(linkRequest.Actions[0].DALRequest.LinkObjects.DALResponseData.Errors);
                Assert.Equal(Enum.GetName(typeof(EventCodeType), EventCodeType.USER_CANCELED), linkResponse.Actions.First().DALRequest.LinkObjects.DALResponseData.Errors[0].Code);
                string expectedMessage = "Canceled";
                Assert.Equal(expectedMessage, linkResponse.Actions.First().DALRequest.LinkObjects.DALResponseData.Errors[0].Description);
            }
        }

        #region --- Event Testing Functions ---

        private void Subject_PublishEvent(EventTypeType eventType, EventCodeType eventCode, List<LinkDeviceResponse> devices, object request, string message)
        {
            if (EventsSeen == null)
                EventsSeen = new List<EventCodeType>();
            EventsSeen.Add(eventCode);
        }

        private void Subject_PublishMonitor(EventTypeType eventType, EventCodeType eventCode, List<LinkDeviceResponse> devices, object request, string message)
        {
            MonitorMessagesSeen.Add(message);
        }

        public bool EventWasHandled(EventCodeType eventCode)
        {
            return (EventsSeen == null) ? false : EventsSeen.Contains(eventCode);
        }

        public bool MonitorMessageWasHandled(string message)
        {
            return MonitorMessagesSeen.Contains(message);
        }

        #endregion --- Event Testing Functions ---
    }
}
