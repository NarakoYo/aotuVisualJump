using System;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;

namespace ImageRecognitionApp.UnitTools
{
    /// <summary>
    /// 图标帮助类，用于处理PNG图标加载
    /// </summary>
    public static class IconHelper
    {
        /// <summary>
        /// 从资源中加载PNG图标
        /// </summary>
        /// <param name="resourcePath">资源路径，例如："/Resources/Icons/setting-gear.png"
        /// <returns>BitmapImage对象</returns>
        public static BitmapImage LoadPngIcon(string resourcePath)
        {
            try
            {
                // 确保路径以斜杠开头
                if (!resourcePath.StartsWith("/"))
                    resourcePath = "/" + resourcePath;

                // 创建Uri，使用pack://application:,,,格式
                var uri = new Uri("pack://application:,,," + resourcePath, UriKind.Absolute);

                // 创建BitmapImage并设置Uri
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.EndInit();

                return bitmap;
            }
            catch (Exception ex)
            {
                // 记录错误并返回空
                Console.WriteLine($"加载PNG图标失败: {ex.Message}");
                // 返回一个空白图像而不是null
                return new BitmapImage();
            }
        }

        /// <summary>
        /// 检查PNG图标文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否存在</returns>
        public static bool IsPngIconExists(string filePath)
        {
            try
            {
                return File.Exists(filePath) && Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}