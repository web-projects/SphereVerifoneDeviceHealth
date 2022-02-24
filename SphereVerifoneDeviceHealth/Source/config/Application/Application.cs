using Common.Execution;
using System;

namespace Config.Application
{
    [Serializable]
    public class Application
    {
        public Colors Colors { get; set; }
        public Modes.Execution ExecutionMode { get; set; } = Modes.Execution.StandAlone;
        public bool TerminalBypassHealthRecord { get; set; }
        public bool DisplayProgressBar { get; set; }
    }

    [Serializable]
    public class Colors
    {
        public string ForeGround { get; set; } = "WHITE";
        public string BackGround { get; set; } = "BLUE";
    }
}
