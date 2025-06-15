using System;
using System.Windows;

namespace Mzl.Video.Process.Models;

/// <summary>
/// 水印移除任务模型
/// </summary>
public class WatermarkRemovalTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string InputPath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public Rect WatermarkArea { get; set; }
    public WatermarkRemovalMethod Method { get; set; } = WatermarkRemovalMethod.Blur;
    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
    public int Progress { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    public TimeSpan? ElapsedTime => StartedAt.HasValue ? 
        (CompletedAt ?? DateTime.Now) - StartedAt.Value : null;
}

public enum WatermarkRemovalMethod
{
    Blur,      // 模糊处理
    Mosaic,    // 马赛克
    Crop,      // 裁剪移除
    Inpaint,   // 智能填充
    Delogo     // Delogo滤镜去除
}
