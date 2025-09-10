using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageRecognitionApp.Assets.UICode;
using ImageRecognitionApp.Converters;
using ImageRecognitionApp.Utils;
using ImageRecognitionApp.WinFun;
using TaskbarProgressState = ImageRecognitionApp.WinFun.TaskbarManager.TaskbarProgressState;

namespace ImageRecognitionApp.Assets.UI
{


    /// <summary>
    /// 应用程序的初始启动窗口
    /// </summary>
    /// <remarks>
    /// 负责显示应用程序启动过程中的初始化状态、进度和相关信息
    /// </remarks>
    public partial class InitialStartupWindow : Window
    {
        private InitialStartupManager _initializationManager;
        private readonly InitialStartupAnimation _animationManager;
        private const int TotalInitializationSteps = 5;
        private TaskbarManager _taskbarManager;
        
        // Windows API常量
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        
        // Windows API导入
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        /// <summary>
        /// 构造函数，初始化窗口组件和资源
        /// </summary>
        public InitialStartupWindow()
        {
            InitializeComponent();
            _initializationManager = new InitialStartupManager();
            _animationManager = new InitialStartupAnimation(this);
            // 初始化任务栏管理器，设置不显示托盘图标
            _taskbarManager = new TaskbarManager(this, false);
            // 设置状态更新回调
            _initializationManager.UpdateStatusCallback = UpdateStatus;
            this.Loaded += InitialStartupWindow_Loaded;
            
            // 初始化UI资源
            InitializeUIResources();
        }

        /// <summary>
        /// 初始化UI资源
        /// </summary>
        private void InitializeUIResources()
        {
            InitializeLocalization();
            InitializeImageResources();
        }

        /// <summary>
        /// 记录日志信息
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogMessage(string message)
        {
            if (App.Current is App app) {
                app.LogMessage(message);
            } else {
                System.Diagnostics.Debug.WriteLine(message);
            }
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="ex">异常对象</param>
        private void LogError(string message, Exception ex)
        {
            LogMessage($"{message}: {ex.Message}");
        }

        /// <summary>
        /// 初始化本地化文本
        /// </summary>
        private void InitializeLocalization()
        {
            try
            {
                // 获取JsonLocalizationHelper实例
                var localizationHelper = JsonLocalizationHelper.Instance;
                if (localizationHelper != null)
                {
                    // 设置应用标题
                    if (AppTitle != null)
                    {
                        AppTitle.Text = localizationHelper.GetString(10001);
                    }

                    // 设置窗口标题
                    try
                    {
                        string windowTitle = localizationHelper.GetString(20000);
                        if (!string.IsNullOrEmpty(windowTitle))
                        {
                            this.Title = windowTitle;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError("获取窗口本地化标题失败", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("初始化本地化资源失败", ex);
            }

            // 获取并设置当前版本号
            try
            {
                // 获取当前程序集
                Assembly assembly = Assembly.GetExecutingAssembly();
                // 获取版本信息
                Version version = assembly.GetName().Version;
                // 设置版本号文本
                if (VersionInfo != null)
                {
                    VersionInfo.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch (Exception ex)
            {
                LogError("获取版本号失败", ex);
            }
        }

        /// <summary>
        /// 初始化图片资源
        /// </summary>
        private void InitializeImageResources()
        {
            try
            {
                var assetHelper = AssetHelper.Instance;
                if (assetHelper == null)
                {
                    (App.Current as App)?.LogMessage("AssetHelper获取失败，无法初始化图像资源");
                    return;
                }

                // 设置背景图片
                SetBackgroundImage(assetHelper);
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"初始化图像资源时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置背景图片
        /// </summary>
        /// <param name="assetHelper">资源助手实例</param>
        private void SetBackgroundImage(AssetHelper assetHelper)
        {
            try
            {
                // 背景图片的sign_id为10015
                var backgroundImage = assetHelper.GetImageAsset(10015);
                if (backgroundImage != null)
                {
                    BackgroundImage.Source = backgroundImage;
                    
                    // 设置背景图片来源文本
                    if (BackgroundImageSource != null)
                    {
                        BackgroundImageSource.Text = "Created by ComfyUI-XL.Wai";
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("设置背景图片失败", ex);
            }
        }

        /// <summary>
        /// 窗口加载完成事件处理程序
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private async void InitialStartupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 在窗口加载后开始初始化过程，先播放淡入动画
            await _animationManager.PlayWindowFadeInAsync();
            await PerformInitializationAsync();
        }
        


        /// <summary>
        /// 执行应用程序初始化流程
        /// </summary>
        /// <returns>异步任务</returns>
        private async Task PerformInitializationAsync()
        {
            try
            {
                InitializeInitializationManager();
                
                // 依次执行各个初始化步骤
                if (!await CheckSystemEnvironmentAsync()) return;
                if (!await LoadApplicationConfigurationAsync()) return;
                if (!await InitializeApplicationResourcesAsync()) return;
                if (!await PrepareMainWindowDataAsync()) return;
                
                // 初始化完成，跳转到主窗口
                await FinalizeInitializationAsync();
            }
            catch (Exception ex)
            {
                // 捕获所有异常，显示友好的错误信息
                await UpdateStatusAsync("初始化过程中发生错误：" + ex.Message, 0);
                LogError("初始化过程中发生错误", ex);
            }
        }
        
        /// <summary>
        /// 初始化初始化管理器
        /// </summary>
        private void InitializeInitializationManager()
        {
            if (_initializationManager == null)
            {
                LogMessage("初始化管理器未初始化，正在创建...");
                _initializationManager = new InitialStartupManager();
            }
        }
        
        /// <summary>
        /// 检查系统环境
        /// </summary>
        /// <returns>是否检查通过</returns>
        private async Task<bool> CheckSystemEnvironmentAsync()
        {
            await UpdateStatusAsync("Checking system environment...", 0);
            try
            {
                await _initializationManager.CheckSystemEnvironmentAsync().ConfigureAwait(false);
                await Task.Delay(300).ConfigureAwait(false); // Simulate time-consuming operation
                return true;
            }
            catch (Exception ex)
            {
                await UpdateStatusAsync($"系统环境检查失败：{ex.Message}", 0);
                LogError("系统环境检查失败", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 加载应用配置
        /// </summary>
        /// <returns>是否加载成功</returns>
        private async Task<bool> LoadApplicationConfigurationAsync()
        {
            await UpdateStatusAsync("Loading configuration files...", 20);
            try
            {
                await _initializationManager.LoadConfigurationAsync().ConfigureAwait(false);
                await Task.Delay(300).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await UpdateStatusAsync($"配置文件加载失败：{ex.Message}", 0);
                LogError("配置文件加载失败", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 初始化应用资源
        /// </summary>
        /// <returns>是否初始化成功</returns>
        private async Task<bool> InitializeApplicationResourcesAsync()
        {
            await UpdateStatusAsync("Initializing resources...", 40);
            try
            {
                await _initializationManager.InitializeResourcesAsync().ConfigureAwait(false);
                await Task.Delay(300).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await UpdateStatusAsync($"资源初始化失败：{ex.Message}", 0);
                LogError("资源初始化失败", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 准备主窗口数据
        /// </summary>
        /// <returns>是否准备成功</returns>
        private async Task<bool> PrepareMainWindowDataAsync()
        {
            await UpdateStatusAsync("Preparing main window data...", 60);
            try
            {
                await _initializationManager.PrepareMainWindowDataAsync().ConfigureAwait(false);
                await Task.Delay(100).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                await UpdateStatusAsync($"主窗口数据准备失败：{ex.Message}", 0);
                LogError("主窗口数据准备失败", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 完成初始化并跳转到主窗口
        /// </summary>
        /// <returns>异步任务</returns>
        private async Task FinalizeInitializationAsync()
        {
            await UpdateStatusAsync("Initialization completed, starting application...", 99);
            await Task.Delay(2000).ConfigureAwait(false); // Simulate time-consuming operation
            await UpdateStatusAsync("Initialization completed, starting application...", 100);
            
            // 跳转到主窗口
            SwitchToMainWindow();
        }

        /// <summary>
        /// 更新初始化状态和进度（同步方法，用于回调）
        /// </summary>
        /// <param name="statusText">状态文本</param>
        /// <param name="progressValue">进度值(0-100)</param>
        private void UpdateStatus(string statusText, int progressValue)
        {
            // 立即执行UI更新
            this.Dispatcher.Invoke(async () =>
            {
                UpdateStatusTextAndProgress(statusText, progressValue);
                
                // 使用动画平滑更新进度条值
                if (InitializationProgress != null && _animationManager != null)
                {
                    await _animationManager.AnimateProgressBarAsync(InitializationProgress, progressValue);
                }
                
                // 更新任务栏进度条
                UpdateTaskbarProgress(statusText, progressValue);
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        
        /// <summary>
        /// 更新初始化状态和进度（异步方法，用于主线程调用）
        /// </summary>
        /// <param name="statusText">状态文本</param>
        /// <param name="progressValue">进度值(0-100)</param>
        /// <returns>异步任务</returns>
        private async Task UpdateStatusAsync(string statusText, int progressValue)
        {
            // 在UI线程上更新状态文本和百分比显示
            this.Dispatcher.Invoke(() =>
            {
                UpdateStatusTextAndProgress(statusText, progressValue);
            }, System.Windows.Threading.DispatcherPriority.Background);
            
            // 使用动画平滑更新进度条值
            if (InitializationProgress != null && _animationManager != null)
            {
                await _animationManager.AnimateProgressBarAsync(InitializationProgress, progressValue).ConfigureAwait(false);
            }
            
            // 更新任务栏进度条
            UpdateTaskbarProgress(statusText, progressValue);
        }
        
        /// <summary>
        /// 更新状态文本和百分比显示
        /// </summary>
        /// <param name="statusText">状态文本</param>
        /// <param name="progressValue">进度值(0-100)</param>
        private void UpdateStatusTextAndProgress(string statusText, int progressValue)
        {
            if (StatusText != null)
            {
                StatusText.Text = statusText;
            }
            
            if (ProgressPercentage != null)
            {
                ProgressPercentage.Text = $"{progressValue}%";
            }
        }
        
        /// <summary>
        /// 更新任务栏进度条
        /// </summary>
        /// <param name="statusText">状态文本</param>
        /// <param name="progressValue">进度值(0-100)</param>
        private void UpdateTaskbarProgress(string statusText, int progressValue)
        {
            if (_taskbarManager != null)
            {
                try
                {
                    // 根据不同的进度值设置不同的任务栏进度条状态
                    if (progressValue == 0 && statusText.Contains("失败"))
                    {
                        // 加载错误中断卡住：红色进度条
                        _taskbarManager.SetProgressState(TaskbarProgressState.Error);
                    }
                    else if (progressValue > 0 && progressValue < 100)
                    {
                        // 根据进度值选择不同的进度模式
                        if (progressValue % 20 == 0)
                        {
                            // 偶数进度点使用正常进度模式
                            _taskbarManager.SetProgressState(TaskbarProgressState.Normal);
                        }
                        else if (progressValue % 15 == 0)
                        {
                            // 特定条件下使用暂停模式
                            _taskbarManager.SetProgressState(TaskbarProgressState.Paused);
                        }
                        else if (progressValue % 25 == 0)
                        {
                            // 特定条件下使用走马灯模式
                            _taskbarManager.SetProgressState(TaskbarProgressState.Indeterminate);
                        }
                        else
                        {
                            // 其他情况使用正常进度模式
                            _taskbarManager.SetProgressState(TaskbarProgressState.Normal);
                        }
                        
                        // 更新进度值
                        _taskbarManager.SetProgressValue((ulong)progressValue, 100);
                    }
                    else if (progressValue == 100)
                    {
                        // 初始化完成，设置正常状态并显示100%进度
                        _taskbarManager.SetProgressState(TaskbarProgressState.Normal);
                        _taskbarManager.SetProgressValue(100, 100);
                    }
                }
                catch (Exception ex)
                {
                    LogError("更新任务栏进度条失败", ex);
                }
            }
        }

        /// <summary>
        /// 切换到主窗口
        /// </summary>
        private void SwitchToMainWindow()
        {
            // 创建一个任务来执行异步操作
            Task.Run(async () =>
            {
                try
                {
                    // 播放淡出动画
                    await _animationManager.PlayWindowFadeOutAsync();
                    
                    // 在UI线程上隐藏窗口和显示主窗口
                    this.Dispatcher.Invoke(() =>
                    {
                        this.Hide();
                        
                        // 创建并显示主窗口
                        var mainWindow = new MainWindow();
                        // 显式设置Application.Current.MainWindow以确保正确的引用
                        Application.Current.MainWindow = mainWindow;
                        mainWindow.Show();
                        
                        // 关闭当前窗口
                        this.Close();
                    });
                }
                catch (Exception ex)
                {
                    LogError("切换到主窗口失败", ex);
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"切换到主窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                    });
                }
            });
        }

        /// <summary>
        /// 鼠标左键按下事件处理程序
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标事件参数</param>
        private void Window_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // 暂时禁用窗口拖动功能
            // 如需启用，取消以下注释
            /*
            try
            {
                // 允许通过鼠标拖动窗口
                this.DragMove();
            }
            catch (Exception ex)
            {
                LogError("窗口拖动失败", ex);
            }
            */
        }
    }
}