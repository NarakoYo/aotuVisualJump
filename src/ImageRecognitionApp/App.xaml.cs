using System;
using System.Windows;
using System.IO;
using ImageRecognitionApp.unit;
using ImageRecognitionApp.WinFun;

namespace ImageRecognitionApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 初始化日志管理器
            LogManager.Instance.Initialize();

            // 初始化本地化工具
            try
            {
                JsonLocalizationHelper.Instance.Initialize();
                // Console.WriteLine("本地化工具初始化成功");
                // LogMessage("本地化工具初始化成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"本地化工具初始化失败: {ex.Message}");
                // LogMessage($"本地化工具初始化失败: {ex.Message}");
                // LogException(ex);
            }

            // 记录程序启动标记
            LogManager.Instance.WriteStartupShutdownLog(true);

            // 单实例检查
            try
            {
                JsonLocalizationHelper.Instance.Initialize();
                // Console.WriteLine("本地化工具初始化成功");
                // LogMessage("本地化工具初始化成功");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"本地化工具初始化失败: {ex.Message}");
                LogMessage($"本地化工具初始化失败: {ex.Message}");
                LogException(ex);
            }

            // 单实例检查
            if (SingleInstanceChecker.CheckIfAlreadyRunning())
            {
                // 已有实例运行，显示警告并退出
                SingleInstanceChecker.ShowDuplicateInstanceWarning();
                this.Shutdown();
                return;
            }

            // 注册退出事件，释放互斥锁并记录关闭标记
            this.Exit += (sender, args) =>
            {
                // 记录程序关闭标记
                LogManager.Instance.WriteStartupShutdownLog(false);
                SingleInstanceChecker.ReleaseMutex();
            };

            // 初始化进程（设置进程名称和图标）
            try
            {
                ProcessHelper.InitializeProcess();
            }
            catch (Exception ex)
            {
                LogMessage($"初始化进程时出错: {ex.Message}");
                LogException(ex);
            };
        }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex);
        }
    }

    private void LogException(Exception ex)
    {
        LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"异常: {ex.Message}\n{ex.StackTrace}");
    }

    public void LogMessage(string message)
    {
        LogManager.Instance.WriteLog(LogManager.LogLevel.Info, message);
    }

    // 主窗口加载完成事件
    private event EventHandler? MainWindowLoaded;

    // 在应用程序中重写OnActivated方法，确保主窗口激活时设置图标
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        MainWindowLoaded?.Invoke(this, EventArgs.Empty);
    }
}