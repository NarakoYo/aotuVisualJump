using ImageRecognitionApp.Assets.UI;
using System;
using System.Windows;

namespace ImageRecognitionApp.Assets.UICode
{
    /// <summary>
    /// 系统信息管理器 - 负责处理系统信息相关的功能
    /// </summary>
    public class SystemInfoManager
    {
        /// <summary>
        /// 显示系统信息窗口
        /// </summary>
        public void ShowSystemInfoWindow()
        {
            try
            {
                // 创建并显示系统信息窗口，传递主窗口作为owner以确保正确的大小和位置
                SystemInfoWindow systemInfoWindow = new SystemInfoWindow(Application.Current.MainWindow);

                // 设置为模态窗口，这样在关闭之前无法操作主窗口
                systemInfoWindow.ShowDialog();

                // 记录日志
                (App.Current as App)?.LogMessage("系统信息窗口已显示");
            }
            catch (Exception ex)
            {
                // 记录错误
                (App.Current as App)?.LogMessage($"显示系统信息窗口时出错: {ex.Message}");
                // 显示错误消息给用户
                MessageBox.Show("显示系统信息窗口时出错: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}