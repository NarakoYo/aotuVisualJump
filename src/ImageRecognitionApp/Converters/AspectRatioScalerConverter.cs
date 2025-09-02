using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 用于计算内容在保持原始比例的情况下如何适应展示区域的转换器
    /// 参考OBS的实现方式，确保内容按比例缩放至展示区域的最大值
    /// </summary>
    public class AspectRatioScalerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查输入参数数量
            if (values.Length < 4)
                return 0.0;

            // 处理可能的空值和类型转换
            double containerWidth = 0.0;
            double containerHeight = 0.0;
            double originalWidth = 0.0;
            double originalHeight = 0.0;
            string dimensionType = "Width";

            // 尝试将容器宽度转换为double
            if (values[0] is double)
                containerWidth = (double)values[0];
            else if (values[0] != null)
                double.TryParse(values[0].ToString(), out containerWidth);

            // 尝试将容器高度转换为double
            if (values[1] is double)
                containerHeight = (double)values[1];
            else if (values[1] != null)
                double.TryParse(values[1].ToString(), out containerHeight);

            // 尝试将原始宽度转换为double
            if (values[2] is double)
                originalWidth = (double)values[2];
            else if (values[2] != null)
                double.TryParse(values[2].ToString(), out originalWidth);

            // 尝试将原始高度转换为double
            if (values[3] is double)
                originalHeight = (double)values[3];
            else if (values[3] != null)
                double.TryParse(values[3].ToString(), out originalHeight);

            // 如果有第五个参数，表示要计算的是宽度还是高度
            if (values.Length >= 5 && values[4] != null)
                dimensionType = values[4].ToString();

            // 处理可能的NaN值和除以零的情况
            if (double.IsNaN(containerWidth) || double.IsNaN(containerHeight) || 
                double.IsNaN(originalWidth) || double.IsNaN(originalHeight) || 
                originalWidth == 0 || originalHeight == 0)
                return 0.0;

            // 计算缩放比例 - 取宽度和高度比例中的较小值，以确保内容完全适应容器
            double scaleX = containerWidth / originalWidth;
            double scaleY = containerHeight / originalHeight;
            double scaleFactor = Math.Min(scaleX, scaleY);

            // 根据dimensionType返回相应的值
            if (dimensionType.Equals("Height", StringComparison.OrdinalIgnoreCase))
            {
                // 返回计算后的高度
                return Math.Max(0.0, originalHeight * scaleFactor);
            }
            else
            {
                // 默认返回计算后的宽度
                return Math.Max(0.0, originalWidth * scaleFactor);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // 不实现反向转换
            throw new NotImplementedException();
        }
    }
}