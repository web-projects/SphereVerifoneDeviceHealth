using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Constants;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.XO.Responses;

namespace Devices.Core.State.Actions
{
    internal class DeviceInitializeDeviceCommunicationStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.InitializeDeviceCommunication;

        public DeviceInitializeDeviceCommunicationStateAction(IDeviceStateController _) : base(_) { }

        public override bool DoDeviceDiscovery()
        {
            LastException = new StateException("device recovery is needed");
            _ = Error(this);
            return true;
        }

        public override Task DoWork()
        {
            string pluginPath = Controller.PluginPath;
            List<ICardDevice> availableCardDevices = null;
            List<ICardDevice> discoveredCardDevices = null;
            List<ICardDevice> validatedCardDevices = null;

            try
            {
                availableCardDevices = Controller.DevicePluginLoader.FindAvailableDevices(pluginPath);

                // filter out devices that are disabled
                if (availableCardDevices.Count > 0)
                {
                    DeviceSection deviceSection = Controller.Configuration;
                    foreach (var device in availableCardDevices)
                    {
                        switch (device.ManufacturerConfigID)
                        {
                            case "IdTech":
                            {
                                device.SortOrder = deviceSection.IdTech.SortOrder;
                                break;
                            }

                            case "Verifone":
                            {
                                device.SortOrder = deviceSection.Verifone.SortOrder;
                                break;
                            }

                            case "Simulator":
                            {
                                device.SortOrder = deviceSection.Simulator.SortOrder;
                                break;
                            }
                        }
                    }
                    availableCardDevices.RemoveAll(x => x.SortOrder == -1);

                    if (availableCardDevices?.Count > 1)
                    {
                        availableCardDevices.Sort();
                    }
                }

                // Probe validated devices
                discoveredCardDevices = new List<ICardDevice>();
                validatedCardDevices = new List<ICardDevice>();
                validatedCardDevices.AddRange(availableCardDevices);

                for (int i = validatedCardDevices.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(availableCardDevices[i].ManufacturerConfigID, DeviceType.NoDevice.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    bool success = false;
                    try
                    {
                        List<DeviceInformation> deviceInformation = availableCardDevices[i].DiscoverDevices();

                        if (deviceInformation == null)
                        {
                            continue;
                        }

                        foreach (var deviceInfo in deviceInformation)
                        {
                            DeviceConfig deviceConfig = new DeviceConfig()
                            {
                                Valid = true
                            };
                            SerialDeviceConfig serialConfig = new SerialDeviceConfig
                            {
                                CommPortName = Controller.Configuration.DefaultDevicePort
                            };
                            deviceConfig.SetSerialDeviceConfig(serialConfig);

                            ICardDevice device = validatedCardDevices[i].Clone() as ICardDevice;

                            device.DeviceEventOccured += Controller.DeviceEventReceived;

                            // Device powered on status capturing: free up the com port and try again.
                            // This occurs when a USB device repowers the USB interface and the virtual port is open.
                            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                            IDeviceCancellationBroker cancellationBroker = Controller.GetCancellationBroker();
                            var timeoutPolicy = cancellationBroker.ExecuteWithTimeoutAsync<List<LinkErrorValue>>(
                                _ => device.Probe(deviceConfig, deviceInfo, out success),
                                Timeouts.DALDeviceRecoveryTimeout,
                                cancellationTokenSource.Token);

                            if (timeoutPolicy.Result.Outcome == Polly.OutcomeType.Failure)
                            {
                                Console.WriteLine($"Unable to obtain device status for - '{device.Name}'.");
                                device.DeviceEventOccured -= Controller.DeviceEventReceived;
                                device?.Disconnect();
                                LastException = new StateException("Unable to find a valid device to connect to.");
                                _ = Error(this);
                                return Task.CompletedTask;
                            }
                            else if (success)
                            {
                                string deviceName = $"{device.DeviceInformation.Manufacturer}-{device.DeviceInformation.Model}";
                                if (Controller.Configuration.Verifone.SupportedDevices.Contains(deviceName))
                                {
                                    discoveredCardDevices.Add(device);
                                }
                                else
                                {
                                    device.DeviceSetIdle();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"device: exception='{e.Message}'");

                        discoveredCardDevices[i].DeviceEventOccured -= Controller.DeviceEventReceived;

                        // Consume failures
                        if (success)
                        {
                            success = false;
                        }
                    }

                    if (success)
                    {
                        continue;
                    }

                    validatedCardDevices.RemoveAt(i);
                }
            }
            catch
            {
                availableCardDevices = new List<ICardDevice>();
            }

            if (discoveredCardDevices?.Count > 0)
            {
                Controller.SetTargetDevices(discoveredCardDevices);
            }

            if (Controller.TargetDevices != null)
            {
                foreach (var device in Controller.TargetDevices)
                {
                    //Controller.LoggingClient.LogInfoAsync($"Device found: name='{device.Name}', model={device.DeviceInformation.Model}, " +
                    //    $"serial={device.DeviceInformation.SerialNumber}");
                    //Console.WriteLine($"DEVICE FOUND: name='{device.Name}', model='{device?.DeviceInformation?.Model}', " +
                    //    $"serial='{device?.DeviceInformation?.SerialNumber}'\n");
                    Controller.DeviceStatusUpdate();
                    device.DeviceSetIdle();
                }
            }
            else
            {
                //Controller.LoggingClient.LogInfoAsync("Unable to find a valid device to connect to.");
                //Console.WriteLine("Unable to find a valid device to connect to.");
                LastException = new StateException("Unable to find a valid device to connect to.");
                _ = Error(this);
                return Task.CompletedTask;
            }

            _ = Complete(this);

            return Task.CompletedTask;
        }
    }
}
