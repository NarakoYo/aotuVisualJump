using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using ImageRecognitionApp.unit;
using System.Windows.Media; // 添加这个引用以使用VisualTreeHelper
using ImageRecognitionApp; // 添加这个引用以使用MainWindow

namespace ImageRecognitionApp.WinFun
{
    /// <summary>
    /// 任务栏管理器，负责处理任务栏图标和任务栏应用程序区的交互功能
    /// 实现Windows标准的任务栏行为，包括窗口最小化、恢复、隐藏到托盘等功能
    /// </summary>
    public class TaskbarManager : IDisposable
    {
        #region 字段定义

        private static TaskbarManager? _instance;    // 单例实例
        
        private IntPtr _windowHandle;                // 窗口句柄
        private readonly uint _taskbarIconId = 1001; // 任务栏图标ID
        private NOTIFYICONDATA _notifyIconData;      // 任务栏图标数据结构
        private IntPtr _iconHandle = IntPtr.Zero;    // 图标句柄，用于资源释放
        private TaskbarAnimation? _taskbarAnimation; // 任务栏动画处理对象
        private ContextMenu? _contextMenu;           // 右键上下文菜单
        private HwndSource? _hwndSource;             // 窗口源对象，用于消息处理
        private bool _isTrayIconVisible = false;     // 托盘图标可见状态
        private bool _isWindowMinimizedToTray = false; // 窗口是否最小化到托盘
        private ITaskbarList3? _taskbarList3;         // 任务栏列表3接口实例，用于任务栏进度条
        private readonly bool _showTrayIconInitially = false; // 是否在初始化时显示托盘图标
        
        // 窗口位置相关字段
        private double _lastWindowLeft = 0;          // 上次窗口的左边界位置
        private double _lastWindowTop = 0;           // 上次窗口的上边界位置
        private double _lastWindowWidth = 800;       // 上次窗口的宽度
        private double _lastWindowHeight = 600;      // 上次窗口的高度
        private WindowState _lastWindowState = WindowState.Normal; // 上次窗口的状态
        
        /// <summary>
        /// 获取TaskbarManager的单例实例
        /// </summary>
        public static TaskbarManager? Instance
        {
            get { return _instance; }
        }
        
        /// <summary>
        /// 获取上次保存的窗口Left位置
        /// </summary>
        public double LastWindowLeft { get { return _lastWindowLeft; } }
        
        /// <summary>
        /// 获取上次保存的窗口Top位置
        /// </summary>
        public double LastWindowTop { get { return _lastWindowTop; } }
        
        /// <summary>
        /// 获取上次保存的窗口宽度
        /// </summary>
        public double LastWindowWidth { get { return _lastWindowWidth; } }
        
        /// <summary>
        /// 获取上次保存的窗口高度
        /// </summary>
        public double LastWindowHeight { get { return _lastWindowHeight; } }
        
        /// <summary>
        /// 获取上次保存的窗口状态
        /// </summary>
        public WindowState LastWindowState { get { return _lastWindowState; } }

        #endregion

        #region 结构体定义

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

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

        #region Win32 API 导入

        // 任务栏进度条相关接口和API
        [ComImport]
        [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            // ITaskbarList methods
            [PreserveSig]
            void HrInit();
            [PreserveSig]
            void AddTab(IntPtr hwnd);
            [PreserveSig]
            void DeleteTab(IntPtr hwnd);
            [PreserveSig]
            void ActivateTab(IntPtr hwnd);
            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2 methods
            [PreserveSig]
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3 methods
            [PreserveSig]
            void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
            [PreserveSig]
            void SetProgressState(IntPtr hwnd, TaskbarProgressState tbpFlags);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IBindCtx pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        [DllImport("shell32.dll")]
        private static extern int CoCreateInstance(ref Guid clsid, IntPtr pUnkOuter,
            uint dwClsContext, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out ITaskbarList3 ppv);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

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

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region 常量定义

        // 任务栏进度条状态枚举
        public enum TaskbarProgressState
        {
            NoProgress = 0,
            Indeterminate = 1,  // 走马灯效果
            Normal = 2,         // 正常加载
            Error = 4,          // 加载错误中断卡住
            Paused = 8          // 加载暂停
        }

        // GUID常量
        private static readonly Guid CLSID_TaskbarList = new Guid("56FDF344-FD6D-11d0-958A-006097C9A090");
        private static readonly Guid IID_ITaskbarList3 = new Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF");
        private const uint CLSCTX_INPROC_SERVER = 1;

        // 任务栏图标消息常量
        private const uint NIM_ADD = 0x00000000;       // 添加任务栏图标
        private const uint NIM_MODIFY = 0x00000001;    // 修改任务栏图标
        private const uint NIM_DELETE = 0x00000002;    // 删除任务栏图标
        private const uint NIF_MESSAGE = 0x00000001;   // 包含回调消息
        private const uint NIF_ICON = 0x00000002;      // 包含图标
        private const uint NIF_TIP = 0x00000004;       // 包含提示文本
        private const uint NIF_INFO = 0x00000010;      // 包含气球通知
        private const uint NIM_SETVERSION = 0x00000004;// 设置版本
        private const uint NOTIFYICON_VERSION_4 = 0x00000004; // 通知图标版本4

        // Windows消息常量
        private const int WM_USER = 0x0400;            // 用户自定义消息基础
        private const int WM_RBUTTONDOWN = 0x0204;     // 鼠标右键按下
        private const int WM_CONTEXTMENU = 0x007B;     // 上下文菜单
        private const int WM_NOTIFYICON = WM_USER + 1; // 通知图标消息
        private const int WM_SYSCOMMAND = 0x0112;      // 系统命令
        private const uint SC_RESTORE = 0xF120;        // 恢复窗口
        private const uint SC_MINIMIZE = 0xF020;       // 最小化窗口
        private const uint SC_MAXIMIZE = 0xF030;       // 最大化窗口
        private const uint SC_CLOSE = 0xF060;          // 关闭窗口
        private const uint SC_SIZE = 0xF000;           // 调整窗口大小
        private const uint WM_LBUTTONDOWN = 0x0201;    // 鼠标左键按下
        private const uint WM_LBUTTONUP = 0x0202;      // 鼠标左键释放
        private const uint WM_RBUTTONUP = 0x0205;      // 鼠标右键释放
        private const uint WM_MBUTTONDOWN = 0x0207;    // 鼠标中键按下
        private const uint WM_MBUTTONUP = 0x0208;      // 鼠标中键释放
        private const uint WM_LBUTTONDBLCLK = 0x0203;  // 鼠标左键双击
        private const uint WM_RBUTTONDBLCLK = 0x0206;  // 鼠标右键双击
        private const uint WM_MBUTTONDBLCLK = 0x0209;  // 鼠标中键双击

        // 窗口显示命令
        private const int SW_HIDE = 0;                 // 隐藏窗口
        private const int SW_SHOWNORMAL = 1;           // 正常显示窗口
        private const int SW_SHOWMINIMIZED = 2;        // 最小化显示窗口
        private const int SW_SHOWMAXIMIZED = 3;        // 最大化显示窗口
        private const int SW_SHOWNOACTIVATE = 4;       // 显示但不激活窗口

        // 图标常量
        private const int IDI_APPLICATION = 0x7F00;    // 默认应用程序图标

        // 菜单常量
        private const uint TPM_RETURNCMD = 0x0100;     // 返回所选菜单项的ID
        private const uint TPM_LEFTBUTTON = 0x0000;    // 左键菜单选择

        #endregion

        #region 构造函数与初始化

        /// <summary>
        /// 构造函数，初始化任务栏管理器
        /// </summary>
        /// <param name="window">要关联的WPF窗口</param>
        /// <param name="showTrayIconInitially">是否在初始化时显示托盘图标</param>
        public TaskbarManager(System.Windows.Window window, bool showTrayIconInitially = false)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window), "窗口不能为空");
            }
            
            // 设置单例实例
            _instance = this;
            _showTrayIconInitially = showTrayIconInitially;

            LogMessage("TaskbarManager: 初始化任务栏管理器");
            LogMessage($"TaskbarManager: 是否在初始化时显示托盘图标: {_showTrayIconInitially}");
            SetupWindowEventHandlers(window);
        }

        /// <summary>
        /// 设置窗口事件处理程序
        /// </summary>
        /// <param name="window">WPF窗口对象</param>
        private void SetupWindowEventHandlers(System.Windows.Window window)
        {
            // 延迟获取窗口句柄，确保窗口已加载
            window.Loaded += (sender, e) =>
            {
                LogMessage("TaskbarManager: 窗口Loaded事件被触发");
                InitializeWindowHandle(window);
                InitializeTaskbarComponents();
            };

            // 监听窗口状态变化
            window.StateChanged += (sender, e) =>
            {
                if (sender is System.Windows.Window wnd)
                {
                    LogMessage($"TaskbarManager: 窗口状态变更为: {wnd.WindowState}");
                    // 根据窗口状态更新任务栏图标的显示状态
                    if (_taskbarAnimation != null)
                    {
                        switch (wnd.WindowState)
                        {
                            case WindowState.Minimized:
                                _taskbarAnimation.MinimizeAnimation();
                                LogMessage("TaskbarManager: 播放窗口最小化动画");
                                break;
                            case WindowState.Normal:
                                // 窗口恢复到正常状态，不需要动画
                                break;
                            case WindowState.Maximized:
                                // 窗口最大化，不需要动画
                                break;
                        }
                    }
                }
            };

            // 监听窗口关闭事件
            window.Closing += (sender, e) =>
            {
                LogMessage("TaskbarManager: 窗口关闭事件被触发");
                // 确保资源释放
                Dispose();
            };
        }

        /// <summary>
        /// 初始化窗口句柄
        /// </summary>
        /// <param name="window">WPF窗口对象</param>
        private void InitializeWindowHandle(System.Windows.Window window)
        {
            try
            {
                _windowHandle = new WindowInteropHelper(window).Handle;
                if (_windowHandle == IntPtr.Zero)
                {
                    LogMessage("TaskbarManager: 窗口句柄获取失败");
                    // 尝试使用主窗口
                    if (System.Windows.Application.Current.MainWindow != null)
                    {
                        _windowHandle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                        LogMessage($"TaskbarManager: 使用主窗口句柄: {_windowHandle}");
                    }
                }
                else
                {
                    LogMessage($"TaskbarManager: 窗口句柄已设置: {_windowHandle}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 初始化窗口句柄错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化任务栏组件
        /// </summary>
        private void InitializeTaskbarComponents()
        {
            try
            {
                if (_windowHandle == IntPtr.Zero)
                {
                    LogMessage("TaskbarManager: 无法初始化，窗口句柄无效");
                    return;
                }

                // 初始化任务栏动画对象
                InitializeTaskbarAnimation();

                // 注册窗口消息处理
                RegisterWindowMessageHandler();

                // 初始化上下文菜单
                InitializeContextMenu();

                // 初始化任务栏进度条接口
                InitializeTaskbarProgressBar();

                // 根据设置决定是否初始化任务栏图标
                if (_showTrayIconInitially)
                {
                    InitializeTaskbarIcon();
                }
                else
                {
                    LogMessage("TaskbarManager: 根据配置，初始化阶段不显示托盘图标");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 初始化任务栏组件错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 初始化任务栏进度条接口
        /// </summary>
        private void InitializeTaskbarProgressBar()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                { // Windows Vista及以上版本支持任务栏进度条
                    _taskbarList3 = (ITaskbarList3)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_TaskbarList));
                    if (_taskbarList3 != null)
                    {
                        _taskbarList3.HrInit();
                        LogMessage("TaskbarManager: 任务栏进度条接口已初始化");
                    }
                }
                else
                {
                    LogMessage("TaskbarManager: 当前操作系统版本不支持任务栏进度条功能");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 初始化任务栏进度条接口错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化任务栏动画对象
        /// </summary>
        private void InitializeTaskbarAnimation()
        {
            try
            {
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    _taskbarAnimation = new TaskbarAnimation(System.Windows.Application.Current.MainWindow);
                    LogMessage("TaskbarManager: 任务栏动画对象已初始化");
                }
                else
                {
                    LogMessage("TaskbarManager: 主窗口为空，无法初始化任务栏动画对象");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 初始化任务栏动画对象错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置任务栏进度条状态
        /// </summary>
        /// <param name="state">进度条状态</param>
        public void SetProgressState(TaskbarProgressState state)
        {
            try
            {
                if (_taskbarList3 != null && _windowHandle != IntPtr.Zero)
                {
                    _taskbarList3.SetProgressState(_windowHandle, state);
                    LogMessage($"TaskbarManager: 设置任务栏进度条状态为: {state}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 设置任务栏进度条状态错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置任务栏进度条值
        /// </summary>
        /// <param name="currentValue">当前进度值</param>
        /// <param name="maximumValue">最大进度值</param>
        public void SetProgressValue(ulong currentValue, ulong maximumValue)
        {
            try
            {
                if (_taskbarList3 != null && _windowHandle != IntPtr.Zero)
                {
                    _taskbarList3.SetProgressValue(_windowHandle, currentValue, maximumValue);
                    LogMessage($"TaskbarManager: 设置任务栏进度条值: {currentValue}/{maximumValue}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 设置任务栏进度条值错误: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 获取窗口图标错误: {ex.Message}");
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
                LogMessage("TaskbarManager: 开始初始化任务栏图标");
                
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
                LogMessage($"TaskbarManager: 任务栏图标配置: hWnd={_windowHandle}, uCallbackMessage={WM_NOTIFYICON}, szTip={appTitle}");

                // 添加任务栏图标
                int addResult = Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
                if (addResult != 0)
                {
                    LogMessage("TaskbarManager: 任务栏图标添加成功");
                    _isTrayIconVisible = true;
                }
                else
                {
                    LogMessage("TaskbarManager: 任务栏图标添加失败");
                    int errorCode = Marshal.GetLastWin32Error();
                    LogMessage($"TaskbarManager: Win32错误代码: {errorCode}");
                }

                // 设置版本
                int versionResult = Shell_NotifyIcon(NIM_SETVERSION, ref _notifyIconData);
                if (versionResult != 0)
                {
                    LogMessage("TaskbarManager: 任务栏图标版本设置成功");
                }
                else
                {
                    LogMessage("TaskbarManager: 任务栏图标版本设置失败");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 初始化任务栏图标错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 注册窗口消息处理
        /// </summary>
        private void RegisterWindowMessageHandler()
        {
            try
            {
                _hwndSource = HwndSource.FromHwnd(_windowHandle);
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook(WndProc);
                    LogMessage("TaskbarManager: 窗口消息钩子已注册");
                }
                else
                {
                    LogMessage("TaskbarManager: 无法获取HwndSource，消息钩子注册失败");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 注册窗口消息处理错误: {ex.Message}");
            }
        }

        #endregion

        #region 窗口消息处理

        /// <summary>
        /// 窗口消息处理
        /// 监听并处理所有任务栏相关的Windows消息
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                // 记录关键消息，避免日志过多
                if (msg == WM_SYSCOMMAND || msg == WM_NOTIFYICON || msg == WM_CONTEXTMENU)
                {
                    LogMessage($"TaskbarManager: 收到窗口消息: 消息ID={msg}, wParam={wParam}, lParam={lParam}");
                }

                // 处理系统命令消息
                if (msg == WM_SYSCOMMAND)
                {
                    uint command = (uint)wParam & 0xFFF0;
                    switch (command)
                    {
                        case SC_RESTORE:
                            // 恢复窗口
                            OnTaskbarShortcutLeftClick();
                            LogMessage("TaskbarManager: 处理SC_RESTORE系统命令");
                            break;
                        case SC_MINIMIZE:
                            // 最小化窗口时播放动画
                            if (_taskbarAnimation != null)
                            {
                                _taskbarAnimation.MinimizeAnimation();
                                LogMessage("TaskbarManager: 播放窗口最小化动画");
                            }
                            break;
                        case SC_MAXIMIZE:
                            // 最大化窗口
                            LogMessage("TaskbarManager: 处理SC_MAXIMIZE系统命令");
                            break;
                        case SC_CLOSE:
                            // 关闭窗口时清理资源
                            LogMessage("TaskbarManager: 处理SC_CLOSE系统命令");
                            break;
                    }
                }
                // 处理通知图标消息
                else if (msg == WM_NOTIFYICON)
                {
                    uint notificationMessage = (uint)lParam;
                    switch (notificationMessage)
                    {
                        case WM_LBUTTONUP:
                            // 左键点击托盘图标
                            OnLeftClick();
                            handled = true;
                            break;
                        case WM_RBUTTONUP:
                            // 右键点击托盘图标
                            OnRightClick();
                            handled = true;
                            break;
                        case WM_LBUTTONDBLCLK:
                            // 左键双击托盘图标
                            OnDoubleClick();
                            handled = true;
                            break;
                        case WM_CONTEXTMENU:
                            // 显示上下文菜单
                            OnContextMenuRequest();
                            handled = true;
                            break;
                    }
                }
                // 处理上下文菜单消息
                else if (msg == WM_CONTEXTMENU)
                {
                    // 检查是否是任务栏快捷方式的右键菜单
                    if (wParam == _windowHandle)
                    {
                        OnTaskbarShortcutRightClick();
                        handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理窗口消息错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }

            return IntPtr.Zero;
        }

        #endregion

        #region 任务栏快捷方式处理

        /// <summary>
        /// 处理任务栏快捷方式左键点击
        /// 当任务栏应用程序区的程序快捷方式处于选中状态时，左键点击优先触发最小化
        /// </summary>
        private void OnTaskbarShortcutLeftClick()
        {
            try
            {
                LogMessage("TaskbarManager: 处理任务栏快捷方式左键点击");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    LogMessage($"TaskbarManager: 当前窗口状态 - WindowState: {mainWindow.WindowState}, Visibility: {mainWindow.Visibility}, ShowInTaskbar: {mainWindow.ShowInTaskbar}, IsActive: {mainWindow.IsActive}");
                    
                    // 先确保窗口在任务栏显示
                    mainWindow.ShowInTaskbar = true;
                    
                    // 检查窗口是否已经可见且被选中（活动状态）
                    if (mainWindow.IsVisible && mainWindow.IsActive)
                    {
                        // 如果窗口已经可见且被选中，优先触发最小化
                        mainWindow.WindowState = WindowState.Minimized;
                        LogMessage("TaskbarManager: 窗口已选中，触发最小化");
                         
                        // 播放最小化动画
                        if (_taskbarAnimation != null && !_taskbarAnimation.IsAnimating)
                        {
                            _taskbarAnimation.MinimizeAnimation();
                            LogMessage("TaskbarManager: 播放窗口最小化动画");
                        }
                    }
                    else
                    {
                        // 窗口未被选中或不可见，执行恢复窗口逻辑
                        // 播放窗口动画
                        if (_taskbarAnimation != null && !_taskbarAnimation.IsAnimating)
                        {
                            if (mainWindow.WindowState == WindowState.Normal && mainWindow.IsVisible)
                            {
                                // 在播放动画前再次确保ShowInTaskbar = true
                                mainWindow.ShowInTaskbar = true;
                                
                                _taskbarAnimation.RestoreAnimation();
                                LogMessage("TaskbarManager: 播放窗口恢复动画");
                            }
                            else
                            {
                                // 窗口未显示或不是Normal状态，先设置状态再播放动画
                                mainWindow.WindowState = WindowState.Normal;
                                mainWindow.Visibility = Visibility.Visible;
                                mainWindow.ShowInTaskbar = true;
                                
                                _taskbarAnimation.RestoreAnimation();
                                LogMessage("TaskbarManager: 恢复窗口状态并播放动画");
                            }
                        }
                        else
                        {
                            // 如果没有动画或动画正在运行，则直接恢复窗口
                            mainWindow.WindowState = WindowState.Normal;
                            mainWindow.Visibility = Visibility.Visible;
                            mainWindow.ShowInTaskbar = true;
                            
                            // 恢复窗口位置
                            RestoreWindowPosition(mainWindow);
                            
                            // 强制激活窗口
                            mainWindow.Activate();
                            mainWindow.Focus();
                              
                            LogMessage("TaskbarManager: 直接恢复并激活窗口");
                            LogMessage($"TaskbarManager: 恢复后窗口状态 - WindowState: {mainWindow.WindowState}, Visibility: {mainWindow.Visibility}, ShowInTaskbar: {mainWindow.ShowInTaskbar}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理任务栏快捷方式左键点击错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 处理任务栏快捷方式右键点击事件
        /// 显示系统菜单
        /// </summary>
        public void OnTaskbarShortcutRightClick()
        {
            try
            {
                LogMessage("TaskbarManager: 处理任务栏快捷方式右键点击");
                
                // 获取当前鼠标位置
                POINT cursorPos;
                if (GetCursorPos(out cursorPos))
                {
                    // 将屏幕坐标转换为窗口客户区坐标
                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // 获取MainWindow实例
                        var window = mainWindow as MainWindow;
                        if (window != null)
                        {
                            // 检查窗口是否处于最大化状态
                            if (window.WindowState == System.Windows.WindowState.Maximized)
                            {
                                LogMessage("TaskbarManager: 窗口处于最大化状态，不显示系统菜单");
                                return;
                            }
                            
                            // 查找标题栏元素
                            var titleBar = window.FindName("TitleBar") as System.Windows.Controls.Border;
                            if (titleBar != null)
                            {
                                // 获取标题栏在屏幕上的位置
                                System.Windows.Point titleBarScreenPos = titleBar.PointToScreen(new System.Windows.Point(0, 0));
                                System.Windows.Rect titleBarRect = new System.Windows.Rect(
                                    titleBarScreenPos.X,
                                    titleBarScreenPos.Y,
                                    titleBar.ActualWidth,
                                    titleBar.ActualHeight
                                );
                                
                                // 检查鼠标是否在标题栏区域内
                                if (titleBarRect.Contains(new System.Windows.Point(cursorPos.x, cursorPos.y)))
                                {
                                    // 将屏幕坐标转换为标题栏内的相对坐标
                                    System.Windows.Point mouseInTitleBar = titleBar.PointFromScreen(new System.Windows.Point(cursorPos.x, cursorPos.y));
                                    
                                    // 检查鼠标是否在标题栏内（相对于标题栏的坐标）
                                    if (mouseInTitleBar.X >= 0 &&
                                        mouseInTitleBar.Y >= 0 &&
                                        mouseInTitleBar.X <= titleBar.ActualWidth &&
                                        mouseInTitleBar.Y <= titleBar.ActualHeight)
                                    {
                                        // 查找鼠标点击的元素
                                        System.Windows.Media.HitTestResult hitTestResult = System.Windows.Media.VisualTreeHelper.HitTest(titleBar, mouseInTitleBar);
                                        System.Windows.DependencyObject clickedElement = hitTestResult?.VisualHit;
                                        
                                        // 检查是否点击在按钮上
                                        if (clickedElement != null)
                                        {
                                            // 向上遍历视觉树，检查是否在按钮或按钮子元素上
                                            System.Windows.Controls.Button button = FindVisualParent<System.Windows.Controls.Button>(clickedElement);
                                            if (button == null)
                                            {
                                                // 非按钮区域，显示系统菜单
                                                ShowSystemMenu();
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                LogMessage("TaskbarManager: 右键点击不在标题栏非按钮区域，不显示系统菜单");
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理任务栏快捷方式右键点击错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 查找视觉树中的父元素
        /// </summary>
        private T FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            System.Windows.DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        /// <summary>
        /// 显示系统菜单
        /// </summary>
        private void ShowSystemMenu()
        {
            try
            {
                LogMessage("TaskbarManager: 显示系统菜单");
                if (_windowHandle != IntPtr.Zero)
                {
                    // 获取系统菜单
                    IntPtr hMenu = GetSystemMenu(_windowHandle, false);
                    if (hMenu != IntPtr.Zero)
                    {
                        // 禁用最大化菜单项
                        // SC_MAXIMIZE = 0xF030
                        // EnableMenuItem(hMenu, SC_MAXIMIZE, MF_BYCOMMAND | MF_DISABLED | MF_GRAYED);
                        
                        // 禁用大小菜单项
                        EnableMenuItem(hMenu, SC_SIZE, MF_BYCOMMAND | MF_DISABLED | MF_GRAYED);
                        
                        // 禁用还原菜单项
                        EnableMenuItem(hMenu, SC_RESTORE, MF_BYCOMMAND | MF_DISABLED | MF_GRAYED);

                        // 获取当前鼠标位置
                        POINT cursorPos;
                        if (GetCursorPos(out cursorPos))
                        {
                            // 设置窗口为前景窗口
                            SetForegroundWindow(_windowHandle);

                            // 显示系统菜单
                            uint menuResult = (uint)TrackPopupMenu(
                                hMenu,              // 菜单句柄
                                TPM_RETURNCMD | TPM_LEFTBUTTON, // 显示选项
                                cursorPos.x,        // X坐标
                                cursorPos.y,        // Y坐标
                                0,                  // 保留参数
                                _windowHandle,      // 窗口句柄
                                IntPtr.Zero         // 矩形区域
                            );

                            // 如果用户选择了菜单项，则发送相应的系统命令
                            if (menuResult > 0)
                            {
                                // 只过滤掉不允许的大小菜单项，允许最大化和还原
                            if (menuResult != SC_SIZE)
                            {
                                PostMessage(_windowHandle, WM_SYSCOMMAND, (IntPtr)menuResult, IntPtr.Zero);
                                LogMessage($"TaskbarManager: 用户选择了系统菜单项: {menuResult}");
                            }
                            else
                            {
                                LogMessage($"TaskbarManager: 忽略菜单项选择: {menuResult}");
                            }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 显示系统菜单错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }
        }
        
        // Windows API常量
         private const uint MF_BYCOMMAND = 0x00000000;
         private const uint MF_DISABLED = 0x00000002;
         private const uint MF_GRAYED = 0x00000001;
         
         // Windows API函数声明
         [DllImport("user32.dll")]
         private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        #endregion

        /// <summary>
        /// 显示任务栏托盘图标
        /// </summary>
        public void ShowTrayIcon()
        {
            try
            {
                if (!_isTrayIconVisible)
                {
                    LogMessage("TaskbarManager: 手动显示任务栏托盘图标");
                    InitializeTaskbarIcon();
                }
                else
                {
                    LogMessage("TaskbarManager: 任务栏图标已经可见");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 显示任务栏图标错误: {ex.Message}");
            }
        }

        #region 托盘图标交互处理

        /// <summary>
        /// 处理托盘图标左键点击
        /// 只负责恢复窗口，不再触发最小化功能
        /// </summary>
        private void OnLeftClick()
        {
            try
            {
                LogMessage("TaskbarManager: 处理托盘图标左键点击");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    LogMessage($"TaskbarManager: 当前窗口状态 - WindowState: {mainWindow.WindowState}, Visibility: {mainWindow.Visibility}, ShowInTaskbar: {mainWindow.ShowInTaskbar}");
                       
                    if (mainWindow.WindowState == WindowState.Minimized || !mainWindow.IsVisible)
                    {
                        // 如果窗口最小化或不可见，则恢复窗口
                        
                        // 先确保窗口在任务栏显示
                        mainWindow.ShowInTaskbar = true;
                        
                        // 播放恢复动画，让动画来负责显示和激活窗口
                        if (_taskbarAnimation != null && !_taskbarAnimation.IsAnimating)
                        {
                            LogMessage("TaskbarManager: 播放窗口恢复动画");
                            // 在播放动画前再次确保ShowInTaskbar = true
                            mainWindow.ShowInTaskbar = true;
                            _taskbarAnimation.RestoreAnimation();
                        }
                        else
                        {
                            // 如果没有动画或动画正在运行，则直接恢复窗口
                            mainWindow.Visibility = Visibility.Visible;
                            mainWindow.ShowInTaskbar = true;
                               
                            // 恢复窗口位置
                            RestoreWindowPosition(mainWindow);
                               
                            // 强制激活窗口
                            mainWindow.Activate();
                            mainWindow.Focus();
                                
                            LogMessage("TaskbarManager: 从托盘恢复并激活窗口");
                            LogMessage($"TaskbarManager: 恢复后窗口状态 - WindowState: {mainWindow.WindowState}, Visibility: {mainWindow.Visibility}, ShowInTaskbar: {mainWindow.ShowInTaskbar}");
                        }
                    }
                    else
                    {
                        // 如果窗口已显示，则只激活窗口，不再最小化
                        if (!mainWindow.IsActive)
                        {
                            mainWindow.Activate();
                            mainWindow.Focus();
                            LogMessage("TaskbarManager: 激活已有窗口");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理托盘图标左键点击错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 处理托盘图标右键点击
        /// </summary>
        private void OnRightClick()
        {
            try
            {
                LogMessage("TaskbarManager: 处理托盘图标右键点击");
                // 显示上下文菜单
                ShowContextMenu();
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理托盘图标右键点击错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理托盘图标双击
        /// </summary>
        private void OnDoubleClick()
        {
            try
            {
                LogMessage("TaskbarManager: 处理托盘图标双击");
                // 双击托盘图标等同于左键点击
                OnLeftClick();
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理托盘图标双击错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理上下文菜单请求
        /// </summary>
        private void OnContextMenuRequest()
        {
            try
            {
                LogMessage("TaskbarManager: 处理上下文菜单请求");
                // 显示上下文菜单
                ShowContextMenu();
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理上下文菜单请求错误: {ex.Message}");
            }
        }

        #endregion

        #region 上下文菜单处理

        /// <summary>
        /// 初始化上下文菜单
        /// </summary>
        private void InitializeContextMenu()
        {
            try
            {
                LogMessage("TaskbarManager: 初始化上下文菜单");
                _contextMenu = new ContextMenu();

                // 添加显示/隐藏窗口菜单项
                MenuItem showHideMenuItem = new MenuItem
                {
                    Header = "显示窗口",
                    Tag = "ShowHideWindow"
                };
                showHideMenuItem.Click += OnShowHideWindowMenuItemClick;
                _contextMenu.Items.Add(showHideMenuItem);

                // 添加分隔符
                _contextMenu.Items.Add(new Separator());

                // 添加最小化窗口菜单项
                MenuItem minimizeMenuItem = new MenuItem
                {
                    Header = "最小化窗口",
                    Tag = "MinimizeWindow"
                };
                minimizeMenuItem.Click += OnMinimizeWindowMenuItemClick;
                _contextMenu.Items.Add(minimizeMenuItem);

                // 添加隐藏到托盘菜单项
                MenuItem hideToTrayMenuItem = new MenuItem
                {
                    Header = "隐藏到托盘",
                    Tag = "HideToTray"
                };
                hideToTrayMenuItem.Click += OnHideToTrayMenuItemClick;
                _contextMenu.Items.Add(hideToTrayMenuItem);

                // 添加分隔符
                _contextMenu.Items.Add(new Separator());

                // 添加退出应用菜单项
                MenuItem exitMenuItem = new MenuItem
                {
                    Header = "退出",
                    Tag = "ExitApplication"
                };
                exitMenuItem.Click += OnExitApplicationMenuItemClick;
                _contextMenu.Items.Add(exitMenuItem);

                LogMessage("TaskbarManager: 上下文菜单已创建");
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 初始化上下文菜单错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示上下文菜单
        /// </summary>
        private void ShowContextMenu()
        {
            try
            {
                LogMessage("TaskbarManager: 显示上下文菜单");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && _contextMenu != null)
                {
                    // 确保上下文菜单已初始化
                    if (_contextMenu.Items.Count == 0)
                    {
                        InitializeContextMenu();
                    }

                    // 更新菜单状态
                    UpdateContextMenuState();

                    // 显示菜单
                    _contextMenu.IsOpen = true;
                    LogMessage("TaskbarManager: 上下文菜单已显示");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 显示上下文菜单错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 公共方法，用于从外部调用显示上下文菜单
        /// </summary>
        public void DisplayContextMenu()
        {
            ShowContextMenu();
        }

        /// <summary>
        /// 更新上下文菜单状态
        /// </summary>
        private void UpdateContextMenuState()
        {
            try
            {
                LogMessage("TaskbarManager: 更新上下文菜单状态");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && _contextMenu != null)
                {
                    // 根据窗口状态更新菜单文本和可用性
                    foreach (var item in _contextMenu.Items)
                    {
                        if (item is MenuItem menuItem && menuItem.Tag != null)
                        {
                            switch (menuItem.Tag.ToString())
                            {
                                case "ShowHideWindow":
                                    // 根据窗口是否可见更新菜单文本
                                    menuItem.Header = mainWindow.IsVisible ? "隐藏窗口" : "显示窗口";
                                    break;
                                case "MinimizeWindow":
                                    // 根据窗口状态更新最小化菜单项可用性
                                    menuItem.IsEnabled = mainWindow.IsVisible && mainWindow.WindowState != WindowState.Minimized;
                                    break;
                                case "HideToTray":
                                    // 隐藏到托盘菜单项始终可用
                                    break;
                                case "ExitApplication":
                                    // 退出应用菜单项始终可用
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 更新上下文菜单状态错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示/隐藏窗口菜单项点击处理
        /// </summary>
        private void OnShowHideWindowMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("TaskbarManager: 处理显示/隐藏窗口菜单项点击");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    if (mainWindow.IsVisible)
                    {
                        // 隐藏窗口
                        mainWindow.Visibility = Visibility.Hidden;
                        _isWindowMinimizedToTray = true;
                        LogMessage("TaskbarManager: 隐藏窗口");
                    }
                    else
                    {
                        // 播放恢复动画，让动画来负责显示和激活窗口
                        if (_taskbarAnimation != null)
                        {
                            LogMessage("TaskbarManager: 播放窗口恢复动画");
                            _taskbarAnimation.RestoreAnimation();
                        }
                        else
                        {
                            // 如果没有动画，则直接恢复窗口
                            mainWindow.Visibility = Visibility.Visible;
                            mainWindow.WindowState = WindowState.Normal;
                            mainWindow.ShowInTaskbar = true;
                            mainWindow.Activate();
                            _isWindowMinimizedToTray = false;
                            LogMessage("TaskbarManager: 显示并激活窗口");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理显示/隐藏窗口菜单项点击错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 最小化窗口菜单项点击处理
        /// </summary>
        private void OnMinimizeWindowMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("TaskbarManager: 处理最小化窗口菜单项点击");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow.IsVisible)
                {
                    mainWindow.WindowState = WindowState.Minimized;
                    LogMessage("TaskbarManager: 最小化窗口");

                    // 播放最小化动画
                    if (_taskbarAnimation != null)
                    {
                        _taskbarAnimation.MinimizeAnimation();
                        LogMessage("TaskbarManager: 播放窗口最小化动画");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理最小化窗口菜单项点击错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 隐藏到托盘菜单项点击处理
        /// </summary>
        private void OnHideToTrayMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("TaskbarManager: 处理隐藏到托盘菜单项点击");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Visibility = Visibility.Hidden;
                    mainWindow.ShowInTaskbar = false;
                    _isWindowMinimizedToTray = true;
                    LogMessage("TaskbarManager: 将窗口隐藏到托盘");

                    // 播放最小化动画
                    if (_taskbarAnimation != null)
                    {
                        _taskbarAnimation.MinimizeAnimation();
                        LogMessage("TaskbarManager: 播放窗口最小化动画");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理隐藏到托盘菜单项点击错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 退出应用菜单项点击处理
        /// </summary>
        private void OnExitApplicationMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("TaskbarManager: 处理退出应用菜单项点击");
                // 清理资源
                Dispose();
                
                // 关闭应用程序
                System.Windows.Application.Current.Shutdown();
                LogMessage("TaskbarManager: 应用程序已退出");
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 处理退出应用菜单项点击错误: {ex.Message}");
            }
        }

        #endregion

        #region 窗口位置管理
        /// <summary>
        /// 保存窗口当前位置和状态
        /// </summary>
        /// <param name="window">要保存位置的窗口</param>
        private void SaveWindowPosition(Window window)
        {
            try
            {
                if (window != null)
                {
                    // 保存窗口位置和尺寸
                    _lastWindowLeft = window.Left;
                    _lastWindowTop = window.Top;
                    _lastWindowWidth = window.Width;
                    _lastWindowHeight = window.Height;
                    _lastWindowState = window.WindowState;
                    
                    LogMessage($"TaskbarManager: 保存窗口位置 - Left: {_lastWindowLeft}, Top: {_lastWindowTop}, Width: {_lastWindowWidth}, Height: {_lastWindowHeight}, State: {_lastWindowState}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 保存窗口位置错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 恢复窗口到上次保存的位置和状态
        /// </summary>
        /// <param name="window">要恢复位置的窗口</param>
        private void RestoreWindowPosition(Window window)
        {
            try
            {
                if (window != null)
                {
                    // 先设置为Normal状态，再恢复位置和尺寸
                    window.WindowState = WindowState.Normal;
                    
                    // 恢复窗口位置
                    window.Left = _lastWindowLeft;
                    window.Top = _lastWindowTop;
                    window.Width = _lastWindowWidth;
                    window.Height = _lastWindowHeight;
                    
                    // 如果上次是最大化状态，则恢复最大化
                    if (_lastWindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Maximized;
                    }
                    
                    LogMessage($"TaskbarManager: 恢复窗口位置 - Left: {_lastWindowLeft}, Top: {_lastWindowTop}, Width: {_lastWindowWidth}, Height: {_lastWindowHeight}, State: {_lastWindowState}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 恢复窗口位置错误: {ex.Message}");
            }
        }
        #endregion

        #region 任务栏辅助功能

        /// <summary>
        /// 将窗口最小化到任务栏（保持任务栏缩略图预览）
        /// </summary>
        public void MinimizeToTaskbar()
        {
            try
            {
                LogMessage("TaskbarManager: 将窗口最小化到任务栏");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // 保存窗口当前位置和状态
                    SaveWindowPosition(mainWindow);
                    
                    // 确保窗口先恢复到原始大小（从最大化或其他状态）
                    if (mainWindow.WindowState != WindowState.Normal)
                    {
                        mainWindow.WindowState = WindowState.Normal;
                        LogMessage("TaskbarManager: 恢复窗口到正常大小");
                    }
                    
                    // 保持窗口可见并在任务栏显示，以支持缩略图预览
                    mainWindow.Visibility = Visibility.Visible;
                    mainWindow.ShowInTaskbar = true;
                    
                    // 先执行窗口最小化操作
                    mainWindow.WindowState = WindowState.Minimized;
                    LogMessage("TaskbarManager: 窗口已最小化");
                    
                    // 然后播放最小化动画
                    if (_taskbarAnimation != null)
                    {
                        _taskbarAnimation.MinimizeAnimation();
                        LogMessage("TaskbarManager: 播放窗口最小化动画");
                    }
                    
                    // 不再使用_isWindowMinimizedToTray标志，直接基于窗口状态判断
                    LogMessage("TaskbarManager: 窗口已最小化到任务栏");
                    LogMessage($"TaskbarManager: 窗口状态 - WindowState: {mainWindow.WindowState}, Visibility: {mainWindow.Visibility}, ShowInTaskbar: {mainWindow.ShowInTaskbar}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 将窗口最小化到任务栏错误: {ex.Message}");
                LogMessage($"TaskbarManager: 错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 将窗口隐藏到托盘（不在任务栏显示）
        /// </summary>
        public void HideWindowToTray()
        {
            try
            {
                LogMessage("TaskbarManager: 将窗口隐藏到托盘");
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // 保存窗口当前位置和状态
                    SaveWindowPosition(mainWindow);
                    
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Visibility = Visibility.Hidden;
                    mainWindow.ShowInTaskbar = false;
                    // 不再使用_isWindowMinimizedToTray标志，直接基于窗口状态判断
                    LogMessage("TaskbarManager: 窗口已隐藏到托盘");

                    // 播放最小化动画
                    if (_taskbarAnimation != null)
                    {
                        _taskbarAnimation.MinimizeAnimation();
                        LogMessage("TaskbarManager: 播放窗口最小化动画");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 将窗口隐藏到托盘错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知消息</param>
        public void ShowNotification(string title, string message)
        {
            try
            {
                LogMessage($"TaskbarManager: 显示通知: {title} - {message}");
                if (_windowHandle != IntPtr.Zero && _isTrayIconVisible)
                {
                    _notifyIconData.szInfoTitle = title;
                    _notifyIconData.szInfo = message;
                    _notifyIconData.uTimeoutOrVersion = 5000; // 5秒后自动消失
                    _notifyIconData.dwInfoFlags = 0; // 无图标
                    _notifyIconData.uFlags |= NIF_INFO;

                    Shell_NotifyIcon(NIM_MODIFY, ref _notifyIconData);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 显示通知错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新托盘图标提示文本
        /// </summary>
        /// <param name="tooltipText">新的提示文本</param>
        public void UpdateTooltip(string tooltipText)
        {
            try
            {
                LogMessage($"TaskbarManager: 更新托盘图标提示文本: {tooltipText}");
                if (_windowHandle != IntPtr.Zero && _isTrayIconVisible)
                {
                    _notifyIconData.szTip = tooltipText;
                    Shell_NotifyIcon(NIM_MODIFY, ref _notifyIconData);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 更新托盘图标提示文本错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 闪烁窗口图标
        /// </summary>
        /// <param name="flashCount">闪烁次数，0表示一直闪烁</param>
        public void FlashIcon(uint flashCount = 0)
        {
            try
            {
                LogMessage($"TaskbarManager: 闪烁窗口图标，次数: {flashCount}");
                if (_windowHandle != IntPtr.Zero)
                {
                    FLASHWINFO flashInfo = new FLASHWINFO
                    {
                        cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                        hwnd = _windowHandle,
                        dwFlags = 3, // 闪烁窗口标题栏和任务栏按钮
                        uCount = flashCount,
                        dwTimeout = 50 // 闪烁间隔（毫秒）
                    };

                    FlashWindowEx(ref flashInfo);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 闪烁窗口图标错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止图标闪烁
        /// </summary>
        public void StopFlashIcon()
        {
            try
            {
                LogMessage("TaskbarManager: 停止图标闪烁");
                if (_windowHandle != IntPtr.Zero)
                {
                    FLASHWINFO flashInfo = new FLASHWINFO
                    {
                        cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                        hwnd = _windowHandle,
                        dwFlags = 0, // 停止闪烁
                        uCount = 0,
                        dwTimeout = 0
                    };

                    FlashWindowEx(ref flashInfo);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 停止图标闪烁错误: {ex.Message}");
            }
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放任务栏管理器资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                LogMessage("TaskbarManager: 释放任务栏管理器资源");

                // 移除窗口消息钩子
                if (_hwndSource != null)
                {
                    _hwndSource.RemoveHook(WndProc);
                    _hwndSource.Dispose();
                    _hwndSource = null;
                    LogMessage("TaskbarManager: 窗口消息钩子已移除");
                }

                // 移除任务栏图标
                if (_isTrayIconVisible && _windowHandle != IntPtr.Zero)
                {
                    Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);
                    _isTrayIconVisible = false;
                    LogMessage("TaskbarManager: 任务栏图标已移除");
                }

                // 销毁图标句柄
                if (_iconHandle != IntPtr.Zero)
                {
                    DestroyIcon(_iconHandle);
                    _iconHandle = IntPtr.Zero;
                    LogMessage("TaskbarManager: 图标句柄已销毁");
                }

                // 释放上下文菜单
                if (_contextMenu != null)
                {
                    _contextMenu.Items.Clear();
                    _contextMenu = null;
                    LogMessage("TaskbarManager: 上下文菜单已释放");
                }

                // 释放任务栏动画对象
                if (_taskbarAnimation != null)
                {
                    _taskbarAnimation.Dispose();
                    _taskbarAnimation = null;
                    LogMessage("TaskbarManager: 任务栏动画对象已释放");
                }

                LogMessage("TaskbarManager: 所有资源已成功释放");
            }
            catch (Exception ex)
            {
                LogMessage($"TaskbarManager: 释放资源错误: {ex.Message}");
            }
        }

        #endregion

        #region 日志辅助方法

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogMessage(string message)
        {
            try
            {
                // 使用TraceHelper记录日志
                TraceHelper.Record($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId}] {message}");
            }
            catch { }
        }

        #endregion
    }
}