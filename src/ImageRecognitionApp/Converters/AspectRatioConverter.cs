using System;
using System.Globalization;
using System.Windows.Data;
using ImageRecognitionApp;
using ImageRecognitionApp.unit;

namespace ImageRecognitionApp.Converters
{
    /// <summary>
    /// 用于宽高比计算的转换器
    /// </summary>
    public class AspectRatioConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查输入参数数量
            if (values.Length < 3)
                return 0.0;

            // 处理可能的空值和类型转换
            double widthValue = 0.0;
            double heightRatio = 10.0; // 默认高度比例
            double widthRatio = 16.0; // 默认宽度比例

            // 尝试将宽度值转换为double
            if (values[0] is double)
                widthValue = (double)values[0];
            else if (values[0] != null)
                double.TryParse(values[0].ToString(), out widthValue);

            // 尝试将目标高度比例转换为double
            if (values[1] is double)
                heightRatio = (double)values[1];
            else if (values[1] != null)
                double.TryParse(values[1].ToString(), out heightRatio);

            // 尝试将目标宽度比例转换为double
            if (values[2] is double)
                widthRatio = (double)values[2];
            else if (values[2] != null)
                double.TryParse(values[2].ToString(), out widthRatio);

            // 处理可能的NaN值和除以零的情况
            if (double.IsNaN(widthValue) || double.IsNaN(heightRatio) || double.IsNaN(widthRatio) || widthRatio == 0)
                return 0.0;

            // 计算高度：宽度 * 高度比例 / 宽度比例
            double heightValue = widthValue * heightRatio / widthRatio;

            (App.Current as App)?.LogMessage($"AspectRatioConverter: [Info] 计算结果 - 宽度: {widthValue}, 高度比例: {heightRatio}, 宽度比例: {widthRatio}, 计算高度: {heightValue}");

            // 确保结果不为负数
            return Math.Max(0.0, heightValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}