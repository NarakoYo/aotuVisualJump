using System;
using System.Threading;
// using ImageRecognitionApp.UnitTools;

namespace ImageRecognitionApp.UnitTools
{
    /// <summary>
    /// 跟踪助手类，用于记录应用程序运行时的跟踪信息
    /// </summary>
    public static class TraceHelper
    {
        /// <summary>
        /// 记录一条跟踪信息
        /// </summary>
        /// <param name="message">要记录的消息</param>
        public static void Record(string message)
        {
            try
            {
                // 使用LogManager记录日志，日志级别设为Info
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, message);
            }
            catch (Exception)
            {
                // 日志记录失败时静默处理，避免影响主流程
            }
        }
    }
}