using System;
using System.IO;

namespace EntertainingIsland.Services;

/// <summary>
/// 简单的文件日志器，将日志写入插件目录下的 Logs 文件夹。
/// 方便不会 C# 的用户查看运行状态。
/// </summary>
public static class Logger
{
    private static readonly string LogDir;
    private static readonly object LockObj = new();

    static Logger()
    {
        // 日志目录放在插件 DLL 所在目录的 Logs 子文件夹
        var dllDir = Path.GetDirectoryName(typeof(Logger).Assembly.Location);
        LogDir = Path.Combine(dllDir ?? ".", "Logs");
        try { Directory.CreateDirectory(LogDir); } catch { }
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(string message)
    {
        Write("ERROR", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    private static void Write(string level, string message)
    {
        try
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var logFile = Path.Combine(LogDir, $"teacher-alert-{today}.log");
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
            lock (LockObj)
            {
                File.AppendAllText(logFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // 日志写入失败不应影响主功能
        }
    }

    /// <summary>
    /// 获取最新日志文件路径
    /// </summary>
    public static string GetLatestLogPath()
    {
        try
        {
            var files = Directory.GetFiles(LogDir, "teacher-alert-*.log");
            return files.Length > 0
                ? files.OrderByDescending(f => f).First()
                : Path.Combine(LogDir, "(暂无日志)");
        }
        catch
        {
            return LogDir;
        }
    }
}
