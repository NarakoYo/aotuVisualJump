using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 用于计算侧边栏宽度的转换器
    /// 计算方式：窗口宽度的1/5
    /// </summary>
    public class SidebarWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double windowWidth)
            {
                // 计算窗口宽度的1/5
                double sidebarWidth = windowWidth / 5;
                // 确保结果不为负数
                return Math.Max(0.0, sidebarWidth);
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
