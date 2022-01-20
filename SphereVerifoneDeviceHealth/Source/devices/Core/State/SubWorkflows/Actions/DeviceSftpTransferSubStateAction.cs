using Common.Constants;
using Common.LoggerManager;
using Common.XO.Device;
using Common.XO.Requests;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.State.Enums;
using FileTransferAppLauncher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal class DeviceSftpTransferSubStateAction : DeviceBaseSubStateAction
    {
        public override DeviceSubWorkflowState WorkflowStateType => DeviceSubWorkflowState.SecurityConfigurationSftpTransfer;

        public DeviceSftpTransferSubStateAction(IDeviceSubStateController _) : base(_) { }

        public override SubStateActionLaunchRules LaunchRules => new SubStateActionLaunchRules
        {
            RequestCancellationToken = true
        };

        public override async Task DoWork()
        {
            if (StateObject != null)
            {
                await ProcessSftpTransfer(StateObject as List<LinkRequest>);
                Controller.SaveState(StateObject);
            }

            _ = Complete(this);
        }

        private async Task ProcessSftpTransfer(List<LinkRequest> linkResponses)
        {
            try
            {
                // Payment: targeted device: multi-connection support
                foreach (LinkRequest linkResponse in linkResponses)
                {
                    if (linkResponse.Actions.Count >= 2 && linkResponse.Actions.Where(e => e.Action == LinkAction.SftpTransfer).Any())
                    {
                        LinkActionRequest linkActionRequest = linkResponse.Actions.Where(e => e.Action == LinkAction.SftpTransfer).First();

                        Debug.WriteLine($"Received from {linkActionRequest}");

                        LinkDeviceIdentifier deviceIdentifier = linkResponse.GetDeviceIdentifier();
                        ICardDevice cardDevice = FindTargetDevice(deviceIdentifier);

                        if (cardDevice != null)
                        {
                            Debug.WriteLine($"SFTP TRANSFER FOR FILE: [{Path.GetFileName(linkActionRequest.SftpRequest.DeviceHealthStatusFilename)}]");

                            // Move file to SFTP staging folder
                            if (File.Exists(linkActionRequest.SftpRequest.DeviceHealthStatusFilename))
                            {
                                string targetFilename = Path.GetFileName(linkActionRequest.SftpRequest.DeviceHealthStatusFilename);
                                string filePath = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(LogDirectories.LogDirectory, LogDirectories.PendingDirectory));
                                filePath = Path.Combine(filePath, targetFilename);
                                File.Move(linkActionRequest.SftpRequest.DeviceHealthStatusFilename, filePath);
                                Logger.info($"DEVICE: FILE IN UPLOAD_PENDING DIRECTORY __ : {targetFilename}");

                                // Launch SftpTransferApp
                                TransferAppLauncher.StartAllProcesses(Assembly.GetEntryAssembly().GetName().Name);
                            }

                            //SftpConnectionParameters clientParams = new SftpConnectionParameters()
                            //{
                            //    Hostname = linkActionRequest.SftpRequest.Hostname,
                            //    Username = linkActionRequest.SftpRequest.Username,
                            //    Password = linkActionRequest.SftpRequest.Password,
                            //    UsePublicKey = linkActionRequest.SftpRequest.UsePublicKey,
                            //    Filename = targerFilename,
                            //    TargetDirectory = linkActionRequest.SftpRequest.TargetDirectory,
                            //    Port = linkActionRequest.SftpRequest.Port
                            //};

                            //Controller.FileTransferProvider.TransferFile(clientParams);

                            await Task.Delay(10);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.error($"{e.Message} {linkResponses}");
            }
        }
    }
}