using Common.Constants;
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.ExtensionMethods;

namespace Devices.Verifone.Helpers
{
    public class HealthStatusCheckImpl
    {
        #region --- attributes ---
        const string Device24HourReboot = "00:00:00";

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

        public TimeZoneInfo WorkstationTimeZone { get; set; }
        public string KIFTerminalTimeZone { get; set; }
        public string DeviceHealthFile { get; set; }
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
        public string VOSVersions { get; set; }
        #endregion --- attributes ---

        private void DeviceLogger(string message) =>
            Logger.info($"{DeviceInformation.Manufacturer}[{DeviceInformation.Model}, {DeviceInformation.SerialNumber}, {DeviceInformation.ComPort}]: {{{message}}}");

        private void DeviceErrorLogger(string message) =>
            Logger.error($"[{Utils.FormatStringAsRequired($"{DeviceInformation.Manufacturer}, {DeviceInformation.Model}, {DeviceInformation.SerialNumber}, {DeviceInformation.ComPort}", Utils.DeviceLogKeyValueLength, Utils.DeviceLogKeyValuePaddingCharacter)}]: {{{message}}}");
        
        public bool ProcessHealthFromExectutionMode() => AppExecConfig.ExecutionMode switch
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

        private bool ConsoleModeOutput()
        {
            bool prodADEKeyFound = false;
            bool testADEKeyFound = false;

            // ADE PROD KEY
            if (ConfigProd.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                bool activeSigningMethodIsSphere = SigningMethodActive.Equals("SPHERE");
                bool activeSigningMethodIsVerifone = SigningMethodActive.Equals("VERIFONE");
                //                  123456789A123456789B123456789C
                Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: FIRMARE VERSION ")}: {DeviceInformation.FirmwareVersion}");
                Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: VOS VERSIONS ")}: {VOSVersions}"); 
                Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? " ?? "} KEY KSN ")}: {ConfigProd.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");

                if (ConfigProd.securityConfigurationObject.SRedCardKSN != null)
                {
                    prodADEKeyFound = true;
                    Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID ")}: {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(4, 6)}");
                    Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{ConfigProd.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID ")}: {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(10, 5)}");
                }
            }

            // ADE TEST KEY
            if (ConfigTest.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                testADEKeyFound = true;
                Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} KEY KSN ")}: {ConfigTest.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");
                Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK KEY_ID ")}: {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(4, 6) ?? "[ *** NOT FOUND *** ]"}");
                Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{ConfigTest.securityConfigurationObject.KeySlotNumber ?? "??"} BDK TRSM ID ")}: {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(10, 5) ?? "[ *** NOT FOUND *** ]"}");
            }
            Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: ADE PROD KEY SLOT ")}: 0x0{DeviceSectionConfig.Verifone.ADEKeySetId}");

            if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdProd)
            {
                Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE PROD KEY STATUS ")}: {(prodADEKeyFound ? "VALID" : "[ *** FAILED VALIDATION *** ]")}");
            }
            else if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdTest)
            {
                Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE TEST KEY STATUS ")}: {(testADEKeyFound ? "VALID" : "[ *** FAILED VALIDATION *** ]")}");
            }
            else
            {
                Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: ADE PROD KEY SLOT ")} : INVALID SLOT");
            }

            // DEBIT PIN KEY
            if (ConfigDebitPin.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DEBIT PIN KEY STORE ")}: {(DeviceSectionConfig.Verifone?.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.ConfigurationHostId ? "IPP" : "VSS")}");
                Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DEBIT PIN KEY SLOT ")}: 0x0{(DeviceSectionConfig.Verifone?.OnlinePinKeySetId)} - DUKPT{DeviceSectionConfig.Verifone?.OnlinePinKeySetId - 1}");
                Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DEBIT PIN KSN ")}: {ConfigDebitPin.securityConfigurationObject.OnlinePinKSN ?? "[ *** NOT FOUND *** ]"}");
            }

            // TERMINAL TIMESTAMP
            if (TerminalDateTime.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                {
                    Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: TERMINAL DATETIME ")}: [ *** NOT FOUND *** ]");
                }
                else
                {
                    string TerminalDateTimeStamp = string.Format("{1}/{2}/{0}-{3}:{4}:{5}",
                        TerminalDateTime.Timestamp.Substring(0, 4), TerminalDateTime.Timestamp.Substring(4, 2), TerminalDateTime.Timestamp.Substring(6, 2),
                        TerminalDateTime.Timestamp.Substring(8, 2), TerminalDateTime.Timestamp.Substring(10, 2), TerminalDateTime.Timestamp.Substring(12, 2));
                    Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: TERMINAL DATETIME ")}: {TerminalDateTimeStamp}");
                }
            }

            // TERMINAL 24 HOUR REBOOT
            if (Reboot24Hour.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                {
                    Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: TERMINAL DATETIME ")}: [ *** NOT SET *** ]");
                }
                else
                {
                    if (string.IsNullOrEmpty(Reboot24Hour.Timestamp) || Reboot24Hour.Timestamp.Length < 6)
                    {
                        Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: 24 HOUR REBOOT ")}: [ *** NOT SET *** ]");
                    }
                    else
                    {
                        string rebootDateTimeStamp = string.Format("{0}:{1}:{2}",
                            Reboot24Hour.Timestamp.Substring(0, 2), Reboot24Hour.Timestamp.Substring(2, 2), Reboot24Hour.Timestamp.Substring(4, 2));
                        Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: 24 HOUR REBOOT ")}: {rebootDateTimeStamp}");
                    }
                }
            }

            // EMV KERNEL CHECKSUM
            if (EmvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV CONFIGURATION ")}: VALID");

                string[] kernelInformation = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                if (kernelInformation.Length == 4)
                {
                    string kernelVersion = string.Format("{0}-{1}-{2}-{3}", kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]);
                    Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV KERNEL CHECKSUM ")}: {kernelVersion}");
                }
                else
                {
                    Console.WriteLine(string.Format("{0}: {1}", Utils.FormatStringAsRequired("VIPA EMV KERNEL CHECKSUM "),
                        EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation));
                }

                bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(DeviceInformation.Model.Substring(0, 4)));
                string emvKernelCheckSum = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET);

                if (IsEngageDevice ? AttendedKernelIsValid(VipaVersions.DALCdbData.EMVVersion.TerminalType, emvKernelCheckSum) :
                                      UnattendedKernelIsValid(VipaVersions.DALCdbData.EMVVersion.TerminalType, emvKernelCheckSum))
                {
                    EmvKernelInformation.kernelConfigurationObject.KernelIsValid = true;
                    Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV KERNEL STATUS ")}: VALID");
                }
                else
                {
                    EmvKernelInformation.kernelConfigurationObject.KernelIsValid = false;
                    Console.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV KERNEL STATUS ")}: [ *** FAILED VALIDATION *** ]");
                }
            }
            else
            {
                Console.WriteLine(string.Format($"DEVICE: FAILED GET KERNEL CHECKSUM REQUEST WITH ERROR=0x{0:X4}\n", EmvKernelInformation.VipaResponse));
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
                    Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: {signature} BUNDLE(S) ")}: VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                }
                else
                {
                    Console.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: {signature} BUNDLE(S) ")}: VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                }
            }

            Console.WriteLine("");

            return true;
        }

        private StringBuilder DisplayHealthStatus(bool configIsValid)
        {
            StringBuilder healthStatus = new StringBuilder($"{DeviceInformation.SerialNumber}");
            healthStatus.Append($"_{DeviceInformation.FirmwareVersion.Replace(".", "")}");
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
        private bool StandAloneModeOutput()
        {
            try
            {
                bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(DeviceInformation.Model.Substring(0, 4)));

                DeviceHealthStatus deviceHealthStatus = new DeviceHealthStatus();

                // VALIDATION STEP 1: PROD-KEYS + DEBIT PIN KEYS
                deviceHealthStatus.PaymentKeysAreValid = SetHealthCheckValidation();

                // VALIDATION STEP 2: package version tags [VIPA, EMV, IDLE]
                deviceHealthStatus.PackagesAreValid = true;
                if (string.IsNullOrEmpty(VipaVersions.DALCdbData.VIPAVersion.DateCode) ||
                    string.IsNullOrEmpty(VipaVersions.DALCdbData.EMVVersion.DateCode) ||
                    (string.IsNullOrEmpty(VipaVersions.DALCdbData.IdleVersion.DateCode) && IsEngageDevice))
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
                    if (IsEngageDevice && string.IsNullOrEmpty(VipaVersions.DALCdbData.IdleVersion.DateCode))
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
                    string localDateTimeStamp = Utils.GetTimeStampToMinutes();

                    Logger.info($"{Utils.FormatStringAsRequired("DEVICE: WORKSTATION LOCAL TIME ", 43)} : {localDateTimeStamp}");
                    Logger.info($"{Utils.FormatStringAsRequired("DEVICE: REPORTED TERMINAL TIME STAMP ", 43)} : {terminalDateTimeStamp}");

                    DateTime localTime = DateTime.Now;
                    DateTime timeAdjusted = localTime;

                    TimeSpan workstationTimeZone = WorkstationTimeZone.GetUtcOffset(localTime);
                    TimeSpan terminalTimeZone = workstationTimeZone;

                    if(!string.IsNullOrEmpty(KIFTerminalTimeZone) && KIFTerminalTimeZone.Length >= 6)
                    {
                        terminalTimeZone = TimeSpan.Parse(KIFTerminalTimeZone.Substring(4, 6));
                    }

                    double hoursDifference = (terminalTimeZone - workstationTimeZone).TotalHours;

                    // Adjust for localtime
                    if (hoursDifference > 0)
                    {
                        timeAdjusted = localTime.AddHours(hoursDifference);
                        localDateTimeStamp = timeAdjusted.ToString("MMddyyyyHHmm");
                        Logger.info(string.Format("DEVICE: WORKSTATION LOCAL TIME ADJUST: [{0}]", localDateTimeStamp));
                    }

                    // avoid clock drift in timestamp comparison
                    deviceHealthStatus.TerminalTimeStampIsValid = terminalDateTimeStamp.Substring(0, terminalDateTimeStamp.Length - 1).Equals(localDateTimeStamp.Substring(0, localDateTimeStamp.Length - 1));

                    if (!deviceHealthStatus.TerminalTimeStampIsValid)
                    {
                        // Adjust +15 MIN
                        DateTime timePlusAdjusted = timeAdjusted;
                        DateTime time15MinutesLater = timePlusAdjusted.AddMinutes(15);
                        string localTime15MinutesLater = time15MinutesLater.ToString("MMddyyyyHHmm");

                        deviceHealthStatus.TerminalTimeStampIsValid = terminalDateTimeStamp.Substring(0, terminalDateTimeStamp.Length - 1).Equals(localTime15MinutesLater.Substring(0, localTime15MinutesLater.Length - 1));

                        if (!deviceHealthStatus.TerminalTimeStampIsValid)
                        {
                            // Adjust -15 MIN
                            DateTime time15MinutesBefore = timePlusAdjusted.AddMinutes(-15);
                            string localTime15MinutesBefore = time15MinutesBefore.ToString("MMddyyyyHHmm");

                            deviceHealthStatus.TerminalTimeStampIsValid = terminalDateTimeStamp.Substring(0, terminalDateTimeStamp.Length - 1).Equals(localTime15MinutesBefore.Substring(0, localTime15MinutesBefore.Length - 1));

                            if (!deviceHealthStatus.TerminalTimeStampIsValid)
                            {
                                DeviceErrorLogger($"TERMINAL TIMESTAMP {terminalDateTimeStamp}: DOES NOT MATCH UTC TIME={localDateTimeStamp}");
                            }
                        }
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

                // VALIDATION STEP 5: EMV Kernel Validation
                if (EmvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    string emvKernelCheckSum = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET);
                    EmvKernelInformation.kernelConfigurationObject.KernelIsValid = IsEngageDevice ? AttendedKernelIsValid(VipaVersions.DALCdbData.EMVVersion.TerminalType, emvKernelCheckSum) :
                                         UnattendedKernelIsValid(VipaVersions.DALCdbData.EMVVersion.TerminalType, emvKernelCheckSum);
                }

                deviceHealthStatus.EmvKernelConfigurationIsValid = EmvKernelInformation.kernelConfigurationObject?.KernelIsValid ?? false;

                bool configIsValid = deviceHealthStatus.PaymentKeysAreValid && deviceHealthStatus.PackagesAreValid && deviceHealthStatus.TerminalTimeStampIsValid &
                    deviceHealthStatus.Terminal24HoureRebootIsValid && deviceHealthStatus.EmvKernelConfigurationIsValid;

                // output status to console window
                StringBuilder healthStatus = DisplayHealthStatus(configIsValid);

                string fileName = healthStatus + ".txt";
                string fileDir = Path.Combine(Directory.GetCurrentDirectory(), LogDirectories.LogDirectory);
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

                        //                       123456789A123456789B123456789C
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: HEALTH TOOL VERSION ")}: {Assembly.GetEntryAssembly().GetName().Version}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: VALIDATOR TIMESTAMP ")}: {fileName.Split('_').GetValue(3).ToString().TrimEnd(new char[] { '.', 't', 'x', 't' })}");

                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: MANUFACTURER ")}: {DeviceInformation.Manufacturer}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: MODEL ")}: {DeviceInformation.Model}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: SERIAL NUMBER")}: {DeviceInformation.SerialNumber}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: FIRMARE VERSION ")}: {DeviceInformation.FirmwareVersion}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: VOS VERSIONS ")}: {VOSVersions}");

                        string prodKeySlotNumberFormatted = string.Format("{0:X2}", VerifoneSettingsSecurityConfiguration.ADEHostIdProd);
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{prodKeySlotNumberFormatted} KEY KSN ")}: {ConfigProd.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");

                        if (ConfigProd.securityConfigurationObject.SRedCardKSN != null)
                        {
                            prodADEKeyFound = true;
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{prodKeySlotNumberFormatted} BDK KEY_ID ")}: {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(4, 6)}");
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{prodKeySlotNumberFormatted} BDK TRSM ID ")}: {ConfigProd.securityConfigurationObject.SRedCardKSN?.Substring(10, 5)}");
                        }
                        else
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{prodKeySlotNumberFormatted} BDK KEY_ID ")}: [ *** NOT FOUND *** ]");
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{prodKeySlotNumberFormatted} BDK TRSM ID ")}: [ *** NOT FOUND *** ]");
                        }
                    }
                    else
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: PAYMENT PROD KEYS ")}: [ *** NOT FOUND *** ]");
                    }

                    // ADE TEST KEY
                    string testKeySlotNumberFormatted = string.Format("{0:X2}", VerifoneSettingsSecurityConfiguration.ADEHostIdTest);
                    if (ConfigTest.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        testADEKeyFound = true;
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{testKeySlotNumberFormatted} KEY KSN ")}: {ConfigTest.securityConfigurationObject.SRedCardKSN ?? "[ *** NOT FOUND *** ]"}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{testKeySlotNumberFormatted} BDK KEY_ID ")}: {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(4, 6) ?? "[ *** NOT FOUND *** ]"}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{testKeySlotNumberFormatted} BDK TRSM ID ")}: {ConfigTest.securityConfigurationObject.SRedCardKSN?.Substring(10, 5) ?? "[ *** NOT FOUND *** ]"}");
                    }
                    else
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{testKeySlotNumberFormatted} KEY KSN ")}: [ *** NOT FOUND *** ]");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{testKeySlotNumberFormatted} BDK KEY_ID ")}: [ *** NOT FOUND *** ]");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: ADE-{testKeySlotNumberFormatted} BDK TRSM ID ")}: [ *** NOT FOUND *** ]");
                    }
                    streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: ADE PROD KEY SLOT ")}: 0x0{DeviceSectionConfig.Verifone.ADEKeySetId}");

                    if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdProd)
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: ADE PROD KEY STATUS ")}: {(prodADEKeyFound ? "VALID" : "[ *** FAILED VALIDATION *** ]")}");
                    }
                    else if (DeviceSectionConfig.Verifone.ADEKeySetId == VerifoneSettingsSecurityConfiguration.ADEHostIdTest)
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: ADE TEST KEY STATUS ")}: {(testADEKeyFound ? "VALID" : "[ *** FAILED VALIDATION *** ]")}");
                    }
                    else
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: ADE PROD KEY SLOT ")}: INVALID SLOT");
                    }

                    // DEBIT PIN KEY
                    if (ConfigDebitPin.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DEBIT PIN KEY STORE ")}: {(DeviceSectionConfig.Verifone?.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.ConfigurationHostId ? "IPP" : "VSS")}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DEBIT PIN KEY SLOT ")}: 0x0{(DeviceSectionConfig.Verifone?.OnlinePinKeySetId)} - DUKPT{DeviceSectionConfig.Verifone?.OnlinePinKeySetId - 1}");
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DEBIT PIN KSN ")}: {ConfigDebitPin.securityConfigurationObject.OnlinePinKSN ?? "[ *** NOT FOUND *** ]"}");
                    }
                    else
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DEBIT PIN KEY ")}: [ *** NOT FOUND *** ]");
                    }

                    // VALIDATION STEP 2: TERMINAL TIMESTAMP
                    if (TerminalDateTime.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: WORKSTATION TIMEZONE ")}: \"{WorkstationTimeZone.DisplayName}\"");

                        if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: TERMINAL DATETIME ")}: [ *** NOT FOUND *** ]");
                        }
                        else
                        {
                            string TerminalDateTimeStamp = string.Format("{1}/{2}/{0}-{3}:{4}:{5}",
                                TerminalDateTime.Timestamp.Substring(0, 4), TerminalDateTime.Timestamp.Substring(4, 2), TerminalDateTime.Timestamp.Substring(6, 2),
                                TerminalDateTime.Timestamp.Substring(8, 2), TerminalDateTime.Timestamp.Substring(10, 2), TerminalDateTime.Timestamp.Substring(12, 2));
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: TERMINAL DATETIME ")}: {TerminalDateTimeStamp}");
                        }
                    }

                    if (deviceHealthStatus.TerminalTimeStampIsValid)
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DATETIME VALIDATION ")}: VALID");
                    }
                    else
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: DATETIME VALIDATION ")}: [ *** FAILED VALIDATION *** ]");
                    }

                    // VALIDATION STEP 3: TERMINAL 24 HOUR REBOOT
                    if (Reboot24Hour.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        if (string.IsNullOrEmpty(TerminalDateTime.Timestamp))
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: TERMINAL DATETIME ")}: [ *** NOT SET *** ]");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(Reboot24Hour.Timestamp) || Reboot24Hour.Timestamp.Length < 6)
                            {
                                streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: 24 HOUR REBOOT ")}: [ *** NOT SET *** ]");
                            }
                            else
                            {
                                string rebootDateTimeStamp = string.Format("{0}:{1}:{2}",
                                    Reboot24Hour.Timestamp.Substring(0, 2), Reboot24Hour.Timestamp.Substring(2, 2), Reboot24Hour.Timestamp.Substring(4, 2));
                                streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: 24 HOUR REBOOT ")}: {rebootDateTimeStamp}");
                            }
                        }
                    }

                    if (deviceHealthStatus.Terminal24HoureRebootIsValid)
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: 24 HOUR REBOOT STATUS")}: VALID");
                    }
                    else
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: 24 HOUR REBOOT STATUS")}: [ *** FAILED VALIDATION *** ]");
                    }

                    // VALIDATION STEP 4: EMV KERNEL CHECKSUM
                    if (EmvKernelInformation.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV CONFIGURATION ")}: VALID");

                        string[] kernelInformation = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                        if (kernelInformation.Length == 4)
                        {
                            string kernelVersion = string.Format("{0}-{1}-{2}-{3}", kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]);
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV KERNEL CHECKSUM ")}: {kernelVersion}");
                        }
                        else
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("VIPA EMV KERNEL CHECKSUM ")}: {EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation}");
                        }

                        string emvKernelCheckSum = EmvKernelInformation.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET);

                        if (IsEngageDevice ? AttendedKernelIsValid(VipaVersions.DALCdbData.EMVVersion.TerminalType, emvKernelCheckSum) :
                                             UnattendedKernelIsValid(VipaVersions.DALCdbData.EMVVersion.TerminalType, emvKernelCheckSum))
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV KERNEL STATUS ")}: VALID");
                            configIsValid = true;
                        }
                        else
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV KERNEL STATUS ")}: [ *** FAILED VALIDATION *** ]");
                        }
                    }
                    else
                    {
                        string vipaResult = string.Format("REQUEST FAILED WITH ERROR=0x{0:X4}", EmvKernelInformation.VipaResponse);
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: KERNEL CHECKSUM ")}: {vipaResult}");
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
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: {signature} BUNDLE(S) ")}: VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                        }
                        else
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired($"DEVICE: {signature} BUNDLE(S) ")}: VIPA{vipaDateCode}, EMV{emvDateCode}, IDLE{idleDateCode}");
                        }

                        if (vipaDateCode.Equals("_NONE"))
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: VIPA CONFIG BUNDLE ")}: [ *** FAILED VALIDATION *** ]");
                        }
                        if (emvDateCode.Equals("_NONE"))
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: EMV CONFIG BUNDLE ")}: [ *** FAILED VALIDATION *** ]");
                        }
                        if (IsEngageDevice && idleDateCode.Equals("_NONE"))
                        {
                            streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: IDLE CONFIG BUNDLE ")}: [ *** FAILED VALIDATION *** ]");
                        }
                    }
                    else
                    {
                        streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: CONFIG BUNDLE(S) ")}: [ *** FAILED VALIDATION *** ]");
                    }

                    streamWriter.WriteLine($"{Utils.FormatStringAsRequired("DEVICE: HEALTH VALIDATION ")}: {(configIsValid ? "PASS" : "FAIL")}");

                    streamWriter.Close();

                    // Store device health file just created
                    DeviceHealthFile = filePath;
                }
            }
            catch (Exception ex)
            {
                DeviceErrorLogger($"DEVICE: VALIDATION ERROR='{ex.Message}'");

                if (AppExecConfig.ExecutionMode == Modes.Execution.Console)
                {
                    Console.WriteLine("DEVICE: SPHERE HEALTH VALIDATION FAILED!\n");
                }

                return false;
            }

            return true;
        }

        private bool AttendedKernelIsValid(string terminaltype, string emvKernelCheckSum) => terminaltype switch
        {
            "attended" => emvKernelCheckSum.Equals(BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM),
            "attendednopin" => emvKernelCheckSum.Equals(BinaryStatusObject.ENGAGE_EMV_KERNEL_NOPIN_CHECKSUM),
            _ => false
        };

        private bool UnattendedKernelIsValid(string terminaltype, string emvKernelCheckSum) => terminaltype switch
        {
            "attended" => emvKernelCheckSum.Equals(BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM),
            _ => false
        };

        #endregion --- device health validation and reporting ---
    }
}
