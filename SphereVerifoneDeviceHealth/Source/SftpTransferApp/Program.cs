using Common.Constants;
using Common.LoggerManager;
using FileTransfer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SftpTransfer
{
    class Program
    {
        static string filePath = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(LogDirectories.LogDirectory, LogDirectories.PendingDirectory));
        static SftpConnection sftpConnection = new SftpConnection();
        static SftpConnectionParameters sftpConnectionParameters;

        static private IConfiguration configuration;
        static private string parentLog;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Checking for the existance of pending files to upload...");
            Debug.WriteLine($"TARGET PATH=[{filePath}]");

            SetupEnvironment(args);

            bool filesLocated = false;
            Stopwatch sw =new Stopwatch();

            Dictionary<string, bool> pendingFileMap = new Dictionary<string, bool>();
            for (int index = 0; index < 3; index++)
            {
                foreach (string pendingTransferFile in Directory.EnumerateFiles(filePath))
                {
                    string fileNameKey = Path.GetFileNameWithoutExtension(pendingTransferFile);
                    filesLocated = true;

                    sw.Restart();
                    Console.WriteLine($"\nAttempting to upload file '{fileNameKey}' to SFTP. Please wait..");
                    if (AttemptSftpFileTransfer(pendingTransferFile))
                    {
                        sw.Stop();

                        MoveFileToCompletedDirectory(pendingTransferFile);
                        Console.WriteLine($"FILE TRANSFERRED=[{fileNameKey}] in {sw.ElapsedMilliseconds:N0}ms");

                        if (pendingFileMap.ContainsKey(fileNameKey))
                        {
                            Console.WriteLine($"Pending file '{fileNameKey}' that failed previous upload attempt has now been successfully uploaded!");
                            pendingFileMap.Remove(fileNameKey);
                        }
                    }
                    else
                    {
                        if (!pendingFileMap.ContainsKey(fileNameKey))
                        {
                            pendingFileMap.Add(fileNameKey, false);
                        }
                        Console.WriteLine($"Encountered an issue while attempting to upload file '{pendingTransferFile}'.");
                    }
                }

                if (pendingFileMap.Count > 0)
                {
                    Console.WriteLine($"\n(Attempt {index + 1} of 3) Waiting a second before re-attempting an SFTP retry upload.");
                    await Task.Delay(1000);
                }
                else if (!filesLocated)
                {
                    break;
                }
            }

            if (filesLocated)
            {
                Console.WriteLine("\nFILE TRANSFER PROCESS COMPLETED.");
            }
            else
            {
                Console.WriteLine("\nNO PENDING FILES WERE FOUND TO UPLOAD TO SFTP. PROGRAM COMPLETE!");
            }
            await Task.Delay(2000);
        }

        static void ParseArguments(string[] args)
        {
            if (args.Length > 0)
            {
                int mainlogArgIndex = Array.IndexOf(args, "--mainlog");

                if (mainlogArgIndex >= 0 && mainlogArgIndex + 1 < args.Length)
                {
                    parentLog = args[mainlogArgIndex + 1];
                }
            }
        }

        static void SetupEnvironment(string[] args)
        {
            // attempt to synchronize writing to the main log - otherwise, create a separate log for sftp transfer status/errors
            //ParseArguments(args);

            // Get appsettings.json config - AddEnvironmentVariables() requires package: Microsoft.Extensions.Configuration.EnvironmentVariables
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // logger
            SetLogging();

            // sftp parameters
            sftpConnectionParameters = GetSftpConfiguration();
        }

        static bool AttemptSftpFileTransfer(string filename)
        {
            sftpConnectionParameters.Filename = filename;
            return sftpConnection.TransferFile(sftpConnectionParameters);
        }

        static void MoveFileToCompletedDirectory(string filename)
        {
            try
            {
                string targetFilename = Path.GetFileName(filename);
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(LogDirectories.LogDirectory, LogDirectories.CompletedDirectory));
                filePath = Path.Combine(filePath, targetFilename);
                File.Move(filename, filePath);
                Logger.info($"DEVICE: FILE IN COMPLETED DIRECTORY _______ : {targetFilename}");
            }
            catch (Exception ex)
            {
                Logger.error($"DEVICE: FILE TRANSFER EXCEPTION={ex.Message}");
            }
        }

        static SftpConnectionParameters GetSftpConfiguration()
        {
            // Microsoft.Extensions.Configuration.Json
            return configuration.GetSection(nameof(SftpConnectionParameters)).Get<SftpConnectionParameters>();
        }

        static string[] GetLoggingLevels(int index)
        {
            return configuration.GetSection("LoggerManager:Logging").GetValue<string>("Levels").Split("|");
        }

        static void SetLogging()
        {
            try
            {
                string[] logLevels = GetLoggingLevels(0);

                if (logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = string.IsNullOrWhiteSpace(parentLog) ? Path.GetFileNameWithoutExtension(fullName) + ".log" : parentLog;
                    string path = Directory.GetCurrentDirectory();
                    string filepath = path + "\\logs\\" + logname;

                    int levels = 0;
                    foreach (string item in logLevels)
                    {
                        foreach (LOGLEVELS level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            levels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(filepath, levels);

                    Logger.info($"{Assembly.GetEntryAssembly().GetName().Name} ({Assembly.GetEntryAssembly().GetName().Version}) - LOGGING INITIALIZED.");
                }
            }
            catch (Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", e.Message);
            }
        }
    }
}
