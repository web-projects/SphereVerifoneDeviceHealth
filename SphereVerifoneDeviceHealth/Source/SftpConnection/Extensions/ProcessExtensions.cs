using System;
using System.Diagnostics;

public static class ProcessExtensions
{
    public static bool IsRunning(this Process process)
    {
        if (process == null)
        {
            return false;
        }

        try
        {
            Process.GetProcessById(process.Id);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }
}