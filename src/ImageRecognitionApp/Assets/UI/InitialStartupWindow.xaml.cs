using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using ImageRecognitionApp.Assets.UICode;
using ImageRecognitionApp.unit;

namespace ImageRecognitionApp.Assets.UI
{
    /// <summary>
    /// 高度转换器：根据参数将窗口高度转换为指定比例的高度，可以选择性地添加偏移量
    /// 参数格式："ratio[+offset]"，例如："0.8"表示高度的80%，"0.8+4"表示高度的80%再加上4像素
    /// </summary>
    public class HeightConverter : IValueConverter
    {
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// InitialStartupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InitialStartupWindow : Window
    {
        private readonly InitializationManager _initializationManager;
        private const int TotalInitializationSteps = 5;

        public InitialStartupWindow()
        {
            InitializeComponent();
            _initializationManager = new InitializationManager();
            this.Loaded += InitialStartupWindow_Loaded;
            
            // 通过JsonLocalizationHelper获取本地化内容并设置应用标题
            try
            {
                var localizationHelper = ImageRecognitionApp.unit.JsonLocalizationHelper.Instance;
                // 确保本地化助手已初始化（虽然App.xaml.cs中应该已经初始化过）
                localizationHelper.Initialize();
                // 获取sign_id为10001的本地化内容
                string localizedTitle = localizationHelper.GetString(10001);
                // 设置应用标题文本
                AppTitle.Text = localizedTitle;
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
            
            // 通过AssetHelper获取并设置Logo图片资源
            try
            {
                var assetHelper = AssetHelper.Instance;
                // 假设Logo的sign_id为10001（与MainWindow中的设置保持一致）
                System.Windows.Media.Imaging.BitmapImage logoImage = assetHelper.GetImageAsset(10001);
                AppLogo.Source = logoImage;
            }
            catch (Exception ex)
            {
                // 如果获取失败，可以记录日志或使用默认图片
                System.Diagnostics.Debug.WriteLine($"获取Logo图片失败: {ex.Message}");
            }

            // 通过AssetHelper获取并设置背景图片资源，sign_id为10015
            try
            {
                var assetHelper = AssetHelper.Instance;
                System.Windows.Media.Imaging.BitmapImage backgroundImage = assetHelper.GetImageAsset(10015);
                BackgroundImage.Source = backgroundImage;
            }
            catch (Exception ex)
            {
                // 如果获取失败，可以记录日志
                System.Diagnostics.Debug.WriteLine($"获取背景图片失败: {ex.Message}");
            }
        }

        private async void InitialStartupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await PerformInitializationAsync();
        }

        private async Task PerformInitializationAsync()
        {
            try
            {
                // 第1步：检查系统环境
                UpdateStatus("检查系统环境...", 0);
                await _initializationManager.CheckSystemEnvironmentAsync();
                await Task.Delay(300); // 模拟耗时操作

                // 第2步：加载配置文件
                UpdateStatus("加载配置文件...", 20);
                await _initializationManager.LoadConfigurationAsync();
                await Task.Delay(300);

                // 第3步：初始化资源
                UpdateStatus("初始化资源...", 40);
                await _initializationManager.InitializeResourcesAsync();
                await Task.Delay(300);

                // 第4步：准备主窗口数据
                UpdateStatus("准备主窗口数据...", 60);
                await _initializationManager.PrepareMainWindowDataAsync();
                await Task.Delay(300);

                // 第5步：初始化完成
                UpdateStatus("初始化完成，正在启动应用...", 100);
                await Task.Delay(500);

                // 切换到主窗口
                SwitchToMainWindow();
            }
            catch (Exception ex)
            {
                // 处理初始化过程中的异常
                UpdateStatus($"初始化失败：{ex.Message}", 0);
                MessageBox.Show($"应用初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void UpdateStatus(string statusText, int progressValue)
        {
            // 在UI线程上更新状态
            this.Dispatcher.Invoke(() =>
            {
                StatusText.Text = statusText;
                InitializationProgress.Value = progressValue;
                ProgressPercentage.Text = $"{progressValue}%";
            }, DispatcherPriority.Background);
        }

        private void SwitchToMainWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                // 关闭初始化窗口
                this.Hide();
                
                // 创建并显示主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();
                
                // 关闭当前窗口
                this.Close();
            });
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 允许通过鼠标拖动窗口
            this.DragMove();
        }
    }
}