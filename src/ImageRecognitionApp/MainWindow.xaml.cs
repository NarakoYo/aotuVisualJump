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
using System.Windows.Media.Animation;
using System.ComponentModel;
using ImageRecognitionApp.WinFun;  // 导入WinFun命名空间
using ImageRecognitionApp.unit;     // 导入unit命名空间
using ImageRecognitionApp.Assets.UI;

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

    // 设置按钮图标路径属性
    private string _settingButtonIconPath = string.Empty;
    public string SettingButtonIconPath
    {
        get => _settingButtonIconPath;
        set
        {
            if (_settingButtonIconPath != value)
            {
                _settingButtonIconPath = value;
                OnPropertyChanged(nameof(SettingButtonIconPath));
            }
        }
    }

    // Logo图片路径属性
    private string _logoImagePath = string.Empty;
    public string LogoImagePath
    {
        get => _logoImagePath;
        set
        {
            if (_logoImagePath != value)
            {
                _logoImagePath = value;
                OnPropertyChanged(nameof(LogoImagePath));
            }
        }
    }

    // 侧边栏按钮文本属性
    private string _launchButtonText = string.Empty;
    public string LaunchButtonText
    {
        get => _launchButtonText;
        set
        {
            if (_launchButtonText != value)
            {
                _launchButtonText = value;
                OnPropertyChanged(nameof(LaunchButtonText));
            }
        }
    }

    private string _visualScriptButtonText = string.Empty;
    public string VisualScriptButtonText
    {
        get => _visualScriptButtonText;
        set
        {
            if (_visualScriptButtonText != value)
            {
                _visualScriptButtonText = value;
                OnPropertyChanged(nameof(VisualScriptButtonText));
            }
        }
    }

    private string _auxiliaryOperationButtonText = string.Empty;
    public string AuxiliaryOperationButtonText
    {
        get => _auxiliaryOperationButtonText;
        set
        {
            if (_auxiliaryOperationButtonText != value)
            {
                _auxiliaryOperationButtonText = value;
                OnPropertyChanged(nameof(AuxiliaryOperationButtonText));
            }
        }
    }

    private string _screenRecordingButtonText = string.Empty;
    public string ScreenRecordingButtonText
    {
        get => _screenRecordingButtonText;
        set
        {
            if (_screenRecordingButtonText != value)
            {
                _screenRecordingButtonText = value;
                OnPropertyChanged(nameof(ScreenRecordingButtonText));
            }
        }
    }

    private string _shortcutKeysButtonText = string.Empty;
    public string ShortcutKeysButtonText
    {
        get => _shortcutKeysButtonText;
        set
        {
            if (_shortcutKeysButtonText != value)
            {
                _shortcutKeysButtonText = value;
                OnPropertyChanged(nameof(ShortcutKeysButtonText));
            }
        }
    }

    private string _directoryManagementButtonText = string.Empty;
    public string DirectoryManagementButtonText
    {
        get => _directoryManagementButtonText;
        set
        {
            if (_directoryManagementButtonText != value)
            {
                _directoryManagementButtonText = value;
                OnPropertyChanged(nameof(DirectoryManagementButtonText));
            }
        }
    }

    private string _consoleButtonText = string.Empty;
    public string ConsoleButtonText
    {
        get => _consoleButtonText;
        set
        {
            if (_consoleButtonText != value)
            {
                _consoleButtonText = value;
                OnPropertyChanged(nameof(ConsoleButtonText));
            }
        }
    }

    private string _toolButtonText = string.Empty;
    public string ToolButtonText
    {
        get => _toolButtonText;
        set
        {
            if (_toolButtonText != value)
            {
                _toolButtonText = value;
                OnPropertyChanged(nameof(ToolButtonText));
            }
        }
    }

    // 下拉菜单按钮图标路径属性
    private string _dropdownMenuButtonIconPath = string.Empty;
    public string DropdownMenuButtonIconPath
    {
        get => _dropdownMenuButtonIconPath;
        set
        {
            if (_dropdownMenuButtonIconPath != value)
            {
                _dropdownMenuButtonIconPath = value;
                OnPropertyChanged(nameof(DropdownMenuButtonIconPath));
            }
        }
    }

    // 窗口控制按钮图标路径属性
    private string _hideToTrayButtonIconPath = string.Empty;
    public string HideToTrayButtonIconPath
    {
        get => _hideToTrayButtonIconPath;
        set
        {
            if (_hideToTrayButtonIconPath != value)
            {
                _hideToTrayButtonIconPath = value;
                OnPropertyChanged(nameof(HideToTrayButtonIconPath));
            }
        }
    }

    private string _minimizeWindowButtonIconPath = string.Empty;
    public string MinimizeWindowButtonIconPath
    {
        get => _minimizeWindowButtonIconPath;
        set
        {
            if (_minimizeWindowButtonIconPath != value)
            {
                _minimizeWindowButtonIconPath = value;
                OnPropertyChanged(nameof(MinimizeWindowButtonIconPath));
            }
        }
    }

    private string _closeWindowButtonIconPath = string.Empty;
    public string CloseWindowButtonIconPath
    {
        get => _closeWindowButtonIconPath;
        set
        {
            if (_closeWindowButtonIconPath != value)
            {
                _closeWindowButtonIconPath = value;
                OnPropertyChanged(nameof(CloseWindowButtonIconPath));
            }
        }
    }

    // 侧边栏按钮图标路径属性
    private string _launchButtonIconPath = string.Empty;
    public string LaunchButtonIconPath
    {
        get => _launchButtonIconPath;
        set
        {
            if (_launchButtonIconPath != value)
            {
                _launchButtonIconPath = value;
                OnPropertyChanged(nameof(LaunchButtonIconPath));
            }
        }
    }

    private string _visualScriptButtonIconPath = string.Empty;
    public string VisualScriptButtonIconPath
    {
        get => _visualScriptButtonIconPath;
        set
        {
            if (_visualScriptButtonIconPath != value)
            {
                _visualScriptButtonIconPath = value;
                OnPropertyChanged(nameof(VisualScriptButtonIconPath));
            }
        }
    }

    private string _auxiliaryOperationButtonIconPath = string.Empty;
    public string AuxiliaryOperationButtonIconPath
    {
        get => _auxiliaryOperationButtonIconPath;
        set
        {
            if (_auxiliaryOperationButtonIconPath != value)
            {
                _auxiliaryOperationButtonIconPath = value;
                OnPropertyChanged(nameof(AuxiliaryOperationButtonIconPath));
            }
        }
    }

    private string _screenRecordingButtonIconPath = string.Empty;
    public string ScreenRecordingButtonIconPath
    {
        get => _screenRecordingButtonIconPath;
        set
        {
            if (_screenRecordingButtonIconPath != value)
            {
                _screenRecordingButtonIconPath = value;
                OnPropertyChanged(nameof(ScreenRecordingButtonIconPath));
            }
        }
    }

    private string _shortcutKeysButtonIconPath = string.Empty;
    public string ShortcutKeysButtonIconPath
    {
        get => _shortcutKeysButtonIconPath;
        set
        {
            if (_shortcutKeysButtonIconPath != value)
            {
                _shortcutKeysButtonIconPath = value;
                OnPropertyChanged(nameof(ShortcutKeysButtonIconPath));
            }
        }
    }

    private string _directoryManagementButtonIconPath = string.Empty;
    public string DirectoryManagementButtonIconPath
    {
        get => _directoryManagementButtonIconPath;
        set
        {
            if (_directoryManagementButtonIconPath != value)
            {
                _directoryManagementButtonIconPath = value;
                OnPropertyChanged(nameof(DirectoryManagementButtonIconPath));
            }
        }
    }

    private string _consoleButtonIconPath = string.Empty;
    public string ConsoleButtonIconPath
    {
        get => _consoleButtonIconPath;
        set
        {
            if (_consoleButtonIconPath != value)
            {
                _consoleButtonIconPath = value;
                OnPropertyChanged(nameof(ConsoleButtonIconPath));
            }
        }
    }

    private string _toolButtonIconPath = string.Empty;
    public string ToolButtonIconPath
    {
        get => _toolButtonIconPath;
        set
        {
            if (_toolButtonIconPath != value)
            {
                _toolButtonIconPath = value;
                OnPropertyChanged(nameof(ToolButtonIconPath));
            }
        }
    }

    // 本地化工具
    private string _collapseButtonIconPath = string.Empty;
    public string CollapseButtonIconPath
    {
        get => _collapseButtonIconPath;
        set
        {
            if (_collapseButtonIconPath != value)
            {
                _collapseButtonIconPath = value;
                OnPropertyChanged(nameof(CollapseButtonIconPath));
            }
        }
    }

    private string _collapseButtonText = string.Empty;
    public string CollapseButtonText
    {
        get => _collapseButtonText;
        set
        {
            if (_collapseButtonText != value)
            {
                _collapseButtonText = value;
                OnPropertyChanged(nameof(CollapseButtonText));
            }
        }
    }

    private JsonLocalizationHelper _localizationHelper => JsonLocalizationHelper.Instance;

    // 实现INotifyPropertyChanged接口
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 下拉菜单按钮点击事件
    private void DropdownMenuButton_Click(object sender, RoutedEventArgs e)
    {
        DropdownMenuPopup.IsOpen = !DropdownMenuPopup.IsOpen;
    }

    // 标题栏右键点击事件处理
    private void TitleBar_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 允许在标题栏非按钮区域显示系统菜单
        e.Handled = false;
    }

    // 系统信息按钮点击事件
    private void SystemInfoButton_Click(object sender, RoutedEventArgs e)
    {
        DropdownMenuPopup.IsOpen = false;

        try
        {
            // 创建系统信息管理器并显示系统信息窗口
            ImageRecognitionApp.Assets.UICode.SystemInfoManager systemInfoManager = new ImageRecognitionApp.Assets.UICode.SystemInfoManager();
            systemInfoManager.ShowSystemInfoWindow();
        }
        catch (Exception ex)
        {
            // 记录错误
            (App.Current as App)?.LogMessage($"显示系统信息时出错: {ex.Message}");
            MessageBox.Show("显示系统信息时出错: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 关于按钮点击事件
    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        DropdownMenuPopup.IsOpen = false;
        // 这里添加关于按钮的具体实现
        MessageBox.Show("关于功能尚未实现", "提示");
    }

    // 跟踪设置按钮是否被点击
    private bool _isSettingButtonClicked = false;

    // Python相关路径
    private readonly string _pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\PythonScripts\\venv\\Scripts\\python.exe");

    // 窗口构造函数
    // 全局右键点击事件处理
    private void MainWindow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 只有在自定义标题栏非按钮区域才允许右键菜单显示
        // 检查点击位置是否在标题栏内
        var titleBar = this.FindName("TitleBar") as Border;
        if (titleBar != null)
        {
            // 获取鼠标在标题栏内的位置
            Point mousePositionInTitleBar = e.GetPosition(titleBar);

            // 检查点击是否在标题栏内（相对于标题栏的坐标）
            if (mousePositionInTitleBar.X >= 0 &&
                mousePositionInTitleBar.Y >= 0 &&
                mousePositionInTitleBar.X <= titleBar.ActualWidth &&
                mousePositionInTitleBar.Y <= titleBar.ActualHeight)
            {
                // 检查是否点击在按钮上
                DependencyObject clickedElement = e.OriginalSource as DependencyObject;
                if (clickedElement != null)
                {
                    // 向上遍历视觉树，检查是否在按钮或按钮子元素上
                    Button button = FindVisualParent<Button>(clickedElement);
                    if (button == null)
                    {
                        // 非按钮区域，允许事件传递到标题栏的MouseRightButtonDown处理程序
                        e.Handled = false;
                        return;
                    }
                }
            }
        }

        // 非标题栏区域或标题栏中的按钮区域，阻止右键菜单
        e.Handled = true;
    }

    /// <summary>
    /// 查找视觉树中的父元素
    /// </summary>
    private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null)
            return null;

        T parent = parentObject as T;
        if (parent != null)
            return parent;
        else
            return FindVisualParent<T>(parentObject);
    }

    // 折叠按钮点击事件处理程序
    private void CollapseButton_Click(object sender, RoutedEventArgs e)
    {
        // 这里实现侧边栏折叠功能
        // 可以通过修改侧边栏的宽度来实现折叠效果
        var sidebar = this.FindName("Sidebar") as Border;
        if (sidebar != null)
        {
            if (sidebar.Width == 200)
            {
                // 折叠侧边栏
                sidebar.Width = 50;
            }
            else
            {
                // 展开侧边栏
                sidebar.Width = 200;
            }
        }
    }

    // 设置按钮点击事件处理程序
    private void SettingButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button != null && button.Template != null)
        {
            var border = button.Template.FindName("border", button) as Border;
            if (border != null)
            {
                // 检查按钮是否已经被选中
                bool isAlreadySelected = border.Tag != null && border.Tag.ToString() == "Selected";
                
                // 如果按钮已经被选中，则不执行任何操作
                if (isAlreadySelected)
                {
                    return;
                }
            }
        }
        
        // 重置所有侧边栏按钮状态（包括其他按钮和设置按钮）
        ResetAllSidebarButtonsState();

        // 设置当前点击按钮的状态
        if (button != null && button.Template != null)
        {
            var border = button.Template.FindName("border", button) as Border;
            if (border != null)
            {
                // 使用Tag属性来标记按钮被选中
                border.Tag = "Selected";
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37373D"));
            }
            
            // 应用选中动画效果
            ApplyButtonAnimation(button, true);
        }

        // 跟踪设置按钮被点击状态
        _isSettingButtonClicked = true;
    }

    // 更新设置按钮背景色
    private void UpdateSettingButtonBackground()
    {
        if (SettingButton != null && SettingButton.Template != null)
        {
            var border = SettingButton.Template.FindName("border", SettingButton) as Border;
            if (border != null)
            {
                if (_isSettingButtonClicked)
                {
                    // 只有在按钮被选中时才直接设置背景色
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37373D"));
                }
                else
                {
                    // 清除直接设置的背景色，恢复样式触发器的控制权
                    border.ClearValue(Border.BackgroundProperty);
                }
            }
        }
    }

    // 重置设置按钮状态（供其他按钮点击事件调用）
    public void ResetSettingButtonState()
    {
        _isSettingButtonClicked = false;

        // 清除设置按钮的选中标记
        if (SettingButton != null && SettingButton.Template != null)
        {
            var border = SettingButton.Template.FindName("border", SettingButton) as Border;
            if (border != null)
            {
                // 检查按钮是否被选中
                bool wasSelected = border.Tag != null && border.Tag.ToString() == "Selected";
                
                border.Tag = null;
                
                // 如果按钮之前是选中状态，应用取消选中的动画效果
                if (wasSelected)
                {
                    ApplyButtonAnimation(SettingButton, false);
                }
            }
        }

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
    private double _restoreWidth = 0; // 新增：存储窗口还原宽度
    private double _restoreHeight = 0; // 新增：存储窗口还原高度

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

        // 添加全局右键点击事件处理，阻止非标题栏区域的右键菜单
        this.PreviewMouseRightButtonDown += MainWindow_PreviewMouseRightButtonDown;

        // 初始化设置按钮状态
        _isSettingButtonClicked = false;

        // 设置窗口最大尺寸，确保不覆盖任务栏
        this.MaxHeight = SystemParameters.WorkArea.Height;
        this.MaxWidth = SystemParameters.WorkArea.Width;

        // 初始化AssetHelper并获取按钮图标路径
        try
        {
            var assetHelper = AssetHelper.Instance;

            // 设置窗口图标和Logo图片（通过AssetHelper获取sign_id=10001的图片资产）
            try
            {
                // 直接使用AssetHelper获取图片资源，而不是只获取路径然后创建BitmapImage
                System.Windows.Media.Imaging.BitmapImage logoImage = assetHelper.GetImageAsset(10001);
                this.Icon = logoImage;
                LogoImagePath = assetHelper.GetAssetPath(10001); // 设置标题栏Logo图片路径
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"设置窗口图标失败: {ex.Message}");
            }

            // 设置按钮图标路径
            string settingIconPath = assetHelper.GetAssetPath(10003);
            SettingButtonIconPath = settingIconPath;

            // 设置下拉菜单按钮图标路径
            string dropdownMenuIconPath = assetHelper.GetAssetPath(10004);
            DropdownMenuButtonIconPath = dropdownMenuIconPath;

            // 设置窗口控制按钮图标路径
            HideToTrayButtonIconPath = assetHelper.GetAssetPath(20006);
            MinimizeWindowButtonIconPath = assetHelper.GetAssetPath(20005);
            CloseWindowButtonIconPath = assetHelper.GetAssetPath(20004);

            // 侧边栏按钮图标路径
            LaunchButtonIconPath = assetHelper.GetAssetPath(10006);
            VisualScriptButtonIconPath = assetHelper.GetAssetPath(10007);
            AuxiliaryOperationButtonIconPath = assetHelper.GetAssetPath(10008);
            ScreenRecordingButtonIconPath = assetHelper.GetAssetPath(10009);
            ShortcutKeysButtonIconPath = assetHelper.GetAssetPath(10010);
            DirectoryManagementButtonIconPath = assetHelper.GetAssetPath(10011);
            ConsoleButtonIconPath = assetHelper.GetAssetPath(10012);
            ToolButtonIconPath = assetHelper.GetAssetPath(10013);
            CollapseButtonIconPath = assetHelper.GetAssetPath(10014);

            // 设置启动按钮默认选中状态
            this.Loaded += (sender, e) =>
            {
                if (LaunchButton != null && LaunchButton.Template != null)
                {
                    var border = LaunchButton.Template.FindName("border", LaunchButton) as Border;
                    if (border != null)
                    {
                        // 添加选中标记
                        border.Tag = "Selected";
                        border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37373D"));
                        
                        // 应用选中动画
                        ApplyButtonAnimation(LaunchButton, true);
                    }
                }
            };
            
        }
        catch (Exception ex)
        {
            (App.Current as App)?.LogMessage($"获取设置按钮图标路径失败: {ex.Message}");
            // 设置默认图标路径
            // SettingButtonIconPath = "pack://application:,,,/Resources/Icons/igoutu/setting-gear.png";
        }

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

                // 设置下拉菜单按钮文本
                SystemInfoButton.Content = _localizationHelper.GetString(10005);
                AboutButton.Content = _localizationHelper.GetString(10004);
            }

            // 设置窗口标题
            this.Title = TitleText;

            // 获取侧边栏按钮文本
            LaunchButtonText = helper.GetString(10006);
            VisualScriptButtonText = helper.GetString(10007);
            AuxiliaryOperationButtonText = helper.GetString(10008);
            ScreenRecordingButtonText = helper.GetString(10009);
            ShortcutKeysButtonText = helper.GetString(10010);
            DirectoryManagementButtonText = helper.GetString(10011);
            ConsoleButtonText = helper.GetString(10012);
            ToolButtonText = helper.GetString(10013);
            CollapseButtonText = helper.GetString(10014);

            // 记录当前语言
            // (App.Current as App)?.LogMessage($"当前语言: {currentLanguage}");
            // (App.Current as App)?.LogMessage($"标题已设置为: {TitleText}");

        }
        catch (Exception ex)
        {
            (App.Current as App)?.LogMessage($"设置标题和按钮文本时出错: {ex.Message}");
            this.Title = "图像识别应用";
            TitleText = "图像识别应用";

            // 设置默认按钮文本
            SettingButtonText = "设置";
            LaunchButtonText = "启动";
            VisualScriptButtonText = "视觉脚本";
            AuxiliaryOperationButtonText = "辅助操控";
            ScreenRecordingButtonText = "屏幕录制";
            ShortcutKeysButtonText = "快捷键";
            DirectoryManagementButtonText = "目录管理";
            ConsoleButtonText = "控制台";
            ToolButtonText = "小工具";
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
        // 停止脚本执行
        StopScriptExecution();
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
            // (App.Current as App)?.LogMessage($"任务栏提示文本已更新为: {TitleText}");
        }
    }

    /// <summary>
    /// 应用于按钮的平滑动画效果
    /// </summary>
    private void ApplyButtonAnimation(Button button, bool isSelected)
    {
        if (button == null || button.Template == null) return;

        var contentGrid = button.Template.FindName("contentGrid", button) as Grid;
        if (contentGrid == null) return;

        var stackPanel = FindVisualChild<StackPanel>(contentGrid);
        if (stackPanel == null) return;

        // 停止任何正在进行的动画
        stackPanel.BeginAnimation(FrameworkElement.MarginProperty, null);
        stackPanel.BeginAnimation(FrameworkElement.RenderTransformProperty, null);

        // 设置RenderTransform（如果不存在）
        if (stackPanel.RenderTransform == null || !(stackPanel.RenderTransform is TransformGroup))
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new TranslateTransform());
            stackPanel.RenderTransform = transformGroup;
        }

        // 获取TranslateTransform
        var translateTransform = (stackPanel.RenderTransform as TransformGroup).Children[0] as TranslateTransform;
        if (translateTransform == null) return;

        // 计算平移量，设置为半个折叠按钮宽度的2/3 (40/2*2/3≈13.33)
        double targetX = isSelected ? 13.33 : 0;
        double currentX = translateTransform.X;

        // 创建动画
        var animation = new DoubleAnimation
        {
            From = currentX,
            To = targetX,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            FillBehavior = FillBehavior.HoldEnd,
            EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
        };

        // 应用动画
        translateTransform.BeginAnimation(TranslateTransform.XProperty, animation);

        // 处理左边框动画
        var leftBorder = contentGrid.FindName("LeftBorder") as Border;
        if (leftBorder != null)
        {
            // 停止任何正在进行的左边框动画
            leftBorder.BeginAnimation(Border.WidthProperty, null);
            leftBorder.BeginAnimation(Border.HeightProperty, null);
            leftBorder.BeginAnimation(Border.OpacityProperty, null);

            // 确保边框可见
            leftBorder.Visibility = Visibility.Visible;

            if (isSelected)
            {
                // 选中时：线从中间向两边延伸
                // 确保基础属性设置正确
                leftBorder.Visibility = Visibility.Visible; // 强制可见
                leftBorder.Width = 4; // 设置宽度
                leftBorder.Opacity = 1; // 完全不透明

                // 获取当前高度，作为动画的起始值
                double fromHeight = leftBorder.ActualHeight;

                // 停止任何正在进行的动画
                leftBorder.BeginAnimation(Border.HeightProperty, null);

                // 为当前动画创建一个唯一标记，以防止旧动画影响新状态
                string animationTag = "SelectedAnimation" + DateTime.Now.Ticks;
                leftBorder.Tag = animationTag;

                // 创建高度动画（从中间向两边延伸的效果）
                var heightAnimation = new DoubleAnimation
                {
                    From = fromHeight,
                    To = 22.5, // 最终高度
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    FillBehavior = FillBehavior.HoldEnd,
                    EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
                };

                // 添加完成事件处理程序
                heightAnimation.Completed += (s, eArgs) =>
                {
                    // 检查动画标记，只有当它仍然是选中动画的标记时才执行操作
                    if (leftBorder.Tag != null && leftBorder.Tag.ToString() == animationTag)
                    {
                        // 确保边框保持在最终状态
                        leftBorder.Height = 22.5;
                        leftBorder.Width = 4;
                        leftBorder.Opacity = 1;
                        leftBorder.Visibility = Visibility.Visible;
                        leftBorder.Tag = null; // 清除标记
                    }
                };

                // 应用高度动画
                leftBorder.BeginAnimation(Border.HeightProperty, heightAnimation);
            }
            else
            {
                // 取消选中时：线继续延伸围绕按钮并且同时逐渐变细后消失
                // 先创建一个故事板来协调多个动画
                var storyboard = new Storyboard();

                // 高度动画 - 继续延伸
                var heightAnimation = new DoubleAnimation
                {
                    From = leftBorder.ActualHeight,
                    To = 45, // 按钮完整高度
                    Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                    FillBehavior = FillBehavior.HoldEnd
                };
                Storyboard.SetTarget(heightAnimation, leftBorder);
                Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(Border.HeightProperty));
                storyboard.Children.Add(heightAnimation);

                // 宽度动画 - 同时逐渐变细
                var widthAnimation = new DoubleAnimation
                {
                    From = leftBorder.ActualWidth,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                    FillBehavior = FillBehavior.HoldEnd
                };
                Storyboard.SetTarget(widthAnimation, leftBorder);
                Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Border.WidthProperty));
                storyboard.Children.Add(widthAnimation);

                // 透明度动画 - 逐渐消失
                var opacityAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                    FillBehavior = FillBehavior.HoldEnd
                };
                Storyboard.SetTarget(opacityAnimation, leftBorder);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Border.OpacityProperty));
                storyboard.Children.Add(opacityAnimation);

                // 为当前动画创建一个唯一标记，以防止旧动画影响新状态
                string animationTag = "DeselectedAnimation" + DateTime.Now.Ticks;
                leftBorder.Tag = animationTag;

                // 动画完成后隐藏边框
                storyboard.Completed += (s, eArgs) =>
                {
                    // 检查动画标记，只有当它仍然是取消选中动画的标记时才执行重置
                    // 这可以防止旧的动画完成事件影响新的选中状态
                    if (leftBorder.Tag != null && leftBorder.Tag.ToString() == animationTag)
                    {
                        leftBorder.Visibility = Visibility.Collapsed;
                        leftBorder.Opacity = 1; // 重置透明度，以便下次显示
                        leftBorder.Width = 4; // 重置宽度
                        leftBorder.Height = 0; // 重置高度
                        leftBorder.Tag = null; // 清除标记
                    }
                };

                // 应用故事板动画
                storyboard.Begin();
            }
        }
    }

    /// <summary>
    /// 查找视觉树中的子元素
    /// </summary>
    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T childElement)
                return childElement;
            
            T descendant = FindVisualChild<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    /// <summary>
    /// 侧边栏按钮点击事件处理程序
    /// </summary>
    private void SidebarButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button != null && button.Template != null)
        {
            var border = button.Template.FindName("border", button) as Border;
            if (border != null)
            {
                // 检查按钮是否已经被选中
                bool isAlreadySelected = border.Tag != null && border.Tag.ToString() == "Selected";
                
                // 如果按钮已经被选中，则不执行任何操作
                if (isAlreadySelected)
                {
                    return;
                }
            }
        }
        
        // 重置所有按钮状态
        ResetAllSidebarButtonsState();

        // 设置当前点击按钮的状态
        if (button != null && button.Template != null)
        {
            var border = button.Template.FindName("border", button) as Border;
            if (border != null)
            {
                // 添加一个标记来表示按钮被选中
                border.Tag = "Selected";
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37373D"));
            }
            
            // 应用选中动画效果
            ApplyButtonAnimation(button, true);
        }

        // 按钮点击后的具体逻辑 - 根据按钮名称显示不同的内容
        if (button.Name == "ScreenRecordingButton")
        {
            try
            {
                // 创建屏幕录制控件实例
                ScreenRecordingControl recordingControl = new ScreenRecordingControl();
                
                // 将控件显示在主内容区域
                MainContentControl.Content = recordingControl;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("加载屏幕录制功能失败：" + ex.Message);
                System.Windows.MessageBox.Show("加载屏幕录制功能失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            // 对于其他按钮，这里可以添加相应的逻辑
            // 当前仅实现了捕捉录制系统按钮的功能
            MainContentControl.Content = null;
        }
    }

    /// <summary>
    /// 重置所有侧边栏按钮的状态
    /// </summary>
    private void ResetAllSidebarButtonsState()
    {
        // 重置设置按钮状态
        ResetSettingButtonState();

        // 重置其他侧边栏按钮状态
        ResetButtonState(LaunchButton);
        ResetButtonState(VisualScriptButton);
        ResetButtonState(AuxiliaryOperationButton);
        ResetButtonState(ScreenRecordingButton);
        ResetButtonState(ShortcutKeysButton);
        ResetButtonState(DirectoryManagementButton);
        ResetButtonState(ConsoleButton);
        ResetButtonState(ToolButton);
    }

    /// <summary>
    /// 重置单个按钮的状态
    /// </summary>
    private void ResetButtonState(Button button)
    {
        if (button != null && button.Template != null)
        {
            var border = button.Template.FindName("border", button) as Border;
            if (border != null)
            {
                // 检查按钮是否被选中
                bool wasSelected = border.Tag != null && border.Tag.ToString() == "Selected";
                
                // 只清除选中标记，不直接设置背景色，让XAML样式触发器自然应用效果
                border.Tag = null;
                // 移除直接设置的背景色，恢复样式触发器的控制权
                border.ClearValue(Border.BackgroundProperty);
                
                // 如果按钮之前是选中状态，应用取消选中的动画效果
                if (wasSelected)
                {
                    ApplyButtonAnimation(button, false);
                }
            }
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
        // 使用TaskbarManager的MinimizeToTaskbar方法将窗口最小化到任务栏
        // 该方法会保持ShowInTaskbar = true，确保任务栏缩略图预览功能正常工作
        _taskbarManager?.MinimizeToTaskbar();
    }

    // 隐藏至托盘
    private void HideToTray(object sender, RoutedEventArgs e)
    {
        // 使用TaskbarManager的HideWindowToTray方法隐藏窗口到托盘
        // 该方法会正确设置_windowMinimizedToTray标志
        _taskbarManager?.HideWindowToTray();

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

    private void CollapseButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // 创建ToolTip对象
            ToolTip tooltip = new ToolTip();
            tooltip.Content = unit.JsonLocalizationHelper.Instance.GetString(10014);

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
            if (this.WindowState == WindowState.Maximized)
            {
                if (e.ClickCount == 2)
                {
                    // 双击标题栏：仅使用保存的还原点和尺寸，不基于鼠标当前位置
                    this.WindowState = WindowState.Normal;
                    _isMaximized = false;
                    
                    // 严格使用之前保存的窗口位置和尺寸信息
                    if (!double.IsNaN(_restorePoint.X) && !double.IsNaN(_restorePoint.Y) && _restoreWidth > 0 && _restoreHeight > 0)
                    {
                        this.Left = _restorePoint.X;
                        this.Top = _restorePoint.Y;
                        this.Width = _restoreWidth;
                        this.Height = _restoreHeight;
                        (App.Current as App)?.LogMessage("双击标题栏，窗口准确还原到最大化前的位置和尺寸：(" + _restorePoint.X + ", " + _restorePoint.Y + ")，尺寸：" + _restoreWidth + "x" + _restoreHeight);
                    }
                    else
                    {
                        (App.Current as App)?.LogMessage("双击标题栏，窗口还原到正常状态（无保存的位置信息）");
                    }
                }
                else
                {
                    // 单击操作：直接拖动窗口，保持最大化状态不变
                    // 直接调用DragMove，Windows会自动处理最大化窗口的拖动行为
                    DragMove();
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

    // 新增：窗口状态变化事件处理 - 确保窗口状态正确处理
    private void Window_StateChanged(object sender, EventArgs e)
    {
        // 获取窗口主容器的Border元素
        var mainBorder = FindName("mainBorder") as Border;
        var titleBar = FindName("TitleBar") as Border;
        var backgroundBorder = FindName("backgroundBorder") as Border;
        
        if (WindowState == WindowState.Maximized)
        {
            // 保存窗口还原位置和尺寸
            if (!_isMaximized)
            {
                _restorePoint = new Point(this.Left, this.Top);
                _restoreWidth = this.Width;
                _restoreHeight = this.Height;
                (App.Current as App)?.LogMessage("窗口最大化，保存还原位置和尺寸：(" + _restorePoint.X + ", " + _restorePoint.Y + ")，尺寸：" + _restoreWidth + "x" + _restoreHeight);
            }
            _isMaximized = true;
            
            // 窗口最大化时，移除圆角效果
            if (mainBorder != null) mainBorder.CornerRadius = new CornerRadius(0);
            if (titleBar != null) titleBar.CornerRadius = new CornerRadius(0);
            if (backgroundBorder != null) backgroundBorder.CornerRadius = new CornerRadius(0);
            (App.Current as App)?.LogMessage("窗口已最大化，移除圆角效果");
        }
        else if (WindowState == WindowState.Normal)
        {
            // 窗口恢复正常状态时，恢复圆角效果
            if (mainBorder != null) mainBorder.CornerRadius = new CornerRadius(14);
            if (titleBar != null) titleBar.CornerRadius = new CornerRadius(14, 14, 0, 0);
            if (backgroundBorder != null) backgroundBorder.CornerRadius = new CornerRadius(18);
            (App.Current as App)?.LogMessage("窗口恢复正常，恢复圆角效果");
        }
        else if (WindowState == WindowState.Minimized)
        {
            // 确保窗口在最小化状态下仍然显示在任务栏中
            ShowInTaskbar = true;
            (App.Current as App)?.LogMessage("窗口已最小化，保持在任务栏显示");
        }
    }

    // 新增：窗口激活事件处理 - 确保窗口被正确激活
    private void Window_Activated(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // 如果被激活时是最小化状态，则恢复为正常状态
            WindowState = WindowState.Normal;
            (App.Current as App)?.LogMessage("窗口被激活，从最小化状态恢复");
        }
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