using Common.Execution;
using Common.Helpers;
using Common.LoggerManager;
using Common.XO.Private;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Config;
using Devices.Common.Helpers;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.Helpers;
using Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ExtensionMethods;

namespace Devices.Verifone.Helpers
{
    public class HealthStatusCheckImpl
    {
        #region --- attributes ---
        const string Device24HourReboot = "07:00:00";

        enum HealthStatusValidationRequired
        {
            [StringValue("NOTREQUIRED")]
            NOTREQUIRED,
            [StringValue("ADETESTKEY")]
            ADETESTKEY,
            [StringValue("DEBITPINKEY")]
            DEBITPINKEY,
        }

        public event DeviceEventHandler DeviceEventOccured;

        public DeviceInformation DeviceInformation { get; set; }
        public AppExecConfig AppExecConfig { get; set; }
        public string SigningMethodActive { get; set; }
        public DeviceSection DeviceSectionConfig { get; set; }
        public LinkDALRequestIPA5Object VipaVersions { get; set; }
        public (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceIdentifier { get; set; }
        public (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) ConfigProd { get; set; }
        public (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) ConfigTest { get; set; }
        public (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) ConfigDebitPin { get; set; }
        public (string Timestamp, int VipaResponse) TerminalDateTime { get; set; }
        public (string Timestamp, int VipaResponse) Reboot24Hour { get; set; }
        public (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) EmvKernelInformation { get; set; }
        #endregion --- attributes ---

        private void DeviceLogger(string message) =>
            Logger.info($"{DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Manufacturer}[{DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model}, {DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.SerialNumber}, {DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Port}]: {{{message}}}");

        private void DeviceErrorLogger(string message) =>
            Logger.error($"{DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Manufacturer}[{DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model}, {DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.SerialNumber}, {DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Port}]: {{{message}}}");

        public int ProcessHealthFromExectutionMode() => AppExecConfig.ExecutionMode switch
        {
            Modes.Execution.Console => ConsoleModeOutput(),
            Modes.Execution.StandAlone => StandAloneModeOutput(),
            _ => throw new Exception("HEALTH CHECK: undefined execution mode")
        };

        #region --- device health validation and reporting ---

        private HealthStatusValidationRequired ValueIsRequired(string value)
        {
            foreach (HealthStatusValidationRequired required in Enum.GetValues(typeof(HealthStatusValidationRequired)))
            {
                if (required.GetStringValue().Equals(value))
                {
                    return required;
                }
            }

            return HealthStatusValidationRequired.NOTREQUIRED;
        }

        private List<HealthStatusValidationRequired> GetHealthConfigurationHealthStatus()
        {
            List<HealthStatusValidationRequired> requiredChecks = new List<HealthStatusValidationRequired>();

            if (AppExecConfig.ExecutionMode == Modes.Execution.StandAlone && !string.IsNullOrEmpty(AppExecConfig.HealthCheckValidationMode))
            {
                string[] requirements = AppExecConfig.HealthCheckValidationMode.Split("|");

                foreach (string value in requirements)
                {
                    HealthStatusValidationRequired isRequired = ValueIsRequired(value);
                    if (isRequired != HealthStatusValidationRequired.NOTREQUIRED)
                    {
                        requiredChecks.Add(isRequired);
                    }
                }

            }

            return requiredChecks;
        }

        /// <summary>
        /// Device Health Validation Requirements
        /// 1. Slot0 Keys  ADE + Debit
        /// 2. Validate Package Tags(Firmware, Cert, Idle)
        /// 3. TimeInjection to UTC(0)
        /// 4. 24restart set to 07:00
        /// </summary>
        /// <param name="ConfigTest"></param>
        /// <param name="ConfigDebitPin"></param>
        /// <returns></returns>
        private bool SetHealthCheckValidation()
        {
            // PROD SLOT IS ALWAYS REQUIRED
            (bool vipaResponseNotValid, bool emptyKSN) configProd = (ConfigProd.VipaResponse != (int)VipaSW1SW2Codes.Success, string.IsNullOrEmpty(ConfigProd.securityConfigurationObject.SRedCardKSN));
            if (configProd.vipaResponseNotValid || configProd.emptyKSN)
            {
                DeviceErrorLogger($"ADEPRODKEY: VIPA-RESPONSE-VALID={configProd.vipaResponseNotValid}, KSN-IS-NULL={configProd.emptyKSN}");
                return false;
            }

            (bool vipaResponseNotValid, bool emptyKSN) configTest = (ConfigTest.VipaResponse != (int)VipaSW1SW2Codes.Success, string.IsNullOrEmpty(ConfigTest.securityConfigurationObject.SRedCardKSN));
            (bool vipaResponseNotValid, bool emptyKSN) configDebitPin = (ConfigDebitPin.VipaResponse != (int)VipaSW1SW2Codes.Success, string.IsNullOrEmpty(ConfigDebitPin.securityConfigurationObject.OnlinePinKSN));

            List<HealthStatusValidationRequired> requiredChecks = GetHealthConfigurationHealthStatus();

            if (requiredChecks.Count > 0)
            {
                foreach (HealthStatusValidationRequired check in requiredChecks)
                {
                    switch (check)
                    {
                        case HealthStatusValidationRequired.ADETESTKEY:
                        {
                            if (configTest.vipaResponseNotValid || configTest.emptyKSN)
                            {
                                DeviceErrorLogger($"ADETESTKEY: VIPA-RESPONSE-VALID={configTest.vipaResponseNotValid}, KSN-IS-NULL={configTest.emptyKSN}");
                                return false;
                            }
                            break;
                        }
                        case HealthStatusValidationRequired.DEBITPINKEY:
                        {
                            if (configDebitPin.vipaResponseNotValid || configDebitPin.emptyKSN)
                            {
                                DeviceErrorLogger($"DEBITPINTKEY: VIPA-RESPONSE-VALID={configDebitPin.vipaResponseNotValid}, KSN-IS-NULL={configDebitPin.emptyKSN}");
                                return false;
                            }
                            break;
                        }
                    }
                }
            }

            return true;
        }

        private int ConsoleModeOutput()
        {
            bool prodADEKeyFound = false;
            bool testADEKeyFound = false;

            // ADE PROD KEY
            if (ConfigProd.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                Console.WriteLine($"DEVICE: FIRMARE VERSION ___: {DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                Console.WriteLine($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {ConfigProd.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");

                if (ConfigProd.securityConfigurationObject.SRedCardKSN != null)
                {
                    prodADEKeyFound = true;
                    Console.WriteLine($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(4, 6)}");
                    Console.WriteLine($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(10, 5)}");
                }
            }

            // ADE TEST KEY
            if (ConfigTest.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                testADEKeyFound = true;
                Console.WriteLine($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {ConfigTest.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");
                Console.WriteLine($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(4, 6) ?? "[ *** NOT FOUND *** ]"}");
                Console.WriteLine($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(10, 5) ?? "[ *** NOT FOUND *** ]"}");
            }
            Console.WriteLine($"DEVICE: ADE PROD KEY SLOT _: 0x0{DeviceSectionConfig.Verifone.ADEKeySetId}");

            if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdProd)
            {
                Console.WriteLine($"DEVICE: ADE PROD KEY SLOT  : {(prodADEKeyFound ? "VALID" : "*** INVALID ***")}");
            }
            else if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdTest)
            {
                Console.WriteLine($"DEVICE: ADE TEST KEY SLOT  : {(testADEKeyFound ? "VALID" : "*** INVALID ***")}");
            }
            else
            {
                Console.WriteLine("DEVICE: ADE PROD KEY SLOT  : INVALID SLOT");
            }

            // DEBIT PIN KEY
            if (ConfigDebitPin.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine($"DEVICE: DEBIT PIN KEY STORE: {(DeviceSectionConfig.Verifone?.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.ConfigurationHostId ? "IPP" : "VSS")}");
                Console.WriteLine($"DEVICE: DEBIT PIN KEY SLOT : 0x0{(DeviceSectionConfig.Verifone?.OnlinePinKeySetId)} - DUKPT{DeviceSectionConfig.Verifone?.OnlinePinKeySetId - 1}");
                Console.WriteLine($"DEVICE: DEBIT PIN KSN _____: {ConfigDebitPin.securityConfigurationObject.OnlinePinKSN ?? "[ *** NOT FOUND *** ]"}");
            }

            // TERMINAL TIMESTAMP
            if (TerminalDateTime.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                {
                    Console.WriteLine("DEVICE: TERMINAL DATETIME _: [ *** NOT FOUND *** ]");
                }
                else
                {
                    string TerminalDateTimeStamp = string.Format("{1}/{2}/{0}-{3}:{4}:{5}",
                        TerminalDateTime.Timestamp.Substring(0, 4), TerminalDateTime.Timestamp.Substring(4, 2), TerminalDateTime.Timestamp.Substring(6, 2),
                        TerminalDateTime.Timestamp.Substring(8, 2), TerminalDateTime.Timestamp.Substring(10, 2), TerminalDateTime.Timestamp.Substring(12, 2));
                    Console.WriteLine($"DEVICE: TERMINAL DATETIME _: {TerminalDateTimeStamp}");
                }
            }

            // TERMINAL 24 HOUR REBOOT
            if (Reboot24Hour.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                {
                    Console.WriteLine("DEVICE: TERMINAL DATETIME_: [ *** NOT SET *** ]");
                }
                else
                {
                    if (string.IsNullOrEmpty(Reboot24Hour.Timestamp) || Reboot24Hour.Timestamp.Length < 6)
                    {
                        Console.WriteLine($"DEVICE: 24 HOUR REBOOT ____: [ *** NOT SET *** ]");
                    }
                    else
                    {
                        string rebootDateTimeStamp = string.Format("{0}:{1}:{2}",
                            Reboot24Hour.Timestamp.Substring(0, 2), Reboot24Hour.Timestamp.Substring(2, 2), Reboot24Hour.Timestamp.Substring(4, 2));
                        Console.WriteLine($"DEVICE: 24 HOUR REBOOT ____: {rebootDateTimeStamp}");
                    }
                }
            }

            // EMV KERNEL CHECKSUM
            if (EmvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine($"DEVICE: EMV CONFIGURATION _: VALID");

                string[] kernelInformation = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                if (kernelInformation.Length == 4)
                {
                    Console.WriteLine(string.Format("DEVICE: EMV KERNEL CHECKSUM: {0}-{1}-{2}-{3}",
                       kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]));
                }
                else
                {
                    Console.WriteLine(string.Format("VIPA EMV KERNEL CHECKSUM __: {0}",
                        EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation));
                }

                bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model.Substring(0, 4)));

                if (EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET).Equals(IsEngageDevice ? BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM : BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    EmvKernelInformation.kernelConfigurationObject.KernelIsValid = true;
                    Console.WriteLine("DEVICE: EMV KERNEL STATUS _: VALID");
                }
                else
                {
                    EmvKernelInformation.kernelConfigurationObject.KernelIsValid = false;
                    Console.WriteLine("DEVICE: EMV KERNEL STATUS _: INVALID");
                }
            }
            else
            {
                Console.WriteLine(string.Format("DEVICE: FAILED GET KERNEL CHECKSUM REQUEST WITH ERROR=0x{0:X4}\n", EmvKernelInformation.VipaResponse));
            }

            // HMAC Secrets Test
            //(string HMAC, int VipaResponse) hmacConfig = VipaDevice.GenerateHMAC();
            //if (hmacConfig.VipaResponse == (int)VipaSW1SW2Codes.Success)
            //{
            //    Console.WriteLine($"DEVICE: HMAC KEYS GENERATED: {hmacConfig.HMAC}\n");
            //}

            // PACKAGE TAGS
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

        private StringBuilder DisplayHealthStatus(bool configIsValid)
        {
            StringBuilder healthStatus = new StringBuilder($"{DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.SerialNumber}");
            healthStatus.Append($"_{DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion.Replace(".", "")}");
            healthStatus.Append($"_{(configIsValid ? "PASS" : "FAIL")}");
            healthStatus.Append($"_{Utils.GetTimeStampToSeconds()}");


            // wait for progress bar to dismiss prior to writing results
            Task.Run(async () =>
            {
                while ((bool)DeviceEventOccured?.Invoke(DeviceEvent.ProgressBarActive, DeviceInformation))
                {
                    await Task.Delay(100);
                }
                await Task.Delay(1000);
                Console.WriteLine(healthStatus);
            });

            return healthStatus;
        }

        /// <summary>
        /// VALIDATION STEPS
        /// 1. PROD KEYS, DEBIT PIN KEYS
        /// 2. PACKAGE VERSION TAGS
        /// 3. UTC TERMINAL TIME CHECK
        /// 4. 24 HOUR REBOOT SET TO 07:00:00
        /// 5. EMV KERNEL CHECKSUM VALIDATION
        /// </summary>
        /// <returns></returns>
        private int StandAloneModeOutput()
        {
            DeviceHealthStatus deviceHealthStatus = new DeviceHealthStatus();

            // VALIDATION STEP 1: PROD-KEYS + DEBIT PIN KEYS
            deviceHealthStatus.PaymentKeysAreValid = SetHealthCheckValidation();

            // VALIDATION STEP 2: package version tags [VIPA, EMV, IDLE]
            deviceHealthStatus.PackagesAreValid = true;
            if (string.IsNullOrEmpty(VipaVersions.DALCdbData.VIPAVersion.DateCode) ||
                string.IsNullOrEmpty(VipaVersions.DALCdbData.EMVVersion.DateCode) ||
                string.IsNullOrEmpty(VipaVersions.DALCdbData.IdleVersion.DateCode))
            {
                if (string.IsNullOrEmpty(VipaVersions.DALCdbData.VIPAVersion.DateCode))
                {
                    DeviceErrorLogger($"BUNDLE VIPA_VER DATECODE: IS-EMPTY");
                    deviceHealthStatus.PackagesAreValid = false;
                }
                if (string.IsNullOrEmpty(VipaVersions.DALCdbData.EMVVersion.DateCode))
                {
                    DeviceErrorLogger($"BUNDLE EMV_VER DATECODE: IS-EMPTY");
                    deviceHealthStatus.PackagesAreValid = false;
                }
                if (string.IsNullOrEmpty(VipaVersions.DALCdbData.IdleVersion.DateCode))
                {
                    DeviceErrorLogger($"BUNDLE IDLE_VER DATECODE: IS-EMPTY");
                    deviceHealthStatus.PackagesAreValid = false;
                }
            }

            // VALIDATION STEP 3: time injection (UTC)
            deviceHealthStatus.TerminalTimeStampIsValid = true;
            if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
            {
                DeviceErrorLogger($"TERMINAL TIMESTAMP: IS-EMPTY");
                deviceHealthStatus.TerminalTimeStampIsValid = false;
            }
            else
            {
                string terminalDateTimeStamp = string.Format("{1}{2}{0}{3}{4}",
                    TerminalDateTime.Timestamp.Substring(0, 4), TerminalDateTime.Timestamp.Substring(4, 2), TerminalDateTime.Timestamp.Substring(6, 2),
                    TerminalDateTime.Timestamp.Substring(8, 2), TerminalDateTime.Timestamp.Substring(10, 2));
                string utcDateTimeStamp = Utils.GetUTCTimeStampToMinutes();

                Logger.info(string.Format("DEVICE: WORKSTATION UTC TIME _______ : [{0}]", utcDateTimeStamp));
                Logger.info(string.Format("DEVICE: REPORTED TERMINAL TIME STAMP : [{0}]", terminalDateTimeStamp));

                // avoid clock drift in timestamp comparison
                deviceHealthStatus.TerminalTimeStampIsValid = terminalDateTimeStamp.Substring(0, terminalDateTimeStamp.Length - 1).Equals(utcDateTimeStamp.Substring(0, utcDateTimeStamp.Length - 1));

                if (!deviceHealthStatus.TerminalTimeStampIsValid)
                {
                    DeviceErrorLogger($"TERMINAL TIMESTAMP {terminalDateTimeStamp}: DOES NOT MATCH UTC TIME={utcDateTimeStamp}");
                }
            }

            // VALIDATION STEP 4: 24 hour reboot set to 07:00
            deviceHealthStatus.Terminal24HoureRebootIsValid = true;
            if (string.IsNullOrEmpty(Reboot24Hour.Timestamp))
            {
                deviceHealthStatus.Terminal24HoureRebootIsValid = false;
                DeviceErrorLogger($"TERMINAL 24-HOUR REBOOT: IS-EMPTY");
            }
            else
            {
                string rebootDateTimeStamp = string.Format("{0}:{1}:{2}",
                            Reboot24Hour.Timestamp.Substring(0, 2), Reboot24Hour.Timestamp.Substring(2, 2), Reboot24Hour.Timestamp.Substring(4, 2));
                deviceHealthStatus.Terminal24HoureRebootIsValid = rebootDateTimeStamp.Equals(HealthStatusCheckImpl.Device24HourReboot);
                if (!deviceHealthStatus.Terminal24HoureRebootIsValid)
                {
                    DeviceErrorLogger($"TERMINAL 24-HOUR REBOOT {rebootDateTimeStamp}: DOES NOT MATCH EXPECTED TIME={HealthStatusCheckImpl.Device24HourReboot}");
                }
            }

            bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model.Substring(0, 4)));

            // VALIDATION STEP 5: EMV Kernel Validation
            if (EmvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                EmvKernelInformation.kernelConfigurationObject.KernelIsValid = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET).Equals(IsEngageDevice ? BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM : BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM,
                    StringComparison.CurrentCultureIgnoreCase);
            }
            
            deviceHealthStatus.EmvKernelConfigurationIsValid = EmvKernelInformation.kernelConfigurationObject.KernelIsValid;

            bool configIsValid = deviceHealthStatus.PaymentKeysAreValid && deviceHealthStatus.PackagesAreValid && deviceHealthStatus.TerminalTimeStampIsValid &
                deviceHealthStatus.Terminal24HoureRebootIsValid && deviceHealthStatus.EmvKernelConfigurationIsValid;

            // output status to console window
            StringBuilder healthStatus = DisplayHealthStatus(configIsValid);

            string fileName = healthStatus + ".txt";
            string fileDir = Directory.GetCurrentDirectory() + "\\logs";
            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }
            string filePath = Path.Combine(fileDir, fileName);

            // save to device specifc file
            using (StreamWriter streamWriter = new StreamWriter(filePath, append: true))
            {
                bool prodADEKeyFound = false;
                bool testADEKeyFound = false;

                // VALIDATION STEP 1: ADE PROD KEY
                if (ConfigProd.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                    bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");

                    streamWriter.WriteLine($"DEVICE: FIRMARE VERSION ___: {DeviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                    streamWriter.WriteLine($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {ConfigProd.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");

                    if (ConfigProd.securityConfigurationObject.SRedCardKSN != null)
                    {
                        prodADEKeyFound = true;
                        streamWriter.WriteLine($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(4, 6)}");
                        streamWriter.WriteLine($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(10, 5)}");
                    }
                }
                else
                {
                    streamWriter.WriteLine("DEVICE: PAYMENT PROD KEYS _: [ *** NOT FOUND *** ]");
                }

                // ADE TEST KEY
                if (ConfigTest.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    testADEKeyFound = true;
                    streamWriter.WriteLine($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ____: {ConfigTest.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");
                    streamWriter.WriteLine($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID _: {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(4, 6) ?? "[ *** NOT FOUND *** ]"}");
                    streamWriter.WriteLine($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID : {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(10, 5) ?? "[ *** NOT FOUND *** ]"}");
                }
                streamWriter.WriteLine($"DEVICE: ADE PROD KEY SLOT _: 0x0{DeviceSectionConfig.Verifone.ADEKeySetId}");

                if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdProd)
                {
                    streamWriter.WriteLine($"DEVICE: ADE PROD KEY SLOT  : {(prodADEKeyFound ? "VALID" : "*** INVALID ***")}");
                }
                else if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdTest)
                {
                    streamWriter.WriteLine($"DEVICE: ADE TEST KEY SLOT  : {(testADEKeyFound ? "VALID" : "*** INVALID ***")}");
                }
                else
                {
                    streamWriter.WriteLine("DEVICE: ADE PROD KEY SLOT  : INVALID SLOT");
                }

                // DEBIT PIN KEY
                if (ConfigDebitPin.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    streamWriter.WriteLine($"DEVICE: DEBIT PIN KEY STORE: {(DeviceSectionConfig.Verifone?.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.ConfigurationHostId ? "IPP" : "VSS")}");
                    streamWriter.WriteLine($"DEVICE: DEBIT PIN KEY SLOT : 0x0{(DeviceSectionConfig.Verifone?.OnlinePinKeySetId)} - DUKPT{DeviceSectionConfig.Verifone?.OnlinePinKeySetId - 1}");
                    streamWriter.WriteLine($"DEVICE: DEBIT PIN KSN _____: {ConfigDebitPin.securityConfigurationObject.OnlinePinKSN ?? "[ *** NOT FOUND *** ]"}");
                }
                else
                {
                    streamWriter.WriteLine("DEVICE: DEBIT PIN KEY _____: [ *** NOT FOUND *** ]");
                }

                // VALIDATION STEP 2: TERMINAL TIMESTAMP
                if (TerminalDateTime.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                    {
                        streamWriter.WriteLine("DEVICE: TERMINAL DATETIME _: [ *** NOT FOUND *** ]");
                    }
                    else
                    {
                        string TerminalDateTimeStamp = string.Format("{1}/{2}/{0}-{3}:{4}:{5}",
                            TerminalDateTime.Timestamp.Substring(0, 4), TerminalDateTime.Timestamp.Substring(4, 2), TerminalDateTime.Timestamp.Substring(6, 2),
                            TerminalDateTime.Timestamp.Substring(8, 2), TerminalDateTime.Timestamp.Substring(10, 2), TerminalDateTime.Timestamp.Substring(12, 2));
                        streamWriter.WriteLine($"DEVICE: TERMINAL DATETIME _: {TerminalDateTimeStamp}");
                    }
                }

                if (!deviceHealthStatus.TerminalTimeStampIsValid)
                {
                    streamWriter.WriteLine("DEVICE: TERMINAL DATETIME _: [ *** FAILED VALIDATION *** ]");
                }

                // VALIDATION STEP 3: TERMINAL 24 HOUR REBOOT
                if (Reboot24Hour.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                    {
                        streamWriter.WriteLine("DEVICE: TERMINAL DATETIME_: [ *** NOT SET *** ]");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Reboot24Hour.Timestamp) || Reboot24Hour.Timestamp.Length < 6)
                        {
                            streamWriter.WriteLine($"DEVICE: 24 HOUR REBOOT ____: [ *** NOT SET *** ]");
                        }
                        else
                        {
                            string rebootDateTimeStamp = string.Format("{0}:{1}:{2}",
                                Reboot24Hour.Timestamp.Substring(0, 2), Reboot24Hour.Timestamp.Substring(2, 2), Reboot24Hour.Timestamp.Substring(4, 2));
                            streamWriter.WriteLine($"DEVICE: 24 HOUR REBOOT ____: {rebootDateTimeStamp}");
                        }
                    }
                }

                if (!deviceHealthStatus.Terminal24HoureRebootIsValid)
                {
                    streamWriter.WriteLine("DEVICE: 24 HOUR REBOOT ____: [ *** FAILED VALIDATION *** ]");
                }

                // VALIDATION STEP 4: EMV KERNEL CHECKSUM
                if (EmvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    streamWriter.WriteLine($"DEVICE: EMV CONFIGURATION _: VALID");

                    string[] kernelInformation = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                    if (kernelInformation.Length == 4)
                    {
                        streamWriter.WriteLine(string.Format("DEVICE: EMV KERNEL CHECKSUM: {0}-{1}-{2}-{3}",
                           kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]));
                    }
                    else
                    {
                        streamWriter.WriteLine(string.Format("VIPA EMV KERNEL CHECKSUM __: {0}",
                            EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation));
                    }

                    if (EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET).Equals(IsEngageDevice ? BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM : BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM,
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
                    //                                    123456789|1234567890123456789|
                    streamWriter.WriteLine(string.Format("DEVICE KERNEL CHECKSUM ____: REQUEST FAILED WITH ERROR=0x{0:X4}", EmvKernelInformation.VipaResponse));
                }

                // VALIDATION STEP 5: PACKAGE TAGS
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
                        streamWriter.WriteLine($"DEVICE: {signature} BUNDLE(S) : VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                    }
                    else
                    {
                        streamWriter.WriteLine($"DEVICE: {signature} BUNDLE(S) __: VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                    }

                    if (vipaDateCode.Equals("_NONE"))
                    {
                        streamWriter.WriteLine("DEVICE: VIPA CONFIG BUNDLE : [ *** FAILED VALIDATION *** ]");
                    }
                    if (emvDateCode.Equals("_NONE"))
                    {
                        streamWriter.WriteLine("DEVICE: EMV CONFIG BUNDLE _: [ *** FAILED VALIDATION *** ]");
                    }
                    if (idleDateCode.Equals("_NONE"))
                    {
                        streamWriter.WriteLine("DEVICE: IDLE CONFIG BUNDLE : [ *** FAILED VALIDATION *** ]");
                    }
                }
                else
                {
                    streamWriter.WriteLine("DEVICE: CONFIG BUNDLE(S) __: [ *** FAILED VALIDATION *** ]");
                }

                streamWriter.WriteLine($"DEVICE: HEALTH VALIDATION _: {(configIsValid ? "PASS" : "FAIL")}");

                streamWriter.Close();
            }

            return 0;
        }

        #endregion --- device health validation and reporting ---
    }
}
