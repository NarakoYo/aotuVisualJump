using System;
using System.Text;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ImageRecognitionApp.UnitTools;
using System.Collections.Generic;
using System.Linq;

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
        /// <param name="owner">可选的所有者窗口参数，用于避免依赖Application.Current.MainWindow的自动设置</param>
        public SystemInfoWindow(Window owner = null)
        {
            InitializeComponent();

            // 添加日志记录以调试窗口状态
            (App.Current as App)?.LogMessage($"SystemInfoWindow constructor: MainWindow reference {(Application.Current.MainWindow == null ? "null" : "exists")}");

            // 检查MainWindow和当前窗口的引用关系
            bool isSameReference = Application.Current.MainWindow == this;
            (App.Current as App)?.LogMessage($"SystemInfoWindow constructor: MainWindow == this? {isSameReference}");
            (App.Current as App)?.LogMessage($"SystemInfoWindow constructor: MainWindow type: {Application.Current.MainWindow?.GetType().Name ?? "null"}, Current window type: {this.GetType().Name}");

            // 设置窗口为模态窗口，改进判断逻辑以确保能够动态设置窗口大小
            // 优先使用传入的owner参数，如果没有传入则尝试使用Application.Current.MainWindow
            if (owner != null)
            {
                // 如果明确传入了owner参数，直接使用它
                this.Owner = owner;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // 设置窗口大小为2/3主窗口大小
                this.Width = owner.ActualWidth * 2 / 3;
                this.Height = owner.ActualHeight * 2 / 3;
                
                (App.Current as App)?.LogMessage($"SystemInfoWindow size set to: {this.Width}x{this.Height} (based on Owner window size: {owner.ActualWidth}x{owner.ActualHeight})");
            }
            else if (Application.Current.MainWindow != null && !(Application.Current.MainWindow is SystemInfoWindow))
            {
                // 如果没有传入owner参数，但有可用的MainWindow且不是SystemInfoWindow类型，则使用MainWindow
                this.Owner = Application.Current.MainWindow;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // 设置窗口大小为2/3主窗口大小
                this.Width = Application.Current.MainWindow.ActualWidth * 2 / 3;
                this.Height = Application.Current.MainWindow.ActualHeight * 2 / 3;
                
                (App.Current as App)?.LogMessage($"SystemInfoWindow size set to: {this.Width}x{this.Height} (based on MainWindow size: {Application.Current.MainWindow.ActualWidth}x{Application.Current.MainWindow.ActualHeight})");
            }
            else
            {
                // 如果没有有效的owner窗口，使用默认居中位置
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                // 设置默认窗口大小
                this.Width = 800;
                this.Height = 600;
                
                (App.Current as App)?.LogMessage("SystemInfoWindow using default size: 800x600, CenterScreen location");
            }

            // 加载本地化文本
            InitializeLocalizedTexts();

            // 加载系统信息
            LoadSystemInformation();

            // 窗口加载完成后设置折叠面板的箭头图标和动画
            this.Loaded += SystemInfoWindow_Loaded;
        }

        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void SystemInfoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 为所有折叠面板添加箭头图标和动画
            SetupExpanderIconsAndAnimations();
        }

        /// <summary>
        /// 为折叠面板设置箭头图标和动画
        /// </summary>
        private void SetupExpanderIconsAndAnimations()
        {
            try
            {
                // 为系统信息折叠面板设置图标和动画
                SetupExpander(SystemInfoExpander);
                // 获取系统信息折叠面板的内容容器
                Panel systemInfoContent = FindVisualChild<Panel>(SystemInfoExpander, "ContentStackPanel");
                if (systemInfoContent == null)
                {
                    // 尝试直接获取折叠面板的内容
                    if (SystemInfoExpander.Content is StackPanel)
                    {
                        systemInfoContent = SystemInfoExpander.Content as StackPanel;
                    }
                }
                if (systemInfoContent != null)
                {
                    // 设置动画
                    SystemInfoAnimation.SetupExpanderAnimation(SystemInfoExpander, systemInfoContent);
                }

                // 为设备信息折叠面板设置图标和动画
                SetupExpander(DeviceInfoExpander);
                // 获取设备信息折叠面板的内容容器
                Panel deviceInfoContent = FindVisualChild<Panel>(DeviceInfoExpander, "ContentStackPanel");
                if (deviceInfoContent == null)
                {
                    // 尝试直接获取折叠面板的内容
                    if (DeviceInfoExpander.Content is StackPanel)
                    {
                        deviceInfoContent = DeviceInfoExpander.Content as StackPanel;
                    }
                }
                if (deviceInfoContent != null)
                {
                    // 设置动画
                    SystemInfoAnimation.SetupExpanderAnimation(DeviceInfoExpander, deviceInfoContent);
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"设置折叠面板图标和动画时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 为单个折叠面板设置图标
        /// </summary>
        private void SetupExpander(Expander expander)
        {
            try
            {
                // 查找图标容器
                Grid iconContainer = FindVisualChild<Grid>(expander, "IconContainer");
                if (iconContainer != null)
                {
                    // 查找箭头图标
                    Image arrowIcon = FindVisualChild<Image>(iconContainer, "ArrowIcon");
                    if (arrowIcon != null)
                    {
                        // 根据当前展开状态设置图标
                        UpdateArrowIcon(expander, arrowIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"为单个折叠面板设置图标时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新箭头图标资源
        /// </summary>
        private void UpdateArrowIcon(Expander expander, Image arrowIcon)
        {
            try
            {
                if (expander != null && arrowIcon != null)
                {
                    // 根据展开状态设置不同的图标
                    int iconId = expander.IsExpanded ? 20007 : 20008;
                    BitmapImage iconImage = AssetHelper.Instance.GetImageAsset(iconId);
                    arrowIcon.Source = iconImage;
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"更新箭头图标时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找指定名称的视觉子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T frameworkElement && frameworkElement.Name == name)
                {
                    return frameworkElement;
                }
                else
                {
                    T result = FindVisualChild<T>(child, name);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取箭头图标的父级折叠面板
        /// </summary>
        private Expander GetParentExpander(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is Expander))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as Expander;
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

                // 设置设备信息项标签
                ModelLabel.Text = GetLocalizedText(20017, "型号:");
                CpuLabel.Text = GetLocalizedText(20018, "中央处理器:");
                MotherboardLabel.Text = GetLocalizedText(20019, "主板:");
                MemoryLabel.Text = GetLocalizedText(20020, "内存:");
                GpuLabel.Text = GetLocalizedText(20021, "图形处理器:");
                DiskLabel.Text = GetLocalizedText(20022, "磁盘:");
                AudioCardLabel.Text = GetLocalizedText(20023, "声卡:");
                NetworkCardLabel.Text = GetLocalizedText(20024, "网卡:");
                MonitorLabel.Text = GetLocalizedText(20025, "显示器:");
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
        /// 加载系统信息并显示在界面上
        /// </summary>
        /// <remarks>
        /// 此方法通过调用一系列专用方法获取各种系统信息，并在对应的UI元素中显示
        /// 每种信息的获取都包含异常处理机制，确保单个信息获取失败不会影响整体功能
        /// </remarks>
        private void LoadSystemInformation()
        {
            try
            {
                // 使用辅助方法减少重复代码
                SetTextWithErrorHandling(WinVersionValue, GetWindowsVersion, "Windows版本");
                SetTextWithErrorHandling(WinBuildValue, GetWindowsBuildNumber, "Windows版本号");
                SetTextWithErrorHandling(OSVersionValue, () => Environment.OSVersion.VersionString, "操作系统版本");
                SetTextWithErrorHandling(DeviceNameValue, () => Environment.MachineName, "设备名称");
                SetTextWithErrorHandling(ArchitectureValue, () => Environment.Is64BitOperatingSystem ? "X86_64" : "X86_32", "系统架构");
                SetTextWithErrorHandling(DeviceIDValue, GetDeviceId, "设备ID");
                SetTextWithErrorHandling(ProductIDValue, GetProductId, "产品ID");
                SetTextWithErrorHandling(ModelValue, GetDeviceModel, "设备型号");
                SetTextWithErrorHandling(CpuValue, GetCpuInfo, "中央处理器信息");
                SetTextWithErrorHandling(MotherboardValue, GetMotherboardInfo, "主板信息");
                SetTextWithErrorHandling(MemoryValue, GetMemoryInfo, "内存信息");
                SetTextWithErrorHandling(GpuValue, GetGpuInfo, "图形处理器信息");
                SetTextWithErrorHandling(DiskValue, GetDiskInfo, "磁盘信息");
                SetTextWithErrorHandling(AudioCardValue, GetAudioCardInfo, "声卡信息");
                SetTextWithErrorHandling(NetworkCardValue, GetNetworkCardInfo, "网卡信息");

                // 显示显示器信息（包含比例图标）
                try
                {
                    DisplayMonitorInfo();
                }
                catch (Exception ex)
                {
                    (App.Current as App)?.LogMessage($"显示显示器信息失败: {ex.Message}");
                    MonitorValue.Children.Clear();
                    TextBlock errorText = new TextBlock();
                    errorText.Text = GetLocalizedText(20016, "未正常获取");
                    errorText.Style = FindResource("InfoValueStyle") as Style;
                    MonitorValue.Children.Add(errorText);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响应用运行
                (App.Current as App)?.LogMessage($"加载系统信息时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 辅助方法：设置文本内容并处理异常
        /// </summary>
        /// <param name="textBlock">目标TextBlock控件</param>
        /// <param name="getValueFunction">获取值的函数</param>
        /// <param name="infoType">信息类型，用于日志记录</param>
        private void SetTextWithErrorHandling(TextBlock textBlock, Func<string?> getValueFunction, string infoType)
        {
            try
            {
                string value = getValueFunction();
                textBlock.Text = string.IsNullOrEmpty(value) ? GetLocalizedText(20016, "未正常获取") : value;
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取{infoType}失败: {ex.Message}");
                textBlock.Text = GetLocalizedText(20016, "未正常获取");
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
                    return "Windows其他版本";
                }
            }
            catch { }

            return string.Empty;
        }

        /// <summary>
        /// 显示显示器信息
        /// </summary>
        /// <remarks>
        /// 此方法负责在界面上显示显示器信息，包括品牌型号ID、分辨率及当前刷新率
        /// 支持多个显示器的换行显示，仅展示实际读取到的内容
        /// </remarks>
        private void DisplayMonitorInfo()
        {
            try
            {
                // 清除现有内容
                MonitorValue.Children.Clear();

                // 获取显示器信息
                List<string> monitorInfos = GetMonitorInfo();

                if (monitorInfos.Count > 0)
                {
                    // 添加每个显示器的信息
                    for (int i = 0; i < monitorInfos.Count; i++)
                    {
                        string monitorInfo = monitorInfos[i];

                        if (!string.IsNullOrEmpty(monitorInfo))
                        {
                            TextBlock textBlock = new TextBlock();
                            textBlock.Text = monitorInfo;
                            textBlock.Style = FindResource("InfoValueStyle") as Style;
                            textBlock.Margin = new Thickness(0, 2, 0, 2); // 移除左边距，只保留上下边距

                            // 添加到界面
                            MonitorValue.Children.Add(textBlock);
                        }
                    }
                }
                else
                {
                    // 未找到显示器信息时显示默认文本
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = GetLocalizedText(20016, "未正常获取");
                    textBlock.Style = FindResource("InfoValueStyle") as Style;
                    textBlock.Margin = new Thickness(0, 2, 0, 2); // 移除左边距，只保留上下边距
                    MonitorValue.Children.Add(textBlock);
                }
            }
            catch (Exception ex)
            {
                // 异常处理
                (App.Current as App)?.LogMessage($"显示显示器信息失败: {ex.Message}");
                MonitorValue.Children.Clear();
                TextBlock errorText = new TextBlock();
                errorText.Text = GetLocalizedText(20016, "未正常获取");
                errorText.Style = FindResource("InfoValueStyle") as Style;
                errorText.Margin = new Thickness(0, 2, 0, 2); // 移除左边距，只保留上下边距
                MonitorValue.Children.Add(errorText);
            }
        }

        /// <summary>
        /// 获取物理显示器的相关信息
        /// </summary>
        /// <returns>包含每个显示器信息的字符串列表</returns>
        private List<string> GetMonitorInfo()
        {
            List<string> monitorInfos = new List<string>();
            Dictionary<string, MonitorInfo> monitorInfoDict = new Dictionary<string, MonitorInfo>();

            try
            {
                // 使用Win32_PnPEntity获取更详细的显示器信息（硬件ID、友好名称）
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, PNPDeviceID, Name, Description FROM Win32_PnPEntity WHERE Service='monitor'"))
                {
                    var monitors = searcher.Get();
                    if (monitors.Count > 0)
                    {
                        foreach (ManagementObject monitor in monitors)
                        {
                            try
                            {
                                // 获取显示器基本信息
                                string deviceId = monitor["DeviceID"]?.ToString() ?? "未知ID";
                                string pnpDeviceId = monitor["PNPDeviceID"]?.ToString() ?? "未知硬件ID";
                                string friendlyName = monitor["Name"]?.ToString() ?? "未知名称";
                                string description = monitor["Description"]?.ToString() ?? "未知描述";

                                // 创建显示器信息对象
                                MonitorInfo info = new MonitorInfo
                                {
                                    DeviceName = deviceId,
                                    HardwareId = pnpDeviceId,
                                    FriendlyName = friendlyName,
                                    Description = description
                                };

                                // 添加到字典中
                                monitorInfoDict[deviceId] = info;
                            }
                            catch (Exception ex)
                            {
                                (App.Current as App)?.LogMessage($"解析单个显示器信息失败: {ex.Message}");
                                // 继续处理下一个显示器
                            }
                        }
                    }
                }

                // 使用Windows API获取显示器的分辨率、刷新率和HDR/SDR信息
                GetDisplayInfo(monitorInfoDict, monitorInfos);

            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取显示器信息失败: {ex.Message}");
            }

            return monitorInfos;
        }

        /// <summary>
        /// 显示器信息类
        /// </summary>
        private class MonitorInfo
        {
            public string? DeviceName { get; set; }
            public string? HardwareId { get; set; }
            public string? FriendlyName { get; set; }
            public string? Description { get; set; }
            public string? Resolution { get; set; }
            public int RefreshRate { get; set; }
            public string? HdrSupport { get; set; }
        }

        /// <summary>
        /// 使用Windows API获取显示器的分辨率、刷新率和HDR/SDR信息
        /// </summary>
        /// <param name="monitorInfoDict">显示器信息字典</param>
        /// <param name="monitorInfos">显示器信息字符串列表</param>
        private void GetDisplayInfo(Dictionary<string, MonitorInfo> monitorInfoDict, List<string> monitorInfos)
        {
            try
            {
                // 使用EnumDisplaySettings获取显示器信息
                int displayIndex = 0;
                DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(dd);

                // 枚举所有显示器
                while (EnumDisplayDevices(null, displayIndex, ref dd, 0))
                {

                    // 只处理实际的显示设备
                    if ((dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0 &&
                        (dd.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) == 0)
                    {
                        DEVMODE dm = new DEVMODE();
                        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

                        // 获取当前显示设置
                        if (dd.DeviceName != null && EnumDisplaySettings(dd.DeviceName, ENUM_CURRENT_SETTINGS, ref dm))
                        {
                            // 计算刷新率
                            int refreshRate = dm.dmDisplayFrequency;
                            if (dm.dmDisplayFrequency > 1)
                            {
                                refreshRate = dm.dmDisplayFrequency;
                            }
                            else
                            {
                                refreshRate = 60; // 默认刷新率
                            }

                            // 获取分辨率
                            string resolution = $"{dm.dmPelsWidth}×{dm.dmPelsHeight}";

                            // 检查HDR/SDR支持
                            string hdrSupport = GetHdrSupportInfo(dd.DeviceName);

                            // 查找对应的显示器信息
                            MonitorInfo monitorInfo = null;
                            string deviceKey = null;

                            // 尝试通过设备名称匹配
                            if (monitorInfoDict != null && dd.DeviceName != null)
                            {
                                foreach (var key in monitorInfoDict.Keys)
                                {
                                    if (key != null && key.Contains(dd.DeviceName.Replace(@"\\\", @"\\"))) // 处理路径中的反斜杠
                                    {
                                        deviceKey = key;
                                        monitorInfo = monitorInfoDict[key];
                                        break;
                                    }
                                }
                            }

                            // 如果找不到对应的信息，创建新的
                            if (monitorInfo == null)
                            {
                                monitorInfo = new MonitorInfo
                                {
                                    DeviceName = dd.DeviceName,
                                    HardwareId = "未知硬件ID",
                                    FriendlyName = dd.DeviceString,
                                    Description = dd.DeviceString,
                                    Resolution = resolution,
                                    RefreshRate = refreshRate,
                                    HdrSupport = hdrSupport
                                };
                            }
                            else
                            {
                                // 更新已有信息
                                monitorInfo.Resolution = resolution;
                                monitorInfo.RefreshRate = refreshRate;
                                monitorInfo.HdrSupport = hdrSupport;
                            }

                            // 构建显示器信息字符串
                            StringBuilder infoBuilder = new StringBuilder();

                            // 添加友好名称/描述
                            if (!string.IsNullOrEmpty(monitorInfo.FriendlyName) && monitorInfo.FriendlyName != "未知名称")
                            {
                                infoBuilder.Append($"{monitorInfo.FriendlyName}");
                            }
                            else if (!string.IsNullOrEmpty(monitorInfo.Description) && monitorInfo.Description != "未知描述")
                            {
                                infoBuilder.Append($"{monitorInfo.Description}");
                            }

                            // 添加分辨率和刷新率
                            infoBuilder.Append($", {monitorInfo.Resolution} @ {monitorInfo.RefreshRate}Hz");

                            // 添加HDR/SDR支持信息（如果有）
                            if (!string.IsNullOrEmpty(monitorInfo.HdrSupport))
                            {
                                infoBuilder.Append($", {monitorInfo.HdrSupport}");
                            }

                            // 添加到列表
                            monitorInfos.Add(infoBuilder.ToString());

                            // 如果是从字典中找到的，从字典中移除，避免重复添加
                            if (deviceKey != null && monitorInfoDict.ContainsKey(deviceKey))
                            {
                                monitorInfoDict.Remove(deviceKey);
                            }
                        }
                    }

                    displayIndex++;
                    dd.cb = Marshal.SizeOf(dd);
                }

                // 添加剩余未匹配的显示器信息
                // foreach (var monitorInfo in monitorInfoDict.Values)
                // {
                //     StringBuilder infoBuilder = new StringBuilder();

                //     if (!string.IsNullOrEmpty(monitorInfo.FriendlyName) && monitorInfo.FriendlyName != "未知名称")
                //     {
                //         infoBuilder.Append($"{monitorInfo.FriendlyName}");
                //     }
                //     else if (!string.IsNullOrEmpty(monitorInfo.Description) && monitorInfo.Description != "未知描述")
                //     {
                //         infoBuilder.Append($"{monitorInfo.Description}");
                //     }

                //     monitorInfos.Add(infoBuilder.ToString());
                // }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"使用Windows API获取显示器信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查显示器的HDR/SDR支持信息
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>HDR/SDR支持信息，如果都不支持则返回空字符串</returns>
        private string GetHdrSupportInfo(string deviceName)
        {
            try
            {
                // 注意：这个方法使用了Windows 10/11的HDR API，需要Windows 10 1709或更高版本
                // 由于我们不能直接使用这些API（需要Windows SDK 10.0.16299.0或更高版本）
                // 这里使用一种简化的方法来检测HDR支持

                // 尝试通过WMI查询Win32_VideoController获取颜色深度和支持的颜色空间
                using (var searcher = new ManagementObjectSearcher("SELECT VideoModeDescription, CurrentBitsPerPixel, CurrentNumberOfColors FROM Win32_VideoController"))
                {
                    foreach (ManagementObject videoController in searcher.Get())
                    {
                        try
                        {
                            // 检查颜色深度，HDR显示器通常支持更高的颜色深度
                            int? bitsPerPixel = videoController["CurrentBitsPerPixel"] as int?;
                            if (bitsPerPixel.HasValue && bitsPerPixel.Value >= 30) // 30位或更高通常支持HDR
                            {
                                return "HDR支持";
                            }

                            // 检查视频模式描述中是否包含HDR相关关键词
                            string videoModeDesc = videoController["VideoModeDescription"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(videoModeDesc) &&
                                (videoModeDesc.Contains("HDR", StringComparison.OrdinalIgnoreCase) ||
                                 videoModeDesc.Contains("高动态范围", StringComparison.OrdinalIgnoreCase)))
                            {
                                return "HDR支持";
                            }
                        }
                        catch { }
                    }
                }

                // 如果没有检测到HDR支持，可以尝试检查是否支持SDR（标准动态范围）
                // 这里简单地认为如果不是HDR，就是SDR
                // 但按照用户要求，如果都不是则不显示，所以这里返回空字符串

            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"检查显示器HDR支持信息失败: {ex.Message}");
            }

            // 如果都不支持或检测失败，则返回空字符串
            return string.Empty;
        }

        // Windows API常量和结构体
        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1;
        private const int DISPLAY_DEVICE_MIRRORING_DRIVER = 0x8;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string? DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string? DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string? DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string? DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string? dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string? dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

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
        /// 获取设备型号
        /// </summary>
        /// <returns>设备型号</returns>
        private string GetDeviceModel()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Model FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string model = obj["Model"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(model))
                        {
                            return model;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 获取中央处理器信息
        /// </summary>
        /// <returns>CPU信息</returns>
        private string GetCpuInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string cpuName = obj["Name"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(cpuName))
                        {
                            return cpuName;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 获取主板信息
        /// </summary>
        /// <returns>主板信息</returns>
        private string GetMotherboardInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;
                        string product = obj["Product"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(manufacturer) && !string.IsNullOrEmpty(product))
                        {
                            return $"{manufacturer} {product}";
                        }
                        else if (!string.IsNullOrEmpty(product))
                        {
                            return product;
                        }
                        else if (!string.IsNullOrEmpty(manufacturer))
                        {
                            return manufacturer;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 获取物理内存信息
        /// </summary>
        /// <returns>内存信息字符串，包含每个内存条的型号、类型、频率和容量</returns>
        private string GetMemoryInfo()
        {
            try
            {
                StringBuilder memoryInfo = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT SMBIOSMemoryType, MemoryType, Speed, Capacity, PartNumber FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            string model = obj["PartNumber"]?.ToString() ?? "未知型号";
                            string memoryType = GetMemoryType(obj);
                            string speed = GetMemorySpeed(obj);
                            string capacityInfo = GetMemoryCapacity(obj);

                            // 格式化内存信息，包含SDRAM技术版本
                            string memoryDetails = $"{model} ({memoryType}  {speed}  {capacityInfo})";

                            if (memoryInfo.Length > 0)
                            {
                                // 多个内存条之间换行显示
                                memoryInfo.Append(Environment.NewLine);
                            }

                            memoryInfo.Append(memoryDetails);
                        }
                        catch (Exception ex)
                        {
                            (App.Current as App)?.LogMessage($"获取内存信息失败: {ex.Message}");
                            // 捕获单个内存信息获取异常，继续处理其他内存
                            if (memoryInfo.Length == 0)
                            {
                                // 如果是第一个内存且出错，添加一个通用错误信息
                                memoryInfo.Append("无法获取内存详情");
                                break;
                            }
                        }
                    }
                }

                string result = memoryInfo.ToString();
                // 确保结果不为null且处理可能的特殊字符
                return result.Replace("\0", string.Empty);
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取内存信息时发生异常: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 从ManagementObject获取内存类型
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>内存类型字符串</returns>
        private string GetMemoryType(ManagementObject obj)
        {
            string memoryType = "未知类型";

            // 首先尝试使用SMBIOSMemoryType
            if (obj["SMBIOSMemoryType"] != null)
            {
                try
                {
                    uint typeCode = Convert.ToUInt32(obj["SMBIOSMemoryType"]);
                    memoryType = GetMemoryTypeFromCode(typeCode);
                }
                catch (Exception ex)
                {
                    (App.Current as App)?.LogMessage($"解析SMBIOSMemoryType失败: {ex.Message}");
                }
            }
            // 如果SMBIOSMemoryType获取失败，再尝试使用MemoryType
            else if (obj["MemoryType"] != null)
            {
                try
                {
                    uint typeCode = Convert.ToUInt32(obj["MemoryType"]);
                    memoryType = GetMemoryTypeFromCode(typeCode);
                }
                catch (Exception ex)
                {
                    (App.Current as App)?.LogMessage($"解析MemoryType失败: {ex.Message}");
                }
            }

            return memoryType;
        }

        /// <summary>
        /// 根据内存类型代码获取内存类型名称
        /// </summary>
        /// <param name="typeCode">内存类型代码</param>
        /// <returns>内存类型名称</returns>
        private string GetMemoryTypeFromCode(uint typeCode)
        {
            switch (typeCode)
            {
                case 1: return "其他";
                case 2: return "DRAM";
                case 3: return "SRAM";
                case 4: return "VRAM";
                case 5: return "EDRAM";
                case 6: return "RAM";
                case 7: return "ROM";
                case 8: return "FLASH";
                case 9: return "EEPROM";
                case 10: return "FEPROM";
                case 11: return "EPROM";
                case 12: return "CDRAM";
                case 13: return "3DRAM";
                case 14: return "SDRAM";
                case 15: return "SGRAM";
                case 16: return "RDRAM";
                case 17: return "DDR SDRAM";
                case 18: return "GDDR SDRAM";
                case 19: return "GDDR2 SDRAM";
                case 20: return "DDR";
                case 21: return "DDR2";
                case 22: return "DDR2 FB-DIMM";
                case 24: return "DDR3";
                case 25: return "DDR3 FB-DIMM";
                case 26: return "DDR4";
                case 27: return "LPDDR";
                case 28: return "DDR5";
                case 29: return "LPDDR2";
                case 30: return "LPDDR3";
                case 31: return "LPDDR4";
                case 32: return "LPDDR5";
                default: return "未知类型";
            }
        }

        /// <summary>
        /// 从ManagementObject获取内存频率
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>内存频率字符串</returns>
        private string GetMemorySpeed(ManagementObject obj)
        {
            string speed = "未知频率";
            if (obj["Speed"] != null)
            {
                try
                {
                    uint memorySpeed = Convert.ToUInt32(obj["Speed"]);
                    speed = $"{memorySpeed} MHz";
                }
                catch (Exception ex)
                {
                    (App.Current as App)?.LogMessage($"解析内存频率失败: {ex.Message}");
                }
            }
            return speed;
        }

        /// <summary>
        /// 从ManagementObject获取内存容量
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>内存容量字符串</returns>
        private string GetMemoryCapacity(ManagementObject obj)
        {
            string capacityInfo = "未知容量";
            if (obj["Capacity"] != null)
            {
                try
                {
                    ulong capacity = Convert.ToUInt64(obj["Capacity"]);
                    double capacityGB = Math.Round((double)capacity / (1024 * 1024 * 1024), 2);
                    capacityInfo = $"{capacityGB} GB";
                }
                catch (Exception ex)
                {
                    (App.Current as App)?.LogMessage($"解析内存容量失败: {ex.Message}");
                }
            }
            return capacityInfo;
        }

        /// <summary>
        /// 获取显卡(GPU)信息
        /// </summary>
        /// <returns>显卡信息字符串，包含显卡名称、制造商和显存大小</returns>
        private string GetGpuInfo()
        {
            StringBuilder gpuInfo = new StringBuilder();

            try
            {
                // 首选通过Win32_VideoController获取详细显卡信息
                if (GetVideoControllerInfo(gpuInfo))
                {
                    return gpuInfo.ToString();
                }

                // 如果Win32_VideoController获取失败，尝试使用Win32_PNPEntity作为备选
                return GetPnpEntityGpuInfo(gpuInfo);
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取GPU信息时发生异常: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 通过Win32_VideoController获取显卡信息
        /// </summary>
        /// <param name="gpuInfo">StringBuilder对象，用于存储获取到的显卡信息</param>
        /// <returns>是否成功获取到显卡信息</returns>
        private bool GetVideoControllerInfo(StringBuilder gpuInfo)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterCompatibility, AdapterRAM FROM Win32_VideoController"))
                {
                    var gpuObjects = searcher.Get();
                    if (gpuObjects.Count == 0)
                    {
                        return false;
                    }

                    foreach (ManagementObject obj in gpuObjects)
                    {
                        try
                        {
                            string gpuName = obj["Name"]?.ToString() ?? "未知型号";
                            string manufacturer = obj["AdapterCompatibility"]?.ToString() ?? "未知制造商";
                            string videoRamInfo = GetVideoRamInfo(obj);

                            // 判断是否为物理显示适配器（虚拟显示适配器通常没有显存）
                            if (videoRamInfo != "未知显存")
                            {
                                // 构建显卡信息字符串
                                string gpuDetails = $"{gpuName} ({manufacturer}) - {videoRamInfo}";

                                if (gpuInfo.Length > 0)
                                {
                                    // 多个显卡之间换行显示
                                    gpuInfo.Append(Environment.NewLine);
                                }

                                gpuInfo.Append(gpuDetails);
                            }
                        }
                        catch (Exception ex)
                        {
                            (App.Current as App)?.LogMessage($"解析单个GPU信息失败: {ex.Message}");
                            // 继续处理下一个GPU，不中断整体流程
                        }
                    }
                    return gpuInfo.Length > 0;
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"通过Win32_VideoController获取GPU信息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从ManagementObject获取显存信息
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>显存信息字符串</returns>
        private string GetVideoRamInfo(ManagementObject obj)
        {
            try
            {
                if (obj["AdapterRAM"] != null)
                {
                    ulong videoRamBytes = Convert.ToUInt64(obj["AdapterRAM"]);

                    // 判断是否为物理显示适配器（虚拟显示适配器通常没有显存）
                    if (videoRamBytes > 0)
                    {
                        // 计算显存大小（GB）- 使用decimal确保精度
                        // 注意：某些系统上WMI返回的显存值可能只是实际值的一半，需要乘以2进行修正
                        decimal videoRamGB = Math.Round((decimal)videoRamBytes * 2 / (1024 * 1024 * 1024), 2);

                        // 默认使用GB为单位，仅在小于1GB时使用MB为单位
                        if (videoRamGB < 1.0m)
                        {
                            // 转换为MB单位显示
                            decimal videoRamMB = Math.Round((decimal)videoRamBytes * 2 / (1024 * 1024), 2);
                            return $"{videoRamMB} MB";
                        }
                        else
                        {
                            // 使用GB单位显示
                            return $"{videoRamGB} GB";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"解析显存大小失败: {ex.Message}");
            }
            return "未知显存";
        }

        /// <summary>
        /// 通过Win32_PNPEntity获取显卡信息作为备选方案
        /// </summary>
        /// <param name="gpuInfo">StringBuilder对象，用于存储获取到的显卡信息</param>
        /// <returns>显卡信息字符串</returns>
        private string GetPnpEntityGpuInfo(StringBuilder gpuInfo)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer FROM Win32_PNPEntity WHERE Service='nvlddmkm' OR Service='amdkmdag' OR Service='igfx'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            string gpuName = obj["Name"]?.ToString() ?? "未知型号";
                            string manufacturer = obj["Manufacturer"]?.ToString() ?? "未知制造商";

                            string gpuDetails = $"{gpuName} ({manufacturer})";

                            if (gpuInfo.Length > 0)
                            {
                                gpuInfo.Append(Environment.NewLine);
                            }
                            gpuInfo.Append(gpuDetails);
                        }
                        catch (Exception ex)
                        {
                            (App.Current as App)?.LogMessage($"解析PNP实体GPU信息失败: {ex.Message}");
                        }
                    }
                }

                // 如果获取到了信息，返回信息字符串，否则返回空字符串
                return gpuInfo.Length > 0 ? gpuInfo.ToString() : string.Empty;
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"通过Win32_PNPEntity获取GPU信息失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取物理磁盘信息
        /// </summary>
        /// <returns>磁盘信息字符串，包含每个磁盘的型号、接口类型和大小</returns>
        private string GetDiskInfo()
        {
            try
            {
                StringBuilder diskInfo = new StringBuilder();
                // 使用Win32_DiskDrive获取物理磁盘信息，包括产品型号、接口类型和大小
                using (var searcher = new ManagementObjectSearcher("SELECT Model, InterfaceType, Size FROM Win32_DiskDrive"))
                {
                    var diskObjects = searcher.Get();
                    if (diskObjects.Count == 0)
                    {
                        (App.Current as App)?.LogMessage("未找到物理磁盘信息");
                        return string.Empty;
                    }

                    foreach (ManagementObject obj in diskObjects)
                    {
                        try
                        {
                            string model = GetDiskModel(obj);
                            string interfaceType = GetDiskInterfaceType(obj);
                            string sizeInfo = GetDiskSize(obj);

                            // 格式化磁盘信息
                            string diskDetails = $"{model} ({interfaceType}  {sizeInfo})";

                            if (diskInfo.Length > 0)
                            {
                                // 多个磁盘之间换行显示
                                diskInfo.Append(Environment.NewLine);
                            }

                            diskInfo.Append(diskDetails);
                        }
                        catch (Exception ex)
                        {
                            (App.Current as App)?.LogMessage($"解析单个磁盘信息失败: {ex.Message}");
                            // 捕获单个磁盘信息获取异常，继续处理其他磁盘
                            if (diskInfo.Length == 0)
                            {
                                // 如果是第一个磁盘且出错，添加一个通用错误信息
                                diskInfo.Append("无法获取磁盘详情");
                                break;
                            }
                        }
                    }
                }

                string result = diskInfo.ToString();
                // 确保结果不为null且处理可能的特殊字符
                return result.Replace("\0", string.Empty);
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取磁盘信息时发生异常: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 从ManagementObject获取磁盘型号
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>磁盘型号字符串</returns>
        private string GetDiskModel(ManagementObject obj)
        {
            try
            {
                return obj["Model"]?.ToString()?.Trim() ?? "未知型号";
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"解析磁盘型号失败: {ex.Message}");
                return "未知型号";
            }
        }

        /// <summary>
        /// 从ManagementObject获取磁盘接口类型
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>磁盘接口类型字符串</returns>
        private string GetDiskInterfaceType(ManagementObject obj)
        {
            try
            {
                string interfaceType = obj["InterfaceType"]?.ToString() ?? "未知类型";
                // 规范化常见的接口类型名称
                switch (interfaceType.ToUpper())
                {
                    case "IDE": return "IDE";
                    case "SATA": return "SATA";
                    case "SCSI": return "SCSI";
                    case "USB": return "USB";
                    case "NVME":
                    case "PCIe":
                    case "PCI EXPRESS": return "NVMe/PCIe";
                    default: return interfaceType;
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"解析磁盘接口类型失败: {ex.Message}");
                return "未知类型";
            }
        }

        /// <summary>
        /// 从ManagementObject获取磁盘大小
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>磁盘大小字符串</returns>
        private string GetDiskSize(ManagementObject obj)
        {
            try
            {
                if (obj["Size"] != null)
                {
                    ulong size = Convert.ToUInt64(obj["Size"]);
                    double sizeGB = Math.Round((double)size / (1024 * 1024 * 1024), 2);
                    return $"{sizeGB} GB";
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"解析磁盘大小失败: {ex.Message}");
            }
            return "未知大小";
        }

        /// <summary>
        /// 获取声卡信息
        /// </summary>
        /// <returns>声卡信息字符串，如果无法获取则返回空字符串</returns>
        private string GetAudioCardInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_SoundDevice"))
                {
                    var soundDevices = searcher.Get();
                    if (soundDevices.Count == 0)
                    {
                        (App.Current as App)?.LogMessage("未找到任何声卡设备");
                        return string.Empty;
                    }

                    foreach (ManagementObject obj in soundDevices)
                    {
                        try
                        {
                            string audioCardName = GetAudioCardName(obj);
                            if (!string.IsNullOrEmpty(audioCardName))
                            {
                                return audioCardName;
                            }
                        }
                        catch (Exception ex)
                        {
                            (App.Current as App)?.LogMessage($"解析单个声卡信息失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取声卡信息时发生异常: {ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>
        /// 从ManagementObject获取声卡名称
        /// </summary>
        /// <param name="obj">ManagementObject对象</param>
        /// <returns>声卡名称字符串</returns>
        private string GetAudioCardName(ManagementObject obj)
        {
            try
            {
                return obj["Name"]?.ToString()?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"解析声卡名称失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取网络适配器信息
        /// </summary>
        /// <returns>网卡信息字符串，包含网卡名称和描述</returns>
        private string GetNetworkCardInfo()
        {
            StringBuilder networkInfo = new StringBuilder();

            try
            {
                // 首选获取物理网卡信息
                if (GetPhysicalNetworkCards(networkInfo))
                {
                    return networkInfo.ToString();
                }

                // 如果没有找到物理网卡，尝试获取所有活动网卡
                return GetActiveNetworkCards(networkInfo);
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取网卡信息时发生异常: {ex.Message}");
                return "无法获取网卡信息";
            }
        }

        /// <summary>
        /// 获取物理网卡信息
        /// </summary>
        /// <param name="networkInfo">StringBuilder对象，用于存储获取到的网卡信息</param>
        /// <returns>是否成功获取到物理网卡信息</returns>
        private bool GetPhysicalNetworkCards(StringBuilder networkInfo)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, Description FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True AND NetConnectionStatus = 2"))
                {
                    var networkObjects = searcher.Get();
                    if (networkObjects.Count == 0)
                    {
                        return false;
                    }

                    foreach (ManagementObject obj in networkObjects)
                    {
                        try
                        {
                            string name = obj["Name"]?.ToString() ?? "未知网卡";
                            string description = obj["Description"]?.ToString() ?? "未知描述";

                            // 网卡显示名称格式：型号 (描述)
                            string networkDetails = $"{name} ({description})";

                            if (networkInfo.Length > 0)
                            {
                                // 多个网卡之间换行显示
                                networkInfo.Append(Environment.NewLine);
                            }

                            networkInfo.Append(networkDetails);
                        }
                        catch (Exception ex)
                        {
                            (App.Current as App)?.LogMessage($"解析单个物理网卡信息失败: {ex.Message}");
                            // 继续处理下一个网卡
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取物理网卡信息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有活动网卡信息
        /// </summary>
        /// <param name="networkInfo">StringBuilder对象，用于存储获取到的网卡信息</param>
        /// <returns>网卡信息字符串</returns>
        private string GetActiveNetworkCards(StringBuilder networkInfo)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, Description FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            string name = obj["Name"]?.ToString() ?? "未知网卡";
                            string description = obj["Description"]?.ToString() ?? "未知描述";

                            string networkDetails = $"{name} ({description})";

                            if (networkInfo.Length > 0)
                            {
                                networkInfo.Append(Environment.NewLine);
                            }

                            networkInfo.Append(networkDetails);
                        }
                        catch (Exception ex)
                        {
                            (App.Current as App)?.LogMessage($"解析单个活动网卡信息失败: {ex.Message}");
                        }
                    }
                }

                // 如果获取到了信息，返回信息字符串，否则返回错误提示
                return networkInfo.Length > 0 ? networkInfo.ToString() : "无法获取网卡信息";
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取活动网卡信息失败: {ex.Message}");
                return "无法获取网卡信息";
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}