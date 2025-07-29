using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;

namespace ImageRecognitionApp.Localization
{
    public static class LocalizationHelper
    {
        private static Dictionary<string, string> _translations = new Dictionary<string, string>();
        private static string _currentLanguage = "zh-CN";
        private static readonly string _defaultLanguage = "zh-CN";

        public static Dictionary<string, string> Strings => _translations;

        public static event PropertyChangedEventHandler? PropertyChanged;

        public static void Initialize()
        {
            LoadLanguage(_currentLanguage);
        }

        public static void LoadLanguage(string languageCode)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "ExcelConfig", $"{languageCode}.json");
            
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "ExcelConfig", $"{_defaultLanguage}.json");
            }

            try
            {
                var jsonContent = File.ReadAllText(filePath);
                _translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                _currentLanguage = languageCode;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(Strings)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载语言文件失败: {ex.Message}");
                _translations = new Dictionary<string, string>();
                _currentLanguage = _defaultLanguage;
            }
        }

        public static string GetString(string key)
        {
            return _translations.TryGetValue(key, out var value) ? value : key;
        }
    }
}