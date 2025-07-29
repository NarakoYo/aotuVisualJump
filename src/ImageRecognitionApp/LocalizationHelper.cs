using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace ImageRecognitionApp
{
    public static class LocalizationHelper
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();
        private static string _currentLanguage = "zh-CN";

        public static event PropertyChangedEventHandler PropertyChanged = delegate { };

        public static Dictionary<string, string> Strings
        {
            get => _strings;
            private set
            {
                _strings = value;
                OnPropertyChanged(nameof(Strings));
            }
        }

        static LocalizationHelper()
        {
            LoadLocalization();
        }

        public static void LoadLocalization()
        {
            try
            {
                var localizationDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization");
                if (!Directory.Exists(localizationDirectory))
                {
                    Directory.CreateDirectory(localizationDirectory);
                }

                var filePath = Path.Combine(localizationDirectory, $"{_currentLanguage}.json");
                // 写入调试日志，记录文件路径和是否存在
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localization_debug.log"), 
                    $"Path: {filePath}\nExists: {File.Exists(filePath)}");

                if (File.Exists(filePath))
                {
                    var jsonContent = File.ReadAllText(filePath);
                    Strings = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                }
                else
                {
                    // 创建默认本地化文件
                    Strings = new Dictionary<string, string>
                    {
                        { "window.title", "图像识别应用" },
                        { "button.load_image", "加载图像" },
                        { "button.analyze", "分析图像" },
                        { "text.status", "就绪" }
                    };
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(Strings, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localization_error.log"), 
                    $"本地化加载错误: {ex}");
                Strings = new Dictionary<string, string>
                {
                    { "window.title", "图像识别应用" },
                    { "button.load_image", "加载图像" },
                    { "button.analyze", "分析图像" },
                    { "text.status", "就绪" }
                };
            }
        }

        private static void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}