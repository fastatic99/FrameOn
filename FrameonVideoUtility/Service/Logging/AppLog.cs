using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FrameonVideoUtility.Service.Logging;

public static class AppLog
{
    private const int MaxLogLines = 500;
    private const int MaxReportLogLines = 100;

    private static readonly List<string> LogLines = new();

    public static event Action<string>? LogReceived;

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Warning(string message) => Write("WARN", message);
    public static void Debug(string message) => Write("DEBUG", message);

    public static string GetLogText()
    {
        lock (LogLines)
        {
            if (LogLines.Count == 0)
            {
                return "No logs have been recorded yet.";
            }

            var builder = new StringBuilder();

            foreach (string line in LogLines)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }

    public static string[] GetRecentLogLinesForReport()
    {
        lock (LogLines)
        {
            return LogLines
                .TakeLast(MaxReportLogLines)
                .ToArray();
        }
    }

    public static string GetPersistentLogFilePath()
    {
        string logDirectory = Path.Combine(Path.GetTempPath(), "FrameOn", "Logs");
        Directory.CreateDirectory(logDirectory);
        return Path.Combine(logDirectory, "FrameOn.log");
    }

    public static void Clear()
    {
        lock (LogLines)
        {
            LogLines.Clear();
        }

        LogReceived?.Invoke("[LOG] Logs cleared.");
    }

    private static void Write(string level, string message)
    {
        string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        lock (LogLines)
        {
            LogLines.Add(logLine);

            if (LogLines.Count > MaxLogLines)
            {
                int removeCount = LogLines.Count - MaxLogLines;
                LogLines.RemoveRange(0, removeCount);
            }
        }

        Console.WriteLine(logLine);

        try
        {
            File.AppendAllText(GetPersistentLogFilePath(), logLine + Environment.NewLine);
        }
        catch
        {
        }

        LogReceived?.Invoke(logLine);
    }
}
