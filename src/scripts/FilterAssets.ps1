# 资产筛选工具脚本
# 该脚本用于在编译或发布阶段筛选项目资产文件，仅保留实际引用的资产

param(
    [string]$ProjectPath = $PSScriptRoot,
    [string]$OutputPath = Join-Path $PSScriptRoot "filtered_assets"
)

# 显示帮助信息
function Show-Help {
    Write-Host "Usage: .\FilterAssets.ps1 [-ProjectPath <项目路径>] [-OutputPath <输出路径>]"
    Write-Host "  -ProjectPath: 项目根目录路径，默认为当前脚本所在目录"
    Write-Host "  -OutputPath: 筛选后的资产输出目录，默认为当前目录下的filtered_assets文件夹"
    exit 0
}

# 检查是否需要显示帮助
if ($args -contains "-help" -or $args -contains "--help" -or $args -contains "-h") {
    Show-Help
}

# 检查项目路径是否存在
if (-not (Test-Path $ProjectPath -PathType Container)) {
    Write-Host "错误: 项目路径 '$ProjectPath' 不存在" -ForegroundColor Red
    exit 1
}

# 构建项目以确保AssetFilterHelper可用
Write-Host "正在构建项目..." -ForegroundColor Green

# 设置项目文件路径
$csprojPath = Join-Path $ProjectPath "src\ImageRecognitionApp\ImageRecognitionApp.csproj"

if (-not (Test-Path $csprojPath -PathType Leaf)) {
    Write-Host "错误: 未找到项目文件 '$csprojPath'" -ForegroundColor Red
    exit 1
}

# 构建项目
try {
    dotnet build "$csprojPath" -c Release -o "$($PSScriptRoot)\temp_build" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "项目构建失败"
    }
    Write-Host "项目构建成功" -ForegroundColor Green
} catch {
    Write-Host "错误: 项目构建失败: $_" -ForegroundColor Red
    exit 1
}

# 运行资产筛选工具
Write-Host "正在筛选资产文件..." -ForegroundColor Green
Write-Host "源项目路径: $ProjectPath"
Write-Host "输出目录: $OutputPath"

# 创建输出目录（如果不存在）
if (-not (Test-Path $OutputPath -PathType Container)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

# 使用dotnet命令运行筛选器
try {
    $filterExe = Join-Path $PSScriptRoot "temp_build\ImageRecognitionApp.exe"
    
    # 这里假设应用程序有一个命令行模式来运行资产筛选
    # 如果没有，则可以考虑使用反射直接调用AssetFilterHelper类
    $command = "& '$filterExe' filter-assets '$ProjectPath' '$OutputPath'"
    Invoke-Expression $command
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "资产筛选完成!" -ForegroundColor Green
    } else {
        throw "资产筛选过程返回非零退出码"
    }
} catch {
    Write-Host "错误: 资产筛选过程中发生错误: $_" -ForegroundColor Red
    exit 1
} finally {
    # 清理临时构建文件
    if (Test-Path "$($PSScriptRoot)\temp_build" -PathType Container) {
        Remove-Item -Path "$($PSScriptRoot)\temp_build" -Recurse -Force | Out-Null
    }
}

# 显示筛选结果统计信息
if (Test-Path $OutputPath -PathType Container) {
    $originalAssetCount = (Get-ChildItem -Path "$ProjectPath\Resources" -Recurse -File).Count
    $filteredAssetCount = (Get-ChildItem -Path $OutputPath -Recurse -File).Count
    
    Write-Host "\n筛选统计信息:" -ForegroundColor Cyan
    Write-Host "原始资产文件数量: $originalAssetCount"
    Write-Host "筛选后资产文件数量: $filteredAssetCount"
    Write-Host "减少的文件数量: $($originalAssetCount - $filteredAssetCount)"
    
    if ($originalAssetCount -gt 0) {
        $reductionPercentage = [math]::Round((($originalAssetCount - $filteredAssetCount) / $originalAssetCount) * 100, 2)
        Write-Host "文件减少比例: $reductionPercentage%"
    }
}

# 提示用户如何使用筛选后的资产
Write-Host "\n使用说明:" -ForegroundColor Yellow
Write-Host "1. 筛选后的资产已保存到: $OutputPath"
Write-Host "2. 在发布时，可以用这个目录中的资源替换项目中的Resources目录"
Write-Host "3. 这将有助于减小编译后文件的体积并缩短编译时间"

# 完成提示
Write-Host "\n操作已完成，请按任意键退出..." -ForegroundColor Green
Read-Host | Out-Null