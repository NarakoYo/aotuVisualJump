using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 高度转换器：根据参数将窗口高度转换为指定比例的高度，可以选择性地添加偏移量
    /// </summary>
    /// <remarks>
    /// 参数格式："ratio[+offset]"，例如："0.8"表示高度的80%，"0.8+4"表示高度的80%再加上4像素
    /// </remarks>
    public class HeightConverter : IValueConverter
    {
        /// <summary>
        /// 将源值转换为目标值
        /// </summary>
        /// <param name="value">要转换的源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的目标值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                // 默认返回窗口高度的4/5
                double ratio = 0.8;
                double offset = 0;
                
                // 如果提供了参数，尝试解析比例值和可选的偏移量
                if (parameter != null)
                {
                    string paramStr = parameter.ToString();
                    // 检查是否包含偏移量
                    if (paramStr.Contains('+'))
                    {
                        string[] parts = paramStr.Split('+');
                        if (parts.Length > 0 && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedRatio))
                        {
                            ratio = parsedRatio;
                        }
                        if (parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedOffset))
                        {
                            offset = parsedOffset;
                        }
                    }
                    // 只有比例值
                    else if (double.TryParse(paramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parameterRatio))
                    {
                        ratio = parameterRatio;
                    }
                }
                
                return height * ratio + offset;
            }
            return value;
        }

        /// <summary>
        /// 将目标值转换回源值（未实现）
        /// </summary>
        /// <param name="value">要转换的目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的源值</returns>
        /// <exception cref="NotImplementedException">此方法未实现</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}