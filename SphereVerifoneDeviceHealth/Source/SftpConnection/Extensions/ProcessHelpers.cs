using System.Diagnostics;

public static class ProcessHelpers
{
    public static bool IsRunning(string name) => Process.GetProcessesByName(name).Length > 0;
}
