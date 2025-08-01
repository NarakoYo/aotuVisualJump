using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NLua;

namespace ImageRecognitionApp.Localization
{
    public static class LuaLocalizationHelper
    {
        private static readonly Dictionary<int, Dictionary<string, string>> _localizationData = new Dictionary<int, Dictionary<string, string>>();
        private static string _currentLanguage = CultureInfo.CurrentCulture.Name.ToLower();
        private static readonly string _defaultLanguage = "zhcn";
        private static readonly string _luaFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "ExcelConfig", "localization.lua");

        /// <summary>
        /// 初始化本地化数据
        /// </summary>
        public static void Initialize()
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
        private static void LoadLocalizationData()
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
                        if (key is int index)
                        {
                            var itemTable = localizationTable[index] as LuaTable;
                            if (itemTable != null)
                            {
                                int signId = Convert.ToInt32(itemTable["sign_id"]);
                                var translations = new Dictionary<string, string>();

                                // 提取所有语言翻译
                                foreach (var langKey in itemTable.Keys)
                                {
                                    if (langKey is string langCode && langCode != "sign_id" && langCode != "isEx")
                                    {
                                        translations[langCode.ToLower()] = Convert.ToString(itemTable[langKey]);
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
        private static bool IsLanguageSupported(string languageCode)
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
        public static string GetString(int signId)
        {
            return GetString(signId, _currentLanguage);
        }

        /// <summary>
        /// 获取指定sign_id和语言的本地化字符串
        /// </summary>
        /// <param name="signId">标识ID</param>
        /// <param name="languageCode">语言代码</param>
        /// <returns>本地化字符串</returns>
        public static string GetString(int signId, string languageCode)
        {
            languageCode = languageCode.ToLower();

            // 检查是否存在该sign_id
            if (!_localizationData.TryGetValue(signId, out var translations))
            {
                return $"[{signId}]";
            }

            // 检查是否存在该语言的翻译
            if (translations.TryGetValue(languageCode, out var value))
            {
                return value;
            }

            // 如果不存在，尝试使用默认语言
            if (languageCode != _defaultLanguage && translations.TryGetValue(_defaultLanguage, out var defaultValue))
            {
                return defaultValue;
            }

            // 如果默认语言也不存在，返回sign_id
            return $"[{signId}]";
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否设置成功</returns>
        public static bool SetCurrentLanguage(string languageCode)
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
        public static List<string> GetSupportedLanguages()
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
