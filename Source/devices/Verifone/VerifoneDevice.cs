using Common.Helpers;
using Common.LoggerManager;
using Common.XO.Device;
using Common.XO.Private;
using Common.XO.Requests;
using Common.XO.Responses;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Config;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.Helpers;
using Devices.Verifone.VIPA.Interfaces;
using Helpers;
using Ninject;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static Common.Execution.Modes;
using static Devices.Verifone.Helpers.HealthStatusCheckImpl;
using StringValueAttribute = Devices.Common.Helpers.StringValueAttribute;

namespace Devices.Verifone
{
    [Export(typeof(ICardDevice))]
    [Export("Verifone-M400", typeof(ICardDevice))]
    [Export("Verifone-P200", typeof(ICardDevice))]
    [Export("Verifone-P400", typeof(ICardDevice))]
    [Export("Verifone-UX300", typeof(ICardDevice))]
    internal class VerifoneDevice : IDisposable, ICardDevice
    {
        public string Name => StringValueAttribute.GetStringValue(DeviceType.Verifone);

        public event PublishEvent PublishEvent;
        public event DeviceEventHandler DeviceEventOccured;
        public event DeviceLogHandler DeviceLogHandler;

        private SerialConnection SerialConnection { get; set; }

        private bool IsConnected { get; set; }

        DeviceConfig deviceConfiguration;
        DeviceSection deviceSectionConfig;

        [Inject]
        internal IVipa VipaConnection { get; set; } = new VIPAImpl();

        public IVipa VipaDevice { get; private set; }

        public Execution ExecutionMode { get; set; } = Execution.Console;

        public string HealthCheckValidationMode { get; set; }

        public DeviceInformation DeviceInformation { get; private set; }

        public string ManufacturerConfigID => DeviceType.Verifone.ToString();

        public int SortOrder { get; set; } = -1;

        int ConfigurationHostId { get => deviceSectionConfig?.Verifone?.ConfigurationHostId ?? VerifoneSettingsSecurityConfiguration.ConfigurationHostId; }

        int OnlinePinKeySetId { get => deviceSectionConfig?.Verifone?.OnlinePinKeySetId ?? VerifoneSettingsSecurityConfiguration.OnlinePinKeySetId; }

        int ADEKeySetId { get => deviceSectionConfig?.Verifone?.ADEKeySetId ?? VerifoneSettingsSecurityConfiguration.ADEKeySetId; }

        string ConfigurationPackageActive { get => deviceSectionConfig?.Verifone?.ConfigurationPackageActive; }

        string SigningMethodActive { get; set; }

        string ActiveCustomerId { get => deviceSectionConfig?.Verifone?.ActiveCustomerId; }

        bool EnableHMAC { get; set; }

        LinkDALRequestIPA5Object VipaVersions { get; set; }

        public VerifoneDevice()
        {

        }

        public object Clone()
        {
            VerifoneDevice clonedObj = new VerifoneDevice();
            return clonedObj;
        }

        public void Dispose()
        {
            VipaConnection?.Dispose();
            IsConnected = false;
        }

        public void Disconnect()
        {
            SerialConnection?.Disconnect();
            IsConnected = false;
        }

        bool ICardDevice.IsConnected(object request)
        {
            return IsConnected;
        }

        private IVipa LocateDevice(LinkDeviceIdentifier deviceIdentifer)
        {
            // If we have single device connected to the work station
            if (deviceIdentifer == null)
            {
                return VipaConnection;
            }

            // get device serial number
            string deviceSerialNumber = DeviceInformation?.SerialNumber;

            if (string.IsNullOrEmpty(deviceSerialNumber))
            {
                // clear up any commands the device might be processing
                //VipaConnection.AbortCurrentCommand();

                //SetDeviceVipaInfo(VipaConnection, true);
                //deviceSerialNumber = deviceVIPAInfo.deviceInfoObject?.LinkDeviceResponse?.SerialNumber;
            }

            if (!string.IsNullOrWhiteSpace(deviceSerialNumber))
            {
                // does device serial number match LinkDeviceIdentifier serial number
                if (deviceSerialNumber.Equals(deviceIdentifer.SerialNumber, StringComparison.CurrentCultureIgnoreCase))
                {
                    return VipaConnection;
                }
                else
                {
                    //VipaConnection.DisplayMessage(VIPADisplayMessageValue.Idle);
                }
            }

            return VipaConnection;
        }

        private void GetBundleSignatures()
        {
            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        VipaVersions = VipaDevice.VIPAVersions(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model, EnableHMAC, ActiveCustomerId);
                    }

                    DeviceSetIdle();
                }
            }

        }

        #region --- execution modes ---

        private List<HealthStatusValidationRequired> GetHealthConfigurationHealthStatus()
        {
            List<HealthStatusValidationRequired> requiredChecks = new List<HealthStatusValidationRequired>();

            if (ExecutionMode == Execution.StandAlone)
            {
                string[] requirements = HealthCheckValidationMode.Split("|");

                foreach (string value in requirements)
                {
                    HealthStatusValidationRequired isRequired = HealthStatusCheckImpl.ValueIsRequired(value);
                    if (isRequired != HealthStatusValidationRequired.NOTREQUIRED)
                    {
                        requiredChecks.Add(isRequired);
                    }
                }
            }

            return requiredChecks;
        }

        private bool SetHealthCheckValidation((bool vipaResponseNotValid, bool emptyKSN) configTest, (bool vipaResponseNotValid, bool emptyKSN) configDebitPin)
        {
            bool status = true;

            List<HealthStatusValidationRequired> requiredChecks = GetHealthConfigurationHealthStatus();
            foreach (HealthStatusValidationRequired check in requiredChecks)
            {
                switch (check)
                {
                    case HealthStatusValidationRequired.ADETESTKEY:
                    {
                        if (configTest.vipaResponseNotValid || configTest.emptyKSN)
                        {
                            status = false;
                        }
                        break;
                    }
                    case HealthStatusValidationRequired.DEBITPINKEY:
                    {
                        if (configDebitPin.vipaResponseNotValid || configDebitPin.emptyKSN)
                        {
                            status = false;
                        }
                        break;
                    }
                }
            }

            return status;
        }

        private int ConsoleModeOutput((DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configProd,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configTest,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configDebitPin,
            (string Timestamp, int VipaResponse) terminalDateTime,
            (string Timestamp, int VipaResponse) reboot24Hour,
            (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) emvKernelInformation)
        {
            bool prodADEKeyFound = false;
            bool testADEKeyFound = false;

            // ADE PROD KEY
            if (configProd.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                Console.WriteLine($"DEVICE: FIRMARE VERSION ___: {deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                Console.WriteLine($"DEVICE: ADE-{configProd.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {configProd.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");

                if (configProd.securityConfigurationObject.SRedCardKSN != null)
                {
                    prodADEKeyFound = true;
                    Console.WriteLine($"DEVICE: ADE-{configProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {configProd.securityConfigurationObject.SRedCardKSN?.Substring(4, 6)}");
                    Console.WriteLine($"DEVICE: ADE-{configProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {configProd.securityConfigurationObject.SRedCardKSN?.Substring(10, 5)}");
                }
            }

            // ADE TEST KEY
            if (configTest.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                testADEKeyFound = true;
                Console.WriteLine($"DEVICE: ADE-{configTest.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {configTest.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");
                Console.WriteLine($"DEVICE: ADE-{configTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {configTest.securityConfigurationObject.SRedCardKSN?.Substring(4, 6) ?? "[ *** NOT FOUND *** ]"}");
                Console.WriteLine($"DEVICE: ADE-{configTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {configTest.securityConfigurationObject.SRedCardKSN?.Substring(10, 5) ?? "[ *** NOT FOUND *** ]"}");
            }
            Console.WriteLine($"DEVICE: ADE PROD KEY SLOT _: 0x0{deviceSectionConfig.Verifone.ADEKeySetId}");

            if (deviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdProd)
            {
                Console.WriteLine($"DEVICE: ADE PROD KEY SLOT  : {(prodADEKeyFound ? "VALID" : "*** INVALID ***")}");
            }
            else if (deviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdTest)
            {
                Console.WriteLine($"DEVICE: ADE TEST KEY SLOT  : {(testADEKeyFound ? "VALID" : "*** INVALID ***")}");
            }
            else
            {
                Console.WriteLine("DEVICE: ADE PROD KEY SLOT  : INVALID SLOT");
            }

            // DEBIT PIN KEY
            if (configDebitPin.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine($"DEVICE: DEBIT PIN KEY STORE: {(deviceSectionConfig.Verifone?.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.ConfigurationHostId ? "IPP" : "VSS")}");
                Console.WriteLine($"DEVICE: DEBIT PIN KEY SLOT : 0x0{(deviceSectionConfig.Verifone?.OnlinePinKeySetId)} - DUKPT{deviceSectionConfig.Verifone?.OnlinePinKeySetId - 1}");
                Console.WriteLine($"DEVICE: DEBIT PIN KSN _____: {configDebitPin.securityConfigurationObject.OnlinePinKSN ?? "[ *** NOT FOUND *** ]"}");
            }

            // TERMINAL TIMESTAMP
            if (terminalDateTime.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (string.IsNullOrEmpty(terminalDateTime.Timestamp))
                {
                    Console.WriteLine("DEVICE: TERMINAL DATETIME _: [ *** NOT FOUND *** ]");
                }
                else
                {
                    string terminalDateTimeStamp = string.Format("{1}/{2}/{0}-{3}:{4}:{5}",
                        terminalDateTime.Timestamp.Substring(0, 4), terminalDateTime.Timestamp.Substring(4, 2), terminalDateTime.Timestamp.Substring(6, 2),
                        terminalDateTime.Timestamp.Substring(8, 2), terminalDateTime.Timestamp.Substring(10, 2), terminalDateTime.Timestamp.Substring(12, 2));
                    Console.WriteLine($"DEVICE: TERMINAL DATETIME _: {terminalDateTimeStamp}");
                }
            }

            // TERMINAL 24 HOUR REBOOT
            if (reboot24Hour.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (string.IsNullOrEmpty(terminalDateTime.Timestamp))
                {
                    Console.WriteLine("DEVICE: TERMINAL DATETIME_: [ *** NOT SET *** ]");
                }
                else
                {
                    if (string.IsNullOrEmpty(reboot24Hour.Timestamp) || reboot24Hour.Timestamp.Length < 6)
                    {
                        Console.WriteLine($"DEVICE: 24 HOUR REBOOT ____: [ *** NOT SET *** ]");
                    }
                    else
                    {
                        string rebootDateTimeStamp = string.Format("{0}:{1}:{2}",
                            reboot24Hour.Timestamp.Substring(0, 2), reboot24Hour.Timestamp.Substring(2, 2), reboot24Hour.Timestamp.Substring(4, 2));
                        Console.WriteLine($"DEVICE: 24 HOUR REBOOT ____: {rebootDateTimeStamp}");
                    }
                }
            }

            // EMV KERNEL CHECKSUM
            if (emvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine($"DEVICE: EMV CONFIGURATION _: VALID");

                string[] kernelInformation = emvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                if (kernelInformation.Length == 4)
                {
                    Console.WriteLine(string.Format("DEVICE: EMV KERNEL CHECKSUM: {0}-{1}-{2}-{3}",
                       kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]));
                }
                else
                {
                    Console.WriteLine(string.Format("VIPA EMV KERNEL CHECKSUM __: {0}",
                        emvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation));
                }

                bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model.Substring(0, 4)));

                if (emvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET).Equals(IsEngageDevice ? BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM : BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("DEVICE: EMV KERNEL STATUS _: VALID");
                }
                else
                {
                    Console.WriteLine("DEVICE: EMV KERNEL STATUS _: INVALID");
                }
            }
            else
            {
                Console.WriteLine(string.Format("DEVICE: FAILED GET KERNEL CHECKSUM REQUEST WITH ERROR=0x{0:X4}\n", emvKernelInformation.VipaResponse));
            }

            // HMAC Secrets Test
            //(string HMAC, int VipaResponse) hmacConfig = VipaDevice.GenerateHMAC();
            //if (hmacConfig.VipaResponse == (int)VipaSW1SW2Codes.Success)
            //{
            //    Console.WriteLine($"DEVICE: HMAC KEYS GENERATED: {hmacConfig.HMAC}\n");
            //}

            // Bundle Versions
            if (VipaVersions.DALCdbData is { })
            {
                string signature = VipaVersions.DALCdbData.VIPAVersion.Signature?.ToUpper() ?? "MISSING";

                // VIPA BUNDLE
                string vipaDateCode = VipaVersions.DALCdbData.VIPAVersion.DateCode ?? "_NONE";

                // EMV CONFIG BUNDLE
                string emvDateCode = VipaVersions.DALCdbData.EMVVersion.DateCode ?? "_NONE";

                // IDLE IMAGE BUNDLE
                string idleDateCode = VipaVersions.DALCdbData.IdleVersion.DateCode ?? "_NONE";

                if (signature.Equals("VERIFONE", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"DEVICE: {signature} BUNDLE(S) : VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                }
                else
                {
                    Console.WriteLine($"DEVICE: {signature} BUNDLE(S) __: VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                }
            }

            Console.WriteLine("");

            return 0;
        }

        private int StandAloneModeOutput((DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configProd,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configTest,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configDebitPin,
            (string Timestamp, int VipaResponse) terminalDateTime,
            (string Timestamp, int VipaResponse) reboot24Hour,
            (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) emvKernelInformation)
        {
            bool status = SetHealthCheckValidation((configTest.VipaResponse != (int)VipaSW1SW2Codes.Success, string.IsNullOrEmpty(configTest.securityConfigurationObject.SRedCardKSN)),
                (configDebitPin.VipaResponse != (int)VipaSW1SW2Codes.Success, string.IsNullOrEmpty(configDebitPin.securityConfigurationObject.OnlinePinKSN)));

            StringBuilder healthStatus = new StringBuilder($"{deviceIdentifier.deviceInfoObject.LinkDeviceResponse.SerialNumber}");
            healthStatus.Append($"_{deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion.Replace(".", "")}");
            healthStatus.Append($"_{(status ? "PASS" : "FAIL")}");
            healthStatus.Append($"_{Utils.GetTimeStampToSeconds()}");

            Console.WriteLine(healthStatus);

            string fileName = healthStatus + ".txt";
            string fileDir = Directory.GetCurrentDirectory() + "\\logs";
            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }
            string filePath = Path.Combine(fileDir, fileName);

            // save to file
            using (StreamWriter streamWriter = new StreamWriter(filePath, append: true))
            {
                bool prodADEKeyFound = false;
                bool testADEKeyFound = false;

                // ADE PROD KEY
                if (configProd.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                    bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                    streamWriter.WriteLine($"DEVICE: FIRMARE VERSION ___: {deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                    streamWriter.WriteLine($"DEVICE: ADE-{configProd.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {configProd.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");

                    if (configProd.securityConfigurationObject.SRedCardKSN != null)
                    {
                        prodADEKeyFound = true;
                        streamWriter.WriteLine($"DEVICE: ADE-{configProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {configProd.securityConfigurationObject.SRedCardKSN?.Substring(4, 6)}");
                        streamWriter.WriteLine($"DEVICE: ADE-{configProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {configProd.securityConfigurationObject.SRedCardKSN?.Substring(10, 5)}");
                    }
                }

                // ADE TEST KEY
                if (configTest.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    testADEKeyFound = true;
                    streamWriter.WriteLine($"DEVICE: ADE-{configTest.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {configTest.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");
                    streamWriter.WriteLine($"DEVICE: ADE-{configTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {configTest.securityConfigurationObject.SRedCardKSN?.Substring(4, 6) ?? "[ *** NOT FOUND *** ]"}");
                    streamWriter.WriteLine($"DEVICE: ADE-{configTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {configTest.securityConfigurationObject.SRedCardKSN?.Substring(10, 5) ?? "[ *** NOT FOUND *** ]"}");
                }
                streamWriter.WriteLine($"DEVICE: ADE PROD KEY SLOT _: 0x0{deviceSectionConfig.Verifone.ADEKeySetId}");

                if (deviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdProd)
                {
                    streamWriter.WriteLine($"DEVICE: ADE PROD KEY SLOT  : {(prodADEKeyFound ? "VALID" : "*** INVALID ***")}");
                }
                else if (deviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdTest)
                {
                    streamWriter.WriteLine($"DEVICE: ADE TEST KEY SLOT  : {(testADEKeyFound ? "VALID" : "*** INVALID ***")}");
                }
                else
                {
                    streamWriter.WriteLine("DEVICE: ADE PROD KEY SLOT  : INVALID SLOT");
                }

                // DEBIT PIN KEY
                if (configDebitPin.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    streamWriter.WriteLine($"DEVICE: DEBIT PIN KEY STORE: {(deviceSectionConfig.Verifone?.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.ConfigurationHostId ? "IPP" : "VSS")}");
                    streamWriter.WriteLine($"DEVICE: DEBIT PIN KEY SLOT : 0x0{(deviceSectionConfig.Verifone?.OnlinePinKeySetId)} - DUKPT{deviceSectionConfig.Verifone?.OnlinePinKeySetId - 1}");
                    streamWriter.WriteLine($"DEVICE: DEBIT PIN KSN _____: {configDebitPin.securityConfigurationObject.OnlinePinKSN ?? "[ *** NOT FOUND *** ]"}");
                }

                // TERMINAL TIMESTAMP
                if (terminalDateTime.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    if (string.IsNullOrEmpty(terminalDateTime.Timestamp))
                    {
                        streamWriter.WriteLine("DEVICE: TERMINAL DATETIME _: [ *** NOT FOUND *** ]");
                    }
                    else
                    {
                        string terminalDateTimeStamp = string.Format("{1}/{2}/{0}-{3}:{4}:{5}",
                            terminalDateTime.Timestamp.Substring(0, 4), terminalDateTime.Timestamp.Substring(4, 2), terminalDateTime.Timestamp.Substring(6, 2),
                            terminalDateTime.Timestamp.Substring(8, 2), terminalDateTime.Timestamp.Substring(10, 2), terminalDateTime.Timestamp.Substring(12, 2));
                        streamWriter.WriteLine($"DEVICE: TERMINAL DATETIME _: {terminalDateTimeStamp}");
                    }
                }

                // TERMINAL 24 HOUR REBOOT
                if (reboot24Hour.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    if (string.IsNullOrEmpty(terminalDateTime.Timestamp))
                    {
                        streamWriter.WriteLine("DEVICE: TERMINAL DATETIME_: [ *** NOT SET *** ]");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(reboot24Hour.Timestamp) || reboot24Hour.Timestamp.Length < 6)
                        {
                            streamWriter.WriteLine($"DEVICE: 24 HOUR REBOOT ____: [ *** NOT SET *** ]");
                        }
                        else
                        {
                            string rebootDateTimeStamp = string.Format("{0}:{1}:{2}",
                                reboot24Hour.Timestamp.Substring(0, 2), reboot24Hour.Timestamp.Substring(2, 2), reboot24Hour.Timestamp.Substring(4, 2));
                            streamWriter.WriteLine($"DEVICE: 24 HOUR REBOOT ____: {rebootDateTimeStamp}");
                        }
                    }
                }

                // EMV KERNEL CHECKSUM
                if (emvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    streamWriter.WriteLine($"DEVICE: EMV CONFIGURATION _: VALID");

                    string[] kernelInformation = emvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                    if (kernelInformation.Length == 4)
                    {
                        streamWriter.WriteLine(string.Format("DEVICE: EMV KERNEL CHECKSUM: {0}-{1}-{2}-{3}",
                           kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]));
                    }
                    else
                    {
                        streamWriter.WriteLine(string.Format("VIPA EMV KERNEL CHECKSUM __: {0}",
                            emvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation));
                    }

                    bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model.Substring(0, 4)));

                    if (emvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET).Equals(IsEngageDevice ? BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM : BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM,
                        StringComparison.CurrentCultureIgnoreCase))
                    {
                        streamWriter.WriteLine("DEVICE: EMV KERNEL STATUS _: VALID");
                    }
                    else
                    {
                        streamWriter.WriteLine("DEVICE: EMV KERNEL STATUS _: INVALID");
                    }
                }
                else
                {
                    streamWriter.WriteLine(string.Format("DEVICE: FAILED GET KERNEL CHECKSUM REQUEST WITH ERROR=0x{0:X4}\n", emvKernelInformation.VipaResponse));
                }

                streamWriter.Close();
            }

            return 0;
        }

        private int ProcessHealthFromExectutionMode((DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configProd,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configTest,
            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configDebitPin,
            (string Timestamp, int VipaResponse) terminalDateTime,
            (string Timestamp, int VipaResponse) reboot24Hour,
            (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) emvKernelInformation) => ExecutionMode switch
            {
                Execution.Console => ConsoleModeOutput(deviceIdentifier, configProd, configTest, configDebitPin, terminalDateTime, reboot24Hour, emvKernelInformation),
                Execution.StandAlone => StandAloneModeOutput(deviceIdentifier, configProd, configTest, configDebitPin, terminalDateTime, reboot24Hour, emvKernelInformation),
                _ => throw new Exception("Undefined execution mode")
            };

        #endregion --- execution modes ----

        public void SetDeviceSectionConfig(DeviceSection config, Execution executionMode, string healthCheckValidationMode)
        {
            deviceSectionConfig = config;

            ExecutionMode = executionMode;
            HealthCheckValidationMode = healthCheckValidationMode;

            // BUNDLE Signatures
            GetBundleSignatures();

            SigningMethodActive = "UNSIGNED";

            if (VipaVersions.DALCdbData is { })
            {
                SigningMethodActive = VipaVersions.DALCdbData.VIPAVersion.Signature?.ToUpper() ?? "MISSING";
            }
            EnableHMAC = SigningMethodActive.Equals("SPHERE", StringComparison.CurrentCultureIgnoreCase) ? false : true;

            if (VipaConnection != null)
            {
                if (ExecutionMode == Execution.Console)
                {
                    Console.WriteLine($"\r\n\r\nACTIVE SIGNATURE _____: {SigningMethodActive.ToUpper()}");
                    Console.WriteLine($"ACTIVE CONFIGURATION _: {deviceSectionConfig.Verifone?.ConfigurationPackageActive}");
                    string onlinePINSource = deviceSectionConfig.Verifone?.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.DUKPTEngineIPP ? "IPP" : "VSS";
                    Console.WriteLine($"ONLINE DEBIT PIN STORE: {onlinePINSource}");
                    Console.WriteLine($"HMAC ENABLEMENT ACTIVE: {EnableHMAC.ToString().ToUpper()}");
                    Console.WriteLine("");
                }
                VipaConnection.LoadDeviceSectionConfig(deviceSectionConfig);
            }
        }

        public List<LinkErrorValue> Probe(DeviceConfig config, DeviceInformation deviceInfo, out bool active)
        {
            DeviceInformation = deviceInfo;
            DeviceInformation.Manufacturer = ManufacturerConfigID;
            DeviceInformation.ComPort = deviceInfo.ComPort;

            SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
            active = IsConnected = VipaConnection.Connect(SerialConnection, DeviceInformation);

            if (active)
            {
                (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaConnection.DeviceCommandReset();

                if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    // check for power on notification: reissue reset command to obtain device information
                    if (deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification != null)
                    {
                        Console.WriteLine($"\nDEVICE EVENT: Terminal ID={deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification?.TerminalID}," +
                            $" EVENT='{deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification?.TransactionStatusMessage}'");

                        deviceIdentifier = VipaConnection.DeviceCommandReset();

                        if (deviceIdentifier.VipaResponse != (int)VipaSW1SW2Codes.Success)
                        {
                            return null;
                        }
                    }

                    if (DeviceInformation != null)
                    {
                        DeviceInformation.Manufacturer = ManufacturerConfigID;
                        DeviceInformation.Model = deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model;
                        DeviceInformation.SerialNumber = deviceIdentifier.deviceInfoObject.LinkDeviceResponse.SerialNumber;
                        DeviceInformation.FirmwareVersion = deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion;
                    }
                    VipaDevice = VipaConnection;
                    deviceConfiguration = config;
                    active = true;

                    //Console.WriteLine($"\nDEVICE PROBE SUCCESS ON {DeviceInformation?.ComPort}, FOR SN: {DeviceInformation?.SerialNumber}");
                }
                else
                {
                    //VipaDevice.CancelResponseHandlers();
                    //Console.WriteLine($"\nDEVICE PROBE FAILED ON {DeviceInformation?.ComPort}\n");
                }
            }
            return null;
        }

        public List<DeviceInformation> DiscoverDevices()
        {
            List<DeviceInformation> deviceInformation = new List<DeviceInformation>();
            Connection.DeviceDiscovery deviceDiscovery = new Connection.DeviceDiscovery();
            if (deviceDiscovery.FindVerifoneDevices())
            {
                foreach (var device in deviceDiscovery.deviceInfo)
                {
                    if (string.IsNullOrEmpty(device.ProductID) || string.IsNullOrEmpty(device.SerialNumber))
                        throw new Exception("The connected device's PID or SerialNumber did not match with the expected values!");

                    deviceInformation.Add(new DeviceInformation()
                    {
                        ComPort = device.ComPort,
                        ProductIdentification = device.ProductID,
                        SerialNumber = device.SerialNumber,
                        VendorIdentifier = Connection.DeviceDiscovery.VID
                    });

                    System.Diagnostics.Debug.WriteLine($"device: ON PORT={device.ComPort} - VERIFONE MODEL={deviceInformation[deviceInformation.Count - 1].ProductIdentification}, " +
                        $"SN=[{deviceInformation[deviceInformation.Count - 1].SerialNumber}], PORT={deviceInformation[deviceInformation.Count - 1].ComPort}");
                }
            }

            // validate COMM Port
            if (!deviceDiscovery.deviceInfo.Any() || deviceDiscovery.deviceInfo[0].ComPort == null || !deviceDiscovery.deviceInfo[0].ComPort.Any())
            {
                return null;
            }

            return deviceInformation;
        }

        public void DeviceSetIdle()
        {
            //Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: SET TO IDLE.");
            if (VipaDevice != null)
            {
                VipaDevice.DisplayMessage(VIPAImpl.VIPADisplayMessageValue.Idle);
            }
        }

        public bool DeviceRecovery()
        {
            Console.WriteLine($"DEVICE: ON PORT={DeviceInformation.ComPort} - DEVICE-RECOVERY");
            return false;
        }

        public List<LinkRequest> GetDeviceResponse(LinkRequest deviceInfo)
        {
            throw new NotImplementedException();
        }

        public LinkRequest GetVerifyAmount(LinkRequest request, CancellationToken cancellationToken)
        {
            LinkActionRequest linkActionRequest = request.Actions.First();
            //IVIPADevice device = LocateDevice(linkActionRequest?.DALRequest?.DeviceIdentifier);
            IVipa device = VipaDevice;

            if (device != null)
            {
                //SelectVerifyAmount(device, request, linkActionRequest, cancellationToken);
                DisplayCustomScreen(request);
            }

            return request;
        }

        public string AmountToDollar(string amount)
        {
            if (amount == null)
            {
                return null;
            }

            string dollarAmount = string.Format("{0:#0.00}", Convert.ToDecimal(amount) / 100);

            return dollarAmount;
        }

        // ------------------------------------------------------------------------
        // Methods that are mapped for usage in their respective sub-workflows.
        // ------------------------------------------------------------------------
        #region --- subworkflow mapping
        public LinkRequest GetStatus(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET STATUS for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");
            return linkRequest;
        }

        public LinkRequest GetActiveKeySlot(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET ACTIVE SLOT for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (int VipaResult, int VipaResponse) response = VipaDevice.GetActiveKeySlot();
                        if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: VIPA ACTIVE ADE KEY SLOT={response.VipaResult}\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED GET ACTIVE SLOT REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest GetEMVKernelChecksum(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET KERNEL CHECKSUM for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) response = VipaDevice.GetEMVKernelChecksum();
                        if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            string[] kernelInformation = response.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                            if (kernelInformation.Length == 4)
                            {
                                Console.WriteLine(string.Format("VIPA KERNEL CHECKSUM={0}-{1}-{2}-{3}",
                                   kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]));
                            }
                            else
                            {
                                Console.WriteLine(string.Format("VIPA KERNEL CHECKSUM={0}",
                                    response.kernelConfigurationObject.ApplicationKernelInformation));
                            }

                            bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model.Substring(0, 4)));

                            if (response.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET).Equals(IsEngageDevice ? BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM : BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM,
                                StringComparison.CurrentCultureIgnoreCase))
                            {
                                Console.WriteLine("VIPA EMV KERNEL VALIDATED");
                            }
                            else
                            {
                                Console.WriteLine("VIPA EMV KERNEL IS INVALID");
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED GET KERNEL CHECKSUM REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest GetSecurityConfiguration(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();

            if (ExecutionMode == Execution.Console)
            {
                Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET SECURITY CONFIGURATION for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");
            }

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        TimeTracker tracker = new TimeTracker();

                        // ADE PROD KEY
                        tracker.StartTracking();
                        (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configProd =
                            VipaDevice.GetSecurityConfiguration(deviceSectionConfig.Verifone.ConfigurationHostId, DeviceInformation.ADEKeySetId);
                        TimeSpan span = tracker.GetTimeLapsed();

                        if (configProd.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Logger.info(string.Format("DEVICE: ADE-PROD KEY READ TIME _____ : [{0:D2}:{1:D2}.{2:D3}]", span.Minutes, span.Seconds, span.Milliseconds));
                        }

                        // ADE TEST KEY
                        tracker.StartTracking();
                        (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configTest =
                            VipaDevice.GetSecurityConfiguration(deviceSectionConfig.Verifone.ConfigurationHostId, configProd.securityConfigurationObject.ADETestSlot);

                        span = tracker.GetTimeLapsed();
                        Logger.info(string.Format("DEVICE: ADE-PROD KEY READ TIME _____ : [{0:D2}:{1:D2}.{2:D3}]", span.Minutes, span.Seconds, span.Milliseconds));

                        tracker.StartTracking();
                        (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) configDebitPin =
                            VipaDevice.GetSecurityConfiguration(deviceSectionConfig.Verifone.ConfigurationHostId, deviceSectionConfig.Verifone.OnlinePinKeySetId);

                        span = tracker.GetTimeLapsed();
                        Logger.info(string.Format("DEVICE: DEBIT PIN KEY READ TIME ____ : [{0:D2}:{1:D2}.{2:D3}]", span.Minutes, span.Seconds, span.Milliseconds));

                        // Terminal datetime
                        tracker.StartTracking();
                        (string Timestamp, int VipaResponse) terminalDateTime = VipaDevice.GetTerminalDateTime();

                        span = tracker.GetTimeLapsed();
                        Logger.info(string.Format("DEVICE: TERMINAL DATETIME READ TIME  : [{0:D2}:{1:D2}.{2:D3}]", span.Minutes, span.Seconds, span.Milliseconds));

                        // 24 HOUR REBOOT
                        tracker.StartTracking();
                        (string Timestamp, int VipaResponse) reboot24Hour = VipaDevice.Get24HourReboot();

                        span = tracker.GetTimeLapsed();
                        Logger.info(string.Format("DEVICE: 24 HOUR REBOOT READ TIME ___ : [{0:D2}:{1:D2}.{2:D3}]", span.Minutes, span.Seconds, span.Milliseconds));

                        // validate configuration
                        tracker.StartTracking();

                        // EMV KERNEL CHECKSUM
                        int vipaResponse = VipaDevice.ValidateConfiguration(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model, SigningMethodActive.Equals("SPHERE"));
                        (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) emvKernelInformation = (null, (int)VipaSW1SW2Codes.Failure);

                        span = tracker.GetTimeLapsed();
                        Logger.info(string.Format("DEVICE: BUNDLE SIGNATURE(S) READ TIME: [{0:D2}:{1:D2}.{2:D3}]", span.Minutes, span.Seconds, span.Milliseconds));

                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            // EMV Kernel Validation
                            emvKernelInformation = VipaDevice.GetEMVKernelChecksum();
                        }

                        // LOG IT
                        ProcessHealthFromExectutionMode(deviceIdentifier, configProd, configTest, configDebitPin, terminalDateTime, reboot24Hour, emvKernelInformation);
                    }
                    DeviceSetIdle();
                }
            }

            return linkRequest;
        }

        public LinkRequest Configuration(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: CONFIGURATION for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, null);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = (int)VipaSW1SW2Codes.Failure;

                        bool activePackageIsEpic = ConfigurationPackageActive.Equals("EPIC");
                        bool activePackageIsNJT = ConfigurationPackageActive.Equals("NJT");
                        bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                        bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                        //TODO: REFACTOR
                        if (activePackageIsEpic || activePackageIsNJT)
                        {
                            vipaResponse = VipaDevice.LockDeviceConfiguration0(activePackageIsEpic, activeSigningMethodIsSphere);
                        }
                        // TTQ MSD ONLY
                        //else if (activePackageIsNJT)
                        //{
                        //    vipaResponse = VipaDevice.ConfigurationPackage(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model, activeSigningMethodIsSphere);
                        //}
                        else
                        {
                            Console.WriteLine($"DEVICE: INVALID CONFIGURATION {ConfigurationPackageActive}\n");
                        }

                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION UPDATED SUCCESSFULLY\n");

                            (DevicePTID devicePTID, int VipaResponse) response = (null, (int)VipaSW1SW2Codes.Success);

                            Console.Write("DEVICE: REQUESTING DEVICE REBOOT...");
                            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceCommandReset();

                            if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine("SUCCESS!");
                            }
                            else
                            {
                                Console.WriteLine("FAILURE - PLEASE REBOOT DEVICE!");
                            }
                            response = VipaDevice.DeviceReboot();

                            if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                // TTQ MSD ONLY
                                //if (activePackageIsNJT)
                                //{
                                //    Console.WriteLine($"DEVICE: REBOOT REQUEST SUCCESSFUL for ID={response.devicePTID.PTID}, SN={response.devicePTID?.SerialNumber}\n");
                                //}
                            }
                            else
                            {
                                Console.WriteLine(string.Format("DEVICE: FAILED REBOOT REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest FeatureEnablementToken(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: FEATURE ENABLEMENT TOKEN for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.FeatureEnablementToken();
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: FET UPDATED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED FET REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest LockDeviceConfiguration0(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: LOCK DEVICE CONFIGURATION 0 for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        bool activePackageIsEpic = ConfigurationPackageActive.Equals("EPIC");
                        bool activePackageIsNJT = ConfigurationPackageActive.Equals("NJT");
                        bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                        bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                        int vipaResponse = VipaDevice.LockDeviceConfiguration0(activePackageIsEpic, activeSigningMethodIsSphere);

                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION LOCKED SUCCESSFULLY\n");

                            Console.Write("DEVICE: REQUESTING DEVICE REBOOT...");
                            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceCommandReset();

                            if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine("SUCCESS!");
                            }
                            else
                            {
                                Console.WriteLine("FAILURE - PLEASE REBOOT DEVICE!");
                            }
                            VipaDevice.DeviceReboot();
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED LOCK CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest LockDeviceConfiguration8(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: LOCK DEVICE CONFIGURATION 8 for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        bool activePackageIsEpic = ConfigurationPackageActive.Equals("EPIC");
                        bool activePackageIsNJT = ConfigurationPackageActive.Equals("NJT");
                        bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                        bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                        int vipaResponse = VipaDevice.LockDeviceConfiguration8(activePackageIsEpic, activeSigningMethodIsSphere);

                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION LOCKED SUCCESSFULLY\n");

                            Console.Write("DEVICE: REQUESTING DEVICE REBOOT...");
                            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceCommandReset();

                            if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine("SUCCESS!");
                            }
                            else
                            {
                                Console.WriteLine("FAILURE - PLEASE REBOOT DEVICE!");
                            }
                            VipaDevice.DeviceReboot();
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED LOCK CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest UnlockDeviceConfiguration(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: UNLOCK DEVICE CONFIGURATION for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.UnlockDeviceConfiguration();
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION UNLOCKED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED UNLOCK CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest AbortCommand(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE: ABORT COMMAND for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");
            return linkRequest;
        }

        public LinkRequest VIPARestart(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: VIPA RESTART with SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) response = VipaDevice.VIPARestart();

                    if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        Console.WriteLine($"DEVICE: VIPA RESTART REQUEST RECEIVED SUCCESSFULLY");
                    }
                    else
                    {
                        Console.WriteLine(string.Format("DEVICE: FAILED VIPA RESTART REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest ResetDevice(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE: RESET DEVICE for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");
            return linkRequest;
        }

        public LinkRequest RebootDevice(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: REBOOT DEVICE with SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (DevicePTID devicePTID, int VipaResponse) response = VipaDevice.DeviceReboot();
                        if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            //Console.WriteLine($"DEVICE: REBOOT SUCCESSFULLY for ID={response.devicePTID.PTID}, SN={response.devicePTID.SerialNumber}\n");
                            Console.WriteLine($"DEVICE: REBOOT REQUEST RECEIVED SUCCESSFULLY");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED REBOOT REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest DeviceExtendedReset(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: DEVICE EXTENDED RESET with SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) response = VipaDevice.DeviceExtendedReset();

                    if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        Console.WriteLine($"DEVICE: EXTENDED RESET REQUEST RECEIVED SUCCESSFULLY");
                    }
                    else
                    {
                        Console.WriteLine(string.Format("DEVICE: FAILED EXTENDED RESET REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest UpdateHMACKeys(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: UPDATE HMAC KEYS for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.UpdateHMACKeys(0, "");
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: HMAC KEYS UPDATED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED HMAC KEYS UPDATE WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                        DeviceSetIdle();
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest GenerateHMAC(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GENERATE HMAC for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (string HMAC, int VipaResponse) config = VipaDevice.GenerateHMAC();
                        if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: HMAC={config.HMAC}\n");
                        }
                        DeviceSetIdle();
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest UpdateIdleScreen(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: UPDATE IDLE SCREEN for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                        bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                        try
                        {
                            int vipaResponse = VipaDevice.UpdateIdleScreen(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model, activeSigningMethodIsSphere, ActiveCustomerId);

                            if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine($"DEVICE: IDLE SCREEN CONFIGURATION UPDATED SUCCESSFULLY\n");

                                // TGZ files require REBOOT
                                //Console.Write("DEVICE: RELOADING CONFIGURATION...");
                                //(DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceExtendedReset();

                                //if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                                //{
                                //    Console.WriteLine("SUCCESS!");
                                //}

                                Console.Write("DEVICE: REQUESTING DEVICE REBOOT...");
                                (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceCommandReset();

                                if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                                {
                                    Console.WriteLine("SUCCESS!");
                                }
                                else
                                {
                                    Console.WriteLine("FAILURE - PLEASE REBOOT DEVICE!");
                                }
                            }
                            else if (vipaResponse == (int)VipaSW1SW2Codes.DeviceNotSupported)
                            {
                                Console.WriteLine(string.Format("DEVICE: UNSUPPORTED DEVICE ERROR=0x{0:X4}\n", vipaResponse));
                            }
                            else
                            {
                                Console.WriteLine(string.Format("DEVICE: FAILED IDLE SCREEN CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"UPDATE IDLE SCREEN COMMAND ERROR=[{e.Message}]");
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest DisplayCustomScreen(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: DISPLAY CUSTOM SCREEN for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        long amount = 9999999;
                        string requestedAmount = amount.ToString();

                        // must contain 5 elements: "title|item 1|item 2|item 3|total"
                        // use '-' for vertical spacing when items 2/3 do not exist, otherwise use a space or leave the item field blank
                        string displayMessage = $"VERIFY AMOUNT|item 1 ..... ${AmountToDollar(requestedAmount)}|-| |Total ...... ${AmountToDollar(requestedAmount)}";

                        (LinkDALRequestIPA5Object LinkActionRequestIPA5Object, int VipaResponse) verifyAmountResponse = VipaDevice.DisplayCustomScreenHTML(displayMessage);

                        if (verifyAmountResponse.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine("DEVICE: CUSTOM SCREEN EXECUTED SUCCESSFULLY - RESPONSE={0}\n", verifyAmountResponse.LinkActionRequestIPA5Object.DALResponseData.Value.Equals("1", StringComparison.OrdinalIgnoreCase) ? "YES" : "NO");
                        }
                        else if (verifyAmountResponse.VipaResponse == (int)VipaSW1SW2Codes.DeviceNotSupported)
                        {
                            Console.WriteLine(string.Format("DEVICE: UNSUPPORTED DEVICE ERROR=0x{0:X4}\n", verifyAmountResponse.VipaResponse));
                        }
                        else if (verifyAmountResponse.VipaResponse == (int)VipaSW1SW2Codes.FileNotFoundOrNotAccessible)
                        {
                            displayMessage = $"VERIFY AMOUNT|Total.....${AmountToDollar(requestedAmount)}|YES|NO";
                            verifyAmountResponse = VipaDevice.DisplayCustomScreen(displayMessage);

                            if (verifyAmountResponse.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine("DEVICE: CUSTOM SCREEN EXECUTED SUCCESSFULLY - RESPONSE={0}\n", verifyAmountResponse.LinkActionRequestIPA5Object.DALResponseData.Value.Equals("1", StringComparison.OrdinalIgnoreCase) ? "YES" : "NO");
                            }
                            else if (verifyAmountResponse.VipaResponse == (int)VipaSW1SW2Codes.DeviceNotSupported)
                            {
                                Console.WriteLine(string.Format("DEVICE: UNSUPPORTED DEVICE ERROR=0x{0:X4}\n", verifyAmountResponse.VipaResponse));
                            }
                            else
                            {
                                Console.WriteLine(string.Format("DEVICE: FAILED DISPLAY CUSTOM SCREEN REQUEST WITH ERROR=0x{0:X4}\n", verifyAmountResponse.VipaResponse));
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED DISPLAY CUSTOM SCREEN REQUEST WITH ERROR=0x{0:X4}\n", verifyAmountResponse.VipaResponse));
                        }

                        /*(LinkDALRequestIPA5Object LinkActionRequestIPA5Object, int VipaResponse) verifyAmountResponse = VipaDevice.DisplayCustomScreen(displayMessage);

                        if (verifyAmountResponse.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine("DEVICE: CUSTOM SCREEN EXECUTED SUCCESSFULLY - RESPONSE={0}\n", verifyAmountResponse.LinkActionRequestIPA5Object.DALResponseData.Value.Equals("1", StringComparison.OrdinalIgnoreCase) ? "YES" : "NO");
                        }
                        else if (verifyAmountResponse.VipaResponse == (int)VipaSW1SW2Codes.DeviceNotSupported)
                        {
                            Console.WriteLine(string.Format("DEVICE: UNSUPPORTED DEVICE ERROR=0x{0:X4}\n", verifyAmountResponse.VipaResponse));
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED DISPLAY CUSTOM SCREEN REQUEST WITH ERROR=0x{0:X4}\n", verifyAmountResponse.VipaResponse));
                        }*/
                    }
                }
            }

            DeviceSetIdle();

            return linkRequest;
        }

        public LinkRequest Reboot24Hour(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: 24 HOUR REBOOT for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        string timestamp = linkRequest.Actions.First().DeviceActionRequest.Reboot24Hour;
                        (string Timestamp, int VipaResponse) deviceResponse = VipaDevice.Reboot24Hour(timestamp);
                        if (deviceResponse.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            if (timestamp.Equals(deviceResponse.Timestamp))
                            {
                                //Console.Write("DEVICE: RELOADING CONFIGURATION...");
                                //(DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceExtendedReset();

                                //if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                                //{
                                //    Console.WriteLine("SUCCESS!");
                                //}
                                //else
                                //{
                                //    Console.WriteLine("FAILURE - PLEASE REBOOT DEVICE!");
                                //}
                                Console.Write("DEVICE: REQUESTING DEVICE REBOOT...");
                                (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceCommandReset();

                                if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                                {
                                    Console.WriteLine("SUCCESS!");
                                }
                                else
                                {
                                    Console.WriteLine("FAILURE - PLEASE REBOOT DEVICE!");
                                }
                                VipaDevice.DeviceReboot();
                            }
                            else
                            {
                                Console.WriteLine($"FAILURE - INCORRECT TIMESTAMP SET: [{deviceResponse.Timestamp}]");
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED 24 HOUR REBOOT REQUEST WITH ERROR=0x{0:X4}\n", deviceResponse.VipaResponse));
                        }
                    }
                }
            }

            DeviceSetIdle();

            return linkRequest;
        }

        public LinkRequest SetTerminalDateTime(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: 24 HOUR REBOOT for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        string timestamp = linkRequest.Actions.First().DeviceActionRequest.TerminalDateTime;
                        (string Timestamp, int VipaResponse) deviceResponse = VipaDevice.SetTerminalDateTime(timestamp);
                        if (deviceResponse.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            // ignore seconds in timestamp comparison
                            if (timestamp.Substring(0, 12).Equals(deviceResponse.Timestamp.Substring(0, 12)))
                            {
                                Console.WriteLine($"SUCCESS -TIMESTAMP SET: [{deviceResponse.Timestamp}]");
                            }
                            else
                            {
                                Console.WriteLine($"FAILURE - INCORRECT TIMESTAMP SET: [{deviceResponse.Timestamp}]");
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED 24 HOUR REBOOT REQUEST WITH ERROR=0x{0:X4}\n", deviceResponse.VipaResponse));
                        }
                    }
                }
            }

            DeviceSetIdle();

            return linkRequest;
        }

        public LinkRequest VIPAVersions(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: VIPA VERSIONS with SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (VipaDevice != null)
            {
                if (!IsConnected)
                {
                    VipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation, DeviceLogHandler);
                    IsConnected = VipaDevice.Connect(SerialConnection, DeviceInformation);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        //bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                        //bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                        VipaVersions = VipaDevice.VIPAVersions(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model, EnableHMAC, ActiveCustomerId);

                        if (VipaVersions.DALCdbData is { })
                        {
                            // VIPA BUNDLE
                            Console.WriteLine($"DEVICE: {VipaVersions.DALCdbData.VIPAVersion.Signature?.ToUpper() ?? "MISSING"} SIGNED BUNDLE: VIPA_VER DATECODE {VipaVersions.DALCdbData.VIPAVersion.DateCode ?? "*** NONE ***"} - BUNDLE VER: [{VipaVersions.DALCdbData.VIPAVersion.Version}]");
                            if (!string.IsNullOrEmpty(VipaVersions.DALCdbData.VIPAVersion?.Version) &&
                                !DeviceInformation.FirmwareVersion.Equals(VipaVersions.DALCdbData.VIPAVersion.Version.Replace("_", ".")))
                            {
                                Console.WriteLine($"!!!!!!: VERSION MISMATCHED - EXPECTED [{DeviceInformation.FirmwareVersion}], " +
                                    $"REPORTED [{VipaVersions.DALCdbData.VIPAVersion.Version.Replace("_", ".")}]");
                                Logger.error($"VIPA_VER.TXT: VERSION MISMATCHED - EXPECTED [{DeviceInformation.FirmwareVersion}], " +
                                    $"REPORTED [{VipaVersions.DALCdbData.VIPAVersion.Version.Replace("_", ".")}]");
                            }

                            // EMV CONFIG BUNDLE
                            Console.WriteLine($"DEVICE: {VipaVersions.DALCdbData.EMVVersion.Signature?.ToUpper() ?? "MISSING"} SIGNED BUNDLE: EMV_VER DATECODE  {VipaVersions.DALCdbData.EMVVersion.DateCode ?? "*** NONE ***"} - BUNDLE VER: [{VipaVersions.DALCdbData.EMVVersion.Version}]");
                            if (!string.IsNullOrEmpty(VipaVersions.DALCdbData.EMVVersion?.Version) &&
                                !DeviceInformation.FirmwareVersion.Equals(VipaVersions.DALCdbData.EMVVersion?.Version?.Replace("_", ".")))
                            {
                                Console.WriteLine($"!!!!!!: VERSION MISMATCHED - EXPECTED [{DeviceInformation.FirmwareVersion}], " +
                                    $"REPORTED [{VipaVersions.DALCdbData.EMVVersion.Version.Replace("_", ".")}]");
                                Logger.error($"EMV_VER.TXT: VERSION MISMATCHED - EXPECTED [{DeviceInformation.FirmwareVersion}], " +
                                    $"REPORTED [{VipaVersions.DALCdbData.EMVVersion.Version.Replace("_", ".")}]");
                            }

                            // IDLE IMAGE BUNDLE
                            Console.WriteLine($"DEVICE: {VipaVersions.DALCdbData.IdleVersion.Signature?.ToUpper() ?? "MISSING"} SIGNED BUNDLE: IDLE_VER DATECODE {VipaVersions.DALCdbData.IdleVersion.DateCode ?? "*** NONE ***"} - BUNDLE VER: [{VipaVersions.DALCdbData.IdleVersion.Version}]");
                        }
                    }
                }
            }

            return linkRequest;
        }

        #endregion --- subworkflow mapping
    }
}
