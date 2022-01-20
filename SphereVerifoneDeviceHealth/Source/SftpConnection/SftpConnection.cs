using Common.LoggerManager;
using FileTransfer.Helpers;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FileTransfer
{
    public class SftpConnection
    {
        private readonly string targetFile = Path.Combine(SftpConstants.TargetDirectory, SftpConstants.PublicKey);

        string PublicKey { get; set; }

        #region --- resources ---
        private bool FindEmbeddedResourceByName(string fileName, string fileTarget)
        {
            bool result = false;

            // Main Assembly contains embedded resources
            Assembly mainAssembly = Assembly.GetExecutingAssembly();
            Debug.WriteLine($"executing assembly: {mainAssembly.FullName}");
            foreach (string name in mainAssembly.GetManifestResourceNames())
            {
                if (name.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    using (Stream stream = mainAssembly.GetManifestResourceStream(name))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        // always create working file
                        FileStream fs = File.Open(fileTarget, FileMode.Create);
                        BinaryWriter bw = new BinaryWriter(fs);
                        byte[] ba = new byte[stream.Length];
                        stream.Read(ba, 0, ba.Length);
                        bw.Write(ba);
                        br.Close();
                        bw.Close();
                        stream.Close();
                        result = true;
                    }
                    break;

                }
            }
            return result;
        }
        #endregion --- resources ---

        private bool HasPassword(string password) => !string.IsNullOrWhiteSpace(password);

        private AuthenticationMethod[] privateKeyObject(string username, string publicKey)
        {
            PrivateKeyFile privateKeyFile = new PrivateKeyFile(publicKey);
            PrivateKeyAuthenticationMethod privateKeyAuthenticationMethod =
                 new PrivateKeyAuthenticationMethod(username, privateKeyFile);

            return new AuthenticationMethod[] { privateKeyAuthenticationMethod };
        }

        public ConnectionInfo GetSftpConnection(SftpConnectionParameters clientParams)
        {
            return new ConnectionInfo(clientParams.Hostname, clientParams.Username,
                privateKeyObject(clientParams.Username, PublicKey));
        }

        private string SetRemoteDirectory(SftpClient sftpClient, string[] fileParts)
        {
            string remotePath = string.Empty;

            foreach (string path in fileParts)
            {
                remotePath += "/" + path;

                try
                {
                    SftpFileAttributes attrs = sftpClient.GetAttributes(remotePath);

                    if (!attrs.IsDirectory)
                    {
                        Logger.error($"SftpConnection transfer unable to create remote directory");
                    }
                }
                catch (SftpPathNotFoundException)
                {
                    sftpClient.CreateDirectory(remotePath);
                }
            }

            return remotePath;
        }

        private bool FileExistsInServer(SftpClient sftpClient, string remotePath)
        {
            try
            {
                SftpFileAttributes attrs = sftpClient.GetAttributes(remotePath);

                if (attrs.IsRegularFile)
                {
                    Logger.error($"SftpConnection file exists on server");
                    return true;
                }
            }
            catch (SftpPathNotFoundException ex)
            {
                Logger.error($"SftpConnection file lookup exception={ex.Message}");
            }

            return false;
        }

        private bool TransferFileWithPassword(SftpConnectionParameters clientParams)
        {
            try
            {
                using (SftpClient sftpClient = new SftpClient(clientParams.Hostname, clientParams.Username, clientParams.Password))
                {
                    sftpClient.Connect();

                    // Create directories as necessary
                    string[] fileParts = FileDirectoryTarget.GetFileTimeStamp(clientParams.Filename);

                    // set directory
                    string remotePath = SetRemoteDirectory(sftpClient, fileParts);

                    using (FileStream fs = new FileStream(clientParams.Filename, FileMode.Open))
                    {
                        sftpClient.BufferSize = 1024;
                        sftpClient.ChangeDirectory(remotePath);
                        
                        // set target directory
                        string filename = Path.GetFileName(clientParams.Filename);

                        // upload file to sftp server
                        if (!FileExistsInServer(sftpClient, filename))
                        {
                            sftpClient.UploadFile(fs, Path.GetFileName(clientParams.Filename));
                        }
                    }

                    sftpClient.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.error($"SftpConnection transfer failed with exception={ex.Message}");
            }

            return false;
        }

        private void TransferFileWithPublicKey(SftpConnectionParameters clientParams)
        {
            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(SftpConstants.TargetDirectory))
                {
                    Directory.CreateDirectory(SftpConstants.TargetDirectory);
                }
                if (FindEmbeddedResourceByName(SftpConstants.PublicKey, targetFile))
                {
                    PublicKey = targetFile;
                    using (SftpClient sftpClient = new SftpClient(GetSftpConnection(clientParams)))
                    {
                        sftpClient.Connect();

                        string[] fileParts = FileDirectoryTarget.GetFileTimeStamp(clientParams.Filename);

                        using (FileStream fs = new FileStream(clientParams.Filename, FileMode.Open))
                        {
                            sftpClient.BufferSize = 1024;
                            sftpClient.UploadFile(fs, Path.GetFileName(clientParams.Filename));
                        }

                        sftpClient.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.error($"SftpConnection transfer failed with exception={ex.Message}");
            }
        }

        public bool TransferFile(SftpConnectionParameters clientParams)
        {
            try
            {
                return TransferFileWithPassword(clientParams);
            }
            catch (Exception ex)
            {
                Logger.error($"SftpConnection exception={ex.Message}");
                return false;
            }
            finally
            {
                // Remove file
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
            }
        }
    }
}
