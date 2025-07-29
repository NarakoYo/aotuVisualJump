# 图像识别脚本工具

![图像识别脚本工具](https://via.placeholder.com/800x450?text=图像识别脚本工具)

## 项目简介（全部由AI生成-无任何协议！！）
这是一个企业级图像识别脚本工具，采用C#和Python混合编程模式，结合OpenCV和PyAutoGUI实现图像识别与自动化操作。工具提供现代化UI界面，支持脚本录制、执行、保存和加载功能，可通过自定义快捷键提高操作效率。

## 功能特点
- 直观的现代化用户界面，支持深色主题
- 脚本录制与回放功能，记录鼠标操作序列
- 强大的图像识别能力，基于OpenCV实现
- 自定义快捷键支持，提升操作效率
- 脚本以JSON格式保存，便于编辑和管理
- 实时执行日志，清晰展示操作过程
- 自适应不同分辨率显示

## 技术架构
- **前端界面**：C# WPF
- **后端逻辑**：Python
- **图像识别**：OpenCV
- **自动化操作**：PyAutoGUI
- **数据格式**：JSON

## 快速开始

### 系统要求
- Windows 10 或更高版本
- .NET 6.0 运行时
- Python 3.8 或更高版本
- 支持1920×1080或更高分辨率的显示器

### 安装步骤
1. 克隆或下载项目代码到本地
2. 进入项目目录
3. 运行以下命令初始化Python环境和依赖：
   ```powershell
   cd src\PythonScripts
   .\venv\Scripts\Activate.ps1
   pip install -r requirements.txt
   ```
4. 打开`src\ImageRecognitionApp\ImageRecognitionApp.sln`解决方案
5. 编译并运行项目

## 使用指南
1. **录制脚本**：点击工具栏"录制脚本"按钮或按**F9**键开始录制，完成后再次按**F9**停止
2. **执行脚本**：在脚本库中选择脚本，点击"执行脚本"按钮或按**F5**键
3. **暂停录制**：录制过程中按**F10**键暂停
4. **保存脚本**：在脚本编辑器中按**Ctrl+S**保存当前脚本

详细使用说明请参见[用户手册](docs/user_manual.md)

## 项目结构
```
demo/
├── src/
│   ├── ImageRecognitionApp/      # C# WPF应用程序
│   └── PythonScripts/            # Python脚本和虚拟环境
├── docs/                         # 文档目录
│   ├── requirements.md           # 需求文档
│   └── user_manual.md            # 用户手册
├── scripts/                      # 脚本保存目录
└── tests/                        # 测试文件
```

## 快捷键
- **F9**：开始/停止录制
- **F5**：执行脚本
- **F10**：暂停录制
- **Ctrl+N**：新建脚本
- **Ctrl+O**：打开脚本
- **Ctrl+S**：保存脚本

## 许可证
[MIT](LICENSE)

## 联系方式
- 项目维护者：[Your Name]
- 邮箱：[your.email@example.com]
- 项目地址：[https://github.com/yourusername/image-recognition-script-tool](https://github.com/yourusername/image-recognition-script-tool)