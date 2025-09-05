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

    private readonly IntPtr _windowHandle;
    // 动画状态标志
    private bool _isAnimating = false;
    
    /// <summary>
    /// 获取动画是否正在运行的状态
    /// </summary>
    public bool IsAnimating
    {
        get { return _isAnimating; }
    }
    private Task? _animationTask = null;
    private const int JUMP_HEIGHT = 10; // 跳跃高度（像素）
    private const int JUMP_DURATION_MS = 200; // 跳跃动画持续时间（毫秒）
    private const int MINIMIZE_ANIMATION_DURATION_MS = 120; // 最小化动画持续时间（毫秒）- 已缩短以提升流畅度
    private const int RESTORE_ANIMATION_DURATION_MS = 120; // 恢复动画持续时间（毫秒）
    private const double MINIMUM_SCALE = 0.0001; // 最小缩放比例
    // 使用当前屏幕的最大刷新率作为动画帧率，提升动画流畅度
    private static readonly int FRAMES_PER_SECOND = GetMaxScreenRefreshRate();
    
    /// <summary>
    /// 获取当前屏幕的最大刷新率
    /// </summary>
    /// <returns>最大刷新率</returns>
    private static int GetMaxScreenRefreshRate()
    {
        try
        {
            // 导入GDI32.dll中的GetDeviceCaps函数
            IntPtr hScreenDC = GetDC(IntPtr.Zero);
            int refreshRate = GetDeviceCaps(hScreenDC, VERTREFRESH);
            ReleaseDC(IntPtr.Zero, hScreenDC);
            
            // 确保获取到有效刷新率，如果获取失败则使用默认值
            return refreshRate > 0 ? refreshRate : 60;
        }
        catch
        {
            // 默认返回60FPS作为fallback
            return 60;
        }
    }
    
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
        
        // 缓入立方曲线
        private double EaseInCubic(double t)
        {
            return t * t * t;
        }

        /// <summary>
        /// 缓出立方曲线
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        /// <summary>
        /// 缓入缓出四次方曲线 - 提供更平滑的动画效果
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private double EaseInOutQuart(double t)
        {
            return t < 0.5 ? 8 * Math.Pow(t, 4) : 1 - Math.Pow(-2 * t + 2, 4) / 2;
        }

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
            Icon? originalIcon = null;
            IntPtr iconHandle = IntPtr.Zero;

            try
            {
                // 获取原始图标
                var window = Application.Current?.MainWindow;
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
                                if (originalIcon != null)
                                {
                                    iconHandle = originalIcon.Handle;
                                }
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
        /// 最小化动画：根据比例缩小画面，并向任务栏中央移动
        /// </summary>
        public void MinimizeAnimation()
        {
            if (_isAnimating)
                return;

            _isAnimating = true;

            _animationTask = Task.Run(() =>
            {
                try
                {
                    // 保存窗口状态信息
                    WindowState originalState = WindowState.Normal;
                    bool originalTopmost = false;
                    Visibility originalVisibility = Visibility.Visible;
                    bool originalIsHitTestVisible = true;
                    System.Windows.Point originalPosition = new System.Windows.Point(0, 0);
                    double originalWidth = 0;
                    double originalHeight = 0;
                    double originalOpacity = 1;
                    double taskbarCenterX = 0;
                    double taskbarCenterY = 0;

                    // 在UI线程上获取窗口信息
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var window = Application.Current.MainWindow;
                        if (window == null)
                            return;

                        // 保存当前窗口状态
                        originalState = window.WindowState;
                        originalTopmost = window.Topmost;
                        originalVisibility = window.Visibility;
                        originalIsHitTestVisible = window.IsHitTestVisible;

                        // 确保窗口是正常状态
                        if (window.WindowState != WindowState.Normal)
                        {
                            window.WindowState = WindowState.Normal;
                            (App.Current as App)?.LogMessage("MinimizeAnimation: 恢复窗口到正常大小以进行动画");
                        }

                        // 获取窗口原始位置和尺寸
                        originalPosition = new System.Windows.Point(window.Left, window.Top);
                        originalWidth = window.Width;
                        originalHeight = window.Height;
                        originalOpacity = window.Opacity;

                        // 获取任务栏中央位置
                        var taskbarPos = GetTaskbarPosition();
                        taskbarCenterX = taskbarPos.left + (taskbarPos.right - taskbarPos.left) / 2;
                        taskbarCenterY = taskbarPos.top + (taskbarPos.bottom - taskbarPos.top) / 2;

                        // 禁用窗口点击测试，防止用户操作
                        window.IsHitTestVisible = false;
                    });

                    // 计算更精确的帧率和动画参数
                    int frameCount = Math.Max(15, (int)(MINIMIZE_ANIMATION_DURATION_MS / 1000.0 * FRAMES_PER_SECOND)); // 至少15帧确保动画可见
                    int delayPerFrame = MINIMIZE_ANIMATION_DURATION_MS / frameCount;
                      
                    // 使用更精确的计时器
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    long frameStartTime = stopwatch.ElapsedMilliseconds;

                    // 执行动画
        long totalDuration = 0;
        long previousFrameTime = stopwatch.ElapsedMilliseconds;
        
        for (int i = 1; i <= frameCount; i++)
        {
            // 计算当前进度（使用缓动函数使动画更自然）
            double progress = (double)i / frameCount;
            double easeProgress = EaseInOutQuart(progress); // 使用更平滑的缓入缓出四次方曲线

                        // 计算当前缩放比例（从1到最小缩放比例）
                        double currentScale = 1 - (1 - MINIMUM_SCALE) * easeProgress;

                        // 计算当前位置（从原始位置到任务栏中央）
                        double currentX = taskbarCenterX - originalWidth / 2 * MINIMUM_SCALE + (originalPosition.X - (taskbarCenterX - originalWidth / 2 * MINIMUM_SCALE)) * (1 - easeProgress);
                        double currentY = taskbarCenterY - originalHeight / 2 * MINIMUM_SCALE + (originalPosition.Y - (taskbarCenterY - originalHeight / 2 * MINIMUM_SCALE)) * (1 - easeProgress);

                        // 计算当前透明度（逐渐降低）
                        double currentOpacity = originalOpacity * (1 - easeProgress);

                        // 只在UI线程上更新窗口属性，提高性能
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var window = Application.Current.MainWindow;
                            if (window == null)
                                return;

                            // 批量更新窗口属性，减少布局和渲染调用
                window.Left = currentX;
                window.Top = currentY;
                window.Width = originalWidth * currentScale;
                window.Height = originalHeight * currentScale;
                window.Opacity = currentOpacity;

                // 避免不必要的UI渲染调用，让WPF自然处理渲染
                        });

                        // 使用更精确的计时和等待方式，避免Thread.Sleep的精度问题
                        long currentFrameTime = stopwatch.ElapsedMilliseconds;
                        long frameTime = currentFrameTime - previousFrameTime;
                        totalDuration += frameTime;
                        
                        // 如果帧时间太短，等待剩余时间
                        if (frameTime < delayPerFrame)
                        {
                            int waitTime = (int)(delayPerFrame - frameTime);
                            // 使用Task.Delay代替Thread.Sleep，提高等待精度
                            Task.Delay(waitTime).Wait();
                        }
                        
                        previousFrameTime = stopwatch.ElapsedMilliseconds;
                    }

                    // 动画结束后，先恢复窗口原始大小和位置，但保持不可见
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var window = Application.Current.MainWindow;
                        if (window == null)
                            return;

                        // 先恢复窗口原始大小和位置
                        window.Left = originalPosition.X;
                        window.Top = originalPosition.Y;
                        window.Width = originalWidth;
                        window.Height = originalHeight;
                        window.Opacity = originalOpacity;
                        
                        // 确保窗口在任务栏显示
                        window.ShowInTaskbar = true;
                        
                        // 然后设置窗口状态为最小化
                        window.WindowState = WindowState.Minimized;

                        // 恢复窗口设置
                        window.IsHitTestVisible = originalIsHitTestVisible;
                        
                        (App.Current as App)?.LogMessage("MinimizeAnimation: 动画完成，恢复窗口原始大小并设置为最小化，确保在任务栏显示");
                    });
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"最小化动画执行出错: {ex.Message}");
            }
            finally
            {
                _isAnimating = false;
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
            Icon? originalIcon = null;
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
    /// 恢复动画：从任务栏中央移动到原始位置，并根据比例放大画面
    /// </summary>
    public void RestoreAnimation()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        _animationTask = Task.Run(() =>
        {
            try
            {
                // 保存窗口状态信息
                WindowState originalState = WindowState.Normal;
                bool originalTopmost = false;
                Visibility originalVisibility = Visibility.Visible;
                bool originalIsHitTestVisible = true;
                System.Windows.Point originalPosition = new System.Windows.Point(0, 0);
                double originalWidth = 0;
                double originalHeight = 0;
                double originalOpacity = 1;
                double taskbarCenterX = 0;
                double taskbarCenterY = 0;

                // 在UI线程上获取窗口信息和初始化
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = Application.Current.MainWindow;
                    if (window == null)
                        return;

                    // 保存当前窗口状态
                    originalState = window.WindowState;
                    originalTopmost = window.Topmost;
                    originalVisibility = window.Visibility;
                    originalIsHitTestVisible = window.IsHitTestVisible;

                    // 获取窗口原始位置和尺寸
                    originalPosition = new System.Windows.Point(window.Left, window.Top);
                    originalWidth = window.Width;
                    originalHeight = window.Height;
                    originalOpacity = window.Opacity;

                    // 获取任务栏中央位置
                    var taskbarPos = GetTaskbarPosition();
                    taskbarCenterX = taskbarPos.left + (taskbarPos.right - taskbarPos.left) / 2;
                    taskbarCenterY = taskbarPos.top + (taskbarPos.bottom - taskbarPos.top) / 2;

                    // 设置窗口初始位置为任务栏中央，尺寸为最小缩放比例
                    window.Left = taskbarCenterX - originalWidth / 2 * MINIMUM_SCALE;
                    window.Top = taskbarCenterY - originalHeight / 2 * MINIMUM_SCALE;
                    window.Width = originalWidth * MINIMUM_SCALE;
                    window.Height = originalHeight * MINIMUM_SCALE;
                    window.Opacity = 0;

                    // 确保窗口状态为正常并先设置为不可见，避免闪烁
                    window.WindowState = WindowState.Normal;
                    window.Visibility = Visibility.Collapsed;

                    // 禁用窗口点击测试，防止用户操作
                    window.IsHitTestVisible = false;
                });

                // 计算更精确的帧率和动画参数
                int frameCount = Math.Max(15, (int)(RESTORE_ANIMATION_DURATION_MS / 1000.0 * FRAMES_PER_SECOND)); // 至少15帧确保动画可见
                int delayPerFrame = RESTORE_ANIMATION_DURATION_MS / frameCount;
                  
                // 使用更精确的计时器
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                long frameStartTime = stopwatch.ElapsedMilliseconds;
                  
                // 执行动画
        long totalDuration = 0;
        long previousFrameTime = stopwatch.ElapsedMilliseconds;
        
        for (int i = 1; i <= frameCount; i++)
        {
            // 计算当前进度（使用缓动函数使动画更自然）
            double progress = (double)i / frameCount;
            double easeProgress = EaseInOutQuart(progress); // 使用更平滑的缓入缓出四次方曲线

                    // 计算当前缩放比例（从最小缩放比例到1）
                    double currentScale = MINIMUM_SCALE + (1 - MINIMUM_SCALE) * easeProgress;

                    // 计算当前位置（从任务栏中央到原始位置）
                    double currentX = taskbarCenterX - originalWidth / 2 * MINIMUM_SCALE + (originalPosition.X - (taskbarCenterX - originalWidth / 2 * MINIMUM_SCALE)) * easeProgress;
                    double currentY = taskbarCenterY - originalHeight / 2 * MINIMUM_SCALE + (originalPosition.Y - (taskbarCenterY - originalHeight / 2 * MINIMUM_SCALE)) * easeProgress;

                    // 计算当前透明度（逐渐增加）
                    double currentOpacity = originalOpacity * easeProgress;

                    // 只在UI线程上更新窗口属性，提高性能
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var window = Application.Current.MainWindow;
                        if (window == null)
                            return;

                        // 在第一帧时将窗口设置为可见，避免闪烁
                        if (i == 1)
                        {
                            window.Visibility = Visibility.Visible;
                        }
                        // 批量更新窗口属性，减少布局和渲染调用
                window.Left = currentX;
                window.Top = currentY;
                window.Width = originalWidth * currentScale;
                window.Height = originalHeight * currentScale;
                window.Opacity = currentOpacity;

                // 避免不必要的UI渲染调用，让WPF自然处理渲染
                    });

                    // 使用更精确的计时和等待方式，避免Thread.Sleep的精度问题
                    long currentFrameTime = stopwatch.ElapsedMilliseconds;
                    long frameTime = currentFrameTime - previousFrameTime;
                    totalDuration += frameTime;
                    
                    // 如果帧时间太短，等待剩余时间
                    if (frameTime < delayPerFrame)
                    {
                        int waitTime = (int)(delayPerFrame - frameTime);
                        // 使用Task.Delay代替Thread.Sleep，提高等待精度
                        Task.Delay(waitTime).Wait();
                    }
                    
                    previousFrameTime = stopwatch.ElapsedMilliseconds;
                }

                // 动画结束后，恢复窗口到原始状态并激活
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var window = Application.Current.MainWindow;
                        if (window == null)
                            return;

                        // 确保窗口先恢复到正常状态
                        window.WindowState = WindowState.Normal;
                        
                        // 重要：明确设置窗口在任务栏显示
                        window.ShowInTaskbar = true;
                          
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
                        }
                          
                        window.Opacity = originalOpacity;
                           
                        // 恢复窗口设置
                        window.IsHitTestVisible = originalIsHitTestVisible;
                           
                        // 确保窗口正确激活，设置焦点
                        // 添加强制激活窗口的逻辑，防止窗口恢复后再次最小化
                        window.Activate();
                        window.Focus();
                        
                        // 添加额外的保护措施，确保窗口状态正确
                        if (window.WindowState != WindowState.Normal && window.WindowState != WindowState.Maximized)
                        {
                            window.WindowState = WindowState.Normal;
                        }
                        
                        // 再次确保窗口在任务栏显示
                        window.ShowInTaskbar = true;
                    });
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"恢复动画执行出错: {ex.Message}");
            }
            finally
            {
                _isAnimating = false;
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