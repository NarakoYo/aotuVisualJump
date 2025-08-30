using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ImageRecognitionApp.Assets.UICode;

namespace ImageRecognitionApp.Assets.UI
{
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