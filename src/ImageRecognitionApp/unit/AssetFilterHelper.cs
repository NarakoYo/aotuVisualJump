using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageRecognitionApp.unit
{
    /// <summary>
    /// 资产文件筛选工具类
    /// 在项目编译或发布阶段，对资产文件进行筛选处理，仅保留项目实际引用的资产文件
    /// 以达到减小编译后文件体积和缩短编译时长的目的
    /// </summary>
    public class AssetFilterHelper
    {
        private const string AssetAllocationFileName = "AssetAllocation.json";
        private const string AssetConfigDirectory = "Resources\\Config";
        private const string ResourcesBaseDirectory = "Resources";
        
        /// <summary>
        /// 资产配置数据模型
        /// </summary>
        private class AssetConfig
        {
            public string Resources { get; set; }
            
            [JsonPropertyName("AssetList")]
            public List<AssetItem> AssetList { get; set; }
        }
        
        /// <summary>
        /// 资产项数据模型
        /// </summary>
        private class AssetItem
        {
            [JsonPropertyName("sign_id")]
            public int SignId { get; set; }
            
            public string Asset { get; set; }
        }
        
        /// <summary>
        /// 筛选资产文件
        /// 分析项目代码中实际引用的sign_id，并生成只包含这些资产的配置文件
        /// </summary>
        /// <param name="projectDirectory">项目根目录路径</param>
        /// <param name="outputDirectory">筛选后的资产输出目录</param>
        public static void FilterAssets(string projectDirectory, string outputDirectory)
        {
            try
            {
                // 1. 解析原始的AssetAllocation.json文件
                string configFilePath = Path.Combine(projectDirectory, AssetConfigDirectory, AssetAllocationFileName);
                var assetConfig = ParseAssetConfig(configFilePath);
                
                if (assetConfig == null || assetConfig.AssetList == null || !assetConfig.AssetList.Any())
                {
                    LogManager.Instance.WriteLog(LogManager.LogLevel.Error, "未找到有效的资产配置文件");
                    return;
                }
                
                // 2. 扫描项目代码，找出所有实际使用的sign_id
                var usedSignIds = ScanProjectForUsedSignIds(projectDirectory);
                
                if (!usedSignIds.Any())
                {
                    LogManager.Instance.WriteLog(LogManager.LogLevel.Warning, "未找到任何被引用的sign_id，将保留所有资产");
                    usedSignIds = assetConfig.AssetList.Select(item => item.SignId).ToList();
                }
                
                // 3. 筛选出被引用的资产
                var filteredAssets = assetConfig.AssetList.Where(item => usedSignIds.Contains(item.SignId)).ToList();
                
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, 
                    $"资产筛选完成: 原始资产数 {assetConfig.AssetList.Count}, 筛选后资产数 {filteredAssets.Count}");
                
                // 4. 创建输出目录结构
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                
                // 5. 复制筛选后的资产文件到输出目录
                CopyFilteredAssets(projectDirectory, outputDirectory, filteredAssets);
                
                // 6. 生成新的AssetAllocation.json文件
                GenerateFilteredAssetConfig(outputDirectory, assetConfig, filteredAssets);
                
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"资产筛选过程中发生错误: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 解析资产配置文件
        /// </summary>
        /// <param name="configFilePath">配置文件路径</param>
        /// <returns>资产配置对象</returns>
        private static AssetConfig ParseAssetConfig(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"资产配置文件不存在: {configFilePath}");
                return null;
            }
            
            try
            {
                string jsonContent = File.ReadAllText(configFilePath);
                return JsonSerializer.Deserialize<AssetConfig>(jsonContent);
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"解析资产配置文件失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 扫描项目代码，找出所有实际使用的sign_id
        /// </summary>
        /// <param name="projectDirectory">项目目录</param>
        /// <returns>被使用的sign_id列表</returns>
        private static List<int> ScanProjectForUsedSignIds(string projectDirectory)
        {
            var usedSignIds = new List<int>();
            
            try
            {
                // 扫描所有C#代码文件
                var csFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
                
                // 匹配AssetHelper.Instance.GetXXX方法调用中的整数sign_id
                string signIdPattern = @"AssetHelper\.Instance\.Get[A-Za-z]+\(\s*(\d+)\s*\)";
                
                foreach (string file in csFiles)
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        MatchCollection matches = Regex.Matches(content, signIdPattern);
                        
                        foreach (Match match in matches)
                        {
                            if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out int signId))
                            {
                                if (!usedSignIds.Contains(signId))
                                {
                                    usedSignIds.Add(signId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.WriteLog(LogManager.LogLevel.Warning, $"扫描文件 {file} 时出错: {ex.Message}");
                    }
                }
                
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"找到 {usedSignIds.Count} 个被引用的sign_id");
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"扫描项目代码时出错: {ex.Message}");
            }
            
            return usedSignIds;
        }
        
        /// <summary>
        /// 复制筛选后的资产文件到输出目录
        /// </summary>
        /// <param name="projectDirectory">项目目录</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="filteredAssets">筛选后的资产列表</param>
        private static void CopyFilteredAssets(string projectDirectory, string outputDirectory, List<AssetItem> filteredAssets)
        {
            try
            {
                int copiedCount = 0;
                int skippedCount = 0;
                
                foreach (var asset in filteredAssets)
                {
                    string sourcePath = Path.Combine(projectDirectory, ResourcesBaseDirectory, asset.Asset.Replace('/', '\\'));
                    string destPath = Path.Combine(outputDirectory, ResourcesBaseDirectory, asset.Asset.Replace('/', '\\'));
                    
                    if (File.Exists(sourcePath))
                    {
                        // 确保目标目录存在
                        string destDir = Path.GetDirectoryName(destPath);
                        if (destDir != null && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        
                        // 复制文件
                        File.Copy(sourcePath, destPath, true);
                        copiedCount++;
                    }
                    else
                    {
                        LogManager.Instance.WriteLog(LogManager.LogLevel.Warning, $"资产文件不存在: {sourcePath}");
                        skippedCount++;
                    }
                }
                
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, 
                    $"资产文件复制完成: 成功复制 {copiedCount} 个文件, 跳过 {skippedCount} 个不存在的文件");
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"复制资产文件时出错: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 生成筛选后的资产配置文件
        /// </summary>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="originalConfig">原始配置</param>
        /// <param name="filteredAssets">筛选后的资产列表</param>
        private static void GenerateFilteredAssetConfig(string outputDirectory, AssetConfig originalConfig, List<AssetItem> filteredAssets)
        {
            try
            {
                // 创建新的配置对象
                var filteredConfig = new AssetConfig
                {
                    Resources = originalConfig.Resources,
                    AssetList = filteredAssets
                };
                
                // 序列化配置对象
                string jsonContent = JsonSerializer.Serialize(filteredConfig, new JsonSerializerOptions { WriteIndented = true });
                
                // 确保配置目录存在
                string configOutputDir = Path.Combine(outputDirectory, AssetConfigDirectory);
                if (!Directory.Exists(configOutputDir))
                {
                    Directory.CreateDirectory(configOutputDir);
                }
                
                // 写入配置文件
                string outputFilePath = Path.Combine(configOutputDir, AssetAllocationFileName);
                File.WriteAllText(outputFilePath, jsonContent);
                
                LogManager.Instance.WriteLog(LogManager.LogLevel.Info, $"生成筛选后的资产配置文件: {outputFilePath}");
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteLog(LogManager.LogLevel.Error, $"生成资产配置文件时出错: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 获取项目中所有可用的资产类型
        /// </summary>
        /// <returns>资产类型列表</returns>
        public static List<string> GetAvailableAssetTypes()
        {
            return new List<string> { "图片", "音频", "视频", "SVG", "图标", "网页内容" };
        }
    }
}