using System;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ImageRecognitionApp.unit;

namespace ImageRecognitionApp.Assets.UI
{
    /// <summary>
    /// 系统信息窗口 - 显示应用程序和系统的相关信息
    /// </summary>
    public partial class SystemInfoWindow : Window
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemInfoWindow()
        {
            InitializeComponent();
            
            // 设置窗口为模态窗口
            this.Owner = Application.Current.MainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            // 设置窗口大小为2/3主窗口大小
            if (Application.Current.MainWindow != null)
            {
                this.Width = Application.Current.MainWindow.ActualWidth * 2 / 3;
                this.Height = Application.Current.MainWindow.ActualHeight * 2 / 3;
            }
            
            // 设置本地化标题
            try
            {
                // 确保本地化工具已初始化
                if (!JsonLocalizationHelper.Instance.IsInitialized)
                {
                    JsonLocalizationHelper.Instance.Initialize();
                }
                
                // 获取sign_id=10005的本地化标题
                string localizedTitle = JsonLocalizationHelper.Instance.GetString(10005);
                if (!string.IsNullOrEmpty(localizedTitle) && 
                    !localizedTitle.StartsWith("未找到") && 
                    !localizedTitle.StartsWith("ERROR_"))
                {
                    this.Title = localizedTitle;
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"设置系统信息窗口标题时出错: {ex.Message}");
            }
            
            // 加载系统信息
            LoadSystemInformation();
        }
        
        /// <summary>
        /// 加载系统信息
        /// </summary>
        private void LoadSystemInformation()
        {
            try
            {
                // 获取.NET Framework版本
                FrameworkVersionText.Text = Environment.Version.ToString();
                
                // 获取操作系统信息
                OSVersionText.Text = $"{Environment.OSVersion.VersionString}";
                
                // 获取系统架构
                ArchitectureText.Text = Environment.Is64BitOperatingSystem ? "64位" : "32位";
                
                // 获取系统内存信息
                try
                {
                    ulong totalMemoryBytes = GetTotalPhysicalMemory();
                    ulong totalMemoryGB = totalMemoryBytes / (1024 * 1024 * 1024);
                    MemoryText.Text = $"{totalMemoryGB} GB";
                }
                catch (Exception)
                {
                    MemoryText.Text = "无法获取";
                }
                
                // 获取CPU信息
                try
                {
                    string cpuInfo = GetCPUInformation();
                    CPUText.Text = cpuInfo;
                }
                catch (Exception)
                {
                    CPUText.Text = "无法获取";
                }
                
                // 获取屏幕分辨率
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                ResolutionText.Text = $"{screenWidth} × {screenHeight}";
                
                // 获取应用程序版本
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                AppVersionText.Text = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                
                // 获取应用程序路径
                AppPathText.Text = AppDomain.CurrentDomain.BaseDirectory;
            }
            catch (Exception ex)
            {
                // 记录错误但不影响应用运行
                (App.Current as App)?.LogMessage($"加载系统信息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取总物理内存大小
        /// </summary>
        /// <returns>总物理内存字节数</returns>
        private ulong GetTotalPhysicalMemory()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    }
                }
            }
            catch { }
            
            return 0;
        }
        
        /// <summary>
        /// 获取CPU信息
        /// </summary>
        /// <returns>CPU信息</returns>
        private string GetCPUInformation()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["Name"].ToString();
                    }
                }
            }
            catch { }
            
            return "未知处理器";
        }
        
        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}