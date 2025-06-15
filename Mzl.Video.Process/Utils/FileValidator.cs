using System;
using System.IO;
using System.Linq;

namespace Mzl.Video.Process.Utils;

/// <summary>
/// 文件验证工具
/// </summary>
public static class FileValidator
{
    /// <summary>
    /// 支持的视频文件扩展名
    /// </summary>
    private static readonly string[] SupportedVideoExtensions = 
    {
        ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".m3u8", ".ts", ".webm", ".3gp"
    };
    
    /// <summary>
    /// 验证是否为支持的视频文件
    /// </summary>
    public static bool IsValidVideoFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;
            
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedVideoExtensions.Contains(extension);
    }
    
    /// <summary>
    /// 验证文件路径是否有效
    /// </summary>
    public static bool IsValidFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;
            
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            return !string.IsNullOrEmpty(directory) && Directory.Exists(directory);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 验证输出文件路径是否有效
    /// </summary>
    public static bool IsValidOutputPath(string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath))
            return false;
            
        try
        {
            var directory = Path.GetDirectoryName(outputPath);
            var fileName = Path.GetFileName(outputPath);
            
            return !string.IsNullOrEmpty(directory) && 
                   !string.IsNullOrEmpty(fileName) && 
                   Directory.Exists(directory);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 获取安全的文件名（移除非法字符）
    /// </summary>
    public static string GetSafeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "output";
            
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        return string.IsNullOrEmpty(safeName) ? "output" : safeName;
    }
    
    /// <summary>
    /// 生成唯一的输出文件名
    /// </summary>
    public static string GenerateUniqueFileName(string directory, string baseName, string extension)
    {
        if (!Directory.Exists(directory))
            return Path.Combine(directory, $"{baseName}.{extension}");
            
        var fileName = $"{baseName}.{extension}";
        var fullPath = Path.Combine(directory, fileName);
        
        if (!File.Exists(fullPath))
            return fullPath;
            
        var counter = 1;
        do
        {
            fileName = $"{baseName}_{counter}.{extension}";
            fullPath = Path.Combine(directory, fileName);
            counter++;
        }
        while (File.Exists(fullPath) && counter < 1000);
        
        return fullPath;
    }
    
    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}
