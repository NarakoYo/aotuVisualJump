using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// Thickness转换器：将四个数值参数转换为Thickness对象
    /// 用于动态创建边距
    /// </summary>
    public class ThicknessConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将四个参数（left, top, right, bottom）转换为Thickness对象
        /// </summary>
        /// <param name="values">四个数值参数：left, top, right, bottom</param>
        /// <param name="targetType">目标类型（Thickness）</param>
        /// <param name="parameter">附加参数（未使用）</param>
        /// <param name="culture">文化信息（未使用）</param>
        /// <returns>包含指定边距的Thickness对象</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 确保有四个参数
            if (values.Length != 4)
                return new Thickness();

            try
            {
                // 尝试将参数转换为double类型
                double left = System.Convert.ToDouble(values[0]);
                double top = System.Convert.ToDouble(values[1]);
                double right = System.Convert.ToDouble(values[2]);
                double bottom = System.Convert.ToDouble(values[3]);

                // 创建Thickness对象
                return new Thickness(left, top, right, bottom);
            }
            catch
            {
                // 如果转换失败，返回默认的Thickness对象
                return new Thickness();
            }
        }

        /// <summary>
        /// 反向转换方法（未实现）
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}