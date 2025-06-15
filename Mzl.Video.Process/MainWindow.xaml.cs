using Mzl.Video.Process.Configuration;
using Mzl.Video.Process.Models;
using Mzl.Video.Process.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace Mzl.Video.Process
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly FFmpegService _ffmpegService;
        private readonly DispatcherTimer _progressTimer;

        private string? _currentVideoPath;
        private string? _currentWatermarkVideoPath;
        private bool _isPlaying;
        private bool _isWatermarkPlaying;
        private bool _isDragging;
        private System.Windows.Point _watermarkStartPoint;

        public MainWindow()
        {
            InitializeComponent();

            _ffmpegService = new FFmpegService(AppConfig.FFmpegBinaryPath);
            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _progressTimer.Tick += ProgressTimer_Tick;

            // 注册事件
            _ffmpegService.ProgressChanged += OnProgressChanged;
            _ffmpegService.LogReceived += OnLogReceived;

            // 初始化配置
            if (!string.IsNullOrEmpty(AppConfig.DefaultInputPath))
            {
                TxtInputFile.Text = AppConfig.DefaultInputPath;
            }

            if (!string.IsNullOrEmpty(AppConfig.DefaultOutputPath))
            {
                TxtOutputFile.Text = AppConfig.DefaultOutputPath;
                TxtWatermarkOutput.Text = AppConfig.DefaultOutputPath;
            }
        }

        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            if (MainTabControl.SelectedItem == ConvertTab)
            {
                if (_isPlaying && VideoPlayer.NaturalDuration.HasTimeSpan)
                {
                    ProgressSlider.Value = VideoPlayer.Position.TotalMilliseconds;
                    TxtCurrentTime.Text = FormatTime(VideoPlayer.Position);
                }
            }
        }

        private void OnProgressChanged(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                if (MainTabControl.SelectedItem == ConvertTab)
                {
                    ConvertProgressBar.Value = progress;
                    TxtConvertProgress.Text = $"{progress:F1}%";
                }
                else if (MainTabControl.SelectedItem == WatermarkTab)
                {
                    WatermarkProgressBar.Value = progress;
                    TxtWatermarkProgress.Text = $"{progress:F1}%";
                }
            });
        }

        private void OnLogReceived(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtConvertLog.Text += message + Environment.NewLine;
                LogScrollViewer.ScrollToEnd();
            });
        }

        #region 视频播放控制

        private void LoadVideo(string filePath)
        {
            try
            {
                _currentVideoPath = filePath;
                TxtSelectedFile.Text = Path.GetFileName(_currentVideoPath);

                VideoPlayer.Source = new Uri(filePath);
                VideoPlayer.LoadedBehavior = MediaState.Manual;
                VideoPlayer.UnloadedBehavior = MediaState.Close;

                if (AppConfig.AutoLoadFirstFrame)
                {
                    VideoPlayer.Play();
                    Task.Delay(TimeSpan.FromSeconds(AppConfig.PreviewFrameTime)).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            VideoPlayer.Pause();
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载视频失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                ProgressSlider.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
                TxtTotalTime.Text = FormatTime(VideoPlayer.NaturalDuration.TimeSpan);
            }
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                VideoPlayer.Pause();
                BtnPlayPause.Content = "▶";
                _isPlaying = false;
                _progressTimer.Stop();
            }
            else
            {
                VideoPlayer.Play();
                BtnPlayPause.Content = "⏸";
                _isPlaying = true;
                _progressTimer.Start();
            }
        }

        private async void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentVideoPath))
            {
                System.Windows.MessageBox.Show("请先选择视频文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var screenshotPath = AppConfig.ScreenshotPath ?? Path.GetDirectoryName(_currentVideoPath);
                var fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var outputPath = Path.Combine(screenshotPath!, fileName);

                var currentTime = VideoPlayer.Position;
                await _ffmpegService.CaptureScreenshotAsync(_currentVideoPath, outputPath, currentTime);

                System.Windows.MessageBox.Show($"截图已保存到：{outputPath}", "截图成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"截图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            ProgressSlider.Value = 0;
            BtnPlayPause.Content = "▶";
            _isPlaying = false;
            _progressTimer.Stop();

            // 回到加载状态
            if (!string.IsNullOrEmpty(_currentVideoPath))
            {
                LoadVideo(_currentVideoPath);
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isDragging) return;

            if (_isPlaying)
            {
                VideoPlayer.Play();
            }
            else
            {
                VideoPlayer.Pause();
            }

            VideoPlayer.Position = TimeSpan.FromMilliseconds(e.NewValue);
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        #endregion 视频播放控制

        #region 文件选择

        private void BtnSelectInputFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|所有文件|*.*",
                Title = "选择输入视频文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtInputFile.Text = openFileDialog.FileName;
                LoadVideo(openFileDialog.FileName);
            }
        }

        private void BtnSelectOutputFile_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "MP4文件|*.mp4|AVI文件|*.avi|所有文件|*.*",
                Title = "选择输出文件位置"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                TxtOutputFile.Text = saveFileDialog.FileName;
            }
        }

        #endregion 文件选择

        #region 视频转换

        private async void BtnStartConvert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtInputFile.Text) || TxtInputFile.Text == "未选择文件")
            {
                System.Windows.MessageBox.Show("请选择输入文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TxtOutputFile.Text) || TxtOutputFile.Text == "未选择位置")
            {
                System.Windows.MessageBox.Show("请选择输出位置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var task = new ConversionTask
            {
                InputPath = TxtInputFile.Text,
                OutputPath = TxtOutputFile.Text,
                Quality = VideoQuality.Normal,
                OutputFormat = "mp4"
            };

            BtnStartConvert.IsEnabled = false;
            BtnStopConvert.IsEnabled = true;
            ConvertProgressBar.Value = 0;
            TxtConvertStatus.Text = "正在转换...";
            TxtConvertLog.Text = "";
            TxtConvertProgress.Text = "0%";

            try
            {
                await _ffmpegService.ConvertVideoAsync(task);
                TxtConvertStatus.Text = "转换完成";
                ConvertProgressBar.Value = 100;
                TxtConvertProgress.Text = "100%";
                System.Windows.MessageBox.Show("视频转换完成！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TxtConvertStatus.Text = "转换失败";
                System.Windows.MessageBox.Show($"转换失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnStartConvert.IsEnabled = true;
                BtnStopConvert.IsEnabled = false;
            }
        }

        private void BtnStopConvert_Click(object sender, RoutedEventArgs e)
        {
            _ffmpegService.StopCurrentProcess();
            TxtConvertStatus.Text = "转换已停止";
            BtnStartConvert.IsEnabled = true;
            BtnStopConvert.IsEnabled = false;
        }

        #endregion 视频转换

        #region 水印处理

        private void BtnSelectWatermarkVideo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|所有文件|*.*",
                Title = "选择水印视频文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadWatermarkVideo(openFileDialog.FileName);
            }
        }

        private void LoadWatermarkVideo(string filePath)
        {
            try
            {
                _currentWatermarkVideoPath = filePath;
                TxtWatermarkVideoFile.Text = Path.GetFileName(_currentWatermarkVideoPath);

                WatermarkVideoPlayer.Source = new Uri(filePath);
                WatermarkVideoPlayer.LoadedBehavior = MediaState.Manual;
                WatermarkVideoPlayer.UnloadedBehavior = MediaState.Close;

                if (AppConfig.AutoLoadFirstFrame)
                {
                    WatermarkVideoPlayer.Play();
                    Task.Delay(TimeSpan.FromSeconds(AppConfig.PreviewFrameTime)).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            WatermarkVideoPlayer.Pause();
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载视频失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnWatermarkPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isWatermarkPlaying)
            {
                WatermarkVideoPlayer.Pause();
                BtnWatermarkPlayPause.Content = "▶";
                _isWatermarkPlaying = false;
            }
            else
            {
                WatermarkVideoPlayer.Play();
                BtnWatermarkPlayPause.Content = "⏸";
                _isWatermarkPlaying = true;
            }
        }

        private async void BtnWatermarkScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentWatermarkVideoPath))
            {
                System.Windows.MessageBox.Show("请先选择水印视频文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var screenshotPath = AppConfig.ScreenshotPath ?? Path.GetDirectoryName(_currentWatermarkVideoPath);
                var fileName = $"WatermarkScreenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var outputPath = Path.Combine(screenshotPath!, fileName);

                var currentTime = WatermarkVideoPlayer.Position;
                await _ffmpegService.CaptureScreenshotAsync(_currentWatermarkVideoPath, outputPath, currentTime);

                System.Windows.MessageBox.Show($"截图已保存到：{outputPath}", "截图成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"截图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnWatermarkStop_Click(object sender, RoutedEventArgs e)
        {
            WatermarkVideoPlayer.Stop();
            BtnWatermarkPlayPause.Content = "▶";
            _isWatermarkPlaying = false;

            // 回到加载状态
            if (!string.IsNullOrEmpty(_currentWatermarkVideoPath))
            {
                LoadWatermarkVideo(_currentWatermarkVideoPath);
            }
        }

        private void WatermarkSelectionCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _watermarkStartPoint = e.GetPosition(WatermarkSelectionCanvas);
            WatermarkSelectionCanvas.CaptureMouse();

            WatermarkSelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(WatermarkSelectionRect, _watermarkStartPoint.X);
            Canvas.SetTop(WatermarkSelectionRect, _watermarkStartPoint.Y);
            WatermarkSelectionRect.Width = 0;
            WatermarkSelectionRect.Height = 0;
        }

        private void WatermarkSelectionCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (WatermarkSelectionCanvas.IsMouseCaptured)
            {
                var currentPoint = e.GetPosition(WatermarkSelectionCanvas);

                var left = Math.Min(_watermarkStartPoint.X, currentPoint.X);
                var top = Math.Min(_watermarkStartPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - _watermarkStartPoint.X);
                var height = Math.Abs(currentPoint.Y - _watermarkStartPoint.Y);

                WatermarkSelectionRect.Width = width;
                WatermarkSelectionRect.Height = height;
                Canvas.SetLeft(WatermarkSelectionRect, left);
                Canvas.SetTop(WatermarkSelectionRect, top);
            }
        }

        private void WatermarkSelectionCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (WatermarkSelectionCanvas.IsMouseCaptured)
            {
                WatermarkSelectionCanvas.ReleaseMouseCapture();
                UpdateWatermarkCoordinates();
            }
        }

        private void UpdateWatermarkCoordinates()
        {
            var x = Canvas.GetLeft(WatermarkSelectionRect);
            var y = Canvas.GetTop(WatermarkSelectionRect);
            var width = WatermarkSelectionRect.Width;
            var height = WatermarkSelectionRect.Height;

            // 基于视频尺寸计算坐标
            var canvasWidth = WatermarkSelectionCanvas.ActualWidth;
            var canvasHeight = WatermarkSelectionCanvas.ActualHeight;

            if (WatermarkVideoPlayer.NaturalVideoWidth > 0 && WatermarkVideoPlayer.NaturalVideoHeight > 0)
            {
                var videoWidth = WatermarkVideoPlayer.NaturalVideoWidth;
                var videoHeight = WatermarkVideoPlayer.NaturalVideoHeight;

                x = (x / canvasWidth) * videoWidth;
                y = (y / canvasHeight) * videoHeight;
                width = (width / canvasWidth) * videoWidth;
                height = (height / canvasHeight) * videoHeight;
            }

            TxtWatermarkX.Text = $"{x:F0}";
            TxtWatermarkY.Text = $"{y:F0}";
            TxtWatermarkWidth.Text = $"{width:F0}";
            TxtWatermarkHeight.Text = $"{height:F0}";
        }

        private void BtnSelectWatermarkOutput_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "MP4文件|*.mp4|AVI文件|*.avi|所有文件|*.*",
                Title = "选择输出文件位置"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                TxtWatermarkOutput.Text = saveFileDialog.FileName;
            }
        }

        private async void BtnStartWatermarkRemoval_Click(object sender, RoutedEventArgs e)
        {
            if (WatermarkSelectionRect.Visibility != Visibility.Visible || WatermarkSelectionRect.Width == 0)
            {
                System.Windows.MessageBox.Show("请先选择水印区域", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TxtWatermarkOutput.Text) || TxtWatermarkOutput.Text == "未选择输出位置")
            {
                System.Windows.MessageBox.Show("请选择输出位置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var task = new WatermarkRemovalTask
            {
                InputPath = _currentWatermarkVideoPath!,
                OutputPath = TxtWatermarkOutput.Text,
                Method = GetSelectedWatermarkMethod(),
                WatermarkArea = new System.Windows.Rect(
                    double.Parse(TxtWatermarkX.Text),
                    double.Parse(TxtWatermarkY.Text),
                    double.Parse(TxtWatermarkWidth.Text),
                    double.Parse(TxtWatermarkHeight.Text))
            };

            BtnStartWatermarkRemoval.IsEnabled = false;
            WatermarkProgressBar.Value = 0;
            TxtWatermarkProgress.Text = "0%";

            try
            {
                await _ffmpegService.RemoveWatermarkAsync(task);
                WatermarkProgressBar.Value = 100;
                TxtWatermarkProgress.Text = "处理完成";
                System.Windows.MessageBox.Show("水印处理完成！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TxtWatermarkProgress.Text = "处理失败";
                System.Windows.MessageBox.Show($"处理失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnStartWatermarkRemoval.IsEnabled = true;
            }
        }

        private void BtnClearWatermarkSelection_Click(object sender, RoutedEventArgs e)
        {
            WatermarkSelectionRect.Visibility = Visibility.Collapsed;
            TxtWatermarkX.Text = "0";
            TxtWatermarkY.Text = "0";
            TxtWatermarkWidth.Text = "0";
            TxtWatermarkHeight.Text = "0";
        }

        private WatermarkRemovalMethod GetSelectedWatermarkMethod()
        {
            if (RbBlur.IsChecked == true) return WatermarkRemovalMethod.Blur;
            if (RbMosaic.IsChecked == true) return WatermarkRemovalMethod.Mosaic;
            if (RbCrop.IsChecked == true) return WatermarkRemovalMethod.Crop;
            if (RbInpaint.IsChecked == true) return WatermarkRemovalMethod.Inpaint;
            if (RbDelogo.IsChecked == true) return WatermarkRemovalMethod.Delogo;
            return WatermarkRemovalMethod.Blur;
        }

        #endregion 水印处理

        #region 窗口控制

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Mzl Video Process v1.0\n\n一个功能强大的视频处理工具", "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        #endregion 窗口控制

        #region 辅助方法

        private string FormatTime(TimeSpan timeSpan)
        {
            return $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _progressTimer?.Stop();
            base.OnClosed(e);
        }

        #endregion 辅助方法
    }
}