using System;

namespace FileTransfer
{
    [Serializable]
    public class SftpConnectionParameters
    {
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Filename { get; set; }
        public int Port { get; set; }
    }
}
