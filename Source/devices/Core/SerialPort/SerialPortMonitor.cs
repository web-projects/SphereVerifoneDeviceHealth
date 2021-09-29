using Devices.Common;
using Devices.Common.Helpers;
using Devices.Core.SerialPort.Interfaces;
using System;
using System.Linq;
using System.Management;

namespace Devices.Core.SerialPort
{
    public class SerialPortMonitor : ISerialPortMonitor
    {
        public event ComPortEventHandler ComportEventOccured;

        private string[] serialPorts;
        private ManagementEventWatcher arrival;
        private ManagementEventWatcher removal;

        public void StartMonitoring()
        {
            serialPorts = GetAvailableSerialPorts();
            MonitorDeviceChanges();
        }

        public void StopMonitoring() => Dispose();

        public void Dispose()
        {
            arrival?.Stop();
            removal?.Stop();
            arrival?.Dispose();
            removal?.Dispose();

            arrival = null;
            removal = null;
        }

        private static string[] GetAvailableSerialPorts() => System.IO.Ports.SerialPort.GetPortNames();

        private void MonitorDeviceChanges()
        {
            try
            {
                //Detect insertion/removal of all USB devices
                WqlEventQuery deviceArrivalQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_USBHub'");   //Query every 1 second for device remove/insert
                WqlEventQuery deviceRemovalQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_USBHub'");
                arrival = new ManagementEventWatcher(deviceArrivalQuery);
                removal = new ManagementEventWatcher(deviceRemovalQuery);

                arrival.EventArrived += (sender, eventArgs) => RaisePortsChangedIfNecessary(PortEventType.Insertion, eventArgs);
                removal.EventArrived += (sender, eventArgs) => RaisePortsChangedIfNecessary(PortEventType.Removal, eventArgs);

                // Start listening for events
                arrival.Start();
                removal.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine($"serial: COMM exception={e.Message}");
            }
        }

        private void RaisePortsChangedIfNecessary(PortEventType eventType, EventArrivedEventArgs eventArgs)
        {
            lock (serialPorts)
            {
                string[] availableSerialPorts = GetAvailableSerialPorts();
                var targetObject = eventArgs.NewEvent["TargetInstance"] as ManagementBaseObject;
                string targetDeviceID = targetObject["DeviceID"]?.ToString() ?? string.Empty;
                if (targetDeviceID.Contains("vid_0801", StringComparison.OrdinalIgnoreCase))        //hardcode value for Magtek
                {
                    ComportEventOccured?.Invoke(eventType, targetDeviceID);
                    return;
                }
                if (eventType == PortEventType.Insertion)
                {
                    if (!serialPorts?.SequenceEqual(availableSerialPorts) ?? false)     //COM ports devices only
                    {
                        var added = availableSerialPorts.Except(serialPorts).ToArray();
                        if (added.Length > 0)
                        {
                            serialPorts = availableSerialPorts;

                            ComportEventOccured?.Invoke(PortEventType.Insertion, added[0]);
                        }
                    }
                }
                else if (eventType == PortEventType.Removal)
                {
                    var removed = serialPorts.Except(availableSerialPorts).ToArray();
                    if (removed.Length > 0)
                    {
                        serialPorts = availableSerialPorts;

                        ComportEventOccured?.Invoke(PortEventType.Removal, removed[0]);
                    }
                }
            }
        }
    }
}
