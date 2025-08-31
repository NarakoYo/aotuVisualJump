using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ImageRecognitionApp.Assets.UI
{
    /// <summary>
    /// 初始化界面动画管理器，负责处理启动过程中的各种动画效果
    /// </summary>
    public class InitialStartupAnimation
    {
        private readonly Window _targetWindow;
        private readonly Dispatcher _dispatcher;
        
        // 配置常量
        private const double DefaultAnimationDuration = 0.5; // 默认动画持续时间（秒）
        private const double ProgressBarAnimationFactor = 0.7; // 进度条动画持续时间因子
        private const double TextAnimationFactor = 0.5; // 文本动画持续时间因子
        private const double TextTransitionFactor = 0.3; // 文本过渡动画持续时间因子
        private const double LoadingIndicatorDuration = 1.5; // 加载指示器旋转持续时间（秒）
        
        /// <summary>
        /// 动画持续时间（秒）
        /// </summary>
        public double AnimationDuration { get; set; } = DefaultAnimationDuration;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="targetWindow">目标窗口</param>
        public InitialStartupAnimation(Window targetWindow)
        {
            _targetWindow = targetWindow ?? throw new ArgumentNullException(nameof(targetWindow));
            _dispatcher = targetWindow.Dispatcher;
        }
        
        /// <summary>
        /// 播放窗口淡入动画
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task PlayWindowFadeInAsync()
        {
            await ExecuteOnUIThreadAsync(() =>
            {
                _targetWindow.Opacity = 0;
                _targetWindow.Visibility = Visibility.Visible;
                
                var fadeInAnimation = CreateDoubleAnimation(0, 1, AnimationDuration);
                _targetWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
            });
            
            // 等待动画完成
            await Task.Delay(TimeSpan.FromSeconds(AnimationDuration));
        }
        
        /// <summary>
        /// 播放窗口淡出动画
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task PlayWindowFadeOutAsync()
        {
            await ExecuteOnUIThreadAsync(() =>
            {
                var fadeOutAnimation = CreateDoubleAnimation(1, 0, AnimationDuration);
                _targetWindow.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
            });
            
            // 等待动画完成
            await Task.Delay(TimeSpan.FromSeconds(AnimationDuration));
        }
        
        /// <summary>
        /// 播放进度条动画
        /// </summary>
        /// <param name="progressBar">进度条控件</param>
        /// <param name="targetValue">目标进度值</param>
        /// <returns>异步任务</returns>
        public async Task AnimateProgressBarAsync(ProgressBar progressBar, double targetValue)
        {
            if (progressBar == null)
                throw new ArgumentNullException(nameof(progressBar));
            
            // 确保目标值在有效范围内
            targetValue = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, targetValue));
            
            double animationDuration = AnimationDuration * ProgressBarAnimationFactor;
            
            await ExecuteOnUIThreadAsync(() =>
            {
                var progressAnimation = CreateDoubleAnimation(progressBar.Value, targetValue, animationDuration);
                progressBar.BeginAnimation(ProgressBar.ValueProperty, progressAnimation);
            });
            
            // 等待动画完成
            await Task.Delay(TimeSpan.FromSeconds(animationDuration));
        }
        
        /// <summary>
        /// 播放文本淡入动画
        /// </summary>
        /// <param name="textBlock">文本控件</param>
        /// <param name="text">要显示的文本</param>
        /// <returns>异步任务</returns>
        public async Task AnimateTextFadeInAsync(TextBlock textBlock, string text)
        {
            if (textBlock == null)
                throw new ArgumentNullException(nameof(textBlock));
            
            double animationDuration = AnimationDuration * TextAnimationFactor;
            
            await ExecuteOnUIThreadAsync(() =>
            {
                // 先设置文本
                textBlock.Text = text;
                // 然后执行淡入动画
                textBlock.Opacity = 0;
                
                var fadeInAnimation = CreateDoubleAnimation(0, 1, animationDuration);
                textBlock.BeginAnimation(TextBlock.OpacityProperty, fadeInAnimation);
            });
            
            // 等待动画完成
            await Task.Delay(TimeSpan.FromSeconds(animationDuration));
        }
        
        /// <summary>
        /// 播放文本淡入淡出过渡动画
        /// </summary>
        /// <param name="textBlock">文本控件</param>
        /// <param name="newText">新文本</param>
        /// <returns>异步任务</returns>
        public async Task AnimateTextTransitionAsync(TextBlock textBlock, string newText)
        {
            if (textBlock == null)
                throw new ArgumentNullException(nameof(textBlock));
            
            double transitionDuration = AnimationDuration * TextTransitionFactor;
            
            // 优化：合并UI线程操作，减少上下文切换
            await ExecuteOnUIThreadAsync(() =>
            {
                // 创建淡出动画
                var fadeOutAnimation = CreateDoubleAnimation(1, 0, transitionDuration);
                
                // 设置完成事件处理程序，在淡出后更新文本并淡入
                fadeOutAnimation.Completed += (sender, e) =>
                {
                    // 更新文本
                    textBlock.Text = newText;
                    
                    // 创建淡入动画
                    var fadeInAnimation = CreateDoubleAnimation(0, 1, transitionDuration);
                    textBlock.BeginAnimation(TextBlock.OpacityProperty, fadeInAnimation);
                };
                
                textBlock.BeginAnimation(TextBlock.OpacityProperty, fadeOutAnimation);
            });
            
            // 等待整个过渡动画完成
            await Task.Delay(TimeSpan.FromSeconds(transitionDuration * 2));
        }
        
        /// <summary>
        /// 播放加载指示器旋转动画
        /// </summary>
        /// <param name="element">要旋转的元素</param>
        /// <returns>异步任务</returns>
        public async Task StartLoadingIndicatorAsync(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            
            await ExecuteOnUIThreadAsync(() =>
            {
                // 检查是否已存在旋转变换
                var rotateTransform = element.RenderTransform as RotateTransform;
                if (rotateTransform == null)
                {
                    rotateTransform = new RotateTransform();
                    element.RenderTransform = rotateTransform;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                
                // 创建循环旋转动画
                var rotateAnimation = CreateDoubleAnimation(0, 360, LoadingIndicatorDuration);
                rotateAnimation.RepeatBehavior = RepeatBehavior.Forever;
                
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            });
        }
        
        /// <summary>
        /// 停止加载指示器旋转动画
        /// </summary>
        /// <param name="element">要停止旋转的元素</param>
        /// <returns>异步任务</returns>
        public async Task StopLoadingIndicatorAsync(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            
            await ExecuteOnUIThreadAsync(() =>
            {
                if (element.RenderTransform is RotateTransform rotateTransform)
                {
                    // 停止动画并重置变换
                    rotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                }
            });
        }
        
        /// <summary>
        /// 创建DoubleAnimation实例
        /// </summary>
        /// <param name="from">起始值</param>
        /// <param name="to">结束值</param>
        /// <param name="durationSeconds">持续时间（秒）</param>
        /// <returns>DoubleAnimation实例</returns>
        private DoubleAnimation CreateDoubleAnimation(double from, double to, double durationSeconds)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                // 使用合适的缓动函数提升动画质量
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
        }
        
        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <returns>异步任务</returns>
        private Task ExecuteOnUIThreadAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            // 如果已经在UI线程上，直接执行
            if (_dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }
            
            // 否则使用TaskCompletionSource创建可等待的任务
            var taskCompletionSource = new TaskCompletionSource<bool>();
            
            try
            {
                // 使用BeginInvoke确保异步执行
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action();
                        taskCompletionSource.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                // 捕获BeginInvoke可能抛出的异常
                taskCompletionSource.SetException(ex);
            }
            
            return taskCompletionSource.Task;
        }
    }
}