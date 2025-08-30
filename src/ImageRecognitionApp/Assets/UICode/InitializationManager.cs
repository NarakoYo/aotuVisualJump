using System;
using System.IO;
using System.Management;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ImageRecognitionApp.Assets.UICode
{
    /// <summary>
    /// 初始化管理器，负责应用程序的所有初始化操作
    /// </summary>
    public class InitializationManager
    {
        /// <summary>
        /// 检查系统环境
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task CheckSystemEnvironmentAsync()
        {
            try
            {
                // 检查.NET框架版本
                CheckDotNetFramework();
                
                // 检查必要的系统资源
                CheckSystemResources();
                
                // 检查目录权限
                await CheckDirectoryPermissionsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("系统环境检查失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task LoadConfigurationAsync()
        {
            try
            {
                // 加载应用程序配置
                await LoadAppConfigAsync();
                
                // 加载用户设置
                await LoadUserSettingsAsync();
                
                // 验证配置有效性
                ValidateConfiguration();
            }
            catch (Exception ex)
            {
                throw new Exception("配置文件加载失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 初始化资源
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task InitializeResourcesAsync()
        {
            try
            {
                // 初始化图像资源
                await InitializeImageResourcesAsync();
                
                // 初始化语言资源
                InitializeLanguageResources();
                
                // 初始化主题资源
                InitializeThemeResources();
            }
            catch (Exception ex)
            {
                throw new Exception("资源初始化失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 准备主窗口数据
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task PrepareMainWindowDataAsync()
        {
            try
            {
                // 加载最近使用的文件列表
                await LoadRecentFilesAsync();
                
                // 初始化工具栏状态
                InitializeToolBarState();
                
                // 预加载常用数据
                await PreloadCommonDataAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("主窗口数据准备失败: " + ex.Message);
            }
        }

        #region 私有辅助方法

        private void CheckDotNetFramework()
        {
            // 检查.NET框架版本
            Version frameworkVersion = Environment.Version;
            if (frameworkVersion.Major < 9)
            {
                throw new Exception($"需要.NET 9.0或更高版本，当前版本: {frameworkVersion}");
            }
        }

        private void CheckSystemResources()
        {
            try
            {
                // 使用System.Management获取系统内存信息
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (var queryObj in searcher.Get())
                    {
                        // 转换为MB
                        ulong totalMemory = Convert.ToUInt64(queryObj["TotalVisibleMemorySize"]) / 1024;
                        ulong freeMemory = Convert.ToUInt64(queryObj["FreePhysicalMemory"]) / 1024;
                        ulong memoryThreshold = totalMemory / 10; // 使用总内存的10%作为阈值
                        
                        // 当可用内存低于总内存的10%或低于512MB时显示警告
                        if (freeMemory < memoryThreshold || freeMemory < 512)
                        {
                            MessageBox.Show($"系统可用内存较低({freeMemory}MB/{totalMemory}MB)，可能影响应用性能", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果获取系统内存信息失败，记录错误但不阻止应用启动
                (App.Current as App)?.LogMessage($"获取系统内存信息失败: {ex.Message}");
            }
        }

        private async Task CheckDirectoryPermissionsAsync()
        {
            // 检查应用程序目录权限
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string testFilePath = Path.Combine(appDirectory, "test_write.txt");
            
            try
            {
                using (FileStream fs = File.Create(testFilePath))
                {
                    await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("test"));
                }
                File.Delete(testFilePath);
            }
            catch
            {
                throw new Exception("应用程序目录无写入权限");
            }
        }

        private async Task LoadAppConfigAsync()
        {
            // 模拟加载应用程序配置
            await Task.Delay(100);
            // 实际项目中，这里应该加载应用程序的配置文件
        }

        private async Task LoadUserSettingsAsync()
        {
            // 模拟加载用户设置
            await Task.Delay(100);
            // 实际项目中，这里应该加载用户的个性化设置
        }

        private void ValidateConfiguration()
        {
            // 验证配置的有效性
            // 实际项目中，这里应该验证配置的完整性和正确性
        }

        private async Task InitializeImageResourcesAsync()
        {
            // 模拟初始化图像资源
            await Task.Delay(100);
            // 实际项目中，这里应该预加载和初始化应用程序需要的图像资源
        }

        private void InitializeLanguageResources()
        {
            // 初始化语言资源
            // 实际项目中，这里应该根据用户设置加载对应的语言资源
        }

        private void InitializeThemeResources()
        {
            // 初始化主题资源
            // 实际项目中，这里应该根据用户设置加载对应的主题资源
        }

        private async Task LoadRecentFilesAsync()
        {
            // 模拟加载最近使用的文件列表
            await Task.Delay(100);
            // 实际项目中，这里应该加载用户最近使用的文件列表
        }

        private void InitializeToolBarState()
        {
            // 初始化工具栏状态
            // 实际项目中，这里应该根据用户设置初始化工具栏的状态
        }

        private async Task PreloadCommonDataAsync()
        {
            // 模拟预加载常用数据
            await Task.Delay(100);
            // 实际项目中，这里应该预加载应用程序常用的数据
        }

        #endregion
    }
}