using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Media;
using System.ComponentModel;
using ImageRecognitionApp.WinFun;  // 导入WinFun命名空间
using ImageRecognitionApp.unit;     // 导入unit命名空间

namespace ImageRecognitionApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
{
    // 任务栏管理器和动画
    private TaskbarManager? _taskbarManager = null;
    private TaskbarAnimation? _taskbarAnimation = null;

    // 窗口标题属性
    private string _titleText = string.Empty;
    public string TitleText
    {
        get => _titleText;
        set
        {
            if (_titleText != value)
            {
                _titleText = value;
                OnPropertyChanged(nameof(TitleText));
            }
        }
    }

    // 设置按钮文本属性
    private string _settingButtonText = string.Empty;
    public string SettingButtonText
    {
        get => _settingButtonText;
        set
        {
            if (_settingButtonText != value)
            {
                _settingButtonText = value;
                OnPropertyChanged(nameof(SettingButtonText));
            }
        }
    }

    // 实现INotifyPropertyChanged接口
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 跟踪设置按钮是否被点击
    private bool _isSettingButtonClicked = false;

    // Python相关路径
    private readonly string _pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\PythonScripts\\venv\\Scripts\\python.exe");

    // 设置按钮点击事件处理程序
    private void SettingButton_Click(object sender, RoutedEventArgs e)
    {
        // 设置按钮被点击状态
        _isSettingButtonClicked = true;
        UpdateSettingButtonBackground();
    }

    // 更新设置按钮背景色
    private void UpdateSettingButtonBackground()
    {
        if (SettingButton != null && SettingButton.Template != null)
        {
            var border = SettingButton.Template.FindName("border", SettingButton) as Border;
            if (border != null)
            {
                border.Background = _isSettingButtonClicked ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37373D")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            }
        }
    }

    // 重置设置按钮状态（供其他按钮点击事件调用）
    public void ResetSettingButtonState()
    {
        _isSettingButtonClicked = false;
        UpdateSettingButtonBackground();
    }
    private readonly string _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\PythonScripts\\image_recognition.py");
    private readonly string _scriptSaveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\scripts");

    // 进程和状态变量
    // 字段声明移至顶部
    private Process? _pythonProcess = null; // 修复CS8618: 设为可空并初始化
    private bool _isRecording = false;
    private bool _isExecuting = false;
    private List<object> _recordingCommands = new List<object>();
    private DateTime _recordingStartTime;
    private DispatcherTimer? _recordingTimer = null;
    // 移除未使用的字段以修复CS0414
    private List<object> _imageCache = new List<object>();
    private bool _isMaximized = false; // 新增：跟踪窗口是否最大化
    private Point _restorePoint; // 新增：存储窗口还原位置

    public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // 设置数据上下文

            // 初始化任务栏管理器和动画
            try
            {
                _taskbarManager = new TaskbarManager(this);
                _taskbarAnimation = new TaskbarAnimation(this);
                (App.Current as App)?.LogMessage("任务栏管理器和动画初始化成功");
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"任务栏初始化错误: {ex.Message}");
                (App.Current as App)?.LogMessage($"错误堆栈: {ex.StackTrace}");
            }

            // 订阅属性变化事件以更新任务栏提示
            this.PropertyChanged += MainWindow_PropertyChanged;

            // 注册窗口关闭事件以释放资源
            this.Closed += MainWindow_Closed;

            // 初始化设置按钮状态
            _isSettingButtonClicked = false;

            // 初始化本地化
            try
            {
                // 初始化本地化助手
                var helper = ImageRecognitionApp.unit.JsonLocalizationHelper.Instance;
                helper.Initialize();

                // 获取标题文本和设置按钮文本
                TitleText = helper.GetString(10001);
                SettingButtonText = helper.GetString(10003);

                // 获取当前语言
                var currentLanguageField = helper.GetType().GetField(
                    "_currentLanguage", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                string currentLanguage = currentLanguageField?.GetValue(helper) as string ?? "未知";

                // 检查本地化数据
                var localizationDataField = helper.GetType().GetField(
                    "_localizationData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var localizationData = localizationDataField?.GetValue(helper) as Dictionary<int, Dictionary<string, string>>;

                // 优先使用中文翻译
                if (localizationData != null && localizationData.ContainsKey(10001))
                {
                    var translations = localizationData[10001];
                       
                    // 优先使用中文翻译
                    if (translations.ContainsKey("zhCn"))
                    {
                        TitleText = translations["zhCn"];
                    }
                    // 如果没有中文翻译，尝试使用ghYh字段
                    else if (translations.ContainsKey("ghYh"))
                    {
                        TitleText = translations["ghYh"];
                    }
                }

                // 设置窗口标题
                this.Title = TitleText;

                // 记录当前语言
                (App.Current as App)?.LogMessage($"当前语言: {currentLanguage}");
                (App.Current as App)?.LogMessage($"标题已设置为: {TitleText}");

            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"设置标题时出错: {ex.Message}");
                this.Title = "图像识别应用";
                TitleText = "图像识别应用";
            }



        InitializeKeyboardShortcuts();
        EnsureScriptDirectoryExists();
        // 禁用窗口边缘拉伸
        this.ResizeMode = ResizeMode.NoResize;

        // 显示任务栏通知
        _taskbarManager?.ShowNotification("应用已启动", "图像识别应用已成功启动");
    }



    // 窗口关闭事件处理程序
    private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // 释放任务栏资源
            _taskbarManager?.Dispose();
            _taskbarAnimation?.StopFlashAnimation();
        }

        /// <summary>
        /// 属性变化事件处理程序
        /// </summary>
        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TitleText) && _taskbarManager != null)
            {
                _taskbarManager.UpdateTooltip(TitleText);
                (App.Current as App)?.LogMessage($"任务栏提示文本已更新为: {TitleText}");
            }
        }

    /// <summary>
    /// 初始化键盘快捷键
    /// </summary>
    private void InitializeKeyboardShortcuts()
    {
        // F9: 开始/停止录制
        InputBindings.Add(new InputBinding(
            new RelayCommand(param => StartStopRecording(null, null)),
            new KeyGesture(Key.F9, ModifierKeys.None)));

        // F5: 执行脚本
        InputBindings.Add(new InputBinding(
            new RelayCommand(param => ExecuteScript(null, null)),
            new KeyGesture(Key.F5, ModifierKeys.None)));

        // F10: 暂停录制
        InputBindings.Add(new InputBinding(
            new RelayCommand(param => PauseRecording(null, null)),
            new KeyGesture(Key.F10, ModifierKeys.None)));
    }

    /// <summary>
    /// 确保脚本保存目录存在
    /// </summary>
    private void EnsureScriptDirectoryExists()
    {
        if (!Directory.Exists(_scriptSaveDir))
        {
            Directory.CreateDirectory(_scriptSaveDir);
        }
    }

    /// <summary>
    /// 开始/停止录制脚本
    /// </summary>
    private void StartStopRecording(object? sender, RoutedEventArgs? e)
    {
        if (_isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    /// <summary>
    /// 开始录制脚本
    /// </summary>
    private void StartRecording()
    {
        if (!_isRecording)
        {
            _isRecording = true;
            _recordingStartTime = DateTime.Now;

            // 任务栏通知和动画
            _taskbarManager?.ShowNotification("开始录制", "脚本录制已开始");
            _taskbarAnimation?.StartFlashAnimation(0, 1000);  // 无限闪烁，间隔1秒
            
            if (_recordingTimer == null)
            {
                _recordingTimer = new DispatcherTimer();
            }
            
            _recordingTimer.Interval = TimeSpan.FromMilliseconds(50);
            _recordingTimer.Tick += RecordingTimer_Tick;
            _recordingTimer.Start();
            
            // Update UI
            // UpdateRecordingButton();
        }
    }

    private void RecordingTimer_Tick(object? sender, EventArgs e)
    {
        if (_recordingTimer == null || _recordingCommands == null) return;
        var mouseState = new { X = Mouse.GetPosition(this).X, Y = Mouse.GetPosition(this).Y };
        _recordingCommands.Add(new { Time = DateTime.Now - _recordingStartTime, action = "move", X = mouseState.X, Y = mouseState.Y });
    }

    private void StopRecording()
    {
        if (_isRecording)
        {
            _isRecording = false;

            // 停止任务栏动画并显示通知
            _taskbarAnimation?.StopFlashAnimation();
            _taskbarManager?.ShowNotification("录制完成", "脚本已录制完成并保存");
            
            if (_recordingTimer != null)
            {
                _recordingTimer.Stop();
                _recordingTimer.Tick -= RecordingTimer_Tick;
            }
            
            // Process recording
            SaveScriptToFile();
            
            // Update UI
            // UpdateRecordingButton();
        }
    }

    private void PauseRecording(object? sender, RoutedEventArgs? e)
    {
        if (_isRecording && _recordingTimer != null)
        {
            _recordingTimer.IsEnabled = !_recordingTimer.IsEnabled;
        }
    }
    private string? _latestScriptPath; // 重新添加脚本路径变量

    private void SaveScriptToFile()
    {
        _latestScriptPath = Path.Combine(_scriptSaveDir, $"script_{DateTime.Now:yyyyMMddHHmmss}.json");
        File.WriteAllText(_latestScriptPath, JsonSerializer.Serialize(_recordingCommands));
    }

    /// <summary>
    /// 停止脚本执行
    /// </summary>
    private void StopScriptExecution()
    {
        try
        {
            // 停止任务栏动画
            _taskbarAnimation?.StopFlashAnimation();

            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                _pythonProcess.Kill();
                _pythonProcess.Dispose();
                _pythonProcess = null;
                (App.Current as App)?.LogMessage("Python进程已停止并释放");
            }
        }
        catch (Exception ex)
        {
            (App.Current as App)?.LogMessage($"停止Python进程时出错: {ex.Message}");
        }
        finally
        {
            _isExecuting = false;
            _pythonProcess = null;
        }
    }

    // 最小化窗口
    private void MinimizeWindow(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    // 隐藏至托盘
    private void HideToTray(object sender, RoutedEventArgs e)
    {
        // 隐藏窗口
        this.Visibility = Visibility.Hidden;
        // 从任务栏中移除
        this.ShowInTaskbar = false;
        // 显示托盘通知
        _taskbarManager?.ShowNotification("应用已最小化到托盘", "点击托盘图标可恢复窗口");
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            // 移除窗口拖动功能，仅保留标题栏拖动
        }
    }

    private void ExecuteScript(object? sender, RoutedEventArgs? e)
    {
        if (_isRecording)
        {
            (App.Current as App)?.LogMessage("录制中，无法执行脚本");
            _taskbarManager?.ShowNotification("无法执行", "录制中，无法执行脚本");
            return;
        }

        if (_isExecuting)
        {
            StopScriptExecution();
            return;
        }

        // 显示执行开始通知
        _taskbarManager?.ShowNotification("开始执行", "脚本执行已开始");
        _taskbarAnimation?.StartFlashAnimation(0, 500);  // 快速闪烁

        try
        {
            // 检查Python路径和脚本路径是否有效
            if (string.IsNullOrEmpty(_pythonPath) || !File.Exists(_pythonPath))
            {
                (App.Current as App)?.LogMessage("Python路径无效或未设置");
                return;
            }

            if (string.IsNullOrEmpty(_scriptPath) || !File.Exists(_scriptPath))
            {
                (App.Current as App)?.LogMessage("脚本路径无效或未设置");
                return;
            }

            if (string.IsNullOrEmpty(_latestScriptPath) || !File.Exists(_latestScriptPath))
            {
                (App.Current as App)?.LogMessage("未找到录制的脚本文件，请先录制脚本");
                return;
            }

            _isExecuting = true;
            _pythonProcess = new Process();
            _pythonProcess.StartInfo.FileName = _pythonPath;
            _pythonProcess.StartInfo.Arguments = $"\"{_scriptPath}\" run \"{_latestScriptPath}\"";
            _pythonProcess.StartInfo.UseShellExecute = false;
            _pythonProcess.StartInfo.RedirectStandardOutput = true;
            _pythonProcess.StartInfo.RedirectStandardError = true;
            _pythonProcess.EnableRaisingEvents = true; // 启用进程事件

            // 添加输出和错误处理事件
            _pythonProcess.OutputDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    (App.Current as App)?.LogMessage($"Python输出: {args.Data}");
                }
            };

            _pythonProcess.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    (App.Current as App)?.LogMessage($"Python错误: {args.Data}");
                }
            };

            // 添加进程退出事件处理
            _pythonProcess.Exited += (s, args) =>
            {
                string title = _pythonProcess?.ExitCode == 0 ? "执行成功" : "执行失败";
                string message = _pythonProcess?.ExitCode == 0 ? "脚本执行成功完成" : "脚本执行失败，请查看日志";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _taskbarManager?.ShowNotification(title, message);
                    _taskbarAnimation?.StopFlashAnimation();
                });

                (App.Current as App)?.LogMessage($"Python进程已退出，退出代码: {_pythonProcess?.ExitCode}");
                _isExecuting = false;
                if (_pythonProcess != null)
                {
                    _pythonProcess.Dispose();
                    _pythonProcess = null;
                }
            };

            _pythonProcess.Start();
            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();

            (App.Current as App)?.LogMessage("Python脚本已启动执行");
        }
        catch (Exception ex)
        {
            (App.Current as App)?.LogMessage($"执行脚本时出错: {ex.Message}");
            _isExecuting = false;
            if (_pythonProcess != null)
            {
                _pythonProcess.Dispose();
                _pythonProcess = null;
            }
        }
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void HideToTrayButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // 创建ToolTip对象
                ToolTip tooltip = new ToolTip();
                tooltip.Content = unit.JsonLocalizationHelper.Instance.GetString(20006);
                
                // 设置ToolTip样式
                Style tooltipStyle = new Style(typeof(ToolTip));
                tooltipStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 30, 30, 30))));
                tooltipStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))));
                tooltip.Style = tooltipStyle;
                
                // 设置显示时长并应用到按钮
                ToolTipService.SetShowDuration(button, 30000);
                button.ToolTip = tooltip;
            }
        }

    private void MinimizeWindowButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // 创建ToolTip对象
                ToolTip tooltip = new ToolTip();
                tooltip.Content = unit.JsonLocalizationHelper.Instance.GetString(20005);
                
                // 设置ToolTip样式
                Style tooltipStyle = new Style(typeof(ToolTip));
                tooltipStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 30, 30, 30))));
                tooltipStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))));
                tooltip.Style = tooltipStyle;
                
                // 设置显示时长并应用到按钮
                ToolTipService.SetShowDuration(button, 30000);
                button.ToolTip = tooltip;
            }
        }

    private void CloseWindowButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // 创建ToolTip对象
                ToolTip tooltip = new ToolTip();
                tooltip.Content = unit.JsonLocalizationHelper.Instance.GetString(20004);
                
                // 设置ToolTip样式
                Style tooltipStyle = new Style(typeof(ToolTip));
                tooltipStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 30, 30, 30))));
                tooltipStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))));
                tooltip.Style = tooltipStyle;
                
                // 设置显示时长并应用到按钮
                ToolTipService.SetShowDuration(button, 30000);
                button.ToolTip = tooltip;
            }
        }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            // 停止脚本执行
            if (_isExecuting)
            {
                StopScriptExecution();
            }

            // 释放定时器
            if (_recordingTimer != null)
            {
                _recordingTimer.Stop();
                _recordingTimer.Tick -= RecordingTimer_Tick;
                _recordingTimer = null;
                (App.Current as App)?.LogMessage("定时器已释放");
            }

            // 清除缓存
            _imageCache.Clear();
            (App.Current as App)?.LogMessage("缓存已清除");

            // 释放任务栏管理器资源
            if (_taskbarManager != null)
            {
                _taskbarManager.Dispose();
                _taskbarManager = null;
                (App.Current as App)?.LogMessage("任务栏管理器已释放");
            }

            // 释放任务栏动画资源
            if (_taskbarAnimation != null)
            {
                _taskbarAnimation.Dispose();
                _taskbarAnimation = null;
                (App.Current as App)?.LogMessage("任务栏动画已释放");
            }

            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            (App.Current as App)?.LogMessage("垃圾回收已执行");
        }
        catch (Exception ex)
        {
            (App.Current as App)?.LogMessage($"窗口关闭时出错: {ex.Message}");
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            // 获取鼠标相对于屏幕的位置
            Point screenPoint = Mouse.GetPosition(null);

            // 检测是否拖动到屏幕顶部
            if (screenPoint.Y <= 5)
            {
                if (!_isMaximized)
                {
                    // 保存当前窗口位置和大小
                    _restorePoint = new Point(this.Left, this.Top);
                    // 最大化窗口
                    this.WindowState = WindowState.Maximized;
                    _isMaximized = true;
                }
                else
                {
                    // 还原窗口
                    this.WindowState = WindowState.Normal;
                    this.Left = _restorePoint.X;
                    this.Top = _restorePoint.Y;
                    _isMaximized = false;
                }
            }
            else
            {
                // 正常拖动窗口
                DragMove();
            }
        }
    }

    // 新增：标题栏按钮区域鼠标按下事件 - 不触发拖动
    private void TitleBarButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 这里不执行DragMove，仅标记事件已处理
        e.Handled = true;
    }
}

/// <summary>
/// 命令实现类
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute; // 修复CS8767: 参数设为可空
    private readonly Func<object?, bool> _canExecute;

    public event EventHandler? CanExecuteChanged = delegate { };

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) // 修复CS1736: 默认值设为null
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? (param => true); // 在构造函数内设置默认值
    }

    public bool CanExecute(object? parameter) // 修复CS8767: 参数设为可空
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter) // 修复CS8767: 参数设为可空
    {
        _execute(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}