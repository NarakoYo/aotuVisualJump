using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Reflection;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ImageRecognitionApp.UnitTools
{
    /// <summary>
    /// 资产处理工具类，用于获取和解析各种类型的资源文件
    /// 支持图片、音频、视频、SVG、图标和网页链接等多种资源格式
    /// </summary>
    public class AssetHelper
    {
        // 单例模式实现
        private static readonly Lazy<AssetHelper> _instance = new Lazy<AssetHelper>(() => new AssetHelper());
        private readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Config", "AssetAllocation.json");
        private string _resourcesBasePath = string.Empty;
        private readonly ConcurrentDictionary<string, string> _assetList = new ConcurrentDictionary<string, string>();
        private bool _isInitialized = false;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly LogManager _logManager = LogManager.Instance;
        private static readonly ConcurrentDictionary<string, CacheItem> _assetCache = new ConcurrentDictionary<string, CacheItem>();
        private static int MAX_CACHE_SIZE = 30; // 初始缓存大小设为较小值，后续可动态调整
        private static int LOW_MEMORY_CACHE_SIZE = 10; // 低内存模式下的缓存大小，后续可动态调整

        /// <summary>
        /// 缓存项，包含资产和最后访问时间
        /// </summary>
        private class CacheItem
        {
            public object Asset { get; set; }
            public DateTime LastAccessTime { get; set; }

            public CacheItem(object asset)
            {
                Asset = asset;
                LastAccessTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 获取AssetHelper的单例实例
        /// </summary>
        public static AssetHelper Instance => _instance.Value;

        /// <summary>
        /// 获取资产处理工具是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private AssetHelper()
        {
            // 初始化时加载配置
            Initialize();
        }

        /// <summary>
        /// 初始化资产配置
        /// </summary>
        public void Initialize()
        {
            try
            {
                if (!_isInitialized)
                {
                    _assetList.Clear();
                    _assetCache.Clear();
                    LoadAssetConfiguration();
                    _isInitialized = true;
                    _logManager.WriteLog(LogManager.LogLevel.Info, "资产配置初始化成功");
                    // _logManager.WriteLog(LogManager.LogLevel.Info, $"资产配置文件路径: {_configFilePath}");
                    // _logManager.WriteLog(LogManager.LogLevel.Info, $"资源基础路径: {_resourcesBasePath}");
                }
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"初始化资产配置时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载资产配置数据
        /// </summary>
        private void LoadAssetConfiguration()
        {
            if (!File.Exists(_configFilePath))
            {
                _logManager.WriteLog(LogManager.LogLevel.Warning, $"资产配置文件不存在: {_configFilePath}");
                throw new FileNotFoundException("资产配置文件不存在", _configFilePath);
            }

            try
            {
                // 读取JSON文件内容
                string jsonContent = File.ReadAllText(_configFilePath, System.Text.Encoding.UTF8);

                // 解析JSON
                using (var doc = JsonDocument.Parse(jsonContent))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("AssetAllocation", out var assetAllocationElement))
                    {
                        // 获取资源基础路径
                        if (assetAllocationElement.TryGetProperty("Resources", out var resourcesElement))
                        {
                            string resourcesPath = resourcesElement.GetString() ?? string.Empty;
                            if (string.IsNullOrEmpty(resourcesPath))
                            {
                                _logManager.WriteLog(LogManager.LogLevel.Warning, "配置文件中的资源路径为空，使用默认路径");
                            }

                            // 构建完整的资源基础路径
                            _resourcesBasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourcesPath));

                            if (!Directory.Exists(_resourcesBasePath))
                            {
                                _logManager.WriteLog(LogManager.LogLevel.Warning, $"资源基础路径不存在: {_resourcesBasePath}");
                                Directory.CreateDirectory(_resourcesBasePath);
                                _logManager.WriteLog(LogManager.LogLevel.Info, $"已创建资源基础路径: {_resourcesBasePath}");
                            }
                        }

                        // 获取资产列表
                        if (assetAllocationElement.TryGetProperty("AssetList", out var assetListElement))
                        {
                            Dictionary<string, int> assetPathCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                            foreach (var item in assetListElement.EnumerateArray())
                            {
                                if (item.TryGetProperty("sign_id", out var signIdElement) &&
                                    item.TryGetProperty("Asset", out var assetElement))
                                {
                                    // 检查sign_id类型是否为数字
                                    string signId = string.Empty;
                                    if (signIdElement.ValueKind == JsonValueKind.Number)
                                    {
                                        signId = signIdElement.GetInt32().ToString();
                                    }
                                    else if (signIdElement.ValueKind == JsonValueKind.String)
                                    {
                                        signId = signIdElement.GetString() ?? string.Empty;
                                        // 尝试将字符串转换为整数，确保sign_id是有效的数字
                                        if (!int.TryParse(signId, out _))
                                        {
                                            _logManager.WriteLog(LogManager.LogLevel.Error, $"发现无效的sign_id类型: {signId}，应为数字类型，已忽略该配置项");
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        _logManager.WriteLog(LogManager.LogLevel.Error, "发现无效的sign_id类型，应为数字或数字字符串，已忽略该配置项");
                                        continue;
                                    }

                                    string assetPath = assetElement.GetString() ?? string.Empty;

                                    if (string.IsNullOrEmpty(signId))
                                    {
                                        _logManager.WriteLog(LogManager.LogLevel.Warning, $"发现资产配置项缺少sign_id，资产路径: {assetPath}");
                                        continue;
                                    }

                                    if (string.IsNullOrEmpty(assetPath))
                                    {
                                        _logManager.WriteLog(LogManager.LogLevel.Warning, $"资产 {signId} 的路径为空");
                                        continue;
                                    }

                                    // 检查sign_id重复
                                    if (_assetList.ContainsKey(signId))
                                    {
                                        _logManager.WriteLog(LogManager.LogLevel.Warning, $"发现重复的sign_id: {signId}，将覆盖原有配置");
                                    }

                                    // 检查资产路径重复
                                    if (assetPathCount.ContainsKey(assetPath))
                                    {
                                        assetPathCount[assetPath]++;
                                        _logManager.WriteLog(LogManager.LogLevel.Warning, $"发现重复的资产路径: {assetPath}，已出现 {assetPathCount[assetPath]} 次");
                                    }
                                    else
                                    {
                                        assetPathCount[assetPath] = 1;
                                    }

                                    _assetList[signId] = assetPath;
                                }
                                else
                                {
                                    _logManager.WriteLog(LogManager.LogLevel.Warning, "发现不完整的资产配置项，缺少必要的字段");
                                }
                            }
                        }
                    }
                    else
                    {
                        _logManager.WriteLog(LogManager.LogLevel.Warning, "配置文件中未找到AssetAllocation节点");
                    }
                }

                _logManager.WriteLog(LogManager.LogLevel.Info, $"已加载资产配置，共 {_assetList.Count} 项资源");
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"加载资产配置时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取资产文件的完整路径
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>资产文件的完整路径</returns>
        public string GetAssetPath(string signId)
        {
            if (string.IsNullOrEmpty(signId))
            {
                _logManager.WriteLog(LogManager.LogLevel.Warning, "尝试获取资产路径时提供了空的sign_id");
                throw new ArgumentNullException(nameof(signId), "资产标识ID不能为空");
            }

            if (!_isInitialized)
            {
                Initialize();
            }

            if (_assetList.TryGetValue(signId, out string assetPath))
            {
                // 构建完整的资产路径
                string fullPath = Path.Combine(_resourcesBasePath, assetPath);

                // 检查文件是否存在
                if (!File.Exists(fullPath))
                {
                    _logManager.WriteLog(LogManager.LogLevel.Warning, $"资产文件不存在: {fullPath}，sign_id: {signId}");
                }

                return fullPath;
            }

            _logManager.WriteLog(LogManager.LogLevel.Error, $"找不到标识为 {signId} 的资产");
            throw new KeyNotFoundException($"找不到标识为 {signId} 的资产");
        }

        /// <summary>
        /// 获取资产文件的完整路径（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>资产文件的完整路径</returns>
        public string GetAssetPath(int signId)
        {
            return GetAssetPath(signId.ToString());
        }

        /// <summary>
        /// 获取图片资源
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>BitmapImage对象</returns>
        public BitmapImage GetImageAsset(string signId)
        {
            string cacheKey = $"image_{signId}";
            if (_assetCache.TryGetValue(cacheKey, out CacheItem? cachedAsset) && cachedAsset.Asset is BitmapImage cachedImage)
            {
                cachedAsset.LastAccessTime = DateTime.Now;
                return cachedImage;
            }

            try
            {
                string filePath = GetAssetPath(signId);
                string fileExtension = Path.GetExtension(filePath).ToLower();

                if (!IsSupportedImageFormat(fileExtension))
                {
                    _logManager.WriteLog(LogManager.LogLevel.Warning, $"文件 {filePath} 不是支持的图片格式，sign_id: {signId}");
                    throw new NotSupportedException($"文件 {filePath} 不是支持的图片格式");
                }

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结对象以提高性能
                AddToCache(cacheKey, bitmap);
                return bitmap;
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"加载图片资源 {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取图片资源（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>BitmapImage对象</returns>
        public BitmapImage GetImageAsset(int signId)
        {
            return GetImageAsset(signId.ToString());
        }

        /// <summary>
        /// 获取图标资源
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>Icon对象</returns>
        public System.Drawing.Icon GetIconAsset(string signId)
        {
            string cacheKey = $"icon_{signId}";
            if (_assetCache.TryGetValue(cacheKey, out CacheItem? cachedAsset) && cachedAsset.Asset is System.Drawing.Icon cachedIcon)
            {
                cachedAsset.LastAccessTime = DateTime.Now;
                return cachedIcon;
            }

            try
            {
                string filePath = GetAssetPath(signId);
                string fileExtension = Path.GetExtension(filePath).ToLower();

                if (fileExtension != ".ico")
                {
                    _logManager.WriteLog(LogManager.LogLevel.Warning, $"文件 {filePath} 不是图标格式(.ico)，sign_id: {signId}");
                    throw new NotSupportedException($"文件 {filePath} 不是图标格式(.ico)");
                }

                System.Drawing.Icon icon = new System.Drawing.Icon(filePath);
                AddToCache(cacheKey, icon);
                return icon;
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"加载图标资源 {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取图标资源（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>Icon对象</returns>
        public System.Drawing.Icon GetIconAsset(int signId)
        {
            return GetIconAsset(signId.ToString());
        }

        /// <summary>
        /// 获取SVG资源内容
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>SVG文件的内容字符串</returns>
        public string GetSvgAssetContent(string signId)
        {
            string cacheKey = $"svg_{signId}";
            if (_assetCache.TryGetValue(cacheKey, out CacheItem? cachedAsset) && cachedAsset.Asset is string cachedSvg)
            {
                cachedAsset.LastAccessTime = DateTime.Now;
                return cachedSvg;
            }

            try
            {
                string filePath = GetAssetPath(signId);
                string fileExtension = Path.GetExtension(filePath).ToLower();

                if (fileExtension != ".svg")
                {
                    _logManager.WriteLog(LogManager.LogLevel.Warning, $"文件 {filePath} 不是SVG格式(.svg)，sign_id: {signId}");
                    throw new NotSupportedException($"文件 {filePath} 不是SVG格式(.svg)");
                }

                string svgContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                AddToCache(cacheKey, svgContent);
                return svgContent;
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"加载SVG资源 {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取SVG资源内容（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>SVG文件的内容字符串</returns>
        public string GetSvgAssetContent(int signId)
        {
            return GetSvgAssetContent(signId.ToString());
        }

        /// <summary>
        /// 获取音频资源
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>MediaPlayer对象</returns>
        public MediaPlayer GetAudioAsset(string signId)
        {
            string cacheKey = $"audio_{signId}";
            if (_assetCache.TryGetValue(cacheKey, out CacheItem? cachedAsset) && cachedAsset.Asset is MediaPlayer cachedPlayer)
            {
                cachedAsset.LastAccessTime = DateTime.Now;
                return cachedPlayer;
            }

            try
            {
                string filePath = GetAssetPath(signId);
                string fileExtension = Path.GetExtension(filePath).ToLower();

                if (!IsSupportedAudioFormat(fileExtension))
                {
                    _logManager.WriteLog(LogManager.LogLevel.Warning, $"文件 {filePath} 不是支持的音频格式，sign_id: {signId}");
                    throw new NotSupportedException($"文件 {filePath} 不是支持的音频格式");
                }

                MediaPlayer mediaPlayer = new MediaPlayer();
                mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
                AddToCache(cacheKey, mediaPlayer);
                return mediaPlayer;
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"加载音频资源 {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取音频资源（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>MediaPlayer对象</returns>
        public MediaPlayer GetAudioAsset(int signId)
        {
            return GetAudioAsset(signId.ToString());
        }

        /// <summary>
        /// 检查文件是否为支持的图片格式
        /// </summary>
        /// <param name="fileExtension">文件扩展名</param>
        /// <returns>是否为支持的图片格式</returns>
        private bool IsSupportedImageFormat(string fileExtension)
        {
            // 支持的图片格式列表
            var supportedFormats = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".svg", ".ico", ".webp", ".jfif", ".heic", ".heif", ".dng", ".cr2", ".nef", ".arw", ".rw2" };
            return supportedFormats.Contains(fileExtension);
        }

        /// <summary>
        /// 检查文件是否为支持的音频格式
        /// </summary>
        /// <param name="fileExtension">文件扩展名</param>
        /// <returns>是否为支持的音频格式</returns>
        private bool IsSupportedAudioFormat(string fileExtension)
        {
            // 支持的音频格式列表
            var supportedFormats = new[] { ".mp3", ".wav", ".wma", ".aac", ".ogg", ".flac", ".m4a", ".alac", ".aiff", ".opus", ".amr" };
            return supportedFormats.Contains(fileExtension);
        }

        /// <summary>
        /// 检查文件是否为支持的视频格式
        /// </summary>
        /// <param name="fileExtension">文件扩展名</param>
        /// <returns>是否为支持的视频格式</returns>
        private bool IsSupportedVideoFormat(string fileExtension)
        {
            // 支持的视频格式列表
            var supportedFormats = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".mpeg", ".mpg", ".3gp", ".m4v", ".vob", ".rmvb", ".ts", ".mts", ".m2ts" };
            return supportedFormats.Contains(fileExtension);
        }

        /// <summary>
        /// 检查字符串是否为有效的URL
        /// </summary>
        /// <param name="urlString">URL字符串</param>
        /// <returns>是否为有效的URL</returns>
        private bool IsValidUrl(string urlString)
        {
            return Uri.TryCreate(urlString, UriKind.Absolute, out Uri? uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// 添加资源到缓存
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="asset">要缓存的资源</param>
        private void AddToCache(string cacheKey, object asset)
        {
            // 检查当前系统内存状态，动态调整缓存大小
            int currentCacheSizeLimit = IsLowMemory() ? LOW_MEMORY_CACHE_SIZE : MAX_CACHE_SIZE;
            
            // 如果缓存已满，使用LRU策略移除最久未使用的项
            if (_assetCache.Count >= currentCacheSizeLimit)
            {
                // 查找最久未使用的缓存项
                var oldestItem = _assetCache.OrderBy(kvp => kvp.Value.LastAccessTime).FirstOrDefault();
                if (oldestItem.Key != null)
                {
                    _assetCache.TryRemove(oldestItem.Key, out _);
                }
            }
            
            _assetCache[cacheKey] = new CacheItem(asset);
        }
        
        /// <summary>
        /// 更新缓存大小限制
        /// </summary>
        /// <param name="newLimit">新的缓存大小限制</param>
        public void UpdateCacheSizeLimit(int newLimit)
        {
            // 确保新的限制值合理
            if (newLimit < 5)
            {
                newLimit = 5; // 设置最小缓存大小为5
                _logManager.WriteLog(LogManager.LogLevel.Warning, "缓存大小限制过小，已调整为最小5项");
            }
            
            if (newLimit > 200)
            {
                newLimit = 200; // 设置最大缓存大小上限为200
                _logManager.WriteLog(LogManager.LogLevel.Warning, "缓存大小限制过大，已调整为最大200项");
            }
            
            // 更新缓存大小限制
            MAX_CACHE_SIZE = newLimit;
            
            // 如果当前缓存大小超过新的限制，立即清理多余的项
            if (_assetCache.Count > MAX_CACHE_SIZE && !IsLowMemory())
            {
                // 需要移除的项数量
                int itemsToRemove = _assetCache.Count - MAX_CACHE_SIZE;
                
                // 查找最久未使用的项并移除
                var itemsToRemoveList = _assetCache.OrderBy(kvp => kvp.Value.LastAccessTime)
                                                 .Take(itemsToRemove)
                                                 .ToList();
                
                foreach (var item in itemsToRemoveList)
                {
                    _assetCache.TryRemove(item.Key, out _);
                }
                
                _logManager.WriteLog(LogManager.LogLevel.Info, 
                    $"已更新缓存大小限制为{newLimit}项，并清理了{itemsToRemove}项缓存");
            }
            else
            {
                _logManager.WriteLog(LogManager.LogLevel.Info, 
                    $"已更新缓存大小限制为{newLimit}项");
            }
        }
        
        /// <summary>
        /// 检查系统是否处于低内存状态
        /// </summary>
        /// <returns>如果系统内存不足则返回true</returns>
        private bool IsLowMemory()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (var queryObj in searcher.Get())
                    {
                        ulong totalMemory = Convert.ToUInt64(queryObj["TotalVisibleMemorySize"]) / 1024; // MB
                        ulong freeMemory = Convert.ToUInt64(queryObj["FreePhysicalMemory"]) / 1024; // MB
                         
                        // 如果可用内存低于总内存的10%或低于512MB，认为是低内存状态
                        bool isLowMemory = freeMemory < totalMemory / 10 || freeMemory < 512;
                         
                        // 在低内存状态下，释放本地化资源
                        if (isLowMemory)
                        {
                            try
                            {
                                // 尝试调用JsonLocalizationHelper释放资源
                                JsonLocalizationHelper.Instance.ReleaseResourcesOnLowMemory();
                                _logManager.WriteLog(LogManager.LogLevel.Info, "已通知本地化助手释放资源");
                            }
                            catch (Exception ex)
                            {
                                _logManager.WriteLog(LogManager.LogLevel.Error, $"调用本地化助手释放资源失败: {ex.Message}");
                            }
                        }
                         
                        return isLowMemory;
                    }
                }
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"获取系统内存信息失败: {ex.Message}");
            }
             
            // 出错时默认返回false
            return false;
        }

        /// <summary>
        /// 获取视频资源
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>MediaPlayer对象</returns>
        public MediaPlayer GetVideoAsset(string signId)
        {
            string cacheKey = $"video_{signId}";
            if (_assetCache.TryGetValue(cacheKey, out CacheItem? cachedAsset) && cachedAsset.Asset is MediaPlayer cachedPlayer)
            {
                // 更新最后访问时间以实现LRU策略
                cachedAsset.LastAccessTime = DateTime.Now;
                return cachedPlayer;
            }

            string filePath = GetAssetPath(signId);
            string fileExtension = Path.GetExtension(filePath).ToLower();

            if (!IsSupportedVideoFormat(fileExtension))
            {
                _logManager.WriteLog(LogManager.LogLevel.Warning, $"文件 {filePath} 不是支持的视频格式，sign_id: {signId}");
                throw new NotSupportedException($"文件 {filePath} 不是支持的视频格式");
            }

            try
            {
                MediaPlayer mediaPlayer = new MediaPlayer();
                mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
                AddToCache(cacheKey, mediaPlayer);
                return mediaPlayer;
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"加载视频资源 {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取视频资源（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>MediaPlayer对象</returns>
        public MediaPlayer GetVideoAsset(int signId)
        {
            return GetVideoAsset(signId.ToString());
        }

        /// <summary>
        /// 获取网页链接内容
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>网页内容字符串</returns>
        public async Task<string> GetWebContentAsync(string signId)
        {
            string cacheKey = $"web_{signId}";
            if (_assetCache.TryGetValue(cacheKey, out CacheItem? cachedAsset) && cachedAsset.Asset is string cachedContent)
            {
                // 更新最后访问时间以实现LRU策略
                cachedAsset.LastAccessTime = DateTime.Now;
                return cachedContent;
            }

            string assetPath = GetAssetPath(signId);

            // 检查资产路径是否已经是URL
            if (!IsValidUrl(assetPath))
            {
                _logManager.WriteLog(LogManager.LogLevel.Warning, $"资产路径 {assetPath} 不是有效的URL，sign_id: {signId}");
                throw new ArgumentException($"资产路径 {assetPath} 不是有效的URL");
            }

            try
            {
                string content = await _httpClient.GetStringAsync(assetPath);
                AddToCache(cacheKey, content);
                return content;
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"加载网页内容 {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取网页链接内容（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>网页内容字符串</returns>
        public async Task<string> GetWebContentAsync(int signId)
        {
            return await GetWebContentAsync(signId.ToString());
        }

        /// <summary>
        /// 获取资源文件流
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>文件流对象</returns>
        public FileStream GetAssetStream(string signId)
        {
            try
            {
                string filePath = GetAssetPath(signId);
                return File.OpenRead(filePath);
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"打开资源文件流 {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取资源文件流（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>文件流对象</returns>
        public FileStream GetAssetStream(int signId)
        {
            return GetAssetStream(signId.ToString());
        }

        /// <summary>
        /// 获取资源的URI
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>资源的URI对象</returns>
        public Uri GetAssetUri(string signId)
        {
            try
            {
                string filePath = GetAssetPath(signId);
                return new Uri(filePath, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"获取资源URI {signId} 时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取资源的URI（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>资源的URI对象</returns>
        public Uri GetAssetUri(int signId)
        {
            return GetAssetUri(signId.ToString());
        }

        /// <summary>
        /// 重新加载资产配置
        /// </summary>
        public void ReloadConfiguration()
        {
            try
            {
                _assetList.Clear();
                _assetCache.Clear();
                _isInitialized = false;
                Initialize();
                _logManager.WriteLog(LogManager.LogLevel.Info, "资产配置已重新加载");
            }
            catch (Exception ex)
            {
                _logManager.WriteLog(LogManager.LogLevel.Error, $"重新加载资产配置时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 检查sign_id是否存在
        /// </summary>
        /// <param name="signId">资产标识ID</param>
        /// <returns>是否存在</returns>
        public bool IsSignIdExists(string signId)
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            return _assetList.ContainsKey(signId);
        }

        /// <summary>
        /// 检查sign_id是否存在（整数版本）
        /// </summary>
        /// <param name="signId">资产标识ID（整数）</param>
        /// <returns>是否存在</returns>
        public bool IsSignIdExists(int signId)
        {
            return IsSignIdExists(signId.ToString());
        }

        /// <summary>
        /// 获取所有资产的sign_id列表
        /// </summary>
        /// <returns>sign_id列表</returns>
        public IEnumerable<string> GetAllSignIds()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            return _assetList.Keys;
        }

        /// <summary>
        /// 清除资源缓存
        /// </summary>
        public void ClearCache()
        {
            _assetCache.Clear();
            _logManager.WriteLog(LogManager.LogLevel.Info, "资产缓存已清除");
        }

        /// <summary>
        /// 获取缓存大小
        /// </summary>
        /// <returns>缓存中的资源数量</returns>
        public int GetCacheSize()
        {
            return _assetCache.Count;
        }
    }
}