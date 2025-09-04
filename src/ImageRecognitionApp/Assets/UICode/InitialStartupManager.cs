using System;
using System.IO;
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
        public Action<string, int> UpdateStatusCallback { get; set; }
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
                
                // 获取资源文件夹路径
                string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                _logManager.WriteLog(LogManager.LogLevel.Info, $"Resource folder path: {resourcesPath}");
                
                // 检查资源文件夹是否存在
                if (Directory.Exists(resourcesPath))
                {
                    // 遍历资源文件夹中的所有文件
                    string[] allAssetFiles = Directory.GetFiles(resourcesPath, "*.*", SearchOption.AllDirectories);
                    int totalAssets = allAssetFiles.Length;
                    int processedAssets = 0;
                    
                    // 输出到UI文本
                    UpdateStatusCallback?.Invoke($"Found {totalAssets} asset files", 45);
                    _logManager.WriteLog(LogManager.LogLevel.Info, $"Found {totalAssets} asset files");
                    
                    // 遍历并处理每个资产文件
                    foreach (string assetFilePath in allAssetFiles)
                    {
                        try
                        {
                            // 获取相对路径（从Resources文件夹开始）
                            string relativePath = assetFilePath.Substring(resourcesPath.Length + 1);
                            // 获取文件名
                            string fileName = Path.GetFileName(assetFilePath);
                            
                            // 更新状态到初始化文本，减少每个资产的进度增量，让进度条动画更平滑
                            UpdateStatusCallback?.Invoke($"Loading: {relativePath}", 45 + (processedAssets * 10 / totalAssets));
                            // _logManager.WriteLog(LogManager.LogLevel.Info, $"Loading: {relativePath}");
                            
                            // 模拟资产加载延迟
                            await Task.Delay(10).ConfigureAwait(false);
                            
                            processedAssets++;
                        }
                        catch (Exception ex)
                        {
                            _logManager.WriteLog(LogManager.LogLevel.Warning, $"Error loading asset {assetFilePath}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    UpdateStatusCallback?.Invoke("Resource folder not found", 50);
                    _logManager.WriteLog(LogManager.LogLevel.Warning, $"Resource folder not found: {resourcesPath}");
                }
                
                // 模拟异步处理时间
                await Task.Delay(100).ConfigureAwait(false);
                
                UpdateStatusCallback?.Invoke("Asset resources initialization completed", 60);
                _logManager.WriteLog(LogManager.LogLevel.Info, "Project all asset resources initialization completed");
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"Error initializing project asset resources: {ex.Message}");
                UpdateStatusCallback?.Invoke($"Error initializing assets: {ex.Message}", 0);
                throw;
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