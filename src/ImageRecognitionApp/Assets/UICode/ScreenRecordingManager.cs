using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Threading;

namespace ImageRecognitionApp.Assets.UICode
{
    /// <summary>
    /// 屏幕录制系统的管理类，负责处理录制功能的核心逻辑
    /// </summary>
    public class ScreenRecordingManager : INotifyPropertyChanged
    {
        #region 事件和委托
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? RecordingStarted;
        public event EventHandler? RecordingStopped;
        public event EventHandler? RecordingPaused;
        public event EventHandler? RecordingResumed;
        public event EventHandler? RecordingTimeUpdated;
        #endregion

        #region 私有字段
        private bool _isRecording = false;
        private bool _isPaused = false;
        private DateTime _startTime;
        private TimeSpan _pausedTime = TimeSpan.Zero;
        private DispatcherTimer _recordingTimer;
        private string _recordingFilePath = string.Empty;
        private ScreenRecordingSettings? _settings;
        private string _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImageRecognitionApp", "ScreenRecordingSettings.json");
        private TimeSpan _currentRecordingTime = TimeSpan.Zero;
        #endregion

        #region 公共属性
        public bool IsRecording
        {
            get { return _isRecording; }
            private set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    OnPropertyChanged("IsRecording");
                }
            }
        }

        public bool IsPaused
        {
            get { return _isPaused; }
            private set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    OnPropertyChanged("IsPaused");
                }
            }
        }

        public TimeSpan CurrentRecordingTime
        {
            get { return _currentRecordingTime; }
            private set
            {
                if (_currentRecordingTime != value)
                {
                    _currentRecordingTime = value;
                    OnPropertyChanged("CurrentRecordingTime");
                    OnRecordingTimeUpdated();
                }
            }
        }

        public ScreenRecordingSettings Settings
        {
            get { return _settings; }
            set
            {
                if (_settings != value)
                {
                    _settings = value;
                    OnPropertyChanged("Settings");
                }
            }
        }

        public string RecordingFilePath
        {
            get { return _recordingFilePath; }
            private set
            {
                if (_recordingFilePath != value)
                {
                    _recordingFilePath = value;
                    OnPropertyChanged("RecordingFilePath");
                }
            }
        }
        #endregion

        #region 构造函数
        public ScreenRecordingManager()
        {
            // 初始化设置
            LoadSettings();
            
            // 初始化计时器
            _recordingTimer = new DispatcherTimer();
            _recordingTimer.Interval = TimeSpan.FromSeconds(1);
            _recordingTimer.Tick += RecordingTimer_Tick;
        }
        #endregion

        #region 设置管理方法
        /// <summary>
        /// 加载录制设置
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ScreenRecordingSettings>(json);
                }
                else
                {
                    // 默认设置
                    _settings = new ScreenRecordingSettings
                    {
                        Quality = "中等质量",
                        Region = "全屏",
                        SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ScreenRecordings"),
                        RecordAudio = true,
                        ShowCursor = true
                    };
                    SaveSettings(); // 保存默认设置
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("加载录制设置失败: " + ex.Message);
                // 使用默认设置
                _settings = new ScreenRecordingSettings
                {
                    Quality = "中等质量",
                    Region = "全屏",
                    SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ScreenRecordings"),
                    RecordAudio = true,
                    ShowCursor = true
                };
            }
        }

        /// <summary>
        /// 保存录制设置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 保存到文件
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(_settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("保存录制设置失败: " + ex.Message);
            }
        }
        #endregion

        #region 录制控制方法
        /// <summary>
        /// 开始录制屏幕
        /// </summary>
        /// <returns>是否成功开始录制</returns>
        public bool StartRecording()
        {
            if (IsRecording)
            {
                return false; // 已经在录制中
            }

            try
            {
                // 创建保存目录（如果不存在）
                if (!Directory.Exists(_settings.SavePath))
                {
                    Directory.CreateDirectory(_settings.SavePath);
                }
                
                // 生成文件名
                string fileName = $"ScreenRecording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                RecordingFilePath = Path.Combine(_settings.SavePath, fileName);
                
                // 开始计时器
                _startTime = DateTime.Now;
                _pausedTime = TimeSpan.Zero;
                CurrentRecordingTime = TimeSpan.Zero;
                _recordingTimer.Start();
                
                IsRecording = true;
                IsPaused = false;
                
                // 触发录制开始事件
                OnRecordingStarted();
                
                // 实际项目中这里应该调用录制库来开始录制
                Console.WriteLine("开始录制屏幕...");
                Console.WriteLine($"保存路径: {RecordingFilePath}");
                Console.WriteLine($"录制设置: 质量={_settings.Quality}, 区域={_settings.Region}, 录制音频={_settings.RecordAudio}, 显示光标={_settings.ShowCursor}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("开始录制失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 停止录制屏幕
        /// </summary>
        /// <returns>是否成功停止录制</returns>
        public bool StopRecording()
        {
            if (!IsRecording)
            {
                return false; // 没有在录制
            }

            try
            {
                // 停止计时器
                _recordingTimer.Stop();
                
                IsRecording = false;
                IsPaused = false;
                
                // 触发录制停止事件
                OnRecordingStopped();
                
                // 实际项目中这里应该调用录制库来停止录制
                Console.WriteLine("停止录制屏幕...");
                Console.WriteLine($"录制文件已保存: {RecordingFilePath}");
                
                // 保存设置
                SaveSettings();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("停止录制失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 暂停录制
        /// </summary>
        /// <returns>是否成功暂停</returns>
        public bool PauseRecording()
        {
            if (!IsRecording || IsPaused)
            {
                return false; // 没有在录制或已经暂停
            }

            try
            {
                IsPaused = true;
                
                // 记录暂停的时间点，用于计算总暂停时间
                _pausedTime += DateTime.Now - _startTime - _pausedTime;
                
                // 触发录制暂停事件
                OnRecordingPaused();
                
                // 实际项目中这里应该调用录制库来暂停录制
                Console.WriteLine("暂停录制...");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("暂停录制失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 恢复录制
        /// </summary>
        /// <returns>是否成功恢复</returns>
        public bool ResumeRecording()
        {
            if (!IsRecording || !IsPaused)
            {
                return false; // 没有在录制或没有暂停
            }

            try
            {
                // 记录新的开始时间，考虑已经暂停的时间
                _startTime = DateTime.Now - (DateTime.Now - _startTime - _pausedTime);
                
                IsPaused = false;
                
                // 触发录制恢复事件
                OnRecordingResumed();
                
                // 实际项目中这里应该调用录制库来恢复录制
                Console.WriteLine("继续录制...");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("恢复录制失败: " + ex.Message);
                return false;
            }
        }
        #endregion

        #region 录制文件管理方法
        /// <summary>
        /// 获取录制文件列表
        /// </summary>
        /// <returns>录制文件路径列表</returns>
        public List<string> GetRecordingFiles()
        {
            List<string> files = new List<string>();
            
            try
            {
                if (Directory.Exists(_settings.SavePath))
                {
                    string[] filePaths = Directory.GetFiles(_settings.SavePath, "*.mp4");
                    Array.Sort(filePaths, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    files.AddRange(filePaths);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("获取录制文件列表失败: " + ex.Message);
            }
            
            return files;
        }

        /// <summary>
        /// 打开录制文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功打开</returns>
        public bool OpenRecordingFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("打开录制文件失败: " + ex.Message);
                return false;
            }
        }
        #endregion

        #region 计时器处理
        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            if (!IsPaused)
            {
                CurrentRecordingTime = DateTime.Now - _startTime;
            }
        }
        #endregion

        #region 事件触发方法
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnRecordingStarted()
        {
            RecordingStarted?.Invoke(this, EventArgs.Empty);
        }

        protected void OnRecordingStopped()
        {
            RecordingStopped?.Invoke(this, EventArgs.Empty);
        }

        protected void OnRecordingPaused()
        {
            RecordingPaused?.Invoke(this, EventArgs.Empty);
        }

        protected void OnRecordingResumed()
        {
            RecordingResumed?.Invoke(this, EventArgs.Empty);
        }

        protected void OnRecordingTimeUpdated()
        {
            RecordingTimeUpdated?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }

    [DataContract]
        public class ScreenRecordingSettings
        {
            [DataMember]
            public string? Quality { get; set; }
            
            [DataMember]
            public string? Region { get; set; }
            
            [DataMember]
            public string? SavePath { get; set; }
        
        [DataMember]
        public bool RecordAudio { get; set; }
        
        [DataMember]
        public bool ShowCursor { get; set; }
        
        // 其他可能的设置属性
        [DataMember]
        public int FrameRate { get; set; } = 30;
        
        [DataMember]
        public int BitRate { get; set; } = 5000000; // 默认5Mbps
    }
}