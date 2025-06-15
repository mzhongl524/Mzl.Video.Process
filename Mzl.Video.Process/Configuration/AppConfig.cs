using System;
using System.IO;
using System.Text.Json;

namespace Mzl.Video.Process.Configuration;

/// <summary>
/// 应用程序配置管理
/// </summary>
public static class AppConfig
{
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MzlVideoProcess",
        "config.json"
    );

    /// <summary>
    /// FFmpeg 可执行文件目录
    /// </summary>
    public static string FFmpegBinaryPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "ffmpeg");

    /// <summary>
    /// 截图保存目录
    /// </summary>
    public static string ScreenshotPath { get; set; } = Environment.CurrentDirectory;

    /// <summary>
    /// 默认输入目录
    /// </summary>
    public static string DefaultInputPath { get; set; } = Environment.CurrentDirectory;

    /// <summary>
    /// 默认输出目录
    /// </summary>
    public static string DefaultOutputPath { get; set; } = Environment.CurrentDirectory;

    /// <summary>
    /// 是否自动加载第一帧
    /// </summary>
    public static bool AutoLoadFirstFrame { get; set; } = true;

    /// <summary>
    /// 预览帧时间（秒）
    /// </summary>
    public static double PreviewFrameTime { get; set; } = 1.0;

    /// <summary>
    /// 支持的视频格式
    /// </summary>
    public static readonly string[] SupportedVideoFormats =
    {
        ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".m3u8", ".ts"
    };

    /// <summary>
    /// 支持的输出格式
    /// </summary>
    public static readonly string[] SupportedOutputFormats =
    {
        "MP4", "AVI", "MOV", "MKV"
    };

    /// <summary>
    /// 视频质量选项
    /// </summary>
    public static readonly string[] VideoQualityOptions =
    {
        "高质量", "中等质量", "低质量", "自定义"
    };

    /// <summary>
    /// 分辨率选项
    /// </summary>
    public static readonly string[] ResolutionOptions =
    {
        "保持原始", "1920×1080", "1280×720", "854×480"
    };

    /// <summary>
    /// 去水印方法选项
    /// </summary>
    public static readonly string[] WatermarkRemovalMethods =
    {
        "模糊处理", "马赛克", "裁剪移除", "智能填充"
    };

    /// <summary>
    /// 验证FFmpeg路径是否有效
    /// </summary>
    public static bool ValidateFFmpegPath()
    {
        var ffmpegPath = Path.Combine(FFmpegBinaryPath, "ffmpeg.exe");
        var ffprobePath = Path.Combine(FFmpegBinaryPath, "ffprobe.exe");

        return File.Exists(ffmpegPath) && File.Exists(ffprobePath);
    }

    /// <summary>
    /// 获取文件对话框过滤器字符串
    /// </summary>
    public static string GetVideoFileFilter()
    {
        return "视频文件|*.mp4;*.avi;*.mov;*.mkv;*.wmv;*.flv;*.m3u8;*.ts|所有文件|*.*";
    }

    /// <summary>
    /// 获取输出文件对话框过滤器字符串
    /// </summary>
    public static string GetOutputFileFilter()
    {
        return "MP4文件|*.mp4|AVI文件|*.avi|MOV文件|*.mov|MKV文件|*.mkv|所有文件|*.*";
    }

    /// <summary>
    /// 配置数据模型
    /// </summary>
    private class ConfigData
    {
        public string FFmpegBinaryPath { get; set; } = @"f:\test";
        public string ScreenshotPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public string DefaultInputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public string DefaultOutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public bool AutoLoadFirstFrame { get; set; } = true;
        public double PreviewFrameTime { get; set; } = 1.0;
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public static void LoadSettings()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<ConfigData>(json);

                if (config != null)
                {
                    FFmpegBinaryPath = config.FFmpegBinaryPath;
                    ScreenshotPath = config.ScreenshotPath;
                    DefaultInputPath = config.DefaultInputPath;
                    DefaultOutputPath = config.DefaultOutputPath;
                    AutoLoadFirstFrame = config.AutoLoadFirstFrame;
                    PreviewFrameTime = config.PreviewFrameTime;
                }
            }
        }
        catch
        {
            // 忽略加载错误，使用默认值
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public static void SaveSettings()
    {
        try
        {
            var config = new ConfigData
            {
                FFmpegBinaryPath = FFmpegBinaryPath,
                ScreenshotPath = ScreenshotPath,
                DefaultInputPath = DefaultInputPath,
                DefaultOutputPath = DefaultOutputPath,
                AutoLoadFirstFrame = AutoLoadFirstFrame,
                PreviewFrameTime = PreviewFrameTime
            };

            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    /// <summary>
    /// 恢复默认设置
    /// </summary>
    public static void RestoreDefaults()
    {
        FFmpegBinaryPath = @"f:\test";
        ScreenshotPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DefaultInputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DefaultOutputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        AutoLoadFirstFrame = true;
        PreviewFrameTime = 1.0;
    }
}