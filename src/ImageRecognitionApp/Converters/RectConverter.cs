using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 矩形转换器：将四个数值参数转换为RectangleGeometry对象
    /// 用于动态创建背景图片的裁切区域
    /// </summary>
    public class RectConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将四个参数（x, y, width, height）转换为RectangleGeometry对象
        /// </summary>
        /// <param name="values">四个数值参数：x, y, width, height</param>
        /// <param name="targetType">目标类型（RectangleGeometry）</param>
        /// <param name="parameter">附加参数（未使用）</param>
        /// <param name="culture">文化信息（未使用）</param>
        /// <returns>包含指定矩形区域的RectangleGeometry对象</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 确保有四个参数
            if (values.Length != 4)
                return new RectangleGeometry();

            try
            {
                // 尝试将参数转换为double类型
                double x = System.Convert.ToDouble(values[0]);
                double y = System.Convert.ToDouble(values[1]);
                double width = System.Convert.ToDouble(values[2]);
                double height = System.Convert.ToDouble(values[3]);

                // 创建矩形区域，设置圆角半径为10
                return new RectangleGeometry(new System.Windows.Rect(x, y, width, height), 10, 10);
            }
            catch
            {
                // 如果转换失败，返回空的矩形区域
                return new RectangleGeometry();
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