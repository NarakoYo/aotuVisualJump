using ImageRecognitionApp.unit;
using System;

namespace ImageRecognitionApp.unit.assist
{
    /// <summary>
    /// 本地化标题助手类，用于获取应用程序的本地化标题
    /// </summary>
    public static class LocalizedTitleHelper
    {
        private const int APP_TITLE_SIGN_ID = 10001;
        private const string DEFAULT_APP_TITLE = "AI视觉识别操作";

        /// <summary>
        /// 获取应用程序的本地化标题
        /// </summary>
        /// <returns>本地化后的应用程序标题</returns>
        public static string GetLocalizedAppTitle()
        {
            try
            {
                // 确保本地化工具已初始化
                if (!JsonLocalizationHelper.Instance.IsInitialized)
                {
                    try
                    {
                        JsonLocalizationHelper.Instance.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"初始化本地化工具失败: {ex.Message}");
                        return DEFAULT_APP_TITLE;
                    }
                }

                // 获取本地化标题
                string localizedTitle = JsonLocalizationHelper.Instance.GetString(APP_TITLE_SIGN_ID);

                // 检查是否获取到有效的本地化标题
                if (string.IsNullOrEmpty(localizedTitle) ||
                    localizedTitle.StartsWith("未找到") ||
                    localizedTitle.StartsWith("ERROR_") ||
                    localizedTitle.Contains("_MISSING_"))
                {
                    Console.WriteLine($"未获取到sign_id={APP_TITLE_SIGN_ID}的本地化文本，使用默认标题: {DEFAULT_APP_TITLE}");
                    return DEFAULT_APP_TITLE;
                }

                return localizedTitle;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取本地化标题时出错: {ex.Message}");
                return DEFAULT_APP_TITLE;
            }
        }
    }
}