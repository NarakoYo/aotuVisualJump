using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 用于减法运算的转换器
    /// 支持多个参数的连续减法，从第一个值开始，依次减去后续所有值
    /// </summary>
    public class SubtractConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查输入参数数量
            if (values.Length < 1)
                return 0.0;

            // 处理可能的空值和类型转换
            double result = 0.0;

            // 尝试将第一个值转换为double
            if (values[0] is double)
                result = (double)values[0];
            else if (values[0] != null)
                double.TryParse(values[0].ToString(), out result);

            // 如果只有一个参数，直接返回
            if (values.Length == 1)
                return result;

            // 依次减去后续所有值
            for (int i = 1; i < values.Length; i++)
            {
                double value = 0.0;
                if (values[i] is double)
                    value = (double)values[i];
                else if (values[i] != null)
                    double.TryParse(values[i].ToString(), out value);

                // 处理可能的NaN值
                if (double.IsNaN(value))
                    continue;

                result -= value;
            }

            // 确保结果不为负数
            return Math.Max(0.0, result);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}