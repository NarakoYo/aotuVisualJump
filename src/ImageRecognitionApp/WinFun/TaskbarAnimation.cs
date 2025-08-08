using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;

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

    private readonly IntPtr _windowHandle;
    private bool _isAnimating = false;
    private Task? _animationTask = null;
    private const int JUMP_HEIGHT = 10; // 跳跃高度（像素）
    private const int JUMP_DURATION_MS = 300; // 跳跃持续时间（毫秒）

    // 用于存储原始图标位置
    private System.Windows.Point _originalIconPosition = new System.Windows.Point(-1, -1);

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
            if (_isAnimating)
                return;

            _isAnimating = true;

            _animationTask = Task.Run(() =>
            {
                var flashInfo = new FLASHWINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                    hwnd = _windowHandle,
                    dwFlags = FLASHW_TRAY | FLASHW_TIMER,
                    uCount = flashCount,
                    dwTimeout = intervalMs
                };

                FlashWindowEx(ref flashInfo);
            });
        }

        /// <summary>
        /// 停止闪烁动画
        /// </summary>
        public void StopFlashAnimation()
        {
            if (!_isAnimating)
                return;

            _isAnimating = false;

            var flashInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = _windowHandle,
                dwFlags = FLASHW_STOP,
                uCount = 0,
                dwTimeout = 0
            };

            FlashWindowEx(ref flashInfo);

            if (_animationTask != null && !_animationTask.IsCompleted)
            {
                _animationTask.Wait();
                _animationTask = null;
            }
        }

        /// <summary>
        /// 执行单次闪烁
        /// </summary>
        public void SingleFlash()
        {
            var flashInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = _windowHandle,
                dwFlags = FLASHW_TRAY,
                uCount = 1,
                dwTimeout = 0
            };

            FlashWindowEx(ref flashInfo);
        }

        /// <summary>
    /// 显示注意力动画（闪烁+高亮）
    /// </summary>
    public void AttentionAnimation()
    {
        // 先闪烁3次
        StartFlashAnimation(3, 300);

        // 短暂延迟后将窗口置于前台
        Task.Delay(1000).ContinueWith(_ =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.Show();
                    Application.Current.MainWindow.Activate();
                    SetForegroundWindow(_windowHandle);
                }
            });
        });
    }

    /// <summary>
    /// 向上跳跃动画
    /// </summary>
    public void JumpUpAnimation()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        _animationTask = Task.Run(() =>
        {
            // 获取窗口图标
            Icon originalIcon = null;
            IntPtr iconHandle = IntPtr.Zero;

            try
            {
                // 获取原始图标
                var window = Application.Current.MainWindow;
                if (window?.Icon != null)
                {
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
                                originalIcon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
                                iconHandle = originalIcon.Handle;
                            }
                        }
                    }
                }

                // 执行跳跃动画
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 这里简化处理，实际应用中可能需要创建一个透明窗口来显示动画
                    // 向上移动图标位置
                    MoveIcon(new System.Windows.Point(_originalIconPosition.X, _originalIconPosition.Y - JUMP_HEIGHT));
                });

                // 等待一段时间
                Task.Delay(JUMP_DURATION_MS / 2).Wait();

                // 恢复原始位置
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MoveIcon(_originalIconPosition);
                });
            }
            finally
            {
                _isAnimating = false;
                if (originalIcon != null)
                {
                    originalIcon.Dispose();
                }
            }
        });
    }

    /// <summary>
    /// 向下跳跃动画
    /// </summary>
    public void JumpDownAnimation()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        _animationTask = Task.Run(() =>
        {
            // 获取窗口图标
            Icon originalIcon = null;
            IntPtr iconHandle = IntPtr.Zero;

            try
            {
                // 获取原始图标
                var window = Application.Current.MainWindow;
                if (window?.Icon != null)
                {
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
                                originalIcon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
                                iconHandle = originalIcon.Handle;
                            }
                        }
                    }
                }

                // 执行跳跃动画
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 这里简化处理，实际应用中可能需要创建一个透明窗口来显示动画
                    // 向下移动图标位置
                    MoveIcon(new System.Windows.Point(_originalIconPosition.X, _originalIconPosition.Y + JUMP_HEIGHT));
                });

                // 等待一段时间
                Task.Delay(JUMP_DURATION_MS / 2).Wait();

                // 恢复原始位置
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MoveIcon(_originalIconPosition);
                });
            }
            finally
            {
                _isAnimating = false;
                if (originalIcon != null)
                {
                    originalIcon.Dispose();
                }
            }
        });
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
        // 停止动画
        if (_isAnimating && _animationTask != null)
        {
            _isAnimating = false;
            if (!_animationTask.IsCompleted)
            {
                try
                {
                    _animationTask.Wait(1000); // 等待最多1秒
                }
                catch (Exception ex)
                {
                    (App.Current as App)?.LogMessage($"等待动画任务完成时出错: {ex.Message}");
                }
                _animationTask = null;
            }
        }
    }
}
}