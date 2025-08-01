using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NLua;

namespace ImageRecognitionApp.unit
{
    public class LuaLocalizationHelper
    {
        private static readonly Lazy<LuaLocalizationHelper> _instance = new Lazy<LuaLocalizationHelper>(() => new LuaLocalizationHelper());
        private readonly Dictionary<int, Dictionary<string, string>> _localizationData = new Dictionary<int, Dictionary<string, string>>();
        private string _currentLanguage = System.Text.RegularExpressions.Regex.Replace(
            CultureInfo.CurrentCulture.Name, 
            "([a-z])([A-Z])", 
            "$1-$2").ToLower();
        private readonly string _defaultLanguage = "zh-cn";
        private readonly string _luaFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "ExcelConfig", "localization.lua");

        public static LuaLocalizationHelper Instance => _instance.Value;

        private LuaLocalizationHelper()
        {
            // 私有构造函数，防止外部实例化
        }

        /// <summary>
        /// 初始化本地化数据
        /// </summary>
        public void Initialize()
        {
            LoadLocalizationData();
            // 确保当前语言有效，如果无效则使用默认语言
            if (!IsLanguageSupported(_currentLanguage))
            {
                _currentLanguage = _defaultLanguage;
            }
        }

        /// <summary>
        /// 加载本地化数据
        /// </summary>
        private void LoadLocalizationData()
        {
            if (!File.Exists(_luaFilePath))
            {
                throw new FileNotFoundException("本地化文件不存在", _luaFilePath);
            }

            try
            {
                using (var lua = new Lua())
                {
                    // 加载并执行Lua文件
                    lua.DoFile(_luaFilePath);

                    // 获取localization表
                    var localizationTable = lua.GetTable("localization");
                    if (localizationTable == null)
                    {
                        throw new Exception("Lua文件中未找到localization表");
                    }

                    // 遍历表中的所有项
                    foreach (var key in localizationTable.Keys)
                    {
                        var itemTable = localizationTable[key] as LuaTable;
                        if (itemTable != null)
                        {
                            // 检查itemTable是否包含sign_id字段
                            if (itemTable.Keys.Cast<object>().Any(k => k.ToString() == "sign_id"))
                            {
                                int signId = Convert.ToInt32(itemTable["sign_id"]);
                                var translations = new Dictionary<string, string>();

                                // 提取所有语言翻译
                                foreach (var langKey in itemTable.Keys)
                                {
                                    if (langKey is string langCode && langCode != "sign_id" && langCode != "isEx")
                                    {
                                        // 将驼峰式语言代码转换为带有连字符的格式（如zhCn -> zh-cn）
                                        var normalizedLangCode = System.Text.RegularExpressions.Regex.Replace(
                                            langCode, 
                                            "([a-z])([A-Z])", 
                                            "$1-$2").ToLower();

                                        object? value = itemTable[langKey];
                                        translations[normalizedLangCode] = Convert.ToString(value) ?? string.Empty;
                                    }
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
            if (string.IsNullOrEmpty(languageCode))
                languageCode = _currentLanguage;

            // 将驼峰式语言代码转换为带连字符的格式
            string normalizedLangCode = System.Text.RegularExpressions.Regex.Replace(languageCode, @"([a-z])([A-Z])", "$1-$2").ToLower();

            if (_localizationData.TryGetValue(signId, out var translations))
            {
                if (translations.TryGetValue(normalizedLangCode, out var value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }

                // 如果当前语言没有找到或值为空，尝试使用默认语言
                if (translations.TryGetValue(_defaultLanguage, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }

                // 如果没有找到当前语言和默认语言的翻译，返回sign_id+缺失语言标记
                return $"{signId}_MISSING_{normalizedLangCode}";
            }
            else
            {
                // 如果未找到sign_id，返回错误码
                return "未找到本地化id";
            }
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否设置成功</returns>
        public bool SetCurrentLanguage(string languageCode)
        {
            languageCode = languageCode.ToLower();

            if (IsLanguageSupported(languageCode))
            {
                _currentLanguage = languageCode;
                return true;
            }

            return false;
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
