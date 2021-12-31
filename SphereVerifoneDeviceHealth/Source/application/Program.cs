using Common.Config.Config;
using Common.Execution;
using Common.LoggerManager;
using Common.XO.Requests;
using Execution;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DEVICE_CORE
{
    class Program
    {
        #region --- Win32 API ---
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_SIZE = 0xF000;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        #endregion --- Win32 API ---

        const int STARTUP_WAIT_DELAY = 2048;
        const int COMMAND_WAIT_DELAY = 4096;
        const int CONFIGURATION_UPDATE_DELAY = 6144;
        static readonly DeviceActivator activator = new DeviceActivator();

        static readonly string[] MENU = new string[]
        {
            " ",
            "============ [ MENU ] ============",
            " c => UPDATE EMV CONFIGURATION",
            " i => UPDATE RAPTOR IDLE SCREEN",
            " k => EMV-KERNEL VALIDATION",
            " r => REBOOT",
            " s => STATUS",
            //" t => TEST HMAC SECRETS",
            //" u => UPDATE HMAC SECRETS",
            " v => ACTIVE ADE SLOT",
            " 0 => LOCK ADE SLOT-0 (PROD)",
            " 8 => LOCK ADE SLOT-8 (TEST)",
            " O => UNLOCK",
            " m => menu",
            " q => QUIT",
            "  "
        };


        static bool applicationIsExiting = false;
        static async Task Main(string[] args)
        {
            SetupWindow();

            // setup working environment
            (DirectoryInfo di, bool allowDebugCommands, Modes.Execution executionMode,
                string healthCheckValidationMode, bool displayProgressBar, bool terminalBypassHealthRecord) = SetupEnvironment();

            if (executionMode == Modes.Execution.Undefined)
            {
                ParseArguments(args);
                executionMode = ParseArguments(args);
            }

            // save current colors
            ConsoleColor foreGroundColor = Console.ForegroundColor;
            ConsoleColor backGroundColor = Console.BackgroundColor;

            // Device discovery
            string pluginPath = Path.Combine(Environment.CurrentDirectory, "DevicePlugins");

            IDeviceApplication application = activator.Start(pluginPath);

            await application.Run(new AppExecConfig
            {
                TerminalBypassHealthRecord = terminalBypassHealthRecord,
                DisplayProgressBar = displayProgressBar,
                ForeGroundColor = foreGroundColor,
                BackGroundColor = backGroundColor,
                ExecutionMode = executionMode,
                HealthCheckValidationMode = healthCheckValidationMode
            }).ConfigureAwait(false);

            switch (executionMode)
            {
                case Modes.Execution.Console:
                {
                    await ConsoleModeOperation(application, allowDebugCommands);
                    break;
                }

                case Modes.Execution.StandAlone:
                {
                    StandAloneOperation(application, Modes.Execution.StandAlone);
                    break;
                }
            }

            applicationIsExiting = true;

            application.Shutdown();

            // delete working directory
            DeleteWorkingDirectory(di);
        }

        static private (DirectoryInfo, bool, Modes.Execution, string, bool, bool) SetupEnvironment()
        {
            DirectoryInfo di = null;

            // create working directory
            if (!Directory.Exists(Constants.TargetDirectory))
            {
                di = Directory.CreateDirectory(Constants.TargetDirectory);
            }

            // Get appsettings.json config - AddEnvironmentVariables() requires package: Microsoft.Extensions.Configuration.EnvironmentVariables
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // logger manager
            SetLogging(configuration);

            // Screen Colors
            SetScreenColors(configuration);

            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            Modes.Execution executionMode = GetApplicationExecutionMode(configuration);
            string healthCheckValidationMode = null;

            if (executionMode == Modes.Execution.StandAlone)
            {
                healthCheckValidationMode = GetHealthCheckStatusSetup(configuration);
            }

            bool displayProgressBar = GetApplicationDisplayProgressBar(configuration);
            bool terminalBypassHealthRecord = GetApplicationTerminalBypassHealthRecord(configuration);

            return (di, AllowDebugCommands(configuration, 0), executionMode, healthCheckValidationMode, displayProgressBar, terminalBypassHealthRecord);
        }

        static private void SetupWindow()
        {
            Console.BufferHeight = Int16.MaxValue - 1;
            //Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
            Console.CursorVisible = false;

            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);

            if (handle != IntPtr.Zero)
            {
                //DeleteMenu(sysMenu, SC_MINIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
            }
        }

        static void ScrollWindowAsNeeded(IDeviceApplication application, Modes.Execution executionMode)
        {
            if (executionMode == Modes.Execution.StandAlone)
            {
                Task.Run(async () =>
                {
                    while (!applicationIsExiting)
                    {
                        await Task.Delay(1000);

                        if (!application.ProgressBarIsActive())
                        {
                            if (Console.CursorTop >= Console.WindowHeight)
                            {
                                // Scoll one line:
                                Console.MoveBufferArea(0, 2, Console.WindowWidth, Console.WindowHeight - 2, 0, 1);
                            }
                        }
                    }
                });
            }
        }

        static Modes.Execution ParseArguments(string[] args)
        {
            Modes.Execution mode = Modes.Execution.Console;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "/C":
                    {
                        mode = Modes.Execution.Console;
                        break;
                    }

                    case "/S":
                    {
                        mode = Modes.Execution.StandAlone;
                        break;
                    }
                }
            }

            return mode;
        }

        static async Task ConsoleModeOperation(IDeviceApplication application, bool allowDebugCommands)
        {
            // GET STATUS
            //await application.Command(LinkDeviceActionType.GetStatus).ConfigureAwait(false);

            DisplayMenu();

            ConsoleKeyInfo keypressed = GetKeyPressed(false);

            while (keypressed.Key != ConsoleKey.Q)
            {
                bool redisplay = false;

                // Check for <ALT> key combinations
                if ((keypressed.Modifiers & ConsoleModifiers.Alt) != 0)
                {
                    if (allowDebugCommands)
                    {
                        switch (keypressed.Key)
                        {
                            case ConsoleKey.A:
                            {
                                break;
                            }
                            case ConsoleKey.D:
                            {
                                //Console.WriteLine("\r\nCOMMAND: [DATETIME]");
                                await application.Command(LinkDeviceActionType.SetTerminalDateTime).ConfigureAwait(false);
                                break;
                            }
                            case ConsoleKey.F:
                            {
                                await application.Command(LinkDeviceActionType.GetSphereHealthFile).ConfigureAwait(false);
                                break;
                            }
                            case ConsoleKey.M:
                            {
                                await application.Command(LinkDeviceActionType.ManualCardEntry).ConfigureAwait(false);
                                break;
                            }
                            case ConsoleKey.R:
                            {
                                await application.Command(LinkDeviceActionType.ReportEMVKernelVersions).ConfigureAwait(false);
                                break;
                            }
                            case ConsoleKey.T:
                            {
                                //Console.WriteLine("\r\nCOMMAND: [TEST]");
                                await application.Command(LinkDeviceActionType.GenerateHMAC).ConfigureAwait(false);
                                break;
                            }
                            case ConsoleKey.U:
                            {
                                //Console.WriteLine("\r\nCOMMAND: [UPDATE]");
                                await application.Command(LinkDeviceActionType.UpdateHMACKeys).ConfigureAwait(false);
                                break;
                            }
                            case ConsoleKey.V:
                            {
                                //Console.WriteLine("\r\nCOMMAND: [VERSION]");
                                await application.Command(LinkDeviceActionType.VIPAVersions).ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    switch (keypressed.Key)
                    {
                        case ConsoleKey.M:
                        {
                            Console.WriteLine("");
                            DisplayMenu();
                            break;
                        }
                        case ConsoleKey.C:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [CONFIGURATION]");
                            await application.Command(LinkDeviceActionType.Configuration).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.H:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [24_HOUR_REBOOT]");
                            await application.Command(LinkDeviceActionType.Reboot24Hour).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.I:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [UPDATE_IDLE_SCREEN]");
                            await application.Command(LinkDeviceActionType.UpdateIdleScreen).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.O:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [UNLOCK]");
                            await application.Command(LinkDeviceActionType.UnlockDeviceConfig).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.K:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [EMV-KERNEL]");
                            await application.Command(LinkDeviceActionType.GetEMVKernelChecksum).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.R:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [REBOOT]");
                            //await application.Command(LinkDeviceActionType.RebootDevice).ConfigureAwait(false);
                            await application.Command(LinkDeviceActionType.VIPARestart).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.S:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [STATUS]");
                            await application.Command(LinkDeviceActionType.GetSecurityConfiguration).ConfigureAwait(false);
                            //Task.Run(async () => await application.Command(LinkDeviceActionType.GetSecurityConfiguration)).GetAwaiter().GetResult();
                            break;
                        }
                        case ConsoleKey.V:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [SLOT]");
                            await application.Command(LinkDeviceActionType.GetActiveKeySlot).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.D0:
                        case ConsoleKey.NumPad0:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [LOCK]");
                            await application.Command(LinkDeviceActionType.LockDeviceConfig0).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.D8:
                        case ConsoleKey.NumPad8:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [LOCK]");
                            await application.Command(LinkDeviceActionType.LockDeviceConfig8).ConfigureAwait(false);
                            break;
                        }
                        case ConsoleKey.X:
                        {
                            //Console.WriteLine("\r\nCOMMAND: [DEVICE-EXTENDED-RESET]");
                            await application.Command(LinkDeviceActionType.DeviceExtendedReset).ConfigureAwait(false);
                            break;
                        }
                        default:
                        {
                            redisplay = false;
                            break;
                        }
                    }
                }

                keypressed = GetKeyPressed(redisplay);
            }

            Console.WriteLine("\r\nCOMMAND: [QUIT]\r\n");
        }

        static void StandAloneOperation(IDeviceApplication application, Modes.Execution executionMode)
        {
            // setup automatic scrolling
            //ScrollWindowAsNeeded(application, executionMode);

            ConsoleKeyInfo keypressed = new ConsoleKeyInfo();

            while (keypressed.Key != ConsoleKey.Q)
            {
                keypressed = GetKeyPressed(false);
            }
        }

        static private void DeleteWorkingDirectory(DirectoryInfo di)
        {
            if (di == null)
            {
                di = new DirectoryInfo(Constants.TargetDirectory);
            }

            if (di != null)
            {
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                di.Delete();
            }
            else if (Directory.Exists(Constants.TargetDirectory))
            {
                di = new DirectoryInfo(Constants.TargetDirectory);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                Directory.Delete(Constants.TargetDirectory);
            }
        }

        static private ConsoleKeyInfo GetKeyPressed(bool redisplay)
        {
            if (redisplay)
            {
                Console.Write("SELECT COMMAND: ");
            }
            return Console.ReadKey(true);
        }

        static private void DisplayMenu()
        {
            foreach (string value in MENU)
            {
                Console.WriteLine(value);
            }

            Console.Write("SELECT COMMAND: ");
        }

        static bool AllowDebugCommands(IConfiguration configuration, int index)
        {
            return configuration.GetSection("Devices:Verifone").GetValue<bool>("AllowDebugCommands");
        }

        static string[] GetLoggingLevels(IConfiguration configuration, int index)
        {
            return configuration.GetSection("LoggerManager:Logging").GetValue<string>("Levels").Split("|");
        }

        static void SetLogging(IConfiguration configuration)
        {
            try
            {
                string[] logLevels = GetLoggingLevels(configuration, 0);

                if (logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = Directory.GetCurrentDirectory();
                    string filepath = path + "\\logs\\" + logname;

                    int levels = 0;
                    foreach (var item in logLevels)
                    {
                        foreach (var level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
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

        static void SetScreenColors(IConfiguration configuration)
        {
            try
            {
                // Set Foreground color
                Console.ForegroundColor = GetColor(configuration.GetSection("Application:Colors").GetValue<string>("ForeGround"));

                // Set Background color
                Console.BackgroundColor = GetColor(configuration.GetSection("Application:Colors").GetValue<string>("BackGround"));

                Console.Clear();
            }
            catch (Exception ex)
            {
                Logger.error("main: SetScreenColors() - exception={0}", ex.Message);
            }
        }

        static bool GetApplicationDisplayProgressBar(IConfiguration configuration)
        {
            return configuration.GetValue<bool>("Application:DisplayProgressBar");
        }

        static bool GetApplicationTerminalBypassHealthRecord(IConfiguration configuration)
        {
            return configuration.GetValue<bool>("Application:TerminalBypassHealthRecord");
        }

        static Modes.Execution GetApplicationExecutionMode(IConfiguration configuration)
        {
            return GetExecutionMode(configuration.GetValue<string>("Application:ExecutionMode"));
        }

        static string GetHealthCheckStatusSetup(IConfiguration configuration)
        {
            return configuration.GetSection("Devices:Verifone").GetValue<string>("HealthStatusValidationRequired");
        }

        static ConsoleColor GetColor(string color) => color switch
        {
            "BLACK" => ConsoleColor.Black,
            "DARKBLUE" => ConsoleColor.DarkBlue,
            "DARKGREEEN" => ConsoleColor.DarkGreen,
            "DARKCYAN" => ConsoleColor.DarkCyan,
            "DARKRED" => ConsoleColor.DarkRed,
            "DARKMAGENTA" => ConsoleColor.DarkMagenta,
            "DARKYELLOW" => ConsoleColor.DarkYellow,
            "GRAY" => ConsoleColor.Gray,
            "DARKGRAY" => ConsoleColor.DarkGray,
            "BLUE" => ConsoleColor.Blue,
            "GREEN" => ConsoleColor.Green,
            "CYAN" => ConsoleColor.Cyan,
            "RED" => ConsoleColor.Red,
            "MAGENTA" => ConsoleColor.Magenta,
            "YELLOW" => ConsoleColor.Yellow,
            "WHITE" => ConsoleColor.White,
            _ => throw new Exception($"Invalid color identifier '{color}'.")
        };

        static Modes.Execution GetExecutionMode(string mode) => mode switch
        {
            "StandAlone" => Modes.Execution.StandAlone,
            "Console" => Modes.Execution.Console,
            _ => Modes.Execution.StandAlone
        };
    }
}
