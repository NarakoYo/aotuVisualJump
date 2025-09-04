using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageRecognitionApp.Assets.UI
{
    /// <summary>
    /// ScreenRecordingControl.xaml 的交互逻辑
    /// 实现屏幕捕获和显示功能
    /// </summary>
    public partial class ScreenRecordingControl : UserControl
    {
        // 屏幕捕获相关的Windows API
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        // 定时器用于定期更新屏幕捕获
        private DispatcherTimer? _captureTimer;
        private bool _isCapturing = false;

        // 鼠标拖动相关变量
        private bool _isDragging = false;
        private System.Windows.Point _lastMousePosition;
        private double _offsetX = 0;
        private double _offsetY = 0;

        public ScreenRecordingControl()
        {
            InitializeComponent();
            Loaded += ScreenRecordingControl_Loaded;
            Unloaded += ScreenRecordingControl_Unloaded;

            // 添加鼠标事件处理程序，实现拖动功能
            MainContentCanvas.MouseLeftButtonDown += MainContentCanvas_MouseLeftButtonDown;
            MainContentCanvas.MouseMove += MainContentCanvas_MouseMove;
            MainContentCanvas.MouseLeftButtonUp += MainContentCanvas_MouseLeftButtonUp;
            MainContentCanvas.MouseLeave += MainContentCanvas_MouseLeave;
        }

        private void ScreenRecordingControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化定时器，设置捕获频率为15fps
            _captureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000 / 15)
            };
            _captureTimer.Tick += CaptureTimer_Tick;

            // 开始捕获屏幕
            StartCapturing();
        }

        private void ScreenRecordingControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 停止捕获屏幕并释放资源
            StopCapturing();
        }

        private void StartCapturing()
        {
            if (!_isCapturing)
            {
                _isCapturing = true;
                _captureTimer.Start();
            }
        }

        private void StopCapturing()
        {
            if (_isCapturing)
            {
                _isCapturing = false;
                _captureTimer.Stop();
                // 清除画布内容
                MainContentCanvas.Children.Clear();
            }
        }

        private void CaptureTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 捕获屏幕
                BitmapSource screenshot = CaptureScreen();
                if (screenshot != null)
                {
                    // 在画布上显示捕获的屏幕内容
                    UpdateCanvasWithScreenshot(screenshot);
                }
            }
            catch (Exception ex)
            {
                // 实际应用中应该有更好的错误处理
                Console.WriteLine("捕获屏幕时出错: " + ex.Message);
            }
        }

        private BitmapSource CaptureScreen()
        {
            // 获取屏幕尺寸
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // 获取桌面窗口
            IntPtr hDesktop = GetDesktopWindow();
            IntPtr hDC = GetDC(hDesktop);
            IntPtr hMemDC = CreateCompatibleDC(hDC);
            IntPtr hBitmap = CreateCompatibleBitmap(hDC, screenWidth, screenHeight);

            try
            {
                // 选择位图到内存DC
                IntPtr hOldBitmap = SelectObject(hMemDC, hBitmap);

                // 使用BitBlt捕获屏幕
                BitBlt(hMemDC, 0, 0, screenWidth, screenHeight, hDC, 0, 0, 0x00CC0020); // SRCCOPY

                // 恢复原来的位图
                SelectObject(hMemDC, hOldBitmap);

                // 将位图转换为WPF的BitmapSource
                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // 防止内存泄漏
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                // 释放资源
                ReleaseDC(hDesktop, hDC);
                DeleteDC(hMemDC);
                DeleteObject(hBitmap);
            }
        }

        private void UpdateCanvasWithScreenshot(BitmapSource screenshot)
        {
            try
            {
                // 清除画布上的旧内容
                MainContentCanvas.Children.Clear();

                // 创建一个Image控件来显示屏幕截图
                System.Windows.Controls.Image image = new System.Windows.Controls.Image
                {
                    Source = screenshot,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    StretchDirection = System.Windows.Controls.StretchDirection.Both,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                // 设置Image的宽度和高度，使其填充整个MainContentCanvas并保持比例
                image.Width = MainContentCanvas.ActualWidth;
                image.Height = MainContentCanvas.ActualHeight;

                // 将Image添加到画布
                MainContentCanvas.Children.Add(image);
                // 设置Image在Canvas中的位置，使其位于左上角并应用偏移量
                double left = 0 + _offsetX;
                double top = 0 + _offsetY;
                Canvas.SetLeft(image, left);
                Canvas.SetTop(image, top);
            }
            catch (Exception ex)
            {
                Console.WriteLine("更新画布显示时出错: " + ex.Message);
            }
        }

        #region 鼠标拖动事件处理

        /// <summary>
        /// 处理鼠标左键按下事件，开始拖动
        /// </summary>
        private void MainContentCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(MainContentCanvas);
            MainContentCanvas.CaptureMouse();
        }

        /// <summary>
        /// 处理鼠标移动事件，实现拖动功能
        /// </summary>
        private void MainContentCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && MainContentCanvas.Children.Count > 0)
            {
                // 获取当前鼠标位置
                System.Windows.Point currentMousePosition = e.GetPosition(MainContentCanvas);
                
                // 计算鼠标移动的偏移量
                double deltaX = currentMousePosition.X - _lastMousePosition.X;
                double deltaY = currentMousePosition.Y - _lastMousePosition.Y;
                
                // 更新总偏移量
                _offsetX += deltaX;
                _offsetY += deltaY;
                
                // 更新鼠标位置记录
                _lastMousePosition = currentMousePosition;
                
                // 更新Image在Canvas中的位置
                if (MainContentCanvas.Children[0] is System.Windows.Controls.Image image)
                {
                    double left = Canvas.GetLeft(image) + deltaX;
                    double top = Canvas.GetTop(image) + deltaY;
                    Canvas.SetLeft(image, left);
                    Canvas.SetTop(image, top);
                }
            }
        }

        /// <summary>
        /// 处理鼠标左键释放事件，结束拖动
        /// </summary>
        private void MainContentCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
            MainContentCanvas.ReleaseMouseCapture();
        }

        /// <summary>
        /// 处理鼠标离开画布事件，结束拖动
        /// </summary>
        private void MainContentCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isDragging = false;
            MainContentCanvas.ReleaseMouseCapture();
        }

        #endregion
    }
}