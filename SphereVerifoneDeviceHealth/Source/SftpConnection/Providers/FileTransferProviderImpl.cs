using Renci.SshNet;

namespace FileTransfer.Providers
{
    public class FileTransferProviderImpl : IFileTransferProvider
    {
        public static SftpConnection sftpConnection;

        public FileTransferProviderImpl()
        {
            sftpConnection = new SftpConnection();
        }

        public SftpConnection GetSftpConnection()
        {
            return sftpConnection;
        }

        public ConnectionInfo GetSftpConnection(SftpConnectionParameters clientParams)
        {
            return sftpConnection.GetSftpConnection(clientParams); ;
        }

        public void TransferFile(SftpConnectionParameters clientParams)
        {
            sftpConnection.TransferFile(clientParams);
        }
    }
}
