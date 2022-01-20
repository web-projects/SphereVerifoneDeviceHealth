using Common.Helpers;
using Common.LoggerManager;
using Config.AppConfig;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FileTransferAppLauncher
{
    public static class TransferAppLauncher
    {
        #region --- Win32 API ---
        // window position
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
        private const short SWP_NOMOVE = 0x02;
        private const short SWP_NOSIZE = 1;
        private const short SWP_NOZORDER = 0x04;
        private const int SWP_SHOWWINDOW = 0x0040;

        public delegate bool CallBackPtr(IntPtr hwnd, int lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        private static extern int EnumWindows(CallBackPtr callPtr, int lPar);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        #endregion --- Win32 API ---

        private static string parentWindowName = string.Empty;
        private static RECT parentWindowRectangle = new RECT();

        static private AppSection GetAppConfiguration()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return configuration.GetSection("Launcher").Get<AppSection>();
        }

        public static List<IntPtr> EnumWindows()
        {
            var result = new List<IntPtr>();

            EnumWindows(new TransferAppLauncher.CallBackPtr((hwnd, lParam) =>
            {
                result.Add(hwnd);
                return true;
            }), 0);

            return result;
        }

        static private void SetParentWindowRectangle()
        {
            IntPtr hWnd = Process.GetProcesses().First(x => x.MainWindowTitle.Contains(parentWindowName))?.Handle ?? IntPtr.Zero;

            if (hWnd != IntPtr.Zero)
            {
                parentWindowRectangle = new RECT();
                GetWindowRect(hWnd, out parentWindowRectangle);
            }
        }

        static private (Process, bool) Launch(string workingDirectory, string fullFileName, string arguments)
        {
            try
            {
                if (ProcessHelpers.IsRunning(fullFileName))
                {
                    Logger.info($"{Utils.FormatStringAsRequired($"SYSTEM: Process already running ", Utils.DeviceLogKeyValueLength, Utils.DeviceLogKeyValuePaddingCharacter)} : '{fullFileName}'");
                    return (null, true);
                }
                else
                {
                    Process process = new Process();

                    process.StartInfo.Verb = "runas";
                    process.StartInfo.FileName = fullFileName;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.WorkingDirectory = workingDirectory;

                    if (arguments is { })
                    {
                        process.StartInfo.Arguments = arguments;
                    }

                    // UseShellExecute = false required to redirect output streams
                    //process.StartInfo.RedirectStandardError = true;
                    //process.StartInfo.RedirectStandardOutput = true;
                    //process.StartInfo.RedirectStandardInput = false;

                    if (!process.Start())
                    {
                        Logger.warning($"Unable to start process '{fullFileName}'.");
                    }
                    else
                    {
                        if (parentWindowRectangle.Left > 0 && parentWindowRectangle.Bottom > 0)
                        {
                            SetWindowPos(process.MainWindowHandle, parentWindowRectangle.Left, parentWindowRectangle.Bottom, HWND_NOTOPMOST,
                                Console.WindowWidth, Console.WindowHeight, SWP_NOZORDER | SWP_SHOWWINDOW);
                        }
                    }

                    //process.BeginOutputReadLine();
                    //process.BeginErrorReadLine();

                    return (process, true);
                }
            }
            catch (Exception ex)
            {
                Logger.error($"Exception while attempting to launch process '{fullFileName}' - {ex.Message}");
                return (null, false);
            }
        }

        static public void StartAllProcesses(string parentWindow)
        {
            // parent window is main executable
            parentWindowName = parentWindow + ".exe";

            AppSection appSection = GetAppConfiguration();

            SetParentWindowRectangle();

            foreach (AppConfiguration appConfiguration in appSection.Apps)
            {
                (Process process, bool status) = Launch(Directory.GetCurrentDirectory(), appConfiguration.Name, appConfiguration.Arguments);

                if ((process == null || process.Id <= 0) && status == false)
                {
                    Logger.error($"Unable to launch '{appConfiguration.Name}' as a child process.");
                }
            }
        }
    }
}
