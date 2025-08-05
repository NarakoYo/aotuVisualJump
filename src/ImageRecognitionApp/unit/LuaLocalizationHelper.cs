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
                Console.WriteLine($"已加载本地化数据，当前语言: {_currentLanguage}");
            }            catch (Exception ex)
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
                // 确保文件以UTF-8编码读取
                string luaContent = File.ReadAllText(_luaFilePath, Encoding.UTF8);
                
                using (var lua = new Lua())
                {
                    // 设置Lua解析器编码为UTF-8
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
                                        // 特殊处理中文和ghYh字段
                                        string normalizedLangCode;
                                        if (langCode.Equals("zhCn", StringComparison.OrdinalIgnoreCase))
                                        {
                                            normalizedLangCode = "zh-cn";
                                        }
                                        else if (langCode.Equals("ghYh", StringComparison.OrdinalIgnoreCase))
                                        {
                                            normalizedLangCode = "gh-yh";
                                        }
                                        else
                                        {
                                            normalizedLangCode = NormalizeLanguageCode(langCode);
                                        }

                                        object? value = itemTable[langKey];
                                        string translation = Convert.ToString(value) ?? string.Empty;
                                        translations[normalizedLangCode] = translation;
                                        // Console.WriteLine($"已加载翻译: sign_id={signId}, lang={normalizedLangCode}, translation={translation}");
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
        /// 使用指定编码加载文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">编码</param>
        /// <returns>文件内容</returns>
        public string LoadFileWithEncoding(string filePath, Encoding encoding)
        {
            // 强制使用UTF-8编码（带BOM）确保中文正确处理
            encoding = new UTF8Encoding(true);
            
            // 直接使用File.ReadAllText并指定编码
            string content = File.ReadAllText(filePath, encoding);
            
            return content;
        }
        
        /// <summary>
        /// 检测文件编码
        /// </summary>
        /// <param name="fileBytes">文件字节数组</param>
        /// <returns>检测到的编码名称</returns>
        private string DetectFileEncoding(byte[] fileBytes)
        {
            // 检查UTF-8 BOM
            if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            {
                return "UTF-8 带BOM";
            }
            // 检查UTF-16 BE BOM
            else if (fileBytes.Length >= 2 && fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
            {
                return "UTF-16 BE";
            }
            // 检查UTF-16 LE BOM
            else if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
            {
                return "UTF-16 LE";
            }
            // 尝试检测是否为UTF-8
            else if (IsUTF8(fileBytes))
            {
                return "UTF-8 无BOM";
            }
            // 默认假设为ANSI/GBK
            else
            {
                return "ANSI/GBK";
            }
        }
        
        /// <summary>
        /// 检查字节数组是否为UTF-8编码
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <returns>是否为UTF-8</returns>
        private bool IsUTF8(byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length)
            {
                if (bytes[i] <= 0x7F)
                {
                    i++;
                }
                else if (bytes[i] >= 0xC0 && bytes[i] <= 0xDF)
                {
                    if (i + 1 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF)
                        return false;
                    i += 2;
                }
                else if (bytes[i] >= 0xE0 && bytes[i] <= 0xEF)
                {
                    if (i + 2 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF)
                        return false;
                    i += 3;
                }
                else if (bytes[i] >= 0xF0 && bytes[i] <= 0xF7)
                {
                    if (i + 3 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF || bytes[i + 3] < 0x80 || bytes[i + 3] > 0xBF)
                        return false;
                    i += 4;
                }
                else
                {
                    return false;
                }
            }
            return true;
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
