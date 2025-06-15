using System;

namespace Mzl.Video.Process.Models;

/// <summary>
/// 转换任务模型
/// </summary>
public class ConversionTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string InputPath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public string OutputFormat { get; set; } = "mp4";
    public VideoQuality Quality { get; set; } = VideoQuality.Normal;
    public VideoResolution Resolution { get; set; } = VideoResolution.Original;
    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
    public int Progress { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    public TimeSpan? ElapsedTime => StartedAt.HasValue ? 
        (CompletedAt ?? DateTime.Now) - StartedAt.Value : null;
}

public enum VideoQuality
{
    High,
    Normal,
    Low,
    Custom
}

public enum VideoResolution
{
    Original,
    UHD_4K,      // 3840x2160
    QHD_2K,      // 2560x1440
    FHD_1080p,   // 1920x1080
    HD_720p,     // 1280x720
    SD_480p      // 854x480
}

public enum ConversionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
