using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Maui.Storage;

namespace MerelyApp;

public static class AppLogger
{
    private static readonly object _lock = new object();

    private static string LogFilePath
    {
        get
        {
            try
            {
                return Path.Combine(FileSystem.AppDataDirectory, "app.log");
            }
            catch
            {
                return "app.log";
            }
        }
    }

    public static void Log(string message)
    {
        try
        {
            var line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
            Debug.WriteLine(line);
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, line);
            }
        }
        catch
        {
            // swallow logging failures
        }
    }

    public static void Log(Exception ex, string? context = null)
    {
        try
        {
            var header = context is null ? "Exception" : context;
            var msg = $"{header}: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}";
            Log(msg);
            if (ex.InnerException != null)
            {
                Log(ex.InnerException, "InnerException");
            }
        }
        catch
        {
            // swallow
        }
    }
}
