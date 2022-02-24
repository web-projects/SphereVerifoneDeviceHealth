using System;
using System.IO;

namespace FileTransfer.Helpers
{
    public static class FileDirectoryTarget
    {
        public static readonly string SfptTargetDirectory = "/Uploads/ValidationLogs";

        public static string[] GetFileTimeStamp(string filename)
        {
            if (File.Exists(filename))
            {
                FileInfo fileInfo = new FileInfo(filename);
                return new string[] { fileInfo.CreationTime.Year.ToString(), fileInfo.CreationTime.Month.ToString(), fileInfo.CreationTime.Day.ToString() };
            }
            return Array.Empty<string>();
        }
    }
}
