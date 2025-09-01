using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;

namespace ImageRecognitionApp.Assets.UI
{
    /// <summary>
    /// ScreenRecordingControl.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenRecordingControl : UserControl, INotifyPropertyChanged
    {
        #region 字段
        private bool _isRecording = false;
        private bool _isPaused = false;
        private DateTime _startTime;
        private DateTime _pauseTime;
        private TimeSpan _pauseDuration = TimeSpan.Zero;
        private DispatcherTimer _recordingTimer;
        private string _recordingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ScreenRecordings");
        private string _currentRecordingFile = string.Empty;
        private List<string> _recordedFiles = new List<string>();

        // 录制音频和显示鼠标光标选项
        private bool _recordAudio = true;
        private bool _showCursor = true;
        #endregion

        #region 属性
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsRecording
        {
            get { return _isRecording; }
            set
            {
                _isRecording = value;
                OnPropertyChanged("IsRecording");
            }
        }

        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                OnPropertyChanged("IsPaused");
            }
        }
        #endregion

        #region 构造函数
        public ScreenRecordingControl()
        {
            InitializeComponent();
            DataContext = this;
            LoadSettings();
            InitializeTimer();
            RefreshRecordingsList();
        }
        #endregion

        #region 方法
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitializeTimer()
        {
            _recordingTimer = new DispatcherTimer();
            _recordingTimer.Interval = TimeSpan.FromSeconds(1);
            _recordingTimer.Tick += RecordingTimer_Tick;
        }

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            if (IsRecording && !IsPaused)
            {
                TimeSpan duration = DateTime.Now - _startTime - _pauseDuration;
                RecordingTimeText.Text = duration.ToString("hh:mm:ss");
            }
        }

        private void LoadSettings()
        {
            // 加载保存路径设置
            if (!Directory.Exists(_recordingPath))
            {
                Directory.CreateDirectory(_recordingPath);
            }
            SavePathTextBox.Text = _recordingPath;

            // 加载录制选项
            RecordAudioCheckBox.IsChecked = _recordAudio;
            ShowCursorCheckBox.IsChecked = _showCursor;
        }

        private void SaveSettings()
        {
            // 保存录制选项
            if (RecordAudioCheckBox.IsChecked.HasValue)
                _recordAudio = RecordAudioCheckBox.IsChecked.Value;
            if (ShowCursorCheckBox.IsChecked.HasValue)
                _showCursor = ShowCursorCheckBox.IsChecked.Value;

            // 更新保存路径
            _recordingPath = SavePathTextBox.Text;
            if (!Directory.Exists(_recordingPath))
            {
                Directory.CreateDirectory(_recordingPath);
            }

            // 显示保存成功提示
            System.Windows.MessageBox.Show("设置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshRecordingsList()
        {
            try
            {
                _recordedFiles.Clear();
                if (Directory.Exists(_recordingPath))
                {
                    string[] files = Directory.GetFiles(_recordingPath, "*.mp4");
                    Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    
                    foreach (string file in files)
                    {
                        _recordedFiles.Add(Path.GetFileName(file));
                    }
                }
                
                RecordingsListBox.ItemsSource = null;
                RecordingsListBox.ItemsSource = _recordedFiles;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("刷新录制列表时出错：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartRecording()
        {
            try
            {
                // 创建保存目录（如果不存在）
                if (!Directory.Exists(_recordingPath))
                {
                    Directory.CreateDirectory(_recordingPath);
                }

                // 生成录制文件名
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentRecordingFile = Path.Combine(_recordingPath, $"Recording_{timestamp}.mp4");

                // 这里添加实际的录制代码
                // 目前只是模拟录制开始
                _startTime = DateTime.Now;
                IsRecording = true;
                IsPaused = false;
                _pauseDuration = TimeSpan.Zero;
                _recordingTimer.Start();

                // 更新按钮状态
                StartRecordingButton.IsEnabled = false;
                StopRecordingButton.IsEnabled = true;
                PauseRecordingButton.IsEnabled = true;

                // 显示录制开始提示音
                try
                {
                    // 播放提示音（如果有音频功能）
                    // SystemSounds.Beep.Play();
                }
                catch { }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("开始录制时出错：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StopRecording();
            }
        }

        private void StopRecording()
        {
            try
            {
                IsRecording = false;
                IsPaused = false;
                _recordingTimer.Stop();

                // 这里添加实际的停止录制代码
                // 目前只是模拟录制停止

                // 更新按钮状态
                StartRecordingButton.IsEnabled = true;
                StopRecordingButton.IsEnabled = false;
                PauseRecordingButton.IsEnabled = false;

                // 重置录制时间显示
                RecordingTimeText.Text = "00:00:00";

                // 刷新录制文件列表
                RefreshRecordingsList();

                // 显示录制停止提示音
                try
                {
                    // 播放提示音（如果有音频功能）
                    // SystemSounds.Beep.Play();
                }
                catch { }

                // 显示录制完成提示
                System.Windows.MessageBox.Show("录制已完成！文件已保存到：" + _currentRecordingFile, "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("停止录制时出错：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PauseRecording()
        {
            if (IsRecording && !IsPaused)
            {
                // 暂停录制
                IsPaused = true;
                _pauseTime = DateTime.Now;
                PauseRecordingButton.Content = "继续录制";
            }
            else if (IsRecording && IsPaused)
            {
                // 继续录制
                IsPaused = false;
                _pauseDuration += DateTime.Now - _pauseTime;
                PauseRecordingButton.Content = "暂停录制";
            }
        }

        private void BrowseSavePath()
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "选择录制文件保存位置";
                folderBrowserDialog.SelectedPath = _recordingPath;
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    SavePathTextBox.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void OpenRecordingFile(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_recordingPath, fileName);
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                else
                {
                    System.Windows.MessageBox.Show("文件不存在：" + filePath, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("打开文件时出错：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 事件处理
        private void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        private void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
        }

        private void PauseRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            PauseRecording();
        }

        private void BrowsePathButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseSavePath();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshRecordingsList();
        }

        private void RecordingsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (RecordingsListBox.SelectedItem != null)
            {
                string selectedFile = RecordingsListBox.SelectedItem.ToString();
                OpenRecordingFile(selectedFile);
            }
        }
        #endregion
    }
}