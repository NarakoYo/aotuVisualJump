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

        // 初始化资产工具类，确保任务栏图标能在应用启动时就显示
        try
        {
            var assetHelper = AssetHelper.Instance;
            LogMessage("资产工具类已初始化");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"资产工具类初始化失败: {ex.Message}");
            LogMessage($"资产工具类初始化失败: {ex.Message}");
        }

        // 设置应用程序图标，确保初始化界面也能显示图标
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo", "logo.ico");
            if (File.Exists(iconPath))
            {
                this.MainWindow = null; // 确保尚未设置主窗口
                this.Properties["IconPath"] = iconPath;
                LogMessage($"应用程序图标已设置: {iconPath}");
            }
            else
            {
                LogMessage($"未找到图标文件: {iconPath}");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"设置应用程序图标时出错: {ex.Message}");
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
        
        // 初始化用户输入监控器但暂时不启动监控
        UserInputMonitor = UserInputMonitor.Instance;
        LogMessage("用户输入监控器已初始化，但等待主界面加载后再启动");
        
        // 注册MainWindowLoaded事件，用于延迟初始化重量级组件
        MainWindowLoaded += (sender, e) =>
        {
            try
            {
                // 在主界面加载完成后再初始化系统资源监控的完整功能
                CheckMemoryStatusAfterMainWindowLoaded();
                LogMessage("主界面加载完成，已初始化系统资源监控完整功能");
            }
            catch (Exception ex)
            {
                LogMessage($"初始化系统资源监控完整功能时出错: {ex.Message}");
            }
        };
        
        // 初始化轻量级的系统资源监控
        InitializeLightweightSystemMonitoring();
        
        // 注册电源状态变化事件
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        
        // 启动轻量级的待机状态检测
        StartStandbyDetection();
        
        // 将进程初始化移到MainWindowLoaded事件中，避免启动时的卡顿
        MainWindowLoaded += (sender, e) =>
        {
            try
            {
                ProcessHelper.InitializeProcess();
                LogMessage("主界面加载完成，已初始化进程设置");
            }
            catch (Exception ex)
            {
                LogMessage($"初始化进程时出错: {ex.Message}");
                LogException(ex);
            }
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
    /// 初始化轻量级的系统资源监控（仅创建必要的计数器，延迟其他操作）
    /// </summary>
    private void InitializeLightweightSystemMonitoring()
    {
        try
        {
            // 只创建内存性能计数器，用于基本的内存监控
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            // 创建但不启动监控定时器
            _performanceMonitorTimer = new DispatcherTimer();
            _performanceMonitorTimer.Interval = TimeSpan.FromSeconds(10); // 每10秒检查一次
            _performanceMonitorTimer.Tick += PerformanceMonitorTimer_Tick;
            
            // 只进行最基本的内存状态检查，不执行复杂的优化
            float availableMemory = _memoryCounter.NextValue();
            LogMessage($"应用程序启动，初始可用内存: {availableMemory:F2} MB");
        }
        catch (Exception ex)
        {
            LogMessage($"初始化轻量级系统资源监控时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 在主界面加载完成后检查内存状态
    /// </summary>
    private void CheckMemoryStatusAfterMainWindowLoaded()
    {
        try
        {
            // 创建CPU性能计数器
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // 第一次调用返回0，需要预热
            
            // 启动监控定时器
            _performanceMonitorTimer?.Start();
            
            // 检查内存状态并执行优化
            float availableMemory = _memoryCounter.NextValue();
            const float LOW_MEMORY_THRESHOLD_MB = 1024; // 1GB
            
            // 检查是否还在启动宽限期内
            bool isInStartupGracePeriod = DateTime.Now - _appStartupTime < TimeSpan.FromSeconds(STARTUP_GRACE_PERIOD_SECONDS);
            _initialStartupPeriod = isInStartupGracePeriod;
            
            if (isInStartupGracePeriod)
            {
                LogMessage($"应用程序仍在启动宽限期内({STARTUP_GRACE_PERIOD_SECONDS}秒)，延迟严格的内存优化策略");
                
                // 在启动宽限期内，只有在内存严重不足时才执行低功耗模式
                if (availableMemory < CRITICAL_MEMORY_THRESHOLD_MB)
                {
                    LogMessage($"在启动宽限期内发现内存严重不足({availableMemory:F2} MB)，需要执行最低限度的内存优化");
                    // 只执行轻度内存优化，不清理图标资源
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                }
            }
            else
            {
                _initialStartupPeriod = false;
                LogMessage("应用程序已完成启动宽限期，现在开始正常执行内存优化策略");
                // 正常执行内存优化策略
                if (availableMemory < LOW_MEMORY_THRESHOLD_MB)
                {
                    LogMessage($"系统内存较低({availableMemory:F2} MB)，应用严格内存优化策略");
                    PerformanceManager.EnterLowPowerMode();
                    
                    // 执行垃圾回收
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                }
            }
            
            // 配置进程内存限制
            ConfigureProcessMemoryLimits();
        }
        catch (Exception ex)
        {
            LogMessage($"检查内存状态时出错: {ex.Message}");
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
        
        // 注册MainWindowLoaded事件处理程序，确保在主界面加载完成后才进行重量级初始化
        MainWindowLoaded += (sender, e) =>
        {
            try
            {
                // 在主界面加载完成后才启动键盘和鼠标钩子来检测用户活动
                PerformanceManager.StartUserActivityMonitoring();
                LogMessage("主界面加载完成，已启动用户活动监控");
                
                // 注册UserInputMonitor的事件处理程序以更新用户活动时间
                UserInputMonitor.Instance.KeyPressed += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
                UserInputMonitor.Instance.MouseClicked += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
                UserInputMonitor.Instance.MouseMoved += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
                UserInputMonitor.Instance.MouseWheel += (sender, e) => PerformanceManager.UpdateLastUserActivityTime();
            }
            catch (Exception ex)
            {
                LogMessage($"启动用户活动监控时出错: {ex.Message}");
            }
        };
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
        // 只使用UserInputMonitor来停止监控，避免重复卸载钩子
        UserInputMonitor?.StopAllMonitoring();
        
        // 注销电源事件
        SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        
        LogMessage("应用程序资源已释放");
    }

    // 主窗口加载完成事件
    private event EventHandler? MainWindowLoaded;
    
    // 标记主窗口是否已经完成加载和初始化
    private bool _mainWindowLoaded = false;

    // 在应用程序中重写OnActivated方法，确保主窗口激活时设置图标
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        
        // 确保应用程序不在关闭过程中，避免在程序已决定退出时触发事件导致异常
        if (!this.ShutdownMode.HasFlag(ShutdownMode.OnExplicitShutdown) && !IsShuttingDown)
        {
            // 只在主窗口第一次激活时触发初始化事件
            if (!_mainWindowLoaded)
            {
                _mainWindowLoaded = true;
                MainWindowLoaded?.Invoke(this, EventArgs.Empty);
                
                // 初始化完成后进行内存状态检查
                CheckMemoryStatusAfterMainWindowLoaded();
            }
            
            // 用户活动检测 - 检查PerformanceManager是否为null
            PerformanceManager?.UpdateLastUserActivityTime();
        }
    }
    
    // 标记应用程序是否正在关闭
    private bool IsShuttingDown { get; set; } = false;
    
    // 应用启动宽限期相关字段
    private static bool _initialStartupPeriod = true;
    private static DateTime _appStartupTime = DateTime.Now;
    private const int STARTUP_GRACE_PERIOD_SECONDS = 10; // 启动宽限期为10秒
    private const float CRITICAL_MEMORY_THRESHOLD_MB = 512; // 512MB，只有在严重内存不足时才在启动初期进入低功耗模式
    
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