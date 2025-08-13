using System;
using System.IO;
using System.Text;

namespace ImageRecognitionApp.unit
{
    /// <summary>
    /// 日志管理器 - 负责日志的创建、管理和写入
    /// </summary>
    public class LogManager
    {
        private static LogManager _instance;
        private static readonly object _lock = new object();
        private string _logDirectory;
        private TimeZoneInfo _timeZone;

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
            // 初始化日志目录
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

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
            // 初始化逻辑已在构造函数中完成
            // 此方法留空，以满足调用要求
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="level">日志严重类型</param>
        /// <param name="message">日志内容</param>
        public void WriteLog(LogLevel level, string message)
        {
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

                // 格式化日志内容
                string logContent = FormatLogContent(now, timeZoneName, level, message);

                // 写入日志
                using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    writer.Write(logContent);
                }
            }
            catch (Exception ex)
            {
                // 处理日志写入异常
                Console.WriteLine($"日志写入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成日志文件名
        /// </summary>
        /// <param name="now">当前时间</param>
        /// <returns>日志文件名</returns>
        private string GenerateLogFileName(DateTime now)
        {
            // 基础文件名格式: 年-月-日-时
            string baseName = $"{now:yyyy-MM-dd-HH}";
            string extension = ".log";
            string fileName = baseName + extension;

            return fileName;
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

            return sb.ToString();
        }
    }
}