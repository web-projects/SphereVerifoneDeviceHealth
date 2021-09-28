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

        public void StopMonitoring()
        {
            Dispose();
        }

        public void Dispose()
        {
            arrival?.Stop();
            removal?.Stop();
            arrival?.Dispose();
            removal?.Dispose();

            arrival = null;
            removal = null;
        }

        private static string[] GetAvailableSerialPorts()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        private void MonitorDeviceChanges()
        {
            try
            {
                var deviceArrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
                var deviceRemovalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");

                arrival = new ManagementEventWatcher(deviceArrivalQuery);
                removal = new ManagementEventWatcher(deviceRemovalQuery);

                arrival.EventArrived += (o, args) => RaisePortsChangedIfNecessary(PortEventType.Insertion);
                removal.EventArrived += (sender, eventArgs) => RaisePortsChangedIfNecessary(PortEventType.Removal);

                // Start listening for events
                arrival.Start();
                removal.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine($"serial: COMM exception={e.Message}");
            }
        }

        private void RaisePortsChangedIfNecessary(PortEventType eventType)
        {
            lock (serialPorts)
            {
                string[] availableSerialPorts = GetAvailableSerialPorts();

                if (eventType == PortEventType.Insertion)
                {
                    if (!serialPorts?.SequenceEqual(availableSerialPorts) ?? false)
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
