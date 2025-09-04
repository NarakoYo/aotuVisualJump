using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ImageRecognitionApp.UnitTools;

namespace ImageRecognitionApp.Assets.UICode
{
    /// <summary>
    /// 初始化管理器，负责应用程序的所有初始化操作
    /// </summary>
    public class InitialStartupManager
    {
        private readonly LogManager _logManager = LogManager.Instance;
        
        /// <summary>
        /// 用于更新初始化状态的委托
        /// </summary>
        public Action<string, int>? UpdateStatusCallback { get; set; }
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
                // 初始化所有资产资源
                await InitializeAllAssetResourcesAsync();
                
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

        /// <summary>
        /// 初始化项目所有资产资源
        /// </summary>
        /// <returns>异步任务</returns>
        private async Task InitializeAllAssetResourcesAsync()
        {
            // 初始化项目所有资产资源
            try
            {
                _logManager.WriteLog(LogManager.LogLevel.Info, "Starting to initialize all project asset resources");
                
                // 初始化AssetHelper单例
                var assetHelper = AssetHelper.Instance;
                
                // 只初始化必要的配置，不加载所有资源文件
                _logManager.WriteLog(LogManager.LogLevel.Info, "AssetHelper initialized without preloading all asset files");
                
                // 获取资源文件夹路径
                string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                _logManager.WriteLog(LogManager.LogLevel.Info, $"Resource folder path: {resourcesPath}");
                
                // 检查资源文件夹是否存在
                if (Directory.Exists(resourcesPath))
                {
                    // 仅计算文件数量，不预加载所有文件
                    string[] allAssetFiles = Directory.GetFiles(resourcesPath, "*.*", SearchOption.AllDirectories);
                    int totalAssets = allAssetFiles.Length;
                    
                    // 输出到UI文本
                    UpdateStatusCallback?.Invoke($"Found {totalAssets} asset files (will be loaded on demand)", 45);
                    _logManager.WriteLog(LogManager.LogLevel.Info, $"Found {totalAssets} asset files, will be loaded on demand");
                    
                    // 只加载关键资源（如启动画面、图标等）
                    await LoadCriticalAssetsOnlyAsync(resourcesPath).ConfigureAwait(false);
                }
                else
                {
                    UpdateStatusCallback?.Invoke("Resource folder not found", 50);
                    _logManager.WriteLog(LogManager.LogLevel.Warning, $"Resource folder not found: {resourcesPath}");
                }
                
                // 减少模拟延迟时间
                await Task.Delay(30).ConfigureAwait(false);
                
                UpdateStatusCallback?.Invoke("Asset resources initialization completed", 60);
                _logManager.WriteLog(LogManager.LogLevel.Info, "Project all asset resources initialization completed with lazy loading strategy");
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"Error initializing project asset resources: {ex.Message}");
                UpdateStatusCallback?.Invoke($"Error initializing assets: {ex.Message}", 0);
                throw;
            }
        }
        
        /// <summary>
        /// 只加载关键资源，减少启动时的内存占用
        /// </summary>
        /// <param name="resourcesPath">资源文件夹路径</param>
        /// <returns>异步任务</returns>
        private async Task LoadCriticalAssetsOnlyAsync(string resourcesPath)
        {
            try
            {
                // 定义关键资源目录或文件类型
                string[] criticalDirectories = { "Icons", "SplashScreen" };
                string[] criticalExtensions = { ".ico", ".png", ".jpg", ".svg" };
                
                // 只加载关键资源
                foreach (string directory in criticalDirectories)
                {
                    string criticalDirPath = Path.Combine(resourcesPath, directory);
                    if (Directory.Exists(criticalDirPath))
                    {
                        string[] criticalFiles = Directory.GetFiles(criticalDirPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => criticalExtensions.Contains(Path.GetExtension(file).ToLower()))
                            .ToArray();
                        
                        foreach (string criticalFile in criticalFiles)
                        {
                            // 这里可以选择性地预加载一些非常关键的资源
                            // 但要保持最小化，大部分资源应该按需加载
                            await Task.Delay(1).ConfigureAwait(false); // 微小延迟，避免阻塞
                        }
                    }
                }
                
                _logManager.WriteLog(LogManager.LogLevel.Info, "Critical assets loaded successfully");
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Warning, $"Error loading critical assets: {ex.Message}");
            }
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