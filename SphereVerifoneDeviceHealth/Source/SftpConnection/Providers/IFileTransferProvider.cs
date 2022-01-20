using Renci.SshNet;

namespace FileTransfer.Providers
{
    public interface IFileTransferProvider
    {
        SftpConnection GetSftpConnection();
        ConnectionInfo GetSftpConnection(SftpConnectionParameters clientParams);
        void TransferFile(SftpConnectionParameters clientParams);
    }
}
