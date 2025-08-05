# 编译打包发布流程指南

## 1. 环境准备

### 1.1 安装必要工具
- .NET SDK 9.0 或更高版本 (项目基于.NET 9.0开发)
- Python 3.8 或更高版本
- PowerShell 5.1 或更高版本

### 1.2 验证环境
打开PowerShell终端，执行以下命令验证安装：
```powershell
# 验证.NET SDK
dotnet --version

# 验证Python
python --version
```

## 2. 依赖安装

### 2.1 C#项目依赖
在项目根目录执行以下命令安装C#依赖：
```powershell
dotnet restore src/ImageRecognitionApp/ImageRecognitionApp.csproj
```

### 2.2 Python脚本依赖
进入PythonScripts目录安装依赖：
```powershell
cd src/PythonScripts
python -m pip install -r requirements.txt
```

## 3. 编译项目

### 3.1 编译C#应用
```powershell
dotnet build src/ImageRecognitionApp/ImageRecognitionApp.csproj -c Release
```
> 注意：编译前建议清除缓存目录，可执行 `dotnet clean` 命令

### 3.2 验证编译结果
编译成功后，检查输出目录：
```powershell
dir src/ImageRecognitionApp/bin/Release/net9.0/
```
应能看到生成的ImageRecognitionApp.dll文件

## 4. 打包可执行文件

### 4.1 发布C#应用
```powershell
dotnet publish src/ImageRecognitionApp/ImageRecognitionApp.csproj -c Release -r win-x64 --self-contained true -o publish/ImageRecognitionApp
```
> 此命令会将应用发布为独立可执行文件，包含所有必要的依赖项

### 4.2 复制Python脚本
```powershell
Copy-Item -Path src/PythonScripts/* -Destination publish/ImageRecognitionApp/PythonScripts -Recurse
```

## 5. 生成最终发布包

### 5.1 创建压缩包
```powershell
Compress-Archive -Path publish/ImageRecognitionApp/* -DestinationPath publish/ImageRecognitionApp_v1.0.zip
```

## 6. 发布说明

### 6.1 发布包内容
生成的ZIP文件包含以下内容：
- 可执行文件ImageRecognitionApp.exe
- 所有依赖的DLL文件
- PythonScripts文件夹及所需脚本
- 配置文件

### 6.2 分发说明
将ZIP文件分发给用户，用户只需解压后运行ImageRecognitionApp.exe即可，无需额外安装依赖。

## 7. 常见问题解决

### 7.1 编译错误
- 确保.NET SDK版本正确
- 执行`dotnet clean`清理之前的构建文件后重试

### 7.2 运行时缺少Python依赖
- 确保已执行步骤2.2安装Python依赖
- 检查publish/ImageRecognitionApp/PythonScripts目录是否包含所有必要文件