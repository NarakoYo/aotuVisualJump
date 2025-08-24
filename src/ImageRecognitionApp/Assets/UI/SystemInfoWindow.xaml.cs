using System;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
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
            
            // 加载本地化文本
            InitializeLocalizedTexts();
            
            // 加载系统信息
            LoadSystemInformation();
        }
        
        /// <summary>
        /// 初始化本地化文本
        /// </summary>
        private void InitializeLocalizedTexts()
        {
            try
            {
                // 确保本地化工具已初始化
                if (!JsonLocalizationHelper.Instance.IsInitialized)
                {
                    JsonLocalizationHelper.Instance.Initialize();
                }
                
                // 设置窗口标题 (sign_id=10005)
                string localizedTitle = JsonLocalizationHelper.Instance.GetString(10005);
                if (!string.IsNullOrEmpty(localizedTitle) && 
                    !localizedTitle.StartsWith("未找到") && 
                    !localizedTitle.StartsWith("ERROR_"))
                {
                    this.Title = localizedTitle;
                }
                
                // 设置系统信息折叠面板标题 (sign_id=20007)
                string systemInfoTitle = JsonLocalizationHelper.Instance.GetString(20007);
                if (!string.IsNullOrEmpty(systemInfoTitle) && 
                    !systemInfoTitle.StartsWith("未找到") && 
                    !systemInfoTitle.StartsWith("ERROR_"))
                {
                    SystemInfoExpander.Header = systemInfoTitle;
                }
                else
                {
                    SystemInfoExpander.Header = "系统信息";
                }
                
                // 设置设备信息折叠面板标题 (sign_id=20008)
                string deviceInfoTitle = JsonLocalizationHelper.Instance.GetString(20008);
                if (!string.IsNullOrEmpty(deviceInfoTitle) && 
                    !deviceInfoTitle.StartsWith("未找到") && 
                    !deviceInfoTitle.StartsWith("ERROR_"))
                {
                    DeviceInfoExpander.Header = deviceInfoTitle;
                }
                else
                {
                    DeviceInfoExpander.Header = "设备信息";
                }
                
                // 设置系统信息项标签
                WinVersionLabel.Text = GetLocalizedText(20009, "Windows版本:");
                WinBuildLabel.Text = GetLocalizedText(20010, "Windows版本号:");
                OSVersionLabel.Text = GetLocalizedText(20011, "操作系统版本:");
                DeviceNameLabel.Text = GetLocalizedText(20012, "设备名称:");
                ArchitectureLabel.Text = GetLocalizedText(20013, "系统架构:");
                DeviceIDLabel.Text = GetLocalizedText(20014, "设备ID:");
                ProductIDLabel.Text = GetLocalizedText(20015, "产品ID:");
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"初始化本地化文本时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取本地化文本，如果获取失败则返回默认值
        /// </summary>
        /// <param name="signId">本地化标识ID</param>
        /// <param name="defaultText">默认文本</param>
        /// <returns>本地化文本或默认文本</returns>
        private string GetLocalizedText(int signId, string defaultText)
        {
            try
            {
                string localizedText = JsonLocalizationHelper.Instance.GetString(signId);
                if (!string.IsNullOrEmpty(localizedText) && 
                    !localizedText.StartsWith("未找到") && 
                    !localizedText.StartsWith("ERROR_"))
                {
                    return localizedText;
                }
            }
            catch { }
            
            return defaultText;
        }
        
        /// <summary>
        /// 加载系统信息
        /// </summary>
        private void LoadSystemInformation()
        {
            try
            {
                // 获取Windows版本
                try
                {
                    string winVersion = GetWindowsVersion();
                    WinVersionValue.Text = string.IsNullOrEmpty(winVersion) ? GetLocalizedText(20016, "未正常获取") : winVersion;
                }
                catch (Exception)
                {
                    WinVersionValue.Text = GetLocalizedText(20016, "未正常获取");
                }
                
                // 获取Windows版本号
                try
                {
                    string winBuild = GetWindowsBuildNumber();
                    WinBuildValue.Text = string.IsNullOrEmpty(winBuild) ? GetLocalizedText(20016, "未正常获取") : winBuild;
                }
                catch (Exception)
                {
                    WinBuildValue.Text = GetLocalizedText(20016, "未正常获取");
                }
                
                // 获取操作系统版本
                try
                {
                    string osVersion = Environment.OSVersion.VersionString;
                    OSVersionValue.Text = string.IsNullOrEmpty(osVersion) ? GetLocalizedText(20016, "未正常获取") : osVersion;
                }
                catch (Exception)
                {
                    OSVersionValue.Text = GetLocalizedText(20016, "未正常获取");
                }
                
                // 获取设备名称
                try
                {
                    string deviceName = Environment.MachineName;
                    DeviceNameValue.Text = string.IsNullOrEmpty(deviceName) ? GetLocalizedText(20016, "未正常获取") : deviceName;
                }
                catch (Exception)
                {
                    DeviceNameValue.Text = GetLocalizedText(20016, "未正常获取");
                }
                
                // 获取系统架构
                try
                {
                    string architecture = Environment.Is64BitOperatingSystem ? "X86_64" : "X86_32";
                    ArchitectureValue.Text = architecture;
                }
                catch (Exception)
                {
                    ArchitectureValue.Text = GetLocalizedText(20016, "未正常获取");
                }
                
                // 获取设备ID
                try
                {
                    string deviceId = GetDeviceId();
                    DeviceIDValue.Text = string.IsNullOrEmpty(deviceId) ? GetLocalizedText(20016, "未正常获取") : deviceId;
                }
                catch (Exception)
                {
                    DeviceIDValue.Text = GetLocalizedText(20016, "未正常获取");
                }
                
                // 获取产品ID
                try
                {
                    string productId = GetProductId();
                    ProductIDValue.Text = string.IsNullOrEmpty(productId) ? GetLocalizedText(20016, "未正常获取") : productId;
                }
                catch (Exception)
                {
                    ProductIDValue.Text = GetLocalizedText(20016, "未正常获取");
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响应用运行
                (App.Current as App)?.LogMessage($"加载系统信息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取Windows版本名称
        /// </summary>
        /// <returns>Windows版本名称</returns>
        private string GetWindowsVersion()
        {
            try
            {
                // 获取Windows版本信息
                Version version = Environment.OSVersion.Version;
                
                // 根据版本号判断Windows版本名称
                if (version.Major == 10 && version.Build >= 22000)
                {
                    return "Windows 11";
                }
                else if (version.Major == 10)
                {
                    return "Windows 10";
                }
                else if (version.Major == 6 && version.Minor == 3)
                {
                    return "Windows 8.1";
                }
                else if (version.Major == 6 && version.Minor == 2)
                {
                    return "Windows 8";
                }
                else if (version.Major == 6 && version.Minor == 1)
                {
                    return "Windows 7";
                }
                else
                {
                    return "Windows 其他版本";
                }
            }
            catch { }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 获取Windows内部版本号
        /// </summary>
        /// <returns>Windows内部版本号</returns>
        private string GetWindowsBuildNumber()
        {
            try
            {
                // 使用ManagementObjectSearcher获取更详细的Windows版本信息
                using (var searcher = new ManagementObjectSearcher("SELECT BuildNumber, CSDVersion FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string buildNumber = obj["BuildNumber"]?.ToString() ?? string.Empty;
                        string csdVersion = obj["CSDVersion"]?.ToString() ?? string.Empty;
                        
                        if (!string.IsNullOrEmpty(buildNumber))
                        {
                            if (!string.IsNullOrEmpty(csdVersion))
                            {
                                return $"{buildNumber} {csdVersion}";
                            }
                            return buildNumber;
                        }
                    }
                }
                
                // 如果上面的方法失败，回退到Environment.OSVersion
                return Environment.OSVersion.Version.Build.ToString();
            }
            catch { }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 获取设备ID
        /// </summary>
        /// <returns>设备ID</returns>
        private string GetDeviceId()
        {
            try
            {
                // 使用ManagementObjectSearcher获取设备ID
                using (var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string uuid = obj["UUID"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(uuid) && !uuid.Equals("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", StringComparison.OrdinalIgnoreCase))
                        {
                            return uuid;
                        }
                    }
                }
            }
            catch { }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 获取产品ID
        /// </summary>
        /// <returns>产品ID</returns>
        private string GetProductId()
        {
            try
            {
                // 使用ManagementObjectSearcher获取产品ID
                using (var searcher = new ManagementObjectSearcher("SELECT IdentificationCode FROM Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string productId = obj["IdentificationCode"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(productId))
                        {
                            return productId;
                        }
                    }
                }
            }
            catch { }
            
            return string.Empty;
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