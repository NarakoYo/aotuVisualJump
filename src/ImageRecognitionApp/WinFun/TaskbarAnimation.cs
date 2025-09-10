using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace ImageRecognitionApp.WinFun
{
    /// <summary>
    /// 任务栏动画类，负责处理任务栏图标的动画效果
    /// </summary>
    public class TaskbarAnimation : IDisposable
    {
        #region Win32 API 导入

        [DllImport("user32.dll")]
        private static extern int FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        #endregion

        #region 常量定义

        private const uint FLASHW_STOP = 0;
        private const uint FLASHW_CAPTION = 1;
        private const uint FLASHW_TRAY = 2;
        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMER = 4;
        private const uint FLASHW_TIMERNOFG = 12;

        #endregion

        #region 结构体定义

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

    private IntPtr _windowHandle;
    // 移除不再需要的动画状态管理变量，简化类结构以优化性能
    
    // 移除不再需要的获取屏幕刷新率方法，简化类结构以优化性能
    
    #region 额外的Win32 API导入
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    
    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
    
    private const int VERTREFRESH = 116; // 垂直刷新率索引
    
    #endregion

    // 用于存储原始图标位置
        private System.Windows.Point _originalIconPosition = new System.Windows.Point(-1, -1);
        
        // 移除不再需要的缓动函数，简化类结构以优化性能

    // 获取和设置任务栏图标位置的P/Invoke
    [DllImport("shell32.dll")]    
    private static extern bool SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private const uint ABM_GETTASKBARPOS = 5;
    private const uint ABM_SETSTATE = 10;
    private const uint ABM_WINDOWPOSCHANGED = 9;

        /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="window">WPF窗口</param>
    public TaskbarAnimation(Window window)
    {
        _windowHandle = new WindowInteropHelper(window).Handle;
        // 获取原始图标位置
        GetOriginalIconPosition();
    }

    /// <summary>
    /// 获取原始图标位置
    /// </summary>
    private void GetOriginalIconPosition()
    {
        if (_originalIconPosition.X == -1 && _originalIconPosition.Y == -1)
        {
            // 这里简化处理，实际应用中可能需要更复杂的方法来获取图标在任务栏的位置
            // 我们假设任务栏在底部
            var taskbarPos = GetTaskbarPosition();
            _originalIconPosition = new System.Windows.Point(taskbarPos.right - 30, taskbarPos.bottom - 30);
        }
    }

    /// <summary>
    /// 获取任务栏位置
    /// </summary>
    /// <returns>任务栏位置矩形</returns>
    private RECT GetTaskbarPosition()
    {
        APPBARDATA data = new APPBARDATA();
        data.cbSize = (uint)Marshal.SizeOf(data);
        SHAppBarMessage(ABM_GETTASKBARPOS, ref data);
        return data.rc;
    }

        /// <summary>
        /// 开始闪烁动画
        /// </summary>
        /// <param name="flashCount">闪烁次数，0表示无限闪烁</param>
        /// <param name="intervalMs">闪烁间隔（毫秒）</param>
        public void StartFlashAnimation(uint flashCount = 0, uint intervalMs = 500)
        {
            // 简化闪烁动画实现，直接使用Windows原生闪烁API
            var flashInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = _windowHandle,
                dwFlags = FLASHW_TRAY | FLASHW_TIMER,
                uCount = flashCount,
                dwTimeout = intervalMs
            };

            FlashWindowEx(ref flashInfo);
            (App.Current as App)?.LogMessage("StartFlashAnimation: 使用Windows原生闪烁API");
        }

        /// <summary>
        /// 停止闪烁动画
        /// </summary>
        public void StopFlashAnimation()
        {
            // 简化停止闪烁动画实现，直接使用Windows原生停止闪烁API
            var flashInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = _windowHandle,
                dwFlags = FLASHW_STOP,
                uCount = 0,
                dwTimeout = 0
            };

            FlashWindowEx(ref flashInfo);
            (App.Current as App)?.LogMessage("StopFlashAnimation: 使用Windows原生停止闪烁API");
        }

        /// <summary>
        /// 执行单次闪烁
        /// </summary>
        public void SingleFlash()
        {
            // 使用Windows原生单次闪烁API
            var flashInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = _windowHandle,
                dwFlags = FLASHW_TRAY,
                uCount = 1,
                dwTimeout = 0
            };

            FlashWindowEx(ref flashInfo);
            (App.Current as App)?.LogMessage("SingleFlash: 使用Windows原生单次闪烁API");
        }

        /// <summary>
    /// 显示注意力动画（闪烁+高亮）
    /// </summary>
    public void AttentionAnimation()
    {
        // 简化注意力动画实现，直接使用Windows原生闪烁API并设置窗口到前台
        var flashInfo = new FLASHWINFO
        {
            cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
            hwnd = _windowHandle,
            dwFlags = FLASHW_TRAY | FLASHW_ALL | FLASHW_TIMER,
            uCount = 3,
            dwTimeout = 300
        };

        FlashWindowEx(ref flashInfo);

        // 直接设置窗口到前台，不使用延迟任务以提高性能
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.Dispatcher.Invoke(() =>
            {
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.Activate();
                SetForegroundWindow(_windowHandle);
            });
        }
        (App.Current as App)?.LogMessage("AttentionAnimation: 使用Windows原生API显示注意力动画");
    }

    /// <summary>
    /// 向上跳跃动画
    /// </summary>
    public void JumpUpAnimation()
    {
        // 简化跳跃动画实现，直接使用单次闪烁代替跳跃效果以优化性能
        SingleFlash();
        (App.Current as App)?.LogMessage("JumpUpAnimation: 使用单次闪烁代替跳跃动画以优化性能");
    }

    /// <summary>
        /// 最小化动画：使用Windows原生动画效果，增强任务栏交互体验
        /// </summary>
        public void MinimizeAnimation()
        {
            try
            {
                // 验证窗口句柄是否有效
                if (_windowHandle == IntPtr.Zero)
                {
                    // 如果句柄无效，尝试重新获取
                    var window = Application.Current.MainWindow;
                    if (window != null)
                    {
                        _windowHandle = new WindowInteropHelper(window).Handle;
                        (App.Current as App)?.LogMessage("MinimizeAnimation: 重新获取窗口句柄");
                    }
                }

                if (_windowHandle != IntPtr.Zero)
                {
                    // 直接设置窗口为最小化，使用Windows原生动画效果
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var window = Application.Current.MainWindow;
                        if (window == null)
                            return;

                        // 确保窗口在任务栏显示
                        window.ShowInTaskbar = true;
                        
                        // 直接设置窗口状态为最小化，让Windows触发原生的向下跳动动画
                        window.WindowState = WindowState.Minimized;
                        (App.Current as App)?.LogMessage("MinimizeAnimation: 触发Windows原生的向下跳动动画效果");
                    });
                }
                else
                {
                    (App.Current as App)?.LogMessage("MinimizeAnimation: 窗口句柄无效，无法触发动画");
                }
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"最小化动画执行出错: {ex.Message}");
            }
        }

    /// <summary>
    /// 向下跳跃动画
    /// </summary>
    public void JumpDownAnimation()
    {
        // 简化跳跃动画实现，直接使用单次闪烁代替跳跃效果以优化性能
        SingleFlash();
        (App.Current as App)?.LogMessage("JumpDownAnimation: 使用单次闪烁代替跳跃动画以优化性能");
    }

    /// <summary>
    /// 恢复动画：使用Windows原生动画效果，增强任务栏交互体验
    /// </summary>
    public void RestoreAnimation()
    {
        try
        {
            // 验证窗口句柄是否有效
            if (_windowHandle == IntPtr.Zero)
            {
                // 如果句柄无效，尝试重新获取
                var window = Application.Current.MainWindow;
                if (window != null)
                {
                    _windowHandle = new WindowInteropHelper(window).Handle;
                    (App.Current as App)?.LogMessage("RestoreAnimation: 重新获取窗口句柄");
                }
            }

            if (_windowHandle != IntPtr.Zero)
            {
                // 直接恢复窗口，不使用闪烁动画
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = Application.Current.MainWindow;
                    if (window == null)
                        return;

                    // 确保窗口在任务栏显示
                    window.ShowInTaskbar = true;
                    // 确保窗口可见 - 这是解决隐藏到托盘后无法还原问题的关键
                    window.Visibility = Visibility.Visible;
                    // 额外调用Show()方法确保窗口被显示
                    window.Show();

                    // 通过TaskbarManager单例访问保存的窗口位置和大小
                    var taskbarManager = TaskbarManager.Instance;
                    if (taskbarManager != null)
                    {
                        // 正确恢复窗口位置和大小
                        window.Left = taskbarManager.LastWindowLeft;
                        window.Top = taskbarManager.LastWindowTop;
                        window.Width = taskbarManager.LastWindowWidth;
                        window.Height = taskbarManager.LastWindowHeight;
                             
                        // 如果上次是最大化状态，则恢复最大化
                        if (taskbarManager.LastWindowState == WindowState.Maximized)
                        {
                            window.WindowState = WindowState.Maximized;
                        }
                        else
                        {
                            // 使用Windows原生动画恢复到普通状态
                            window.WindowState = WindowState.Normal;
                        }
                    }
                    else
                    {
                        // 备用方案：直接恢复到普通状态
                        window.WindowState = WindowState.Normal;
                    }

                    // 确保窗口在屏幕内（防止窗口位置超出屏幕范围）
                    EnsureWindowInScreen(window);
                    
                    // 使用Windows API设置窗口到前台，增强原生体验
                    SetForegroundWindow(new WindowInteropHelper(window).Handle);
                    
                    // 确保窗口正确激活
                    window.Activate();
                    window.Focus();
                    
                    // 临时设置为Topmost然后立即取消，确保窗口能被用户看到
                    window.Topmost = true;
                    window.Topmost = false;
                    
                    (App.Current as App)?.LogMessage("RestoreAnimation: 使用Windows原生任务栏动画恢复窗口完成");
                });
            }
            else
            {
                (App.Current as App)?.LogMessage("RestoreAnimation: 窗口句柄无效，无法执行闪烁动画");
            }
        }
        catch (Exception ex)
        {
            (App.Current as App)?.LogMessage($"恢复动画执行出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 确保窗口在屏幕可见范围内
    /// </summary>
    /// <param name="window">要检查的窗口</param>
    private void EnsureWindowInScreen(Window window)
    {
        try
        {
            // 获取主屏幕工作区
            var screen = SystemParameters.WorkArea;
            
            // 检查并修正窗口位置
            if (window.Left < screen.Left)
                window.Left = screen.Left;
            if (window.Top < screen.Top)
                window.Top = screen.Top;
            
            // 确保窗口不会超出屏幕右边界
            if (window.Left + window.Width > screen.Right)
                window.Left = screen.Right - window.Width;
            // 确保窗口不会超出屏幕下边界
            if (window.Top + window.Height > screen.Bottom)
                window.Top = screen.Bottom - window.Height;
            
            // 确保窗口至少有一部分可见
            if (window.Left >= screen.Right || window.Top >= screen.Bottom)
            {
                // 如果窗口完全在屏幕外，则居中显示
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Left = (screen.Width - window.Width) / 2;
                window.Top = (screen.Height - window.Height) / 2;
            }
        }
        catch (Exception ex)
        {
            (App.Current as App)?.LogMessage($"EnsureWindowInScreen执行出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 移动图标到指定位置
    /// </summary>
    /// <param name="position">目标位置</param>
    private void MoveIcon(System.Windows.Point position)
    {
        // 注意：这里只是一个示例实现，实际上Windows不允许直接移动任务栏图标
        // 真实应用中，可能需要创建一个自定义的任务栏图标或使用第三方库
        // 这里我们通过修改图标的方式来模拟移动效果
        // 在实际项目中，你可能需要使用如TaskbarIcon库等第三方组件

        // 记录日志，表示图标移动
        (App.Current as App)?.LogMessage($"图标移动到位置: X={position.X}, Y={position.Y}");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 简化Dispose方法，不再需要等待动画任务完成
        (App.Current as App)?.LogMessage("TaskbarAnimation: 资源已释放");
    }
}
}