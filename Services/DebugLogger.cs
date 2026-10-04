using System;
using System.Diagnostics;

namespace HostelManagementSystem.Services;

public static class DebugLogger
{
    public static void Log(string message)
    {
        Debug.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] [HMS] {message}"
        );
    }

    public static void Error(
        string message,
        Exception? exception = null)
    {
        Debug.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] [HMS ERROR] {message}"
        );

        if (exception != null)
        {
            Debug.WriteLine(
                exception.ToString()
            );
        }
    }
}

