using System;
using System.Text;
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

                // 获取设备型号
                try
                {
                    string model = GetDeviceModel();
                    ModelValue.Text = string.IsNullOrEmpty(model) ? GetLocalizedText(20016, "未正常获取") : model;
                }
                catch (Exception)
                {
                    ModelValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取中央处理器信息
                try
                {
                    string cpu = GetCpuInfo();
                    CpuValue.Text = string.IsNullOrEmpty(cpu) ? GetLocalizedText(20016, "未正常获取") : cpu;
                }
                catch (Exception)
                {
                    CpuValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取主板信息
                try
                {
                    string motherboard = GetMotherboardInfo();
                    MotherboardValue.Text = string.IsNullOrEmpty(motherboard) ? GetLocalizedText(20016, "未正常获取") : motherboard;
                }
                catch (Exception)
                {
                    MotherboardValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取内存信息
                try
                {
                    string memory = GetMemoryInfo();
                    MemoryValue.Text = string.IsNullOrEmpty(memory) ? GetLocalizedText(20016, "未正常获取") : memory;
                }
                catch (Exception)
                {
                    MemoryValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取图形处理器信息
                try
                {
                    string gpu = GetGpuInfo();
                    GpuValue.Text = string.IsNullOrEmpty(gpu) ? GetLocalizedText(20016, "未正常获取") : gpu;
                }
                catch (Exception)
                {
                    GpuValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取磁盘信息
                try
                {
                    string disk = GetDiskInfo();
                    DiskValue.Text = string.IsNullOrEmpty(disk) ? GetLocalizedText(20016, "未正常获取") : disk;
                }
                catch (Exception)
                {
                    DiskValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取声卡信息
                try
                {
                    string audioCard = GetAudioCardInfo();
                    AudioCardValue.Text = string.IsNullOrEmpty(audioCard) ? GetLocalizedText(20016, "未正常获取") : audioCard;
                }
                catch (Exception)
                {
                    AudioCardValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取网卡信息
                try
                {
                    string networkCard = GetNetworkCardInfo();
                    NetworkCardValue.Text = string.IsNullOrEmpty(networkCard) ? GetLocalizedText(20016, "未正常获取") : networkCard;
                }
                catch (Exception)
                {
                    NetworkCardValue.Text = GetLocalizedText(20016, "未正常获取");
                }

                // 获取显示器信息
                try
                {
                    string monitor = GetMonitorInfo();
                    MonitorValue.Text = string.IsNullOrEmpty(monitor) ? GetLocalizedText(20016, "未正常获取") : monitor;
                }
                catch (Exception)
                {
                    MonitorValue.Text = GetLocalizedText(20016, "未正常获取");
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
        /// 获取内存信息
        /// </summary>
        /// <returns>内存信息</returns>
        private string GetMemoryInfo()
        {
            try
            {
                StringBuilder memoryInfo = new StringBuilder();
                // 使用Win32_PhysicalMemory获取内存详细信息，添加MemoryType和SMBIOSMemoryType以获取更准确的SDRAM技术版本
                using (var searcher = new ManagementObjectSearcher("SELECT PartNumber, Speed, Capacity, MemoryType, SMBIOSMemoryType FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            // 获取产品型号
                            string model = obj["PartNumber"]?.ToString() ?? "未知型号";
                            
                            // 获取SDRAM技术版本
                            string memoryType = "未知类型";
                            if (obj["MemoryType"] != null)
                            {
                                try
                                {
                                    uint typeCode = Convert.ToUInt32(obj["MemoryType"]);
                                    // 扩展MemoryType编码映射，处理更多可能的值
                                    switch (typeCode)
                                    {
                                        case 0: 
                                            memoryType = "未知";
                                            // 尝试从SMBIOSMemoryType获取更准确的类型
                                            if (obj["SMBIOSMemoryType"] != null)
                                            {
                                                try
                                                {
                                                    uint smbiosType = Convert.ToUInt32(obj["SMBIOSMemoryType"]);
                                                    switch (smbiosType)
                                                    {
                                                        case 20: memoryType = "DDR"; break;
                                                        case 21: memoryType = "DDR2"; break;
                                                        case 24: memoryType = "DDR3"; break;
                                                        case 26: memoryType = "DDR4"; break;
                                                        case 28: memoryType = "DDR5"; break;
                                                    }
                                                }
                                                catch { }
                                            }
                                            break;
                                        case 20: memoryType = "DDR"; break;
                                        case 21: memoryType = "DDR2"; break;
                                        case 24: memoryType = "DDR3"; break;
                                        case 26: memoryType = "DDR4"; break;
                                        case 28: memoryType = "DDR5"; break;
                                        // 添加更多可能的类型编码
                                        case 1: memoryType = "其他"; break;
                                        case 2: memoryType = "DRAM"; break;
                                        case 3: memoryType = "SRAM"; break;
                                        case 4: memoryType = "VRAM"; break;
                                        case 5: memoryType = "EDRAM"; break;
                                        case 6: memoryType = "RAM"; break;
                                        case 7: memoryType = "ROM"; break;
                                        case 8: memoryType = "FLASH"; break;
                                        case 9: memoryType = "EEPROM"; break;
                                        case 10: memoryType = "FEPROM"; break;
                                        case 11: memoryType = "EPROM"; break;
                                        case 12: memoryType = "CDRAM"; break;
                                        case 13: memoryType = "3DRAM"; break;
                                        case 14: memoryType = "SDRAM"; break;
                                        case 15: memoryType = "SGRAM"; break;
                                        case 16: memoryType = "RDRAM"; break;
                                        case 17: memoryType = "DDR SDRAM"; break;
                                        case 18: memoryType = "GDDR SDRAM"; break;
                                        case 19: memoryType = "GDDR2 SDRAM"; break;
                                        case 22: memoryType = "DDR2 FB-DIMM"; break;
                                        case 25: memoryType = "DDR3 FB-DIMM"; break;
                                        case 27: memoryType = "LPDDR"; break;
                                        case 29: memoryType = "LPDDR2"; break;
                                        case 30: memoryType = "LPDDR3"; break;
                                        case 31: memoryType = "LPDDR4"; break;
                                        case 32: memoryType = "LPDDR5"; break;
                                        default: memoryType = $"DDR{typeCode}"; break;
                                    }
                                }
                                catch { }
                            }
                            
                            // 获取内存频率
                            string speed = "未知频率";
                            if (obj["Speed"] != null)
                            {
                                try
                                {
                                    uint memorySpeed = Convert.ToUInt32(obj["Speed"]);
                                    speed = $"{memorySpeed} MHz";
                                }
                                catch { }
                            }
                            
                            // 获取容量
                            string capacityInfo = "未知容量";
                            if (obj["Capacity"] != null)
                            {
                                try
                                {
                                    ulong capacity = Convert.ToUInt64(obj["Capacity"]);
                                    double capacityGB = Math.Round((double)capacity / (1024 * 1024 * 1024), 2);
                                    capacityInfo = $"{capacityGB} GB";
                                }
                                catch { }
                            }
                            
                            // 格式化内存信息，包含SDRAM技术版本
                            string memoryDetails = $"{model} ({memoryType}  {speed}  {capacityInfo})";
                            
                            if (memoryInfo.Length > 0)
                            {
                                // 多个内存条之间换行显示
                                memoryInfo.Append(Environment.NewLine);
                            }
                            
                            memoryInfo.Append(memoryDetails);
                        }
                        catch (Exception)
                        {
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
                // 确保结果不为空
                return !string.IsNullOrEmpty(result) ? result : string.Empty;
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 获取图形处理器信息
        /// </summary>
        /// <returns>GPU信息</returns>
        private string GetGpuInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string gpuName = obj["Name"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(gpuName))
                        {
                            return gpuName;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 获取磁盘信息
        /// </summary>
        /// <returns>磁盘信息</returns>
        private string GetDiskInfo()
        {
            try
            {
                StringBuilder diskInfo = new StringBuilder();
                // 使用Win32_DiskDrive获取物理磁盘信息，包括产品型号、接口类型和大小
                using (var searcher = new ManagementObjectSearcher("SELECT Model, InterfaceType, Size FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            // 获取产品型号
                            string model = obj["Model"]?.ToString() ?? "未知型号";
                            // 获取接口类型
                            string interfaceType = obj["InterfaceType"]?.ToString() ?? "未知类型";
                            // 获取大小
                            string sizeInfo = "未知大小";
                            
                            if (obj["Size"] != null)
                            {
                                try
                                {
                                    ulong size = Convert.ToUInt64(obj["Size"]);
                                    double sizeGB = Math.Round((double)size / (1024 * 1024 * 1024), 2);
                                    sizeInfo = $"{sizeGB} GB";
                                }
                                catch { }
                            }
                            
                            // 格式化磁盘信息
                            string diskDetails = $"{model} ({interfaceType}  {sizeInfo})";
                            
                            if (diskInfo.Length > 0)
                            {
                                // 多个磁盘之间换行显示
                                diskInfo.Append(Environment.NewLine);
                            }
                            
                            diskInfo.Append(diskDetails);
                        }
                        catch (Exception)
                        {
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
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取声卡信息
        /// </summary>
        /// <returns>声卡信息</returns>
        private string GetAudioCardInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_SoundDevice"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string audioCardName = obj["Name"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(audioCardName))
                        {
                            return audioCardName;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 获取网卡信息
        /// </summary>
        /// <returns>网卡信息</returns>
        private string GetNetworkCardInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_NetworkAdapter WHERE NetConnectionStatus=2"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string networkCardName = obj["Name"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(networkCardName))
                        {
                            return networkCardName;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 获取显示器信息
        /// </summary>
        /// <returns>显示器信息</returns>
        private string GetMonitorInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_DesktopMonitor"))
                {
                    StringBuilder monitorInfo = new StringBuilder();
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string monitorName = obj["Caption"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(monitorName))
                        {
                            if (monitorInfo.Length > 0)
                            {
                                monitorInfo.Append(Environment.NewLine);
                            }
                            monitorInfo.Append(monitorName);
                        }
                    }
                    return monitorInfo.ToString();
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