using System;

namespace Mzl.Video.Process.Models;

/// <summary>
/// 视频信息模型
/// </summary>
public class VideoInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Format { get; set; } = "";
    public long FileSize { get; set; }
    public string VideoCodec { get; set; } = "";
    public string AudioCodec { get; set; } = "";
    public double FrameRate { get; set; }
    public string Resolution => $"{Width}x{Height}";
    public string DurationText => $"{(int)Duration.TotalMinutes:D2}:{Duration.Seconds:D2}";
    public string FileSizeText => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
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
