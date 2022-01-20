using Common.Execution;
using Common.XO.Device;
using Common.XO.Private;
using Common.XO.Requests;
using Common.XO.Responses;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Config;
using Devices.Common.Helpers;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.Tests.Helpers;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.Helpers;
using Devices.Verifone.VIPA.Interfaces;
using Moq;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TestHelper;
using Xunit;
using static Common.XO.Responses.LinkEventResponse;
using static Devices.Verifone.VIPA.VIPAImpl;
using static XO.ProtoBuf.LogMessage.Types;
using EventCodeType = Common.XO.Responses.LinkEventResponse.EventCodeType;

namespace Devices.Verifone.Tests
{
    public class VerifoneDeviceTests
    {
        readonly VerifoneDevice subject;

        readonly DeviceConfig deviceConfig;
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
                VendorIdentifier = "BADDCACA",
                FirmwareVersion = "1.2.3"
            };

            subject = new VerifoneDevice();

            deviceConfig = new DeviceConfig()
            {
                Valid = true,
                SupportedTransactions = new SupportedTransactions()
                {
                    EnableContactEMV = false,
                    EnableContactlessEMV = false,
                    EnableContactlessMSR = true,
                    ContactEMVConfigIsValid = true,
                    EMVKernelValidated = true
                }
            };
            Helper.SetFieldValueToInstance<DeviceConfig>("deviceConfiguration", false, false, subject, deviceConfig);

            subject.AppExecConfig = new Execution.AppExecConfig()
            {
                ExecutionMode = Modes.Execution.StandAlone
            };

            LinkDALRequestIPA5Object vipaVersions = new LinkDALRequestIPA5Object()
            {
                DALCdbData = new DALCDBData()
                {
                    VIPAVersion = new DALBundleVersioning() { DateCode = "1234" },
                    EMVVersion = new DALBundleVersioning() { DateCode = "1234" },
                    IdleVersion = new DALBundleVersioning() { DateCode = "1234" }
                }
            };
            Helper.SetPropertyValueToInstance<LinkDALRequestIPA5Object>("VipaVersions", false, false, subject, vipaVersions);

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
            mockIVipa.Setup(e => e.Connect(It.IsAny<VerifoneConnection>(), It.IsAny<DeviceInformation>())).Returns(expectedValue);

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

        [Theory]
        [InlineData(false, null, null, null)]
        [InlineData(true, null, null, null)]
        [InlineData(true, "sft", null, null)]
        [InlineData(true, "sft", "demo", null)]
        [InlineData(true, "sft", "demo", "demo123")]
        public void GetSecurityConfiguration_SetsupforSftpTransfer_WhenAppropriate(bool hasSftp, string hostname, string username, string password)
        {
            mockIVipa.Setup(e => e.Connect(It.IsAny<VerifoneConnection>(), deviceInformation)).Returns(true);
            Assert.True(SetVIPAMockConnectionForMockedDevice(mockIVipa.Object));

            DeviceSection deviceSection = new DeviceSection()
            {
                Verifone = new VerifoneSettings() { ConfigurationHostId = 0, ADEKeySetId = 8 }
            };
            Execution.AppExecConfig appExecConfig = new Execution.AppExecConfig()
            {
                ExecutionMode = Modes.Execution.StandAlone
            };
            subject.SetDeviceSectionConfig(deviceSection, appExecConfig, false);

            LinkRequest linkRequest = RequestBuilder.LinkGetPaymentRequest();
            LinkActionRequest linkActionRequest = linkRequest.Actions.First();

            linkActionRequest.DeviceRequest = new LinkDeviceRequest()
            {
                DeviceIdentifier = new LinkDeviceIdentifier()
                {
                    Manufacturer = "ACMEINC",
                    Model = "DOPE",
                    SerialNumber = "DEADBEEF"
                }
            };

            (DeviceInfoObject deviceInfoObject, int VipaResult) deviceIdentifier = SetDeviceVIPAInfo();
            mockIVipa.Setup(e => e.DeviceCommandReset()).Returns(deviceIdentifier);
            mockIVipa.Setup(e => e.GetSecurityConfiguration(It.IsAny<byte>(), It.IsAny<byte>())).Returns((new SecurityConfigurationObject(), (int)VipaSW1SW2Codes.Success));
            mockIVipa.Setup(e => e.GetTerminalDateTime()).Returns(("", (int)VipaSW1SW2Codes.Success));
            mockIVipa.Setup(e => e.Get24HourReboot()).Returns(("", (int)VipaSW1SW2Codes.Success));
            mockIVipa.Setup(e => e.ValidateConfiguration(It.IsAny<string>(), It.IsAny<bool>())).Returns((int)VipaSW1SW2Codes.Success);

            subject.VipaDevice.ConnectionConfiguration(null, DeviceEventHandlerStub, null);
            subject.DeviceEventOccured += DeviceEventHandlerStub;

            if (hasSftp)
            {
                subject.AppExecConfig.SftpConnectionParameters = new FileTransfer.SftpConnectionParameters()
                {
                    Hostname = hostname,
                    Username = username,
                    Password = password
                };
            }

            LinkRequest linkResponse = subject.GetSecurityConfiguration(linkRequest);

            if (hasSftp)
            {
                if (linkRequest.Actions.Count == 2 && linkRequest.Actions[1].Action == LinkAction.SftpTransfer)
                {
                    Assert.NotNull(linkRequest.Actions[1].SftpRequest);
                    Assert.Equal(hostname, linkRequest.Actions[1].SftpRequest.Hostname);
                    Assert.Equal(username, linkRequest.Actions[1].SftpRequest.Username);
                    Assert.Equal(password, linkRequest.Actions[1].SftpRequest.Password);
                }
            }

            Assert.Equal(DeviceEvent.ProgressBarActive, lastDeviceEvent);
        }

        #region --- Helper Methods ---
        private bool SetVIPAMockConnectionForMockedDevice(IVipa vipaDevice)
        {
            LinkDeviceResponse deviceInfo = new LinkDeviceResponse()
            {
                Manufacturer = deviceInformation.Manufacturer,
                Model = string.IsNullOrEmpty(deviceInformation.Model) ? "ANYMODEL" : deviceInformation.Model,
                SerialNumber = deviceInformation.SerialNumber,
                FirmwareVersion = "1.2.3.0"
            };
            DeviceInfoObject deviceInfoObject = new DeviceInfoObject()
            {
                LinkDeviceResponse = deviceInfo
            };
            (DeviceInfoObject deviceInfoObject, int VipaResult) expectedValue = (deviceInfoObject, (int)VipaSW1SW2Codes.Success);

            Helper.SetPropertyValueToInstance<IVipa>("VipaDevice", true, false, subject, vipaDevice);
            Helper.SetPropertyValueToInstance<IVipa>("VipaConnection", false, false, subject, vipaDevice);
            //mockIVipa.Setup(e => e.ResetDevice()).Returns(expectedValue);
            mockIVipa.Setup(e => e.GetDeviceHealth(It.IsAny<SupportedTransactions>())).Returns(expectedValue);
            //mockIVipa.Setup(e => e.GetDeviceInfo()).Returns(expectedValue);
            //mockIVipa.Setup(e => e.ConnectionConfiguration(It.IsAny<SerialDeviceConfig>(), It.IsAny<DeviceEventHandler>(), It.IsAny<DeviceLogHandler>()));
            mockIVipa.Setup(e => e.Connect(It.IsAny<VerifoneConnection>(), deviceInformation)).Returns(true);

            (DeviceInfoObject deviceInfoObject, int VipaResult) linkDeviceResponseValue = SetDeviceVIPAInfo();
            Assert.NotNull(linkDeviceResponseValue.deviceInfoObject);

            Assert.Null(subject.Probe(deviceConfig, deviceInformation, out bool active));
            return active;
        }

        private (DeviceInfoObject deviceInfoObject, int VipaResult) SetDeviceVIPAInfo()
        {
            LinkDeviceResponse deviceInfo = new LinkDeviceResponse()
            {
                Manufacturer = subject.ManufacturerConfigID,
                Model = subject.Name,
                SerialNumber = "DEADBEEF",
                FirmwareVersion = "1.2.3.4"
            };
            DeviceInfoObject deviceInfoObject = new DeviceInfoObject()
            {
                LinkDeviceResponse = deviceInfo
            };
            (DeviceInfoObject deviceInfoObject, int) linkDeviceResponseValue = (deviceInfoObject, (int)VipaSW1SW2Codes.Success);
            Helper.SetFieldValueToInstance<(DeviceInfoObject deviceInfoObject, int VipaResponse)>("deviceVIPAInfo", false, false, subject, linkDeviceResponseValue);
            return linkDeviceResponseValue;
        }
        #endregion --- Helper Methods ---

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

        private DeviceEvent lastDeviceEvent;

        internal object DeviceEventHandlerStub(DeviceEvent deviceEvent, DeviceInformation deviceInformation)
        {
            lastDeviceEvent = deviceEvent;
            return false;
        }

        #endregion --- Event Testing Functions ---
    }
}
