using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ImageRecognitionApp.Utils
{
    /// <summary>
    /// 用户输入监控器 - 监控键盘按键、鼠标点击和移动、音频和摄像头输入
    /// </summary>
    public class UserInputMonitor
    {
        #region 单例模式实现

        // 私有静态实例
        private static readonly Lazy<UserInputMonitor> _instance = new Lazy<UserInputMonitor>(() => new UserInputMonitor());

        // 私有构造函数
        private UserInputMonitor()
        {
            InitializeMonitor();
        }

        // 公开的实例访问属性
        public static UserInputMonitor Instance => _instance.Value;

        #endregion

        #region 成员变量

        // 键盘钩子相关
        private IntPtr _keyboardHookId = IntPtr.Zero;
        private static KeyboardHookCallback _keyboardHookCallback;

        // 鼠标钩子相关
        private IntPtr _mouseHookId = IntPtr.Zero;
        private static MouseHookCallback _mouseHookCallback;

        // 监控状态标志
        private bool _isKeyboardMonitoringEnabled = true;
        private bool _isMouseMonitoringEnabled = true;
        private bool _isAudioMonitoringEnabled = false;
        private bool _isCameraMonitoringEnabled = false;

        // 最后输入时间戳
        private DateTime _lastInputTime = DateTime.Now;

        #endregion

        #region 委托和事件定义

        // 键盘事件委托
        public delegate void KeyboardEventHandler(object sender, KeyEventArgs e);
        public event KeyboardEventHandler KeyPressed;
        public event KeyboardEventHandler KeyReleased;

        // 鼠标事件委托
        public delegate void MouseEventHandler(object sender, MouseEventArgs e);
        public event MouseEventHandler MouseClicked;
        public event MouseEventHandler MouseMoved;
        public event MouseEventHandler MouseWheel;

        // Windows组件点击事件委托
        public delegate void ComponentClickedEventHandler(object sender, ComponentClickedEventArgs e);
        public event ComponentClickedEventHandler ComponentClicked;

        // 音频输入事件委托
        public delegate void AudioInputEventHandler(object sender, AudioInputEventArgs e);
        public event AudioInputEventHandler AudioInputDetected;

        // 摄像头输入事件委托
        public delegate void CameraInputEventHandler(object sender, CameraInputEventArgs e);
        public event CameraInputEventHandler CameraInputDetected;

        #endregion

        #region 监控配置属性

        /// <summary>
        /// 是否启用键盘监控
        /// </summary>
        public bool IsKeyboardMonitoringEnabled
        {
            get => _isKeyboardMonitoringEnabled;
            set
            {
                if (_isKeyboardMonitoringEnabled != value)
                {
                    _isKeyboardMonitoringEnabled = value;
                    UpdateKeyboardHook();
                }
            }
        }

        /// <summary>
        /// 是否启用鼠标监控
        /// </summary>
        public bool IsMouseMonitoringEnabled
        {
            get => _isMouseMonitoringEnabled;
            set
            {
                if (_isMouseMonitoringEnabled != value)
                {
                    _isMouseMonitoringEnabled = value;
                    UpdateMouseHook();
                }
            }
        }

        /// <summary>
        /// 是否启用音频监控
        /// </summary>
        public bool IsAudioMonitoringEnabled
        {
            get => _isAudioMonitoringEnabled;
            set
            {
                if (_isAudioMonitoringEnabled != value)
                {
                    _isAudioMonitoringEnabled = value;
                    UpdateAudioMonitoring();
                }
            }
        }

        /// <summary>
        /// 是否启用摄像头监控
        /// </summary>
        public bool IsCameraMonitoringEnabled
        {
            get => _isCameraMonitoringEnabled;
            set
            {
                if (_isCameraMonitoringEnabled != value)
                {
                    _isCameraMonitoringEnabled = value;
                    UpdateCameraMonitoring();
                }
            }
        }

        #endregion

        #region 初始化和清理方法

        /// <summary>
        /// 初始化监控器
        /// </summary>
        private void InitializeMonitor()
        {
            // 初始化钩子和监控组件
            _keyboardHookCallback = KeyboardHookCallbackImplementation;
            _mouseHookCallback = MouseHookCallbackImplementation;

            // 注册钩子
            UpdateKeyboardHook();
            UpdateMouseHook();
        }

        /// <summary>
        /// 启动所有监控
        /// </summary>
        public void StartAllMonitoring()
        {
            IsKeyboardMonitoringEnabled = true;
            IsMouseMonitoringEnabled = true;
            IsAudioMonitoringEnabled = true;
            IsCameraMonitoringEnabled = true;
        }

        /// <summary>
        /// 停止所有监控
        /// </summary>
        public void StopAllMonitoring()
        {
            IsKeyboardMonitoringEnabled = false;
            IsMouseMonitoringEnabled = false;
            IsAudioMonitoringEnabled = false;
            IsCameraMonitoringEnabled = false;
        }

        #endregion

        #region 钩子相关的Windows API

        // 钩子类型定义
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        // 键盘消息
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        // 鼠标消息
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;

        // 委托定义
        private delegate IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam);

        // 导入Windows API函数
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookCallback lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, MouseHookCallback lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        #endregion

        #region 键盘监控实现

        /// <summary>
        /// 更新键盘钩子状态
        /// </summary>
        private void UpdateKeyboardHook()
        {
            if (_isKeyboardMonitoringEnabled && _keyboardHookId == IntPtr.Zero)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookCallback, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            else if (!_isKeyboardMonitoringEnabled && _keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 键盘钩子回调函数实现
        /// </summary>
        private IntPtr KeyboardHookCallbackImplementation(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);

                // 更新最后输入时间
                _lastInputTime = DateTime.Now;

                // 处理按键按下事件
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    KeyPressed?.Invoke(this, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(Application.Current.MainWindow), 0, key));
                }
                // 处理按键释放事件
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    KeyReleased?.Invoke(this, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(Application.Current.MainWindow), 0, key));
                }
            }

            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        #endregion

        #region 鼠标监控实现

        /// <summary>
        /// 更新鼠标钩子状态
        /// </summary>
        private void UpdateMouseHook()
        {
            if (_isMouseMonitoringEnabled && _mouseHookId == IntPtr.Zero)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookCallback, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            else if (!_isMouseMonitoringEnabled && _mouseHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 鼠标钩子回调函数实现
        /// </summary>
        private IntPtr MouseHookCallbackImplementation(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // 从lParam中获取鼠标位置
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                Point mousePosition = new Point(hookStruct.pt.x, hookStruct.pt.y);

                // 更新最后输入时间
                _lastInputTime = DateTime.Now;

                // 处理鼠标点击事件
                if (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN ||
                    wParam == (IntPtr)WM_LBUTTONUP || wParam == (IntPtr)WM_RBUTTONUP)
                {
                    MouseButtonState leftButton = MouseButtonState.Released;
                    MouseButtonState rightButton = MouseButtonState.Released;

                    if (wParam == (IntPtr)WM_LBUTTONDOWN)
                        leftButton = MouseButtonState.Pressed;
                    else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                        rightButton = MouseButtonState.Pressed;

                    // 创建一个基本的MouseEventArgs，并设置必要的属性
                    MouseEventArgs e = new MouseEventArgs(Mouse.PrimaryDevice, 0);
                    
                    // 这里我们不能直接设置位置和按钮状态，因为这些属性是只读的
                    // 但我们可以创建自定义的鼠标事件数据类来传递这些信息
                    // 为了简化，我们使用基本的MouseEventArgs并通过事件传递
                    // 实际应用中可能需要创建自定义事件参数类

                    MouseClicked?.Invoke(this, e);
                }
                // 处理鼠标移动事件
                else if (wParam == (IntPtr)WM_MOUSEMOVE)
                {
                    // 创建一个基本的MouseEventArgs
                    MouseEventArgs e = new MouseEventArgs(Mouse.PrimaryDevice, 0);

                    MouseMoved?.Invoke(this, e);
                }
                // 处理鼠标滚轮事件
                else if (wParam == (IntPtr)WM_MOUSEWHEEL)
                {
                    // 获取滚轮增量
                    int delta = Marshal.ReadInt32(lParam, 8);

                    // 创建一个包含滚轮增量的MouseWheelEventArgs
                    MouseWheelEventArgs e = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, delta);
                    e.RoutedEvent = UIElement.MouseWheelEvent;

                    MouseWheel?.Invoke(this, e);
                }

                // 检测Windows组件点击
                DetectComponentClick(mousePosition);
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        #endregion

        #region Windows组件点击检测

        /// <summary>
        /// 检测Windows组件点击
        /// </summary>
        private void DetectComponentClick(Point mousePosition)
        {
            try
            {
                // 获取鼠标位置下的元素
                var element = FindElementAtPoint(mousePosition);
                if (element != null)
                {
                    string componentName = element.GetType().Name;
                    string componentId = GetElementName(element);

                    // 触发组件点击事件
                    ComponentClicked?.Invoke(this, new ComponentClickedEventArgs(element, componentName, componentId, mousePosition));
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不中断监控
                LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"UserInputMonitor: 检测组件点击时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 在指定点查找元素
        /// </summary>
        private object FindElementAtPoint(Point point)
        {
            try
            {
                // 将屏幕坐标转换为WPF坐标
                if (Application.Current.MainWindow != null)
                {
                    Point windowPoint = Application.Current.MainWindow.PointFromScreen(point);
                    return System.Windows.Media.VisualTreeHelper.HitTest(Application.Current.MainWindow, windowPoint)?.VisualHit;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取元素名称
        /// </summary>
        private string GetElementName(object element)
        {
            try
            {
                if (element is FrameworkElement frameworkElement)
                {
                    return !string.IsNullOrEmpty(frameworkElement.Name) ? frameworkElement.Name : frameworkElement.GetType().Name;
                }
            }
            catch { }

            return "Unknown";
        }

        #endregion

        #region 音频和摄像头监控（简化版）

        /// <summary>
        /// 更新音频监控状态
        /// </summary>
        private void UpdateAudioMonitoring()
        {
            if (_isAudioMonitoringEnabled)
            {
                // 初始化音频监控逻辑
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, "UserInputMonitor: 音频监控已启用");
                // 实际项目中需要实现具体的音频捕获和分析逻辑
            }
            else
            {
                // 停止音频监控
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, "UserInputMonitor: 音频监控已禁用");
            }
        }

        /// <summary>
        /// 更新摄像头监控状态
        /// </summary>
        private void UpdateCameraMonitoring()
        {
            if (_isCameraMonitoringEnabled)
            {
                // 初始化摄像头监控逻辑
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, "UserInputMonitor: 摄像头监控已启用");
                // 实际项目中需要实现具体的摄像头捕获和分析逻辑
            }
            else
            {
                // 停止摄像头监控
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, "UserInputMonitor: 摄像头监控已禁用");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取上次用户输入的时间
        /// </summary>
        public DateTime GetLastInputTime()
        {
            return _lastInputTime;
        }

        /// <summary>
        /// 获取自上次用户输入以来的时间间隔
        /// </summary>
        public TimeSpan GetIdleTime()
        {
            return DateTime.Now - _lastInputTime;
        }

        #endregion

        #region 结构体定义

        /// <summary>
        /// 鼠标钩子结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// 点结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        #endregion

        #region 自定义事件参数类

        /// <summary>
        /// 组件点击事件参数
        /// </summary>
        public class ComponentClickedEventArgs : EventArgs
        {
            public object Component { get; }
            public string ComponentType { get; }
            public string ComponentId { get; }
            public Point ClickPosition { get; }
            public DateTime Timestamp { get; }

            public ComponentClickedEventArgs(object component, string componentType, string componentId, Point clickPosition)
            {
                Component = component;
                ComponentType = componentType;
                ComponentId = componentId;
                ClickPosition = clickPosition;
                Timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// 音频输入事件参数
        /// </summary>
        public class AudioInputEventArgs : EventArgs
        {
            public float VolumeLevel { get; }
            public byte[] AudioData { get; }
            public DateTime Timestamp { get; }

            public AudioInputEventArgs(float volumeLevel, byte[] audioData = null)
            {
                VolumeLevel = volumeLevel;
                AudioData = audioData;
                Timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// 摄像头输入事件参数
        /// </summary>
        public class CameraInputEventArgs : EventArgs
        {
            public int FrameWidth { get; }
            public int FrameHeight { get; }
            public byte[] FrameData { get; }
            public DateTime Timestamp { get; }

            public CameraInputEventArgs(int frameWidth, int frameHeight, byte[] frameData = null)
            {
                FrameWidth = frameWidth;
                FrameHeight = frameHeight;
                FrameData = frameData;
                Timestamp = DateTime.Now;
            }
        }

        #endregion
    }
}