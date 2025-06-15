using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Mzl.Video.Process.Configuration;

namespace Mzl.Video.Process
{
    /// <summary>
    /// 设置窗口
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            TxtFFmpegPath.Text = AppConfig.FFmpegBinaryPath;
            TxtScreenshotPath.Text = AppConfig.ScreenshotPath;
            TxtDefaultInputPath.Text = AppConfig.DefaultInputPath;
            TxtDefaultOutputPath.Text = AppConfig.DefaultOutputPath;
            ChkAutoLoadFrame.IsChecked = AppConfig.AutoLoadFirstFrame;
            TxtPreviewTime.Text = AppConfig.PreviewFrameTime.ToString();
        }        private void BtnBrowseFFmpeg_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择 FFmpeg 可执行文件目录",
                SelectedPath = TxtFFmpegPath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtFFmpegPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseScreenshot_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择截图保存目录",
                SelectedPath = TxtScreenshotPath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtScreenshotPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseDefaultInput_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择默认输入视频目录",
                SelectedPath = TxtDefaultInputPath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtDefaultInputPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseDefaultOutput_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择默认输出视频目录",
                SelectedPath = TxtDefaultOutputPath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtDefaultOutputPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnRestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("确定要恢复所有设置为默认值吗？", "确认", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                AppConfig.RestoreDefaults();
                LoadSettings();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(TxtFFmpegPath.Text))
                {
                    System.Windows.MessageBox.Show("请设置 FFmpeg 可执行文件目录！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Directory.Exists(TxtFFmpegPath.Text))
                {
                    System.Windows.MessageBox.Show("FFmpeg 目录不存在！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证预览时间
                if (!double.TryParse(TxtPreviewTime.Text, out double previewTime) || previewTime < 0 || previewTime > 10)
                {
                    System.Windows.MessageBox.Show("预览帧时间必须是0-10之间的数字！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 保存设置
                AppConfig.FFmpegBinaryPath = TxtFFmpegPath.Text;
                AppConfig.ScreenshotPath = TxtScreenshotPath.Text;
                AppConfig.DefaultInputPath = TxtDefaultInputPath.Text;
                AppConfig.DefaultOutputPath = TxtDefaultOutputPath.Text;
                AppConfig.AutoLoadFirstFrame = ChkAutoLoadFrame.IsChecked ?? true;
                AppConfig.PreviewFrameTime = previewTime;

                AppConfig.SaveSettings();
                
                System.Windows.MessageBox.Show("设置已保存！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存设置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
