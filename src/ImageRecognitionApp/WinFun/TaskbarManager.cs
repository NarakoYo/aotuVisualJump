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

    private readonly IntPtr _windowHandle;
    private readonly uint _taskbarIconId = 1001; // 设置默认图标ID
    private NOTIFYICONDATA _notifyIconData;
    private IntPtr _iconHandle = IntPtr.Zero; // 存储图标句柄以便释放
    private readonly TaskbarAnimation _taskbarAnimation; // 任务栏动画引用

    #endregion

        #region Win32 API 导入

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("shell32.dll")]
        private static extern int Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

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
            _windowHandle = new WindowInteropHelper(window).Handle;
            _taskbarAnimation = new TaskbarAnimation(window);
            InitializeTaskbarIcon();

            // 注册窗口消息处理
            HwndSource.FromHwnd(_windowHandle)?.AddHook(WndProc);
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
                _notifyIconData = new NOTIFYICONDATA
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                    hWnd = _windowHandle,
                    uID = _taskbarIconId,
                    uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
                    uCallbackMessage = WM_NOTIFYICON,
                    hIcon = GetWindowIconHandle(),
                    szTip = System.Windows.Application.Current.MainWindow?.Title ?? "Image Recognition App"
                };

                // 添加任务栏图标
                int addResult = Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
                if (addResult != 0)
                {
                    (App.Current as App)?.LogMessage("任务栏图标添加成功");
                }
                else
                {
                    (App.Current as App)?.LogMessage("任务栏图标添加失败");
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
                if (msg == WM_NOTIFYICON)
                {
                    // 处理任务栏图标消息
                    uint message = (uint)lParam;
                    (App.Current as App)?.LogMessage($"接收到任务栏消息: {message}");

                    switch (message)
                    {
                        case 0x0201:  // 鼠标左键点击
                            OnLeftClick();
                            handled = true;
                            break;
                        case 0x0204:  // 鼠标右键点击
                            OnRightClick();
                            handled = true;
                            break;
                        default:
                            (App.Current as App)?.LogMessage($"未知的任务栏消息: {message}");
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
                // 切换窗口显示/隐藏
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    if (System.Windows.Application.Current.MainWindow.IsVisible)
                    {
                        // 窗口可见，隐藏窗口并播放向下跳跃动画
                        System.Windows.Application.Current.MainWindow.Hide();
                        _taskbarAnimation.JumpDownAnimation();
                        (App.Current as App)?.LogMessage("窗口已隐藏，播放向下跳跃动画");
                    }
                    else
                    {
                        // 窗口不可见，显示窗口并播放向上跳跃动画
                        System.Windows.Application.Current.MainWindow.Show();
                        SetForegroundWindow(_windowHandle);
                        _taskbarAnimation.JumpUpAnimation();
                        (App.Current as App)?.LogMessage("窗口已显示，播放向上跳跃动画");
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