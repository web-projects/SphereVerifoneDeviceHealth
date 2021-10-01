using Common.Execution;
using System;

namespace Execution
{
    public class AppExecConfig
    {
        public bool DisplayProgressBar;
        public ConsoleColor ForeGroundColor { get; set; }
        public ConsoleColor BackGroundColor { get; set; }
        public Modes.Execution ExecutionMode { get; set; }
        public string HealthCheckValidationMode { get; set; }
    }
}
