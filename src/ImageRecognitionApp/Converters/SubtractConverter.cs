using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 用于减法运算的转换器
    /// </summary>
    public class SubtractConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查输入参数数量
            if (values.Length < 2)
                return 0.0;

            // 处理可能的空值和类型转换
            double firstValue = 0.0;
            double secondValue = 0.0;

            // 尝试将第一个值转换为double
            if (values[0] is double)
                firstValue = (double)values[0];
            else if (values[0] != null)
                double.TryParse(values[0].ToString(), out firstValue);

            // 尝试将第二个值转换为double
            if (values[1] is double)
                secondValue = (double)values[1];
            else if (values[1] != null)
                double.TryParse(values[1].ToString(), out secondValue);

            // 处理可能的NaN值
            if (double.IsNaN(firstValue) || double.IsNaN(secondValue))
                return 0.0;

            // 确保结果不为负数
            return Math.Max(0.0, firstValue - secondValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}