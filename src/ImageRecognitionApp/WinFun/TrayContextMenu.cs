using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System;

namespace ImageRecognitionApp.WinFun
{
    /// <summary>
    /// 托盘上下文菜单管理类
    /// 专门处理托盘上下文菜单的功能和布局UI
    /// </summary>
    public class TrayContextMenu
    {
        // 上下文菜单实例
        private ContextMenu _contextMenu;

        // 退出菜单项
        private MenuItem _exitMenuItem;

        // 日志记录委托
        private readonly Action<string> _logAction;

        /// <summary>
        /// 退出菜单项点击事件
        /// </summary>
        public event EventHandler ExitMenuItemClick;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logAction">日志记录委托</param>
        public TrayContextMenu(Action<string> logAction = null)
        {
            _logAction = logAction;
            InitializeContextMenu();
        }

        /// <summary>
        /// 初始化上下文菜单
        /// </summary>
        private void InitializeContextMenu()
        {
            try
            {
                LogMessage("TrayContextMenu: 初始化上下文菜单");
                _contextMenu = new ContextMenu
                {
                    // 设置暗黑主题背景
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 65, 68)),
                    BorderThickness = new Thickness(1),
                    // 增加菜单内边距
                    Padding = new Thickness(4)
                };

                // 添加分隔符（在退出按钮上方）
                Separator separator = new Separator
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 65, 68)),
                    Margin = new Thickness(0)
                };
                _contextMenu.Items.Add(separator);

                // 添加退出应用菜单项
                _exitMenuItem = new MenuItem
                {
                    Header = "退出",
                    Tag = "ExitApplication",
                    // 设置暗黑主题
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                    // 增加字体大小
                    FontSize = 14,
                    // 增加菜单项高度
                    Height = 32,
                    // 设置边距
                    Padding = new Thickness(8, 6, 8, 6),
                    // 鼠标悬停样式
                    Style = CreateMenuItemStyle()
                };
                _exitMenuItem.Click += OnExitMenuItemClick;
                _contextMenu.Items.Add(_exitMenuItem);

                LogMessage("TrayContextMenu: 上下文菜单已创建");
            }
            catch (Exception ex)
            {
                LogMessage($"TrayContextMenu: 初始化上下文菜单错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建菜单项样式，用于设置鼠标悬停效果
        /// </summary>
        /// <returns>菜单项样式</returns>
        private Style CreateMenuItemStyle()
        {
            Style menuItemStyle = new Style(typeof(MenuItem));

            // 鼠标悬停触发器
            Trigger mouseOverTrigger = new Trigger
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };

            // 鼠标悬停时的背景色
            Setter backgroundSetter = new Setter
            {
                Property = MenuItem.BackgroundProperty,
                Value = new SolidColorBrush(System.Windows.Media.Color.FromRgb(54, 104, 223)) // #3668DF
            };

            mouseOverTrigger.Setters.Add(backgroundSetter);
            menuItemStyle.Triggers.Add(mouseOverTrigger);

            return menuItemStyle;
        }

        /// <summary>
        /// 显示上下文菜单
        /// </summary>
        public void Show()
        {
            try
            {
                LogMessage("TrayContextMenu: 显示上下文菜单");
                if (_contextMenu != null)
                {
                    // 确保上下文菜单已初始化
                    if (_contextMenu.Items.Count == 0)
                    {
                        InitializeContextMenu();
                    }

                    // 更新菜单状态
                    UpdateMenuState();

                    // 如果菜单已经打开，先关闭它再重新打开（修复bug：再次右键只刷新菜单）
                    if (_contextMenu.IsOpen)
                    {
                        _contextMenu.IsOpen = false;
                    }

                    // 获取当前鼠标位置（屏幕坐标）
                    System.Windows.Point mousePosition = System.Windows.Input.Mouse.GetPosition(null);

                    // 设置上下文菜单的放置位置（修复bug：菜单位置应该在托盘图标的上方，距离6px）
                    _contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
                    // 计算菜单在托盘图标上方6px的位置
                    double menuX = mousePosition.X;
                    double menuY = mousePosition.Y - 6; // 距离托盘图标上方6px

                    // 获取菜单的期望位置
                    _contextMenu.PlacementRectangle = new System.Windows.Rect(menuX, menuY, 0, 0);
                    _contextMenu.HorizontalOffset = 0;
                    _contextMenu.VerticalOffset = 0;

                    // 确保菜单有一个逻辑父元素，以便点击外部可以关闭菜单（修复bug：点击任务栏非菜单区域无法关闭菜单）
                    if (_contextMenu.Parent == null)
                    {
                        // 创建一个不可见的FrameworkElement作为菜单的父元素
                        FrameworkElement dummyParent = new FrameworkElement();
                        // 为了确保点击外部可以关闭菜单，我们需要使dummyParent可见（虽然在屏幕外）
                        dummyParent.Width = 0;
                        dummyParent.Height = 0;
                        dummyParent.ContextMenu = _contextMenu;
                        // WPF不需要手动设置根元素，上下文菜单显示时会自动处理点击外部关闭的行为
                    }

                    // 显示菜单
                    _contextMenu.IsOpen = true;
                    LogMessage("TrayContextMenu: 上下文菜单已显示");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"TrayContextMenu: 显示上下文菜单错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新上下文菜单状态
        /// </summary>
        private void UpdateMenuState()
        {
            try
            {
                LogMessage("TrayContextMenu: 更新上下文菜单状态");
                // 由于我们现在只有退出按钮，且它始终可用，所以这个方法简化为空实现
                // 保留此方法是为了保持接口兼容性和未来扩展
            }
            catch (Exception ex)
            {
                LogMessage($"TrayContextMenu: 更新上下文菜单状态错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 退出菜单项点击处理
        /// </summary>
        private void OnExitMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("TrayContextMenu: 处理退出菜单项点击");
                // 触发外部事件
                ExitMenuItemClick?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogMessage($"TrayContextMenu: 处理退出菜单项点击错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 日志记录方法
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogMessage(string message)
        {
            _logAction?.Invoke(message);
        }

        /// <summary>
        /// 获取上下文菜单实例
        /// </summary>
        public ContextMenu ContextMenu => _contextMenu;
    }
}