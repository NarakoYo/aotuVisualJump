using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;
namespace ImageRecognitionApp.UnitTools
{
    /// <summary>
    /// 日志管理器 - 负责日志的创建、管理和写入
    /// 提供单例访问模式，支持异步写入、日志滚动和级别过滤
    /// </summary>
    public class LogManager
    {
        #region 私有字段
        private static LogManager? _instance;
        private static readonly object _lock = new object();
        private string _logDirectory;
        private TimeZoneInfo _timeZone;
        private int _maxFileSizeKB = 1024; // 默认最大文件大小1MB
        private int _maxFilesPerDay = 10;  // 默认每天最多10个文件
        private LogLevel _minLogLevel = LogLevel.Info; // 默认日志级别
        private string _fileNameFormat = "yyyy-MM-dd-HH"; // 默认文件名格式
        private readonly object _fileLock = new object(); // 文件写入锁
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        #endregion

        #region 配置属性
        /// <summary>
        /// 最大文件大小(KB)
        /// </summary>
        public int MaxFileSizeKB
        {
            get => _maxFileSizeKB;
            set => _maxFileSizeKB = value > 0 ? value : 1024;
        }

        /// <summary>
        /// 每天最多文件数
        /// </summary>
        public int MaxFilesPerDay
        {
            get => _maxFilesPerDay;
            set => _maxFilesPerDay = value > 0 ? value : 10;
        }

        /// <summary>
        /// 最小日志级别
        /// </summary>
        public LogLevel MinLogLevel
        {
            get => _minLogLevel;
            set => _minLogLevel = value;
        }

        /// <summary>
        /// 日志文件名格式
        /// </summary>
        public string FileNameFormat
        {
            get => _fileNameFormat;
            set => _fileNameFormat = !string.IsNullOrEmpty(value) ? value : "yyyy-MM-dd-HH";
        }

        /// <summary>
        /// 日志目录
        /// </summary>
        public string LogDirectory
        {
            get => _logDirectory;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _logDirectory = value;
                    EnsureDirectoryExists();
                }
            }
        }
        #endregion

        /// <summary>
        /// 日志严重类型枚举
        /// </summary>
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Fatal
        }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private LogManager()
        {
            // 从配置文件加载设置
            LoadConfiguration();

            // 初始化日志目录
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            EnsureDirectoryExists();

            // 初始化时区 - 优先使用系统时区，否则使用UTC
            try
            {
                _timeZone = TimeZoneInfo.Local;
            }
            catch
            {
                _timeZone = TimeZoneInfo.Utc;
            }
        }

        /// <summary>
        /// 确保日志目录存在
        /// </summary>
        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建日志目录失败: {ex.Message}");
                // 回退到应用程序基目录
                _logDirectory = AppDomain.CurrentDomain.BaseDirectory;
                EnsureDirectoryExists();
            }
        }

        /// <summary>
        /// 从配置文件加载设置
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // 从app.config加载配置
                string maxFileSize = ConfigurationManager.AppSettings["Log.MaxFileSizeKB"];
                if (!string.IsNullOrEmpty(maxFileSize) && int.TryParse(maxFileSize, out int size))
                {
                    MaxFileSizeKB = size;
                }

                string maxFiles = ConfigurationManager.AppSettings["Log.MaxFilesPerDay"];
                if (!string.IsNullOrEmpty(maxFiles) && int.TryParse(maxFiles, out int files))
                {
                    MaxFilesPerDay = files;
                }

                string minLevel = ConfigurationManager.AppSettings["Log.MinLevel"];
                if (!string.IsNullOrEmpty(minLevel) && Enum.TryParse(minLevel, out LogLevel level))
                {
                    MinLogLevel = level;
                }

                string fileNameFormat = ConfigurationManager.AppSettings["Log.FileNameFormat"];
                if (!string.IsNullOrEmpty(fileNameFormat))
                {
                    FileNameFormat = fileNameFormat;
                }
            }
            catch (Exception ex)
            {
                // 配置加载失败，使用默认值
                Console.WriteLine($"日志配置加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取日志管理器实例
        /// </summary>
        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化日志管理器
        /// </summary>
        public void Initialize()
        {
            // 验证日志目录
            EnsureDirectoryExists();

            // 记录初始化信息
            WriteLog(LogLevel.Info, "日志管理器初始化完成");
        }

        /// <summary>
        /// 异步初始化日志管理器
        /// </summary>
        /// <returns>初始化任务</returns>
        public async Task InitializeAsync()
        {
            EnsureDirectoryExists();
            await WriteLogAsync(LogLevel.Info, "日志管理器异步初始化完成");
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="level">日志严重类型</param>
        /// <param name="message">日志内容</param>
        public void WriteLog(LogLevel level, string message)
        {
            // 检查日志级别
            if (level < _minLogLevel)
                return;

            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                // 获取当前时间（根据时区）
                DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
                string timeZoneName = _timeZone.Id;

                // 生成日志文件名
                string fileName = GenerateLogFileName(now);
                string filePath = Path.Combine(_logDirectory, fileName);

                // 检查文件大小并滚动
                CheckAndRollLogFile(filePath);

                // 格式化日志内容
                string logContent = FormatLogContent(now, timeZoneName, level, message);

                // 写入日志
                lock (_fileLock)
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                    {
                        writer.Write(logContent);
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理日志写入异常
                Console.WriteLine($"日志写入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步写入日志
        /// </summary>
        /// <param name="level">日志严重类型</param>
        /// <param name="message">日志内容</param>
        /// <returns>写入任务</returns>
        public async Task WriteLogAsync(LogLevel level, string message)
        {
            // 检查日志级别
            if (level < _minLogLevel)
                return;

            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                // 获取当前时间（根据时区）
                DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
                string timeZoneName = _timeZone.Id;

                // 生成日志文件名
                string fileName = GenerateLogFileName(now);
                string filePath = Path.Combine(_logDirectory, fileName);

                // 检查文件大小并滚动
                CheckAndRollLogFile(filePath);

                // 格式化日志内容
                string logContent = FormatLogContent(now, timeZoneName, level, message);

                // 异步写入日志
                await WriteToFileAsync(filePath, logContent);
            }
            catch (Exception ex)
            {
                // 处理日志写入异常
                Console.WriteLine($"异步日志写入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录程序启动或关闭的标记日志
        /// </summary>
        /// <param name="isStartup">是否为启动标记</param>
        public void WriteStartupShutdownLog(bool isStartup)
        {
            try
            {
                // 获取当前时间
                DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
                string formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss");

                // 获取本地化文本
                string localizedText = JsonLocalizationHelper.Instance.GetString(10001);

                // 生成日志内容
                string logContent = $"[{formattedTime}]+{localizedText}+{(isStartup ? "Start" : "Close" + "\n")}";

                // 写入日志
                WriteLog(LogLevel.Info, logContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入启动/关闭日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">要写入的内容</param>
        /// <returns>异步任务</returns>
        private async Task WriteToFileAsync(string filePath, string content)
        {
            // 使用异步锁
            await _semaphore.WaitAsync();
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    await writer.WriteAsync(content);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 检查文件大小并滚动日志
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private void CheckAndRollLogFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > _maxFileSizeKB * 1024)
            {
                // 文件超过最大大小，需要滚动
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                string fileName = Path.GetFileNameWithoutExtension(filePath) ?? "log";
                string extension = Path.GetExtension(filePath) ?? ".log";

                // 确保目录存在
                if (!Directory.Exists(directory))
                    directory = LogDirectory;

                // 查找可用的滚动文件名
                int counter = 1;
                string rolledFilePath;
                do
                {
                    rolledFilePath = Path.Combine(directory, $"{fileName}.{counter}{extension}");
                    counter++;
                } while (File.Exists(rolledFilePath) && counter < 1000); // 防止无限循环

                try
                {
                    // 重命名文件
                    File.Move(filePath, rolledFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志文件滚动失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 生成日志文件名
        /// </summary>
        /// <param name="now">当前时间</param>
        /// <returns>日志文件名</returns>
        public string GenerateLogFileName(DateTime now)
        {
            // 使用配置的文件名格式
            string baseName = now.ToString(_fileNameFormat);
            string extension = ".log";
            return baseName + extension;
        }

        /// <summary>
        /// 格式化日志内容
        /// </summary>
        /// <param name="time">触发时间</param>
        /// <param name="timeZone">时区</param>
        /// <param name="level">日志严重类型</param>
        /// <param name="message">日志内容</param>
        /// <returns>格式化后的日志内容</returns>
        private string FormatLogContent(DateTime time, string timeZone, LogLevel level, string message)
        {
            StringBuilder sb = new StringBuilder();

            // 格式化触发时间和时区
            string timeStr = time.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string timeZoneStr = $"({timeZone})";

            // 格式化日志严重类型
            string levelStr = level.ToString().ToUpper();

            // 构建日志头部
            sb.AppendLine($"[{timeStr}-{timeZoneStr}-{levelStr}-{message.Split('\n')[0]}]");

            // 处理多行日志的缩进
            if (message.Contains('\n'))
            {
                string[] lines = message.Split('\n');
                for (int i = 1; i < lines.Length; i++)
                {
                    sb.AppendLine($"    {lines[i]}");
                }
            }

            // 添加换行符分隔不同日志条目
            // sb.AppendLine();

            return sb.ToString();
        }

        #region 辅助方法
        /// <summary>
        /// 清理过期日志文件
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        public void CleanupOldLogs(int daysToKeep)
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return;

                DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                foreach (string file in Directory.GetFiles(_logDirectory, "*.log"))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理过期日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前日志目录大小
        /// </summary>
        /// <returns>目录大小(MB)</returns>
        public double GetLogDirectorySize()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return 0;

                DirectoryInfo dirInfo = new DirectoryInfo(_logDirectory);
                long size = dirInfo.EnumerateFiles("*.log", SearchOption.AllDirectories).Sum(f => f.Length);
                return size / (1024.0 * 1024.0); // 转换为MB
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取日志目录大小失败: {ex.Message}");
                return -1;
            }
        }
        #endregion
    }
}