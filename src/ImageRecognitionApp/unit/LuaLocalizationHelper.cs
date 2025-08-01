using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NLua;

namespace ImageRecognitionApp.unit
{
    public class LuaLocalizationHelper
    {
        private static readonly Lazy<LuaLocalizationHelper> _instance = new Lazy<LuaLocalizationHelper>(() => new LuaLocalizationHelper());
        private readonly Dictionary<int, Dictionary<string, string>> _localizationData = new Dictionary<int, Dictionary<string, string>>();
        private string _currentLanguage;
        private readonly string _defaultLanguage = "zh-cn";

        /// <summary>
        /// 标准化语言代码格式
        /// </summary>
        /// <param name="languageCode">原始语言代码</param>
        /// <returns>标准化后的语言代码</returns>
        private string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return _defaultLanguage;

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
        private readonly string _luaFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "ExcelConfig", "localization.lua");

        public static LuaLocalizationHelper Instance => _instance.Value;

        private LuaLocalizationHelper()
        {
            // 私有构造函数，防止外部实例化
            _currentLanguage = NormalizeLanguageCode(CultureInfo.CurrentCulture.Name);
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
                // 确保当前语言有效，如果无效则使用默认语言
                if (!IsLanguageSupported(_currentLanguage))
                {
                    _currentLanguage = _defaultLanguage;
                    Console.WriteLine($"警告: 当前语言 '{_currentLanguage}' 不受支持，已切换到默认语言 '{_defaultLanguage}'");
                }
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
            if (!File.Exists(_luaFilePath))
            {
                throw new FileNotFoundException("本地化文件不存在", _luaFilePath);
            }

            try
            {
                using (var lua = new Lua())
                {
                    // 设置Lua解析器编码为UTF-8
                    lua.RegisterFunction("LoadFileWithEncoding", this, typeof(LuaLocalizationHelper).GetMethod("LoadFileWithEncoding"));
                    
                    
                    // 使用UTF-8编码加载Lua文件
                    string luaContent = LoadFileWithEncoding(_luaFilePath, Encoding.UTF8);
                    lua.DoString(luaContent);

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
                                        // 使用标准化方法处理语言代码
                                        var normalizedLangCode = NormalizeLanguageCode(langCode);

                                        object? value = itemTable[langKey];
                                        // 确保翻译文本是UTF-8编码
                                        string translation = Convert.ToString(value) ?? string.Empty;
                                        translations[normalizedLangCode] = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(translation));
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
            try
            {
                if (string.IsNullOrEmpty(languageCode))
                    languageCode = _currentLanguage;

                // 使用标准化方法处理语言代码
                string normalizedLangCode = NormalizeLanguageCode(languageCode);

                if (_localizationData.TryGetValue(signId, out var translations))
                {
                    if (translations.TryGetValue(normalizedLangCode, out var value) && !string.IsNullOrEmpty(value))
                    {
                        // 确保返回的字符串是UTF-8编码
                        return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value));
                    }

                    // 如果当前语言没有找到或值为空，尝试使用默认语言
                    if (translations.TryGetValue(_defaultLanguage, out value) && !string.IsNullOrEmpty(value))
                    {
                        // 确保返回的字符串是UTF-8编码
                        return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value));
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
            catch (Exception ex)
            {
                Console.WriteLine($"获取本地化字符串时出错: {ex.Message}");
                return $"ERROR_{signId}";
            }
        }

        /// <summary>
        /// 使用指定编码加载文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">编码</param>
        /// <returns>文件内容</returns>
        public string LoadFileWithEncoding(string filePath, Encoding encoding)
        {
            return File.ReadAllText(filePath, encoding);
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
