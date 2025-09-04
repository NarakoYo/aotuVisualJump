using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Runtime.InteropServices;
using ImageRecognitionApp.UnitTools;

namespace ImageRecognitionApp.Utils
{
    /// <summary>
    /// 全局性能管理器，负责监控和调整应用程序的性能模式
    /// </summary>
    public class PerformanceManager
    {
        private DateTime _lastUserActivityTime;
        private const int USER_INACTIVITY_TIMEOUT_MS = 60000; // 60秒无活动进入低功耗模式
        private bool _isInLowPowerMode = false;
        private LowLevelKeyboardHook _keyboardHook;
        private LowLevelMouseHook _mouseHook;
        
        // 存储各组件的性能配置
        private Dictionary<string, PerformanceProfile> _performanceProfiles = new Dictionary<string, PerformanceProfile>();
        
        /// <summary>
        /// 性能配置变更事件
        /// </summary>
        public event EventHandler<PerformanceProfile> PerformanceProfileChanged;
        
        public PerformanceManager()
        {
            // 初始化默认性能配置
            _performanceProfiles["Default"] = new PerformanceProfile { 
                TimerInterval = 100, 
                ImageProcessingQuality = 50, 
                CacheSize = 100
            };
            
            _performanceProfiles["LowPower"] = new PerformanceProfile {
                TimerInterval = 500, 
                ImageProcessingQuality = 20, 
                CacheSize = 20
            };
            
            _performanceProfiles["HighPerformance"] = new PerformanceProfile {
                TimerInterval = 50, 
                ImageProcessingQuality = 100, 
                CacheSize = 500
            };
        }
        
        /// <summary>
        /// 根据系统资源使用情况调整性能模式
        /// </summary>
        public void AdjustPerformanceMode(float cpuUsage, float availableMemory)
        {
            // 检查用户活动状态
            bool isUserInactive = DateTime.Now - _lastUserActivityTime > TimeSpan.FromMilliseconds(USER_INACTIVITY_TIMEOUT_MS);
            
            // 策略1: 用户长时间无活动，强制进入低功耗模式
            if (isUserInactive && !_isInLowPowerMode)
            {
                EnterLowPowerMode();
                return;
            }
            
            // 策略2: 用户恢复活动，退出低功耗模式
            if (!isUserInactive && _isInLowPowerMode)
            {
                ExitLowPowerMode();
                return;
            }
            
            // 策略3: 根据CPU和内存使用情况动态调整
            if (!_isInLowPowerMode)
            {
                if (cpuUsage > 70) // CPU使用率超过70%
                {
                    // 主动降低性能以避免系统过载
                    ApplyPerformanceProfile("LowPower");
                }
                else if (cpuUsage < 10 && availableMemory > 1024) // CPU使用率低于10%且可用内存大于1GB
                {
                    // 系统资源充足，可以提升性能
                    ApplyPerformanceProfile("HighPerformance");
                }
                else
                {
                    // 正常性能模式
                    ApplyPerformanceProfile("Default");
                }
            }
        }
        
        /// <summary>
        /// 进入低功耗模式
        /// </summary>
        public void EnterLowPowerMode()
        {
            if (!_isInLowPowerMode)
            {
                _isInLowPowerMode = true;
                ApplyPerformanceProfile("LowPower");
                
                // 清理内存
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                (Application.Current as App)?.LogMessage("应用程序已进入低功耗模式");
            }
        }
        
        /// <summary>
        /// 退出低功耗模式，恢复正常性能
        /// </summary>
        public void ExitLowPowerMode()
        {
            if (_isInLowPowerMode)
            {
                _isInLowPowerMode = false;
                ApplyPerformanceProfile("Default");
                (Application.Current as App)?.LogMessage("应用程序已退出低功耗模式");
            }
        }
        
        /// <summary>
        /// 应用性能配置
        /// </summary>
        private void ApplyPerformanceProfile(string profileName)
        {
            if (_performanceProfiles.TryGetValue(profileName, out var profile))
            {
                // 通知各个模块调整它们的性能参数
                PerformanceProfileChanged?.Invoke(this, profile);
            }
        }
        
        /// <summary>
        /// 更新最后用户活动时间
        /// </summary>
        public void UpdateLastUserActivityTime()
        {
            _lastUserActivityTime = DateTime.Now;
        }
        
        /// <summary>
        /// 启动用户活动监控
        /// </summary>
        public void StartUserActivityMonitoring()
        {
            try
            {
                // 创建键盘钩子
                _keyboardHook = new LowLevelKeyboardHook();
                _keyboardHook.KeyPressed += (s, e) => UpdateLastUserActivityTime();
                _keyboardHook.Install();
                
                // 创建鼠标钩子
                _mouseHook = new LowLevelMouseHook();
                _mouseHook.MouseMove += (s, e) => UpdateLastUserActivityTime();
                _mouseHook.MouseClick += (s, e) => UpdateLastUserActivityTime();
                _mouseHook.Install();
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"安装用户活动钩子失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 停止用户活动监控
        /// </summary>
        public void StopUserActivityMonitoring()
        {
            try
            {
                _keyboardHook?.Uninstall();
                _mouseHook?.Uninstall();
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"卸载用户活动钩子失败: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 性能配置类，定义应用程序的性能参数
    /// </summary>
    public class PerformanceProfile
    {
        /// <summary>
        /// 定时器间隔（毫秒）
        /// </summary>
        public int TimerInterval { get; set; }
        
        /// <summary>
        /// 图像处理质量（0-100）
        /// </summary>
        public int ImageProcessingQuality { get; set; }
        
        /// <summary>
        /// 缓存大小
        /// </summary>
        public int CacheSize { get; set; }
    }
    
    /// <summary>
    /// 低级键盘钩子，用于捕获全局键盘事件
    /// </summary>
    public class LowLevelKeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        
        public LowLevelKeyboardHook()
        {
            _proc = HookCallback;
        }
        
        // 定义键盘事件
        public event EventHandler<int> KeyPressed;
        
        public void Install()
        {
            _hookID = SetHook(_proc);
        }
        
        public void Uninstall()
        {
            UnhookWindowsHookEx(_hookID);
        }
        
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                KeyPressed?.Invoke(null, vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
    
    /// <summary>
    /// 低级鼠标钩子，用于捕获全局鼠标事件
    /// </summary>
    public class LowLevelMouseHook
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private LowLevelMouseProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;
        
        public event EventHandler<MouseHookEventArgs> MouseMove;
        public event EventHandler<MouseHookEventArgs> MouseClick;
        
        public LowLevelMouseHook()
        {
            _proc = HookCallback;
        }
        
        public void Install()
        {
            _hookID = SetHook(_proc);
        }
        
        public void Uninstall()
        {
            UnhookWindowsHookEx(_hookID);
        }
        
        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // 提取鼠标坐标
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                int x = hookStruct.pt.x;
                int y = hookStruct.pt.y;
                
                // 处理鼠标移动事件
                if (wParam == (IntPtr)WM_MOUSEMOVE)
                {
                    MouseMove?.Invoke(null, new MouseHookEventArgs { X = x, Y = y, Button = MouseButton.None });
                }
                // 处理鼠标点击事件
                else if (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    MouseButton button = wParam == (IntPtr)WM_LBUTTONDOWN ? MouseButton.Left : MouseButton.Right;
                    MouseClick?.Invoke(null, new MouseHookEventArgs { X = x, Y = y, Button = button });
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
    
    /// <summary>
    /// 鼠标事件参数
    /// </summary>
    public class MouseHookEventArgs : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; }
    }
    
    /// <summary>
    /// 鼠标按钮枚举
    /// </summary>
    public enum MouseButton
    {
        None,
        Left,
        Right,
        Middle
    }
}