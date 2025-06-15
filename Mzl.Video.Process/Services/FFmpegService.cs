using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mzl.Video.Process.Models;

namespace Mzl.Video.Process.Services;

/// <summary>
/// FFmpeg服务类，处理视频相关操作
/// </summary>
public class FFmpegService
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private System.Diagnostics.Process? _currentProcess;
    private TimeSpan _totalDuration = TimeSpan.Zero;

    public event Action<double>? ProgressChanged;

    public event Action<string>? LogReceived;

    public FFmpegService(string ffmpegBinaryFolder)
    {
        _ffmpegPath = Path.Combine(ffmpegBinaryFolder, "ffmpeg.exe");
        _ffprobePath = Path.Combine(ffmpegBinaryFolder, "ffprobe.exe");

        if (!File.Exists(_ffmpegPath))
            throw new FileNotFoundException($"FFmpeg未找到: {_ffmpegPath}");

        if (!File.Exists(_ffprobePath))
            throw new FileNotFoundException($"FFprobe未找到: {_ffprobePath}");
    }

    /// <summary>
    /// 获取视频信息
    /// </summary>
    public async Task<VideoInfo> GetVideoInfoAsync(string videoPath)
    {
        // 替换路径中的反斜杠为正斜杠
        videoPath = videoPath.Replace('\\', '/');
        var arguments = $"-v quiet -print_format json -show_format -show_streams \"{videoPath}\"";
        var output = await RunProcessAsync(_ffprobePath, arguments);

        return ParseVideoInfo(videoPath, output);
    }

    /// <summary>
    /// 转换视频格式
    /// </summary>
    public async Task ConvertVideoAsync(ConversionTask task)
    {
        // 确保路径中的反斜杠被替换为正斜杠
        task.InputPath = task.InputPath.Replace('\\', '/');
        task.OutputPath = task.OutputPath.Replace('\\', '/');

        // 首先获取视频信息以获得总时长
        var videoInfo = await GetVideoInfoAsync(task.InputPath);
        _totalDuration = videoInfo.Duration;

        var arguments = BuildConversionArguments(task);
        LogReceived?.Invoke($"开始转换: {task.InputPath}");
        LogReceived?.Invoke($"视频时长: {_totalDuration}");
        LogReceived?.Invoke($"命令参数: {arguments}");

        await RunProcessWithProgressAsync(_ffmpegPath, arguments);

        // 当流程成功完成时发送100%进度
        ProgressChanged?.Invoke(100);
    }

    /// <summary>
    /// 移除水印
    /// </summary>
    public async Task RemoveWatermarkAsync(WatermarkRemovalTask task)
    {
        // 确保路径中的反斜杠被替换为正斜杠
        task.InputPath = task.InputPath.Replace('\\', '/');
        task.OutputPath = task.OutputPath.Replace('\\', '/');

        // 首先获取视频信息以获得总时长
        var videoInfo = await GetVideoInfoAsync(task.InputPath);
        _totalDuration = videoInfo.Duration;

        var arguments = BuildWatermarkRemovalArguments(task);
        LogReceived?.Invoke($"开始去水印: {task.InputPath}");
        LogReceived?.Invoke($"视频时长: {_totalDuration}");
        LogReceived?.Invoke($"命令参数: {arguments}");

        await RunProcessWithProgressAsync(_ffmpegPath, arguments);

        // 当流程成功完成时发送100%进度
        ProgressChanged?.Invoke(100);
    }

    /// <summary>
    /// 停止当前处理
    /// </summary>
    public void StopCurrentProcess()
    {
        try
        {
            _currentProcess?.Kill();
            _currentProcess = null;
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke($"停止进程时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 截取视频帧
    /// </summary>
    /// <param name="videoPath">视频文件路径</param>
    /// <param name="outputPath">截图保存路径</param>
    /// <param name="timePosition">截图时间位置</param>
    public async Task CaptureScreenshotAsync(string videoPath, string outputPath, TimeSpan timePosition)
    {
        // 确保路径正确
        videoPath = videoPath.Replace('\\', '/');
        outputPath = outputPath.Replace('\\', '/');

        // 构建FFmpeg截图命令
        var timeString = $"{timePosition.Hours:D2}:{timePosition.Minutes:D2}:{timePosition.Seconds:D2}.{timePosition.Milliseconds:D3}";
        var arguments = $"-y -ss {timeString} -i \"{videoPath}\" -vframes 1 -q:v 2 \"{outputPath}\"";

        LogReceived?.Invoke($"开始截图: {videoPath} 在 {timeString}");
        LogReceived?.Invoke($"保存路径: {outputPath}");
        LogReceived?.Invoke($"命令参数: {arguments}");

        await RunProcessAsync(_ffmpegPath, arguments);

        LogReceived?.Invoke("截图完成");
    }

    private string BuildConversionArguments(ConversionTask task)
    {
        var args = new StringBuilder();
        args.Append($"-y -allowed_extensions ALL -i \"{task.InputPath}\"");

        // 视频编码设置
        switch (task.Quality)
        {
            case VideoQuality.High:
                args.Append(" -c:v libx264 -crf 18 -preset medium");
                break;

            case VideoQuality.Normal:
                args.Append(" -c:v libx264 -crf 23 -preset medium");
                break;

            case VideoQuality.Low:
                args.Append(" -c:v libx264 -crf 28 -preset fast");
                break;
        }

        // 分辨率设置
        if (task.Resolution != VideoResolution.Original)
        {
            var (width, height) = GetResolutionSize(task.Resolution);
            args.Append($" -s {width}x{height}");
        }

        // 音频编码
        args.Append(" -c:a aac -b:a 128k");

        // 输出格式
        switch (task.OutputFormat.ToLower())
        {
            case "mp4":
                args.Append(" -f mp4");
                break;

            case "avi":
                args.Append(" -f avi");
                break;

            case "mov":
                args.Append(" -f mov");
                break;

            case "mkv":
                args.Append(" -f matroska");
                break;
        }

        args.Append($" \"{task.OutputPath}\"");
        return args.ToString();
    }

    private string BuildWatermarkRemovalArguments(WatermarkRemovalTask task)
    {
        var args = new StringBuilder();
        args.Append($"-y -i \"{task.InputPath}\"");

        // 根据不同的去水印方法构建参数
        switch (task.Method)
        {
            case WatermarkRemovalMethod.Blur:
                // 使用高斯模糊
                args.Append($" -vf \"crop={task.WatermarkArea.Width}:{task.WatermarkArea.Height}:{task.WatermarkArea.X}:{task.WatermarkArea.Y},gblur=sigma=20[blurred];[0:v][blurred]overlay={task.WatermarkArea.X}:{task.WatermarkArea.Y}\"");
                break;

            case WatermarkRemovalMethod.Mosaic:
                // 使用马赛克效果
                args.Append($" -vf \"crop={task.WatermarkArea.Width}:{task.WatermarkArea.Height}:{task.WatermarkArea.X}:{task.WatermarkArea.Y},scale=10:10,scale={task.WatermarkArea.Width}:{task.WatermarkArea.Height}:flags=neighbor[mosaic];[0:v][mosaic]overlay={task.WatermarkArea.X}:{task.WatermarkArea.Y}\"");
                break;

            case WatermarkRemovalMethod.Crop:
                // 裁剪移除水印区域
                args.Append($" -vf \"crop={1920 - task.WatermarkArea.Width}:{1080 - task.WatermarkArea.Height}:0:0\"");
                break;

            case WatermarkRemovalMethod.Delogo:
                // 使用delogo滤镜
                args.Append($" -vf \"delogo=x={task.WatermarkArea.X}:y={task.WatermarkArea.Y}:w={task.WatermarkArea.Width}:h={task.WatermarkArea.Height}\"");
                break;

            case WatermarkRemovalMethod.Inpaint:
                // 简单的颜色填充
                args.Append($" -vf \"drawbox=x={task.WatermarkArea.X}:y={task.WatermarkArea.Y}:w={task.WatermarkArea.Width}:h={task.WatermarkArea.Height}:color=black:t=fill\"");
                break;
        }

        args.Append($" \"{task.OutputPath}\"");
        return args.ToString();
    }

    private (int width, int height) GetResolutionSize(VideoResolution resolution)
    {
        return resolution switch
        {
            VideoResolution.UHD_4K => (3840, 2160),
            VideoResolution.QHD_2K => (2560, 1440),
            VideoResolution.FHD_1080p => (1920, 1080),
            VideoResolution.HD_720p => (1280, 720),
            VideoResolution.SD_480p => (854, 480),
            _ => (1920, 1080)
        };
    }

    private async Task<string> RunProcessAsync(string fileName, string arguments)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"FFmpeg执行失败: {error}");
        }

        return output;
    }

    private async Task RunProcessWithProgressAsync(string fileName, string arguments)
    {
        // 替换路径中的反斜杠为正斜杠
        fileName = fileName.Replace('\\', '/');

        _currentProcess = new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        _currentProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                LogReceived?.Invoke(e.Data);
        };

        _currentProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                LogReceived?.Invoke(e.Data);
                ParseProgress(e.Data);
            }
        };

        _currentProcess.Start();
        _currentProcess.BeginOutputReadLine();
        _currentProcess.BeginErrorReadLine();

        await _currentProcess.WaitForExitAsync();

        if (_currentProcess.ExitCode != 0)
        {
            throw new Exception("FFmpeg处理失败");
        }
    }

    private void ParseProgress(string logLine)
    {
        // 解析FFmpeg输出中的进度信息
        // 例如: frame= 1234 fps= 25 q=28.0 size=   12345kB time=00:01:23.45 bitrate=1234.5kbits/s speed=1.23x

        if (logLine.Contains("time=") && logLine.Contains("fps="))
        {
            var timeMatch = Regex.Match(logLine, @"time=(\d+):(\d+):(\d+\.\d+)");
            if (timeMatch.Success)
            {
                var hours = int.Parse(timeMatch.Groups[1].Value);
                var minutes = int.Parse(timeMatch.Groups[2].Value);
                var seconds = double.Parse(timeMatch.Groups[3].Value);

                var currentTime = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);

                // 使用实际的视频总时长计算准确的进度百分比
                if (_totalDuration.TotalSeconds > 0)
                {
                    var progress = (int)((currentTime.TotalSeconds / _totalDuration.TotalSeconds) * 100);
                    progress = Math.Min(progress, 99); // 确保不超过99%，100%由完成时发送
                    ProgressChanged?.Invoke(progress);

                    // 输出调试信息
                    LogReceived?.Invoke($"进度更新: {currentTime:hh\\:mm\\:ss} / {_totalDuration:hh\\:mm\\:ss} ({progress}%)");
                }
                else
                {
                    // 回退到估算进度
                    var estimatedProgress = Math.Min(90, (int)(currentTime.TotalSeconds / 10));
                    ProgressChanged?.Invoke(estimatedProgress);
                }
            }
        }
    }

    private VideoInfo ParseVideoInfo(string filePath, string jsonOutput)
    {
        // 简化的视频信息解析
        // 实际应用中应该使用JSON序列化库如System.Text.Json或Newtonsoft.Json
        var info = new VideoInfo
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Format = Path.GetExtension(filePath).TrimStart('.').ToUpper()
        };

        // 这里应该解析JSON输出获取详细信息
        // 为了简化，使用默认值
        try
        {
            var fileInfo = new FileInfo(filePath);
            info.FileSize = fileInfo.Length;

            // 解析时长信息
            var durationMatch = Regex.Match(jsonOutput, @"""duration""\s*:\s*""(\d+\.\d+)""");
            if (durationMatch.Success)
            {
                var durationSeconds = double.Parse(durationMatch.Groups[1].Value);
                info.Duration = TimeSpan.FromSeconds(durationSeconds);
            }
            else
            {
                // 如果无法解析，使用默认值
                info.Duration = TimeSpan.FromMinutes(5); // 假设5分钟
            }

            // 可以通过正则表达式或JSON解析获取更多信息
            if (jsonOutput.Contains("\"width\""))
            {
                var widthMatch = Regex.Match(jsonOutput, @"""width""\s*:\s*(\d+)");
                if (widthMatch.Success)
                    info.Width = int.Parse(widthMatch.Groups[1].Value);
            }

            if (jsonOutput.Contains("\"height\""))
            {
                var heightMatch = Regex.Match(jsonOutput, @"""height""\s*:\s*(\d+)");
                if (heightMatch.Success)
                    info.Height = int.Parse(heightMatch.Groups[1].Value);
            }
        }
        catch
        {
            // 忽略解析错误，使用默认值
            info.Width = 1920;
            info.Height = 1080;
            info.Duration = TimeSpan.FromMinutes(5);
        }

        return info;
    }
}