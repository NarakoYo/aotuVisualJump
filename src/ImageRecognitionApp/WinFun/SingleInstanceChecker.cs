using System;
using ImageRecognitionApp.Utils;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.IO;
// using NLua;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace ImageRecognitionApp.WinFun
{
    /// <summary>
    /// 单实例检查器，确保程序只有一个实例运行
    /// </summary>
    public class SingleInstanceChecker
    {
        private const string MUTEX_NAME = "ImageRecognitionApp_Mutex";
        private static Mutex? _mutex = null;
        private static Window? _warningWindow; // 保持弹窗引用 (可为null)

        /// <summary>
        /// 检查是否已有实例在运行
        /// </summary>
        /// <returns>如果已有实例运行则返回true，否则返回false</returns>
        public static bool CheckIfAlreadyRunning()
        {
            // 创建互斥锁，若已存在则返回false
            bool createdNew;
            _mutex = new Mutex(true, MUTEX_NAME, out createdNew);
            return !createdNew;
        }

        /// <summary>
        /// 释放互斥锁
        /// </summary>
        public static void ReleaseMutex()
        {
            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                finally
                {
                    _mutex = null;
                }
            }
        }

        /// <summary>
        /// 显示重复启动提示窗口
        /// </summary>
        public static void ShowDuplicateInstanceWarning()
        {
            // 注册主窗口的点击事件监听
            RegisterMainWindowClickEvents();
            try
            {
                // 获取本地化文本
                string? warningMessage = GetLocalizedString("20001");
                if (string.IsNullOrEmpty(warningMessage))
                {
                    warningMessage = "程序已经在运行中，无法重复启动。";
                }

                // 创建提示窗口
                // 获取弹窗标题本地化文本
                string? windowTitle = GetLocalizedString("20002");
                if (string.IsNullOrEmpty(windowTitle))
                {
                    windowTitle = "提示";
                }

                // 如果已有弹窗存在，先关闭它并清理资源
                if (_warningWindow != null)
                {
                    try
                    {
                        _warningWindow.Close();
                    }
                    catch { }
                    finally
                    {
                        _warningWindow = null;
                    }
                }

                // 创建新的弹窗实例
                _warningWindow = new Window
                {
                    Title = windowTitle,
                    Width = 350,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = true,
                    WindowStyle = WindowStyle.None, // 无边框窗口，取消关闭按钮
                    Background = System.Windows.Media.Brushes.Transparent, // 透明背景，用于实现圆角效果
                    AllowsTransparency = true // 允许透明
                };

                // 确保主窗口存在且不是当前窗口
                if (System.Windows.Application.Current.MainWindow != null && !System.Windows.Application.Current.MainWindow.Equals(_warningWindow))
                {
                    _warningWindow.Owner = System.Windows.Application.Current.MainWindow;
                }

                // 创建主网格容器
                Grid mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // 添加主内容容器，并设置圆角效果
                Border mainBorder = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                    BorderThickness = new Thickness(1)
                };

                // 添加标题栏
                Border titleBorder = new Border
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40)),
                    Height = 30,
                    CornerRadius = new CornerRadius(10, 10, 0, 0)
                };

                TextBlock titleText = new TextBlock
                {
                    Text = windowTitle,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(5),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold
                };

                titleBorder.Child = titleText;
                mainGrid.Children.Add(titleBorder);

                // 创建内容网格
                Grid contentGrid = new Grid
                {
                    Margin = new Thickness(20)
                };

                // 定义内容网格的行和列
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // 添加消息文本
                TextBlock messageText = new TextBlock
                {
                    Text = warningMessage,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center
                };

                // 获取确认按钮文本
                string? okButtonText = GetLocalizedString("20003");
                if (string.IsNullOrEmpty(okButtonText))
                {
                    okButtonText = "确定";
                }

                // 添加确认按钮并设置暗色系样式
                Button okButton = new Button
                {
                    Content = okButtonText,
                    Width = 80,
                    Height = 30,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80)),
                    BorderThickness = new Thickness(1)
                };

                okButton.Click += (sender, e) => _warningWindow.Close();

                // 放置文本和按钮到内容网格
                Grid.SetRow(messageText, 0);
                Grid.SetColumn(messageText, 1);
                Grid.SetRow(okButton, 2);
                Grid.SetColumn(okButton, 1);
                contentGrid.Children.Add(messageText);
                contentGrid.Children.Add(okButton);

                // 将内容网格添加到主网格
                Grid.SetRow(contentGrid, 1);
                mainGrid.Children.Add(contentGrid);

                // 将mainGrid添加到mainBorder
                mainBorder.Child = mainGrid;
                
                // 设置窗口内容
                _warningWindow.Content = mainBorder;

                // 显示模态窗口
                _warningWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"显示重复实例警告时出错: {ex.Message}");
                (App.Current as App)?.LogMessage($"错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 注册主窗口点击事件
        /// </summary>
        private static void RegisterMainWindowClickEvents()
        {
            if (System.Windows.Application.Current?.MainWindow == null)
                return;

            // 为主窗口添加点击事件
            System.Windows.Application.Current.MainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;

            // 遍历主窗口所有子元素并添加点击事件
            AddClickEventsToAllChildren(System.Windows.Application.Current.MainWindow);
        }

        /// <summary>
        /// 为主窗口及其子元素添加点击事件
        /// </summary>
        /// <param name="element">UI元素</param>
        private static void AddClickEventsToAllChildren(UIElement element)
        {
            if (element is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                {
                    AddClickEventsToAllChildren(child);
                }
            }
            else if (element is ContentControl contentControl && contentControl.Content is UIElement contentElement)
            {
                AddClickEventsToAllChildren(contentElement);
            }
            else if (element is ItemsControl itemsControl)
            {
                // 为ItemsControl的子元素添加事件
                itemsControl.PreviewMouseDown += MainWindow_PreviewMouseDown;
            }

            // 为当前元素添加点击事件
            if (element != System.Windows.Application.Current?.MainWindow)
            {
                element.PreviewMouseDown += MainWindow_PreviewMouseDown;
            }
        }

        /// <summary>
        /// 主窗口点击事件处理程序
        /// </summary>
        private static void MainWindow_PreviewMouseDown(object? sender, MouseButtonEventArgs e)
        {
            // 检查弹窗是否存在且可见
            if (_warningWindow != null && _warningWindow.IsVisible)
            {
                // 触发弹窗惊醒提示
                AlertWarningWindow();
                e.Handled = true; // 阻止事件继续传播
            }
        }

        /// <summary>
        /// 弹窗惊醒提示
        /// </summary>
        private static void AlertWarningWindow()
        {
            if (_warningWindow == null)
                return;

            // 将弹窗置于最前端
            _warningWindow.Topmost = false;
            _warningWindow.Topmost = true;

            // 改变弹窗边框色以吸引注意
            Border mainBorder = _warningWindow.Content as Border;
            if (mainBorder != null)
            {
                // 保存原始边框颜色
                Brush originalBorderBrush = mainBorder.BorderBrush;
                
                // 设置警告边框颜色
                mainBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 100, 100)); // 红色边框

                // 恢复原始边框颜色
                System.Timers.Timer timer = new System.Timers.Timer(500);
                timer.Elapsed += (sender, e) =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        mainBorder.BorderBrush = originalBorderBrush;
                    });
                    timer.Dispose();
                };
                timer.Start();
                return;
            }
            
            // 如果mainBorder为空，则使用原来的背景色变化方案作为后备
            System.Windows.Media.Color originalColor = ((System.Windows.Media.SolidColorBrush)_warningWindow.Background).Color;
            _warningWindow.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 100, 100)); // 红色背景

            // 恢复原始背景色
            System.Timers.Timer fallbackTimer = new System.Timers.Timer(500);
            fallbackTimer.Elapsed += (sender, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _warningWindow.Background = new SolidColorBrush(originalColor);
                });
                fallbackTimer.Dispose();
            };
            fallbackTimer.Start();
        }

        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        /// <param name="signId">字符串标识</param>
        /// <returns>本地化后的字符串</returns>
        private static string? GetLocalizedString(string signId)
        {
            try
            {
                (App.Current as App)?.LogMessage($"尝试获取本地化字符串: signId={signId}");
                if (!int.TryParse(signId, out int id))
                {
                    (App.Current as App)?.LogMessage($"无效的signId格式: {signId}");
                    return null;
                }

                // 使用JsonLocalizationHelper获取本地化文本
                string result = JsonLocalizationHelper.Instance.GetString(id);
                (App.Current as App)?.LogMessage($"获取本地化字符串结果: id={id}, result={result ?? "null"}");

                // 检查是否找到本地化文本
                if (string.IsNullOrEmpty(result) || result.StartsWith("未找到") || result.StartsWith("ERROR_"))
                {
                    (App.Current as App)?.LogMessage($"警告: 未找到有效的本地化文本 for id={id}");
                    // 返回默认值
                    if (id == 20001) return "程序已经在运行中，无法重复启动。";
                    if (id == 20002) return "提示";
                    if (id == 20003) return "确定";
                    return $"[{id}]";
                }
                return result;
            }
            catch (Exception ex)
            {
                (App.Current as App)?.LogMessage($"获取本地化字符串时出错: {ex.Message}");
                (App.Current as App)?.LogMessage($"错误堆栈: {ex.StackTrace}");
                return null;
            }
        }
    }
}