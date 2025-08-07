using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 乘法转换器 - 将值乘以指定的参数
    /// </summary>
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return value;

            if (double.TryParse(value.ToString(), out double valueDouble) &&
                double.TryParse(parameter.ToString(), out double paramDouble))
            {
                return valueDouble * paramDouble;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return value;

            if (double.TryParse(value.ToString(), out double valueDouble) &&
                double.TryParse(parameter.ToString(), out double paramDouble) &&
                paramDouble != 0)
            {
                return valueDouble / paramDouble;
            }

            return value;
        }
    }
}