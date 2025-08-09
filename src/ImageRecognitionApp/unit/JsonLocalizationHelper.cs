using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageRecognitionApp.unit
{
    public class JsonLocalizationHelper
    {
        private static readonly Lazy<JsonLocalizationHelper> _instance = new Lazy<JsonLocalizationHelper>(() => new JsonLocalizationHelper());
        private readonly Dictionary<int, Dictionary<string, string>> _localizationData = new Dictionary<int, Dictionary<string, string>>();
        private string _currentLanguage;
        private readonly string _defaultLanguage = "zh-cn";
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "ExcelConfig", "localization.json");

        public static JsonLocalizationHelper Instance => _instance.Value;

        private JsonLocalizationHelper()
        {
            // 私有构造函数，防止外部实例化
            _currentLanguage = NormalizeLanguageCode(CultureInfo.CurrentCulture.Name);
        }

        /// <summary>
        /// 标准化语言代码格式
        /// </summary>
        /// <param name="languageCode">原始语言代码</param>
        /// <returns>标准化后的语言代码</returns>
        private string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return _defaultLanguage;

            // 特殊处理ghYh字段
            if (languageCode.Equals("ghYh", StringComparison.OrdinalIgnoreCase))
                return "gh-yh";

            // 处理 CultureInfo.Name 格式 (如 zh-CN, en-US)
            if (languageCode.Contains('-'))
            {
                var parts = languageCode.Split('-');
                if (parts.Length >= 2)
                    return $"{parts[0].ToLower()}-{parts[1].ToLower()}";
                return parts[0].ToLower();
            }
            // 处理驼峰格式 (如 zhCn, enUs)
            else if (System.Text.RegularExpressions.Regex.IsMatch(languageCode, @"[a-z][A-Z]"))
            {
                return System.Text.RegularExpressions.Regex.Replace(
                    languageCode, 
                    "([a-z])([A-Z])", 
                    "$1-$2").ToLower();
            }
            // 处理其他格式
            else
            {
                return languageCode.ToLower();
            }
        }

        /// <summary>
        /// 初始化本地化数据
        /// </summary>
        public void Initialize()
        {
            try
            {
                LoadLocalizationData();
                // 标准化当前语言代码
                _currentLanguage = NormalizeLanguageCode(_currentLanguage);
                Console.WriteLine($"已加载本地化数据，当前语言: {_currentLanguage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化本地化数据时出错: {ex.Message}");
                // 在出错情况下确保使用默认语言
                _currentLanguage = _defaultLanguage;
            }
        }

        /// <summary>
        /// 加载本地化数据
        /// </summary>
        private void LoadLocalizationData()
        {
            if (!File.Exists(_jsonFilePath))
            {
                throw new FileNotFoundException("本地化文件不存在", _jsonFilePath);
            }

            try
            {
                // 读取JSON文件内容
                string jsonContent = File.ReadAllText(_jsonFilePath, System.Text.Encoding.UTF8);

                // 解析JSON
                using (var doc = JsonDocument.Parse(jsonContent))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("localization", out var localizationArray))
                    {
                        foreach (var item in localizationArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("sign_id", out var signIdElement))
                            {
                                // 尝试获取sign_id的整数值
                                if (!signIdElement.TryGetInt32(out int signId))
                                {
                                    Console.WriteLine($"警告: 跳过非数值类型的sign_id: {signIdElement.ToString()}");
                                    continue;
                                }

                                // 检查isEx字段是否存在且为数值类型
                                bool isExValid = true;
                                if (item.TryGetProperty("isEx", out var isExElement))
                                {
                                    // 检查isEx是否为数值类型或布尔类型
                                    if (isExElement.ValueKind != JsonValueKind.Number && isExElement.ValueKind != JsonValueKind.True && isExElement.ValueKind != JsonValueKind.False)
                                    {
                                        Console.WriteLine($"警告: 跳过isEx为非数值类型的条目，sign_id: {signId}");
                                        isExValid = false;
                                    }
                                }

                                if (!isExValid)
                                {
                                    continue;
                                }

                                var translations = new Dictionary<string, string>();

                                // 遍历所有属性
                                foreach (var property in item.EnumerateObject())
                                {
                                    string propertyName = property.Name;
                                    if (propertyName == "sign_id" || propertyName == "isEx")
                                        continue;

                                    // 标准化语言代码
                                    string normalizedLangCode;
                                    if (propertyName.Equals("zhCn", StringComparison.OrdinalIgnoreCase))
                                    {
                                        normalizedLangCode = "zh-cn";
                                    }
                                    else if (propertyName.Equals("ghYh", StringComparison.OrdinalIgnoreCase))
                                    {
                                        normalizedLangCode = "gh-yh";
                                    }
                                    else
                                    {
                                        normalizedLangCode = NormalizeLanguageCode(propertyName);
                                    }

                                    // 获取翻译值
                                    string translation = property.Value.GetString() ?? string.Empty;
                                    translations[normalizedLangCode] = translation;
                                }

                                _localizationData[signId] = translations;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载本地化数据时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 检查语言是否被支持
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否支持</returns>
        private bool IsLanguageSupported(string languageCode)
        {
            // 如果没有数据，直接返回false
            if (_localizationData.Count == 0)
            {
                return false;
            }

            // 检查是否有任何条目包含该语言代码
            return _localizationData.Values.Any(translations => translations.ContainsKey(languageCode));
        }

        /// <summary>
        /// 获取指定sign_id的本地化字符串
        /// </summary>
        /// <param name="signId">标识ID</param>
        /// <returns>本地化字符串</returns>
        public string GetString(int signId)
        {
            return GetString(signId, _currentLanguage);
        }

        /// <summary>
        /// 验证传入参数与sign_id对应值是否一致
        /// </summary>
        /// <param name="signId">标识ID</param>
        /// <param name="expectedValue">期望的值</param>
        /// <returns>是否一致</returns>
        public bool ValidateSignIdValue(int signId, string expectedValue)
        {
            if (_localizationData.TryGetValue(signId, out var translations))
            {
                // 检查当前语言是否有对应翻译
                if (translations.TryGetValue(_currentLanguage, out var currentValue))
                {
                    return currentValue == expectedValue;
                }
                // 检查默认语言是否有对应翻译
                else if (translations.TryGetValue(_defaultLanguage, out var defaultValue))
                {
                    return defaultValue == expectedValue;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取指定sign_id和语言的本地化字符串
        /// </summary>
        /// <param name="signId">标识ID</param>
        /// <param name="languageCode">语言代码</param>
        /// <returns>本地化字符串</returns>
        public string GetString(int signId, string languageCode)
        {
            try
            {
                if (string.IsNullOrEmpty(languageCode))
                    languageCode = _currentLanguage;

                // 使用标准化方法处理语言代码
                string normalizedLangCode = NormalizeLanguageCode(languageCode);

                // 检查翻译数据是否存在
                if (_localizationData.TryGetValue(signId, out var translations))
                {
                    if (translations.TryGetValue(normalizedLangCode, out var value) && !string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine($"找到翻译: {value}");
                        return value;
                    }

                    // 如果当前语言没有找到或值为空，尝试使用ghYh字段
                    if (translations.TryGetValue("gh-yh", out value) && !string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine($"未找到当前语言翻译，使用ghYh字段值: {value}");
                        return value;
                    }

                    // 如果没有找到当前语言和ghYh字段的翻译，返回sign_id+缺失语言标记
                    return $"{signId}_MISSING_{normalizedLangCode}";
                }
                else
                {
                    // 如果未找到sign_id，返回错误码
                    return "未找到本地化id";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取本地化字符串时出错: {ex.Message}");
                return $"ERROR_{signId}";
            }
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否设置成功</returns>
        public bool SetCurrentLanguage(string languageCode)
        {
            try
            {
                if (string.IsNullOrEmpty(languageCode))
                {
                    Console.WriteLine("错误: 语言代码不能为空");
                    return false;
                }

                // 标准化语言代码
                var normalizedLanguage = NormalizeLanguageCode(languageCode);

                if (!IsLanguageSupported(normalizedLanguage))
                {
                    Console.WriteLine($"错误: 语言 '{normalizedLanguage}' 不受支持");
                    return false;
                }

                _currentLanguage = normalizedLanguage;
                Console.WriteLine($"已切换到语言: {_currentLanguage}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置语言时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取当前支持的所有语言代码
        /// </summary>
        /// <returns>语言代码列表</returns>
        public List<string> GetSupportedLanguages()
        {
            if (_localizationData.Count == 0)
            {
                return new List<string>();
            }

            // 获取所有唯一的语言代码
            var languages = new HashSet<string>();
            foreach (var translations in _localizationData.Values)
            {
                foreach (var langCode in translations.Keys)
                {
                    languages.Add(langCode);
                }
            }

            return languages.ToList();
        }
    }
}