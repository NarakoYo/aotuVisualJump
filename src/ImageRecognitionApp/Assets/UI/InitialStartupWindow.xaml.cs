using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ImageRecognitionApp.Assets.UICode;
using ImageRecognitionApp.UnitTools;
using ImageRecognitionApp.WinFun;
using TaskbarProgressState = ImageRecognitionApp.WinFun.TaskbarManager.TaskbarProgressState;

namespace ImageRecognitionApp.Assets.UI
{
    /// <summary>
    /// 高度转换器：根据参数将窗口高度转换为指定比例的高度，可以选择性地添加偏移量
    /// </summary>
    /// <remarks>
    /// 参数格式："ratio[+offset]"，例如："0.8"表示高度的80%，"0.8+4"表示高度的80%再加上4像素
    /// </remarks>
    public class HeightConverter : IValueConverter
    {
        /// <summary>
        /// 将源值转换为目标值
        /// </summary>
        /// <param name="value">要转换的源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的目标值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                // 默认返回窗口高度的4/5
                double ratio = 0.8;
                double offset = 0;
                
                // 如果提供了参数，尝试解析比例值和可选的偏移量
                if (parameter != null)
                {
                    string paramStr = parameter.ToString();
                    // 检查是否包含偏移量
                    if (paramStr.Contains('+'))
                    {
                        string[] parts = paramStr.Split('+');
                        if (parts.Length > 0 && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedRatio))
                        {
                            ratio = parsedRatio;
                        }
                        if (parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedOffset))
                        {
                            offset = parsedOffset;
                        }
                    }
                    // 只有比例值
                    else if (double.TryParse(paramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parameterRatio))
                    {
                        ratio = parameterRatio;
                    }
                }
                
                return height * ratio + offset;
            }
            return value;
        }

        /// <summary>
        /// 将目标值转换回源值（未实现）
        /// </summary>
        /// <param name="value">要转换的目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的源值</returns>
        /// <exception cref="NotImplementedException">此方法未实现</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 应用程序的初始启动窗口
    /// </summary>
    /// <remarks>
    /// 负责显示应用程序启动过程中的初始化状态、进度和相关信息
    /// </remarks>
    public partial class InitialStartupWindow : Window
    {
        private readonly InitialStartupManager _initializationManager;
        private readonly InitialStartupAnimation _animationManager;
        private const int TotalInitializationSteps = 5;
        private TaskbarManager _taskbarManager;

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
        /// 初始化UI资源，包括本地化文本和图片资源
        /// </summary>
        private void InitializeUIResources()
        {
            try
            {
                // 初始化本地化文本资源
                InitializeLocalization();
                
                // 初始化图片资源
                InitializeImageResources();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化UI资源时发生错误: {ex.Message}");
                // 即使发生错误也继续执行，因为这些资源不是应用程序运行的关键
            }
        }

        /// <summary>
        /// 初始化本地化文本资源
        /// </summary>
        private void InitializeLocalization()
        {
            // 设置应用标题文本
            try
            {
                // 获取本地化助手实例
                var localizationHelper = JsonLocalizationHelper.Instance;
                
                // 确保本地化助手已初始化（虽然App.xaml.cs中应该已经初始化过）
                localizationHelper.Initialize();
                
                // 获取sign_id为10001的本地化内容
                string localizedTitle = localizationHelper.GetString(10001);
                
                // 设置应用标题文本
                if (!string.IsNullOrEmpty(localizedTitle))
                {
                    AppTitle.Text = localizedTitle;
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
                    System.Diagnostics.Debug.WriteLine($"获取窗口本地化标题失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // 如果获取失败，保留默认标题
                System.Diagnostics.Debug.WriteLine($"获取本地化标题失败: {ex.Message}");
            }
            
            // 获取并设置当前版本号
            try
            {
                // 获取当前程序集
                Assembly assembly = Assembly.GetExecutingAssembly();
                // 获取版本信息
                Version version = assembly.GetName().Version;
                // 设置版本号文本
                VersionInfo.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }
            catch (Exception ex)
            {
                // 如果获取失败，保留默认版本号
                System.Diagnostics.Debug.WriteLine($"获取版本号失败: {ex.Message}");
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
                
                // 设置Logo图片资源
                if (AppLogo != null)
                {
                    // 假设Logo的sign_id为10001（与MainWindow中的设置保持一致）
                    var logoImage = assetHelper.GetImageAsset(10001);
                    if (logoImage != null)
                    {
                        AppLogo.Source = logoImage;
                    }
                }
                
                // 设置背景图片资源
                if (BackgroundImage != null)
                {
                    // 背景图片的sign_id为10015
                    var backgroundImage = assetHelper.GetImageAsset(10015);
                    if (backgroundImage != null)
                    {
                        BackgroundImage.Source = backgroundImage;
                        
                        // 设置背景图片来源文本
                        if (BackgroundImageSource != null)
                        {
                            // 这里可以根据实际情况获取真实的图片来源信息
                            // 目前设置为静态文本
                            BackgroundImageSource.Text = "Created by ComfyUI-XL.Wai";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化图片资源失败: {ex.Message}");
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
        /// <returns>表示异步操作的任务</returns>
        private async Task PerformInitializationAsync()
        {
            try
            {
                // 使用ConfigureAwait(false)避免不必要的UI上下文切换，提高性能
                // Step 1: Checking system environment
                await UpdateStatusAsync("Checking system environment...", 0);
                await _initializationManager.CheckSystemEnvironmentAsync().ConfigureAwait(false);
                await Task.Delay(300).ConfigureAwait(false); // Simulate time-consuming operation

                // Step 2: Loading configuration files
                await UpdateStatusAsync("Loading configuration files...", 20);
                await _initializationManager.LoadConfigurationAsync().ConfigureAwait(false);
                await Task.Delay(300).ConfigureAwait(false);

                // Step 3: Initializing resources
                await UpdateStatusAsync("Initializing resources...", 40);
                await _initializationManager.InitializeResourcesAsync().ConfigureAwait(false);
                await Task.Delay(300).ConfigureAwait(false);

                // Step 4: Preparing main window data
                await UpdateStatusAsync("Preparing main window data...", 60);
                await _initializationManager.PrepareMainWindowDataAsync().ConfigureAwait(false);
                await Task.Delay(100).ConfigureAwait(false);

                // Step 5: Initialization completed
                await UpdateStatusAsync("Initialization completed, starting application...", 99);
                await Task.Delay(2000).ConfigureAwait(false); // Simulate time-consuming operation
                // Initialization completed
                await UpdateStatusAsync("Initialization completed, starting application...", 100);

                // 切换到主窗口
                SwitchToMainWindow();
            }
            catch (Exception ex)
            {
                // 处理初始化过程中的异常
                await UpdateStatusAsync($"初始化失败：{ex.Message}", 0);
                // 在UI线程上显示错误消息
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"应用初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                });
            }
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
                if (StatusText != null)
                {
                    StatusText.Text = statusText;
                }
                
                if (ProgressPercentage != null)
                {
                    ProgressPercentage.Text = $"{progressValue}%";
                }
                
                // 使用动画平滑更新进度条值
                // 将异步操作包装在同步方法中执行
                if (InitializationProgress != null && _animationManager != null)
                {
                    await _animationManager.AnimateProgressBarAsync(InitializationProgress, progressValue);
                }
                
                // 更新任务栏进度条
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
                        System.Diagnostics.Debug.WriteLine("更新任务栏进度条失败: " + ex.Message);
                    }
                }
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
                if (StatusText != null)
                {
                    StatusText.Text = statusText;
                }
                
                if (ProgressPercentage != null)
                {
                    ProgressPercentage.Text = $"{progressValue}%";
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
            
            // 使用动画平滑更新进度条值
            if (InitializationProgress != null && _animationManager != null)
            {
                await _animationManager.AnimateProgressBarAsync(InitializationProgress, progressValue).ConfigureAwait(false);
            }
            
            // 更新任务栏进度条
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
                    System.Diagnostics.Debug.WriteLine("更新任务栏进度条失败: " + ex.Message);
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
                    System.Diagnostics.Debug.WriteLine($"切换到主窗口失败: {ex.Message}");
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
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // try
            // {
            //     // 允许通过鼠标拖动窗口
            //     this.DragMove();
            // }
            // catch { /* 忽略拖动过程中可能出现的异常 */ }
        }
    }
}