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

namespace ImageRecognitionApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
{
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

    // 实现INotifyPropertyChanged接口
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Python相关路径
    private readonly string _pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\PythonScripts\\venv\\Scripts\\python.exe");
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

            // 直接设置标题以测试本地化
            try
            {
                // 检查本地化助手是否初始化
                // Console.WriteLine("检查本地化助手初始化状态...");
                var helper = ImageRecognitionApp.unit.LuaLocalizationHelper.Instance;
                helper.Initialize();
                // Console.WriteLine("本地化助手已初始化");

                // 获取当前语言
                var currentLanguageField = helper.GetType().GetField(
                    "_currentLanguage", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                string currentLanguage = currentLanguageField?.GetValue(helper) as string ?? "未知";
                // Console.WriteLine($"当前语言: {currentLanguage}");

                // 尝试获取标题文本
                string title = helper.GetString(10001);
                // Console.WriteLine($"获取到的标题文本: {title}");

                // 检查本地化数据
                var localizationDataField = helper.GetType().GetField(
                    "_localizationData", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var localizationData = localizationDataField?.GetValue(helper) as Dictionary<int, Dictionary<string, string>>;
                // Console.WriteLine($"本地化数据: {localizationData != null}");

                if (localizationData != null)
                {
                    // Console.WriteLine($"本地化数据条目数: {localizationData.Count}");
                    if (localizationData.ContainsKey(10001))
                    {
                        // Console.WriteLine($"找到signId=10001的翻译数据");
                        var translations = localizationData[10001];
                        // foreach (var lang in translations.Keys)
                        // {
                        //     Console.WriteLine($"{lang}: {translations[lang]}");
                        // }
                    }
                    else
                    {
                        // Console.WriteLine("未找到signId=10001的翻译数据");
                    }
                }

                // 调试信息：输出当前语言
                (App.Current as App)?.LogMessage($"当前语言: {currentLanguage}");

                // 调试信息：检查本地化数据中的翻译
                if (localizationData != null && localizationData.ContainsKey(10001))
                {
                    var translations = localizationData[10001];
                    
                    // 输出所有可用的翻译
                    foreach (var lang in translations.Keys)
                    {
                        Console.WriteLine($"[调试] 语言: {lang}, 翻译: {translations[lang]}");
                    }
                    
                    // 直接测试中文输出
                    Console.WriteLine($"[调试] 直接输出中文: 测试中文显示");
                    
                    // 优先使用中文翻译
                    if (translations.ContainsKey("zh-cn"))
                    {
                        string zhTranslation = translations["zh-cn"];
                        (App.Current as App)?.LogMessage($"本地化数据中的中文翻译: {zhTranslation}");
                        title = zhTranslation;
                    }
                    // 如果没有中文翻译，尝试使用ghYh字段
                    else if (translations.ContainsKey("gh-yh"))
                    {
                        string ghTranslation = translations["gh-yh"];
                        (App.Current as App)?.LogMessage($"未找到中文翻译，使用ghYh字段: {ghTranslation}");
                        title = ghTranslation;
                    }
                    else
                    {
                        (App.Current as App)?.LogMessage("本地化数据中未找到zh-cn翻译和gh-yh字段");
                    }
                }

                // 设置标题
                this.Title = title;
                TitleText = title; // 更新绑定的属性
                (App.Current as App)?.LogMessage($"标题已设置为: {title}");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置标题时出错: {ex.Message}");
                (App.Current as App)?.LogMessage($"设置标题时出错: {ex.Message}");
                this.Title = "图像识别应用";
                TitleText = "图像识别应用"; // 更新绑定的属性
            }



        InitializeKeyboardShortcuts();
        EnsureScriptDirectoryExists();
        // 禁用窗口边缘拉伸
        this.ResizeMode = ResizeMode.NoResize;
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
        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            _pythonProcess.Kill();
            _pythonProcess.Dispose();
        }
        _isExecuting = false;
    }
    private void MinimizeWindow(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
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
            return;
        }

        if (_isExecuting)
        {
            StopScriptExecution();
            return;
        }

        _isExecuting = true;
        _pythonProcess = new Process();
        _pythonProcess.StartInfo.FileName = _pythonPath;
        _pythonProcess.StartInfo.Arguments = $"\"{_scriptPath}\" run \"{_latestScriptPath}\"";
        _pythonProcess.StartInfo.UseShellExecute = false;
        _pythonProcess.StartInfo.RedirectStandardOutput = true;
        _pythonProcess.StartInfo.RedirectStandardError = true;
        _pythonProcess.Start();
        _pythonProcess.BeginOutputReadLine();
        _pythonProcess.BeginErrorReadLine();
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 释放Python进程
        if (_pythonProcess != null)
        {
            _pythonProcess.Kill();
            _pythonProcess.Dispose();
            _pythonProcess = null;
        }
        
        // 释放定时器
        if (_recordingTimer != null)
        {
            _recordingTimer.Stop();
            _recordingTimer.Tick -= RecordingTimer_Tick;
            _recordingTimer = null;
        }
        
        // 清除缓存
        _imageCache.Clear();
        
        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
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