using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ImageRecognitionApp.unit;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 用于计算设置按钮与上一个按钮间距的转换器
    /// 计算方式：侧边栏高度 - 折叠按钮的高度 - 折叠按钮上下的间距 - 其他按钮的高度和 - 其他按钮之间的间距和 - 设置按钮的高度
    /// </summary>
    public class SettingButtonSpacingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查输入参数数量
            if (values.Length < 1)
                return 0.0;

            // 处理可能的空值和类型转换
            double sidebarHeight = 0.0;

            // 尝试将第一个值(侧边栏高度)转换为double
            if (values[0] is double)
                sidebarHeight = (double)values[0];
            else if (values[0] != null)
                double.TryParse(values[0].ToString(), out sidebarHeight);

            // 折叠按钮的高度
            double collapseButtonHeight = 40.0;
            // 折叠按钮上下的间距总和
            double collapseButtonSpacing = 10.0; // 上5px，下5px
            // 其他按钮的高度和（8个按钮，每个45px）
            double otherButtonsHeightSum = 8 * 45.0;
            // 其他按钮之间的间距和（7个间距，每个5px）
            double otherButtonsSpacingSum = 7 * 5.0;
            // 设置按钮的高度
            double settingButtonHeight = 45.0;

            // 计算设置按钮与上一个按钮的间距
            double spacing = sidebarHeight - collapseButtonHeight - collapseButtonSpacing - 
                            otherButtonsHeightSum - otherButtonsSpacingSum - settingButtonHeight;

            // 使用日志管理器记录调试信息
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, "===== SettingButtonSpacingConverter 调试信息 =====");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"侧边栏高度: {sidebarHeight}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"折叠按钮高度: {collapseButtonHeight}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"折叠按钮间距总和: {collapseButtonSpacing}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"其他按钮高度总和: {otherButtonsHeightSum}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"其他按钮间距总和: {otherButtonsSpacingSum}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"设置按钮高度: {settingButtonHeight}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"计算得到的间距: {spacing}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"返回的最终间距: {Math.Max(0.0, spacing)}");
            LogManager.Instance.WriteLog(LogManager.LogLevel.Info, "==============================================");

            // 确保结果不为负数
            double topMargin = Math.Max(0.0, spacing);
            
            // 返回Thickness对象，只设置顶部间距
            return new Thickness(0, topMargin, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}