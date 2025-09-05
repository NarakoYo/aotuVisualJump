using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Collections.Concurrent;

namespace ImageRecognitionApp.Assets.UI
{
    /// <summary>
    /// 系统信息窗口折叠面板动画管理器
    /// </summary>
    public static class SystemInfoAnimation
    {
        /// <summary>
        /// 动画持续时间（毫秒）
        /// </summary>
        private const int AnimationDuration = 300;
        
        /// <summary>
        /// 存储每个Expander对应的当前运行的Storyboard，用于中断处理
        /// </summary>
        private static readonly ConcurrentDictionary<Expander, Storyboard> _runningAnimations = new ConcurrentDictionary<Expander, Storyboard>();

        /// <summary>
        /// 设置折叠面板的展开/折叠动画
        /// </summary>
        /// <param name="expander">目标折叠面板</param>
        /// <param name="contentPanel">面板内容容器</param>
        public static void SetupExpanderAnimation(Expander expander, Panel contentPanel)
        {
            if (expander == null || contentPanel == null)
                return;

            // 订阅展开状态变化事件
            expander.Expanded += (sender, e) =>
            {
                AnimateExpansion(expander, contentPanel);
            };

            expander.Collapsed += (sender, e) =>
            {
                AnimateCollapse(expander, contentPanel);
            };

            // 初始状态设置
            if (!expander.IsExpanded)
            {
                // 如果初始是折叠状态，隐藏所有子元素
                HideAllChildren(contentPanel);
            }
        }

        /// <summary>
        /// 执行展开动画
        /// </summary>
        /// <param name="expander">目标折叠面板</param>
        /// <param name="contentPanel">面板内容容器</param>
        private static void AnimateExpansion(Expander expander, Panel contentPanel)
        {
            try
            {
                if (contentPanel.Children.Count == 0)
                    return;

                // 取消当前正在运行的动画（如果有）
                CancelRunningAnimation(expander);

                // 确保内容可见
                contentPanel.Visibility = Visibility.Visible;
                
                // 准备所有子元素
                PrepareChildrenForExpansion(contentPanel);

                // 创建并启动展开动画序列
                Storyboard storyboard = new Storyboard();
                storyboard.FillBehavior = FillBehavior.HoldEnd;
                
                // 保存动画引用以便后续可能的中断
                _runningAnimations[expander] = storyboard;

                // 计算每个子元素的延迟时间
                double delayPerItem = AnimationDuration / (contentPanel.Children.Count * 2.0);

                for (int i = 0; i < contentPanel.Children.Count; i++)
                {
                    UIElement child = contentPanel.Children[i] as UIElement;
                    if (child == null)
                        continue;

                    // 透明度动画
                    DoubleAnimation opacityAnimation = CreateOpacityAnimation(0, 1, AnimationDuration, delayPerItem * i);
                    Storyboard.SetTarget(opacityAnimation, child);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                    storyboard.Children.Add(opacityAnimation);

                    // 位置动画 - 向下移动而不是向上，避免与标题重叠
                    TranslateTransform transform = GetOrCreateTranslateTransform(child);
                    DoubleAnimation translateYAnimation = CreateTranslateYAnimation(10, 0, AnimationDuration, delayPerItem * i);
                    Storyboard.SetTarget(translateYAnimation, transform);
                    Storyboard.SetTargetProperty(translateYAnimation, new PropertyPath(TranslateTransform.YProperty));
                    storyboard.Children.Add(translateYAnimation);
                }

                // 动画完成后清理引用
                storyboard.Completed += (sender, e) =>
                {
                    Storyboard completedStoryboard;
                    _runningAnimations.TryRemove(expander, out completedStoryboard);
                };

                // 启动动画
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"展开动画执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行折叠动画
        /// </summary>
        /// <param name="expander">目标折叠面板</param>
        /// <param name="contentPanel">面板内容容器</param>
        private static void AnimateCollapse(Expander expander, Panel contentPanel)
        {
            try
            {
                if (contentPanel.Children.Count == 0)
                    return;

                // 取消当前正在运行的动画（如果有）
                CancelRunningAnimation(expander);

                // 确保内容可见以便动画正常显示
                foreach (UIElement child in contentPanel.Children)
                {
                    if (child != null)
                    {
                        child.Visibility = Visibility.Visible;
                    }
                }

                // 确保内容面板本身可见
                contentPanel.Visibility = Visibility.Visible;
                
                // 创建并启动折叠动画序列
                Storyboard storyboard = new Storyboard();
                storyboard.FillBehavior = FillBehavior.HoldEnd;
                
                // 保存动画引用以便后续可能的中断
                _runningAnimations[expander] = storyboard;

                // 计算每个子元素的延迟时间（从最后一个元素开始）
                double delayPerItem = AnimationDuration / (contentPanel.Children.Count * 2.0);

                for (int i = contentPanel.Children.Count - 1; i >= 0; i--)
                {
                    UIElement child = contentPanel.Children[i] as UIElement;
                    if (child == null)
                        continue;

                    // 透明度动画
                    DoubleAnimation opacityAnimation = CreateOpacityAnimation(1, 0, AnimationDuration, delayPerItem * (contentPanel.Children.Count - 1 - i));
                    Storyboard.SetTarget(opacityAnimation, child);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                    storyboard.Children.Add(opacityAnimation);

                    // 位置动画 - 向下移动而不是向上，避免与标题重叠
                    TranslateTransform transform = GetOrCreateTranslateTransform(child);
                    DoubleAnimation translateYAnimation = CreateTranslateYAnimation(0, 10, AnimationDuration, delayPerItem * (contentPanel.Children.Count - 1 - i));
                    Storyboard.SetTarget(translateYAnimation, transform);
                    Storyboard.SetTargetProperty(translateYAnimation, new PropertyPath(TranslateTransform.YProperty));
                    storyboard.Children.Add(translateYAnimation);
                }

                // 动画完成后完全折叠
                storyboard.Completed += (sender, e) =>
                {
                    // 清理动画引用
                    Storyboard completedStoryboard;
                    _runningAnimations.TryRemove(expander, out completedStoryboard);
                    
                    // 先隐藏所有子元素，然后隐藏内容面板
                    HideAllChildren(contentPanel);
                    
                    // 确保内容面板在动画完成后也被隐藏
                    contentPanel.Visibility = Visibility.Collapsed;
                };

                // 启动动画
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                (Application.Current as App)?.LogMessage($"折叠动画执行失败: {ex.Message}");
                // 发生异常时，直接隐藏所有子元素
                HideAllChildren(contentPanel);
            }
        }

        /// <summary>
        /// 为展开动画准备所有子元素
        /// </summary>
        /// <param name="contentPanel">内容面板</param>
        private static void PrepareChildrenForExpansion(Panel contentPanel)
        {
            foreach (UIElement child in contentPanel.Children)
            {
                if (child == null)
                    continue;

                // 设置初始状态
                child.Opacity = 0;
                child.Visibility = Visibility.Visible;

                // 确保有变换 - 向下移动而不是向上，避免与标题重叠
                GetOrCreateTranslateTransform(child).Y = 10;
            }
        }

        /// <summary>
        /// 隐藏所有子元素
        /// </summary>
        /// <param name="contentPanel">内容面板</param>
        private static void HideAllChildren(Panel contentPanel)
        {
            if (contentPanel == null)
                return;

            foreach (object childObj in contentPanel.Children)
            {
                if (childObj is UIElement child)
                {
                    child.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 取消当前正在运行的动画（如果有）
        /// </summary>
        /// <param name="expander">目标折叠面板</param>
        private static void CancelRunningAnimation(Expander expander)
        {
            if (_runningAnimations.TryGetValue(expander, out Storyboard runningStoryboard))
            {
                try
                {
                    runningStoryboard.Stop();
                    runningStoryboard.Children.Clear();
                }
                catch (Exception ex)
                {
                    (Application.Current as App)?.LogMessage($"取消动画失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取或创建TranslateTransform
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <returns>TranslateTransform对象</returns>
        private static TranslateTransform GetOrCreateTranslateTransform(UIElement element)
        {
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                // 查找现有的TranslateTransform
                foreach (Transform transform in transformGroup.Children)
                {
                    if (transform is TranslateTransform translateTransform)
                    {
                        return translateTransform;
                    }
                }

                // 如果没有找到，添加一个新的TranslateTransform
                TranslateTransform newTranslateTransform = new TranslateTransform();
                transformGroup.Children.Add(newTranslateTransform);
                return newTranslateTransform;
            }
            else if (element.RenderTransform is TranslateTransform existingTransform)
            {
                return existingTransform;
            }
            else
            {
                // 创建新的TransformGroup并添加TranslateTransform
                TransformGroup newTransformGroup = new TransformGroup();
                TranslateTransform translateTransform = new TranslateTransform();
                newTransformGroup.Children.Add(translateTransform);
                element.RenderTransform = newTransformGroup;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
                return translateTransform;
            }
        }

        /// <summary>
        /// 创建透明度动画
        /// </summary>
        /// <param name="fromValue">起始值</param>
        /// <param name="toValue">结束值</param>
        /// <param name="duration">持续时间（毫秒）</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>DoubleAnimation对象</returns>
        private static DoubleAnimation CreateOpacityAnimation(double fromValue, double toValue, int duration, double delay)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = fromValue,
                To = toValue,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                BeginTime = TimeSpan.FromMilliseconds(delay),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            return animation;
        }

        /// <summary>
        /// 创建Y轴位移动画
        /// </summary>
        /// <param name="fromValue">起始值</param>
        /// <param name="toValue">结束值</param>
        /// <param name="duration">持续时间（毫秒）</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>DoubleAnimation对象</returns>
        private static DoubleAnimation CreateTranslateYAnimation(double fromValue, double toValue, int duration, double delay)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = fromValue,
                To = toValue,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                BeginTime = TimeSpan.FromMilliseconds(delay),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            return animation;
        }
    }
}