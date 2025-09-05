using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using ImageRecognitionApp.WinFun;
using ImageRecognitionApp.Utils; // 添加这个引用以使用PerformanceManager类
using Microsoft.Win32; // 添加这个引用以使用SystemEvents和PowerModeChangedEventArgs

namespace ImageRecognitionApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // 性能监控相关变量
    private DispatcherTimer? _performanceMonitorTimer;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memoryCounter;
    private const int STANDBY_CHECK_INTERVAL_MS = 5000; // 5秒检查一次系统待机状态
    
    // 全局性能管理器
    public PerformanceManager? PerformanceManager { get; private set; }
    
    // 用户输入监控器
    public UserInputMonitor? UserInputMonitor { get; private set; }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;

        // 初始化日志管理器
        LogManager.Instance.Initialize();
        
        // 处理命令行参数
        if (ProcessCommandLineArgs(e.Args))
        {
            // 命令行模式执行完毕，直接退出
            this.Shutdown();
            return;
        }

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
        
        // 初始化性能管理器
        PerformanceManager = new PerformanceManager();
        
        // 初始化用户输入监控器
        UserInputMonitor = UserInputMonitor.Instance;
        UserInputMonitor.StartAllMonitoring();
        LogMessage("用户输入监控器已初始化并启动");
        
        // 初始化系统资源监控
        InitializeSystemResourceMonitoring();
        
        // 注册电源状态变化事件
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        
        // 启动待机状态检测
        StartStandbyDetection();
        
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
    
    /// <summary>
    /// 处理命令行参数
    /// </summary>
    /// <param name="args">命令行参数数组</param>
    /// <returns>是否已处理命令行参数并应退出应用程序</returns>
    private bool ProcessCommandLineArgs(string[] args)
    {
        if (args.Length > 0)
        {
            // 检查是否为资产筛选命令
            if (args[0].Equals("filter-assets", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string projectPath = args.Length > 1 ? args[1] : AppDomain.CurrentDomain.BaseDirectory;
                    string outputPath = args.Length > 2 ? args[2] : Path.Combine(projectPath, "filtered_assets");
                    
                    Console.WriteLine("开始筛选资产文件...");
                    Console.WriteLine($"项目路径: {projectPath}");
                    Console.WriteLine($"输出目录: {outputPath}");
                    
                    // 调用资产筛选工具
                    AssetFilterHelper.FilterAssets(projectPath, outputPath);
                    
                    Console.WriteLine("资产筛选完成!");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"资产筛选过程中发生错误: {ex.Message}");
                    return true;
                }
            }
            // 可以添加其他命令行参数的处理
        }
        
        return false;
    }

    private void App_DispatcherUnhandledException(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
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
    
    /// <summary>
    /// 初始化系统资源监控
    /// </summary>
    private void InitializeSystemResourceMonitoring()
    {
        try
        {
            // 创建CPU性能计数器
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // 第一次调用返回0，需要预热
            
            // 创建内存性能计数器
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            // 创建并启动监控定时器 - 降低监控频率以减少资源消耗
            _performanceMonitorTimer = new DispatcherTimer();
            _performanceMonitorTimer.Interval = TimeSpan.FromSeconds(10); // 每10秒检查一次，而不是5秒
            _performanceMonitorTimer.Tick += PerformanceMonitorTimer_Tick;
            _performanceMonitorTimer.Start();
            
            // 应用启动时立即检查内存状态
            CheckMemoryStatusOnStartup();
            
            LogMessage("系统资源监控已初始化");
        }
        catch (Exception ex)
        {
            LogMessage($"初始化系统资源监控时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 应用启动时检查内存状态，实施初始内存优化策略
    /// </summary>
    private void CheckMemoryStatusOnStartup()
    {
        try
        {
            // 立即获取当前可用内存
            float availableMemory = _memoryCounter.NextValue();
            
            // 如果可用内存低于阈值，启动时就应用低功耗模式
            const float LOW_MEMORY_THRESHOLD_MB = 1024; // 1GB
            if (availableMemory < LOW_MEMORY_THRESHOLD_MB)
            {
                LogMessage($"系统内存较低({availableMemory:F2} MB)，启动时应用严格内存优化策略");
                PerformanceManager.EnterLowPowerMode();
                
                // 立即执行垃圾回收
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
            }
            
            // 配置进程内存限制
            ConfigureProcessMemoryLimits();
        }
        catch (Exception ex)
        {
            LogMessage($"检查启动内存状态时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 配置进程内存限制，帮助操作系统更好地管理应用内存
    /// </summary>
    private void ConfigureProcessMemoryLimits()
    {
        try
        {
            Process currentProcess = Process.GetCurrentProcess();
            
            // 设置进程工作集大小限制，帮助系统更有效地管理内存
            // 注意：这些值应该根据实际应用需求进行调整
            int maxWorkingSetMb = 100; // 100MB作为初始最大工作集大小
            int minWorkingSetMb = 20;  // 20MB作为初始最小工作集大小
            
            currentProcess.MaxWorkingSet = (IntPtr)(maxWorkingSetMb * 1024 * 1024);
            currentProcess.MinWorkingSet = (IntPtr)(minWorkingSetMb * 1024 * 1024);
            
            LogMessage($"进程内存限制已配置: 最小={minWorkingSetMb}MB, 最大={maxWorkingSetMb}MB");
        }
        catch (Exception ex)
        {
            LogMessage($"配置进程内存限制时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 性能监控定时器回调
    /// </summary>
    private void PerformanceMonitorTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            // 获取当前CPU使用率和可用内存
            float cpuUsage = _cpuCounter.NextValue();
            float availableMemory = _memoryCounter.NextValue();
            
            // 记录系统资源使用情况
            LogMessage($"CPU使用率: {cpuUsage:F2}%, 可用内存: {availableMemory:F2} MB");
            
            // 根据系统资源使用情况调整性能模式
            PerformanceManager.AdjustPerformanceMode(cpuUsage, availableMemory);
        }
        catch (Exception ex)
        {
            LogMessage($"性能监控出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 启动待机状态检测
    /// </summary>
    private void StartStandbyDetection()
    {
        // 初始化最后用户活动时间
        PerformanceManager.UpdateLastUserActivityTime();
        
        // 启动键盘和鼠标钩子来检测用户活动
        PerformanceManager.StartUserActivityMonitoring();
        
        // 注册UserInputMonitor的事件处理程序以更新用户活动时间
        UserInputMonitor.KeyPressed += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
        UserInputMonitor.MouseClicked += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
        UserInputMonitor.MouseMoved += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
        UserInputMonitor.MouseWheel += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
    }
    
    /// <summary>
    /// 系统电源模式变化事件处理
    /// </summary>
    private void SystemEvents_PowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        switch (e.Mode)
        {
            case PowerModes.Resume:
                LogMessage("系统从待机状态恢复");
                PerformanceManager.ExitLowPowerMode();
                break;
            case PowerModes.Suspend:
                LogMessage("系统进入待机状态");
                PerformanceManager.EnterLowPowerMode();
                break;
            case PowerModes.StatusChange:
                // 电源状态改变（如从电池切换到交流电）
                break;
        }
    }
    
    /// <summary>
    /// 应用程序退出时释放资源
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        
        // 停止性能监控
        if (_performanceMonitorTimer != null)
        {
            _performanceMonitorTimer.Stop();
            _performanceMonitorTimer = null;
        }
        
        // 释放性能计数器
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        
        // 停止用户活动监控
        PerformanceManager?.StopUserActivityMonitoring();
        UserInputMonitor?.StopAllMonitoring();
        
        // 注销电源事件
        SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        
        LogMessage("应用程序资源已释放");
    }

    // 主窗口加载完成事件
    private event EventHandler? MainWindowLoaded;

    // 在应用程序中重写OnActivated方法，确保主窗口激活时设置图标
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        
        // 确保应用程序不在关闭过程中，避免在程序已决定退出时触发事件导致异常
        if (!this.ShutdownMode.HasFlag(ShutdownMode.OnExplicitShutdown) && !IsShuttingDown)
        {
            MainWindowLoaded?.Invoke(this, EventArgs.Empty);
            
            // 用户活动检测 - 检查PerformanceManager是否为null
            PerformanceManager?.UpdateLastUserActivityTime();
        }
    }
    
    // 标记应用程序是否正在关闭
    private bool IsShuttingDown { get; set; } = false;
    
    // 重写Shutdown方法，设置关闭标记
    public new void Shutdown()
    {
        IsShuttingDown = true;
        base.Shutdown();
    }
    
    public new void Shutdown(int exitCode)
    {
        IsShuttingDown = true;
        base.Shutdown(exitCode);
    }
}