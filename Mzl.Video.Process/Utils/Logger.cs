using System;
using System.IO;
using System.Text;

namespace Mzl.Video.Process.Utils;

/// <summary>
/// 简单的日志记录工具
/// </summary>
public static class Logger
{
    private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mzl.Video.Process", "Logs");
    private static readonly string LogFile = Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
    
    static Logger()
    {
        // 确保日志目录存在
        Directory.CreateDirectory(LogDirectory);
    }
    
    /// <summary>
    /// 记录信息日志
    /// </summary>
    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }
    
    /// <summary>
    /// 记录警告日志
    /// </summary>
    public static void Warning(string message)
    {
        WriteLog("WARN", message);
    }
    
    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void Error(string message)
    {
        WriteLog("ERROR", message);
    }
    
    /// <summary>
    /// 记录错误日志（包含异常信息）
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        WriteLog("ERROR", $"{message}: {ex.Message}\n{ex.StackTrace}");
    }
    
    /// <summary>
    /// 写入日志
    /// </summary>
    private static void WriteLog(string level, string message)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(LogFile, logEntry + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // 忽略日志写入错误
        }
    }
    
    /// <summary>
    /// 获取日志目录路径
    /// </summary>
    public static string GetLogDirectory()
    {
        return LogDirectory;
    }
    
    /// <summary>
    /// 清理旧日志文件（保留最近7天）
    /// </summary>
    public static void CleanupOldLogs()
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-7);
            var logFiles = Directory.GetFiles(LogDirectory, "app-*.log");
            
            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }
}
