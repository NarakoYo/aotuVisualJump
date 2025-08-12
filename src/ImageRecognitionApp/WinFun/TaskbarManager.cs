using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing; // 添加对System.Drawing的引用
using ImageRecognitionApp.WinFun;

namespace ImageRecognitionApp.WinFun
{
    /// <summary>
    /// 任务栏管理器，负责处理任务栏图标的交互功能
    /// </summary>
    public class TaskbarManager : IDisposable
    {
        #region 字段定义

    private IntPtr _windowHandle;
    private readonly uint _taskbarIconId = 1001; // 设置默认图标ID
    private NOTIFYICONDATA _notifyIconData;
    private IntPtr _iconHandle = IntPtr.Zero; // 存储图标句柄以便释放
    private TaskbarAnimation? _taskbarAnimation; // 任务栏动画引用

    #endregion

        #region Win32 API 导入

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("shell32.dll")]
        private static extern int Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern int FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        private static extern IntPtr RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        #endregion

        #region 常量定义

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_INFO = 0x00000010;
        private const uint NIM_SETVERSION = 0x00000004;
        private const uint NOTIFYICON_VERSION_4 = 0x00000004;
        private const uint WM_USER = 0x00000400;
        private const uint WM_NOTIFYICON = WM_USER + 1024;
        private const int IDI_APPLICATION = 0x7F00;

        #endregion

        #region 结构体定义

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            // 其他字段省略，根据需要添加
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="window">WPF窗口</param>
        public TaskbarManager(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window), "窗口不能为空");
            }
            
            // 延迟获取窗口句柄，确保窗口已加载
            window.Loaded += (sender, e) =>
            {
                _windowHandle = new WindowInteropHelper(window).Handle;
                if (_windowHandle == IntPtr.Zero)
                {
                    (App.Current as App)?.LogMessage("TaskbarManager: 窗口句柄获取失败");
                    // 尝试使用主窗口
                    if (System.Windows.Application.Current.MainWindow != null)
                    {
                        _windowHandle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                        (App.Current as App)?.LogMessage($"TaskbarManager: 使用主窗口句柄: {_windowHandle}");
                    }
                }
                else
                {
                    (App.Current as App)?.LogMessage($"TaskbarManager: 窗口句柄已设置: {_windowHandle}");
                }
                
                // 确保窗口句柄有效后再初始化任务栏图标
                if (_windowHandle != IntPtr.Zero)
                {
                    _taskbarAnimation = new TaskbarAnimation(window);
                    InitializeTaskbarIcon();

                    // 注册窗口消息处理
                    HwndSource.FromHwnd(_windowHandle)?.AddHook(WndProc);
                }
                else
                {
                    (App.Current as App)?.LogMessage("TaskbarManager: 无法初始化，窗口句柄无效");
                }
            };
        }

        /// <summary>
        /// 获取窗口图标的Win32句柄
        /// </summary>
        /// <returns>图标句柄</returns>
        private IntPtr GetWindowIconHandle()
        {
            try
            {
                // 尝试从应用程序主窗口获取图标
                var window = System.Windows.Application.Current.MainWindow;
                if (window?.Icon != null)
                {
                    // 将WPF ImageSource转换为System.Drawing.Icon
                    var bitmapSource = window.Icon as BitmapSource;
                    if (bitmapSource != null)
                    {
                        using (var stream = new MemoryStream())
                        {
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                            encoder.Save(stream);
                            stream.Seek(0, SeekOrigin.Begin);

                            using (var bmp = new System.Drawing.Bitmap(stream))
                            {
                                _iconHandle = bmp.GetHicon();
                                return _iconHandle;
                            }
                        }
                    }
                }

                // 如果无法从窗口获取，使用默认应用程序图标
                _iconHandle = LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
                return _iconHandle;
            }
            catch
            {
                // 出错时返回默认应用程序图标
                _iconHandle = LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
                return _iconHandle;
            }
        }

        /// <summary>
        /// 初始化任务栏图标
        /// </summary>
        private void InitializeTaskbarIcon()
        {
            try
            {
                (App.Current as App)?.LogMessage("开始初始化任务栏图标");
                
                // 获取应用程序标题
                string appTitle = "Image Recognition App";
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    appTitle = mainWindow.TitleText;
                }
                else if (System.Windows.Application.Current.MainWindow != null)
                {
                    appTitle = System.Windows.Application.Current.MainWindow.Title;
                }
                
                _notifyIconData = new NOTIFYICONDATA
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                    hWnd = _windowHandle,
                    uID = _taskbarIconId,
                    uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
                    uCallbackMessage = WM_NOTIFYICON,
                    hIcon = GetWindowIconHandle(),
                    szTip = appTitle
                };
                (App.Current as App)?.LogMessage($"任务栏图标配置: hWnd={_windowHandle}, uCallbackMessage={WM_NOTIFYICON}, szTip={appTitle}");

                // 添加任务栏图标
                int addResult = Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
                if (addResult != 0)
                {
                    (App.Current as App)?.LogMessage("任务栏图标添加成功");
                }
                else
                {
                    (App.Current as App)?.LogMessage("任务栏图标添加失败");
                    int errorCode = Marshal.GetLastWin32Error();
                    (App.Current as App)?.LogMessage($"Win32错误代码: {errorCode}");
                }

                // 设置版本
                int versionResult = Shell_NotifyIcon(NIM_SETVERSION, ref _notifyIconData);
                if (versionResult != 0)
                {
                    (App.Current as App)?.LogMessage("任务栏图标版本设置成功");
                }
                else
                {
                    (App.Current as App)?.LogMessage("任务栏图标版本设置失败");
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"初始化任务栏图标错误: {ex.Message}");
                (App.Current as App)?.LogMessage($"错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 窗口消息处理
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                // (App.Current as App)?.LogMessage($"收到窗口消息: 消息ID={msg}, wParam={wParam}, lParam={lParam}");
                
                if (msg == WM_NOTIFYICON)
                {
                    // 处理任务栏图标消息
                    // (App.Current as App)?.LogMessage($"确认是任务栏图标消息: 消息ID={msg}, wParam={wParam}, lParam={lParam}");
                    uint message = (uint)lParam;
                    uint iconId = (uint)wParam;
                    // (App.Current as App)?.LogMessage($"接收到任务栏消息: 类型={message}, 图标ID={iconId}");

                    switch (message)
                    {
                        case 0x0201:  // 鼠标左键点击
                            (App.Current as App)?.LogMessage("确认是鼠标左键点击");
                            OnLeftClick();
                            handled = true;
                            break;
                        case 0x0202:  // 鼠标左键释放
                            (App.Current as App)?.LogMessage("收到鼠标左键释放消息");
                            handled = true;
                            break;
                        case 0x0204:  // 鼠标右键点击
                            (App.Current as App)?.LogMessage("收到鼠标右键点击消息");
                            OnRightClick();
                            handled = true;
                            break;
                        default:
                            (App.Current as App)?.LogMessage($"未知的任务栏消息: {message}");
                            (App.Current as App)?.LogMessage($"收到未知的任务栏图标消息类型: lParam={lParam}, wParam={wParam}");
                            break;
                    }
                }
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"窗口消息处理错误: {ex.Message}");
                (App.Current as App)?.LogMessage($"错误堆栈: {ex.StackTrace}");
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 鼠标左键点击事件
        /// </summary>
        private void OnLeftClick()
        {
            try
            {
                (App.Current as App)?.LogMessage("执行OnLeftClick方法");
                // 切换窗口激活/最小化/隐藏
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    (App.Current as App)?.LogMessage($"窗口当前状态: {mainWindow.WindowState}, 是否激活: {mainWindow.IsActive}, 是否可见: {mainWindow.Visibility}, 窗口句柄: {_windowHandle}");
                    
                    // 如果窗口可见且不是最小化状态，则将其最小化
                    if (mainWindow.Visibility == Visibility.Visible && mainWindow.WindowState != WindowState.Minimized)
                    {
                        (App.Current as App)?.LogMessage("窗口可见且不是最小化状态，准备最小化");
                        mainWindow.WindowState = WindowState.Minimized;
                        (App.Current as App)?.LogMessage("窗口已设置为最小化状态");
                        if (_taskbarAnimation != null)
                        {
                            _taskbarAnimation.JumpDownAnimation();
                            (App.Current as App)?.LogMessage("窗口已最小化，播放向下跳跃动画");
                        }
                        else
                        {
                            (App.Current as App)?.LogMessage("窗口已最小化，但_taskbarAnimation为null，无法播放动画");
                        }
                    }
                    else
                    {
                        // 窗口处于最小化状态或隐藏状态，恢复并激活
                        (App.Current as App)?.LogMessage("窗口处于最小化或隐藏状态，准备恢复");
                        mainWindow.WindowState = WindowState.Normal;
                        (App.Current as App)?.LogMessage("窗口已恢复为正常状态");
                        mainWindow.Visibility = Visibility.Visible;
                        (App.Current as App)?.LogMessage("窗口已设置为可见");
                        mainWindow.ShowInTaskbar = true;
                        (App.Current as App)?.LogMessage("窗口已设置为在任务栏显示");
                        mainWindow.Show();
                        (App.Current as App)?.LogMessage("窗口已显示");
                        mainWindow.Activate();
                        (App.Current as App)?.LogMessage("窗口已激活");
                        // 确保窗口前置
                        bool setForegroundResult = SetForegroundWindow(_windowHandle);
                        (App.Current as App)?.LogMessage($"SetForegroundWindow结果: {setForegroundResult}");
                        if (!setForegroundResult)
                        {
                            (App.Current as App)?.LogMessage("SetForegroundWindow调用失败，尝试其他方法激活窗口");
                            // 尝试通过用户32 API强制激活
                            IntPtr currentForeground = GetForegroundWindow();
                            uint currentThreadId = GetWindowThreadProcessId(currentForeground, IntPtr.Zero);
                            uint ourThreadId = GetCurrentThreadId();
                            
                            (App.Current as App)?.LogMessage($"当前前景窗口: {currentForeground}, 当前线程ID: {currentThreadId}, 我们的线程ID: {ourThreadId}");
                            if (currentThreadId != ourThreadId)
                            {
                                (App.Current as App)?.LogMessage("附加线程输入");
                                AttachThreadInput(currentThreadId, ourThreadId, true);
                                setForegroundResult = SetForegroundWindow(_windowHandle);
                                (App.Current as App)?.LogMessage($"附加线程后SetForegroundWindow结果: {setForegroundResult}");
                                AttachThreadInput(currentThreadId, ourThreadId, false);
                                (App.Current as App)?.LogMessage("分离线程输入");
                            }
                        }
                        if (_taskbarAnimation != null)
                        {
                            _taskbarAnimation.JumpUpAnimation();
                            (App.Current as App)?.LogMessage("窗口已激活，播放向上跳跃动画");
                        }
                        else
                        {
                            (App.Current as App)?.LogMessage("窗口已激活，但_taskbarAnimation为null，无法播放动画");
                        }
                    }
                }
                else
                {
                    (App.Current as App)?.LogMessage("主窗口为空");
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"左键点击处理错误: {ex.Message}");
                (App.Current as App)?.LogMessage($"错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 鼠标右键点击事件
        /// </summary>
        private void OnRightClick()
        {
            // 可以在这里显示上下文菜单
            // 例如：显示"显示窗口"、"退出"等选项
        }

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知消息</param>
        public void ShowNotification(string title, string message)
        {
            _notifyIconData.szInfoTitle = title;
            _notifyIconData.szInfo = message;
            _notifyIconData.uFlags |= NIF_INFO;
            _notifyIconData.dwInfoFlags = 0; // 无图标
            _notifyIconData.uTimeoutOrVersion = 5000; // 5秒后自动消失

            Shell_NotifyIcon(NIM_MODIFY, ref _notifyIconData);
        }

        /// <summary>
        /// 更新任务栏图标的鼠标悬停提示文本
        /// </summary>
        /// <param name="tooltipText">新的提示文本</param>
        public void UpdateTooltip(string tooltipText)
        {
            if (_notifyIconData.szTip != tooltipText)
            {
                _notifyIconData.szTip = tooltipText;
                _notifyIconData.uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE;
                
                Shell_NotifyIcon(NIM_MODIFY, ref _notifyIconData);
                (App.Current as App)?.LogMessage($"任务栏图标提示文本已更新: {tooltipText}");
            }
        }

        /// <summary>
        /// 闪烁任务栏图标
        /// </summary>
        /// <param name="flashCount">闪烁次数</param>
        public void FlashIcon(uint flashCount = 3)
        {
            var flashInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = _windowHandle,
                dwFlags = 3,  // 闪烁窗口标题和任务栏按钮
                uCount = flashCount,
                dwTimeout = 0
            };

            FlashWindowEx(ref flashInfo);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            // 删除任务栏图标
            Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);
            // 移除窗口消息钩子
            HwndSource.FromHwnd(_windowHandle)?.RemoveHook(WndProc);
            // 释放图标句柄
            if (_iconHandle != IntPtr.Zero)
            {
                DestroyIcon(_iconHandle);
                _iconHandle = IntPtr.Zero;
            }
        }
    }
}