using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ImageRecognitionApp.unit;

namespace ImageRecognitionApp.WinFun
{
    /// <summary>
    /// 进程助手类，用于设置进程图标和名称
    /// </summary>
    public static class ProcessHelper
    {
        // 导入Windows API
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int DEFAULT_SIGN_ID = 10001;
        private const string DEFAULT_APP_NAME = "AI视觉识别操作";

        /// <summary>
        /// 设置进程名称（通过修改主窗口标题）
        /// </summary>
        /// <param name="processName">进程名称</param>
        public static void SetProcessName(string processName)
        {
            try
            {
                // 获取主窗口
                Window mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // 使用WindowInteropHelper获取窗口句柄
                    IntPtr hWnd = new WindowInteropHelper(mainWindow).Handle;
                    if (hWnd != IntPtr.Zero)
                    {
                        // 设置窗口标题
                        SetWindowText(hWnd, processName);
                    }
                }
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"设置进程名称时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置进程图标
        /// </summary>
        /// <param name="iconPath">图标路径</param>
        public static void SetProcessIcon(string iconPath)
        {
            try
            {
                // 获取主窗口
                Window mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // 使用WindowInteropHelper获取窗口句柄
                    IntPtr hWnd = new WindowInteropHelper(mainWindow).Handle;
                    if (hWnd != IntPtr.Zero)
                    {
                        // 加载图标
                        using (Icon icon = new Icon(iconPath))
                        {
                            // 设置大图标和小图标
                            SendMessage(hWnd, WM_SETICON, ICON_SMALL, icon.Handle);
                            SendMessage(hWnd, WM_SETICON, ICON_BIG, icon.Handle);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"设置进程图标时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化进程，设置进程名称和图标
        /// </summary>
        public static void InitializeProcess()
        {
            try
            {
                // 获取进程名称（使用LocalizedTitleHelper获取本地化标题）
                string appName = ImageRecognitionApp.unit.assist.LocalizedTitleHelper.GetLocalizedAppTitle();
                (Application.Current as App)?.LogMessage($"已获取应用名称: {appName}");

                // 设置进程名称
                SetProcessName(appName);

                // 设置进程图标
                SetProcessIconWithFallback();
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"初始化进程时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试使用多个路径设置进程图标
        /// </summary>
        private static void SetProcessIconWithFallback()
        {
            try
            {
                // 首先尝试使用应用程序目录下的图标
                string iconPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo", "logo.ico"));
                (Application.Current as App)?.LogMessage($"尝试使用图标路径: {iconPath}");

                if (File.Exists(iconPath))
                {
                    SetProcessIcon(iconPath);
                }
                else
                {
                    (Application.Current as App)?.LogMessage($"错误: 未找到图标文件: {iconPath}");
                    // 尝试使用项目路径
                    string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    if (!string.IsNullOrEmpty(assemblyLocation))
                    {
                        string assemblyDir = Path.GetDirectoryName(assemblyLocation);
                        if (!string.IsNullOrEmpty(assemblyDir))
                        {
                            string combinedPath = Path.Combine(assemblyDir, "..", "..", "Resources", "logo", "logo.ico");
                            if (!string.IsNullOrEmpty(combinedPath))
                            {
                                string projectIconPath = Path.GetFullPath(combinedPath);
                                if (!string.IsNullOrEmpty(projectIconPath))
                                {
                                    (Application.Current as App)?.LogMessage($"尝试使用项目路径图标: {projectIconPath}");
                                    if (File.Exists(projectIconPath))
                                    {
                                        SetProcessIcon(projectIconPath);
                                        (Application.Current as App)?.LogMessage($"使用项目路径设置进程图标: {projectIconPath}");
                                    }
                                    else
                                    {
                                        (Application.Current as App)?.LogMessage($"错误: 项目路径未找到图标文件: {projectIconPath}");
                                    }
                                }
                                else
                                {
                                    (Application.Current as App)?.LogMessage("错误: 无法获取有效的项目图标路径");
                                }
                            }
                            else
                            {
                                (Application.Current as App)?.LogMessage("错误: 无法组合有效的项目图标路径");
                            }
                        }
                        else
                        {
                            (Application.Current as App)?.LogMessage("错误: 无法获取程序集目录，无法设置项目路径图标");
                        }
                    }
                    else
                    {
                        (Application.Current as App)?.LogMessage("错误: 无法获取程序集位置，无法设置项目路径图标");
                    }
                }
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"设置进程图标时出错: {ex.Message}");
            }
        }
    }
}