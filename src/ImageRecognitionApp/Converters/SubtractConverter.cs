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
            if (values.Length < 2 || !(values[0] is double firstValue) || !(values[1] is double secondValue))
                return 0.0;

            return firstValue - secondValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}