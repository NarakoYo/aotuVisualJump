# 图像识别脚本工具 - 需求文档

## 1. 项目概述
本项目是一个企业级图像识别脚本工具，采用C#和Python混合编程模式，结合OpenCV和PyAutoGUI实现图像识别与自动化操作。工具提供现代化UI界面，支持脚本录制、执行、保存和加载功能，可通过自定义快捷键提高操作效率。该工具已实现多语言支持并修复了中文显示问题。

> 最新更新：增加了日志记录规范化文档，详细说明了日志严重程度级别标记的使用规范；修复了内容框边框显示问题（通过设置Panel.ZIndex和调整布局），解决了AssemblyInfo.cs文件导致的编译错误，优化了缓存清理流程。

## 2. 功能需求

### 2.1 用户界面
- 固定窗口大小为1207×763像素，禁用窗口调整功能
- 支持根据系统分辨率自动缩放界面元素（默认分辨率1920×1080）
- 提供多级UI结构，包括菜单栏、工具栏、标签页和状态栏
- 实现三个主要功能标签页：脚本编辑器、执行日志和脚本库
- 界面采用深色主题设计，确保长时间使用时的视觉舒适度
- 支持多语言界面，已修复中文显示问题

### 2.2 脚本录制与执行
- 支持通过鼠标操作录制用户交互过程
- 提供脚本执行功能，可回放录制的操作序列
- 实现录制暂停/继续功能
- 支持脚本保存为JSON格式，包含操作类型、坐标位置、延迟时间等信息
- 可加载已保存的脚本并执行

### 2.3 图像识别
- 集成OpenCV库实现屏幕图像捕获功能
- 支持模板匹配算法，可识别屏幕上的特定图像元素
- 提供基于图像识别结果执行鼠标点击操作的功能
- 支持Bitblt方法进行高效屏幕捕获

### 2.4 快捷键系统
- F9：开始/停止脚本录制
- F5：执行当前选中的脚本
- F10：暂停/继续录制
- Ctrl+N：新建脚本
- Ctrl+O：打开脚本
- Ctrl+S：保存脚本

### 2.5 日志系统
- 实时记录脚本执行过程和结果
- 显示操作时间戳和状态信息
- 支持错误信息捕获和显示
- 日志内容自动滚动到底部

## 3. 技术规格

### 3.1 开发语言与框架
- C#：使用WPF框架构建用户界面
- Python：实现图像识别和自动化操作逻辑
- .NET 9.0（项目主要开发版本）
- Python 3.8或更高版本
- Lua：用于本地化支持

> 注意：项目使用WPF的SubtractConverter实现动态尺寸计算，确保UI元素适应不同窗口大小。

### 3.2 第三方库与工具
- OpenCV：用于图像识别和处理
- PyAutoGUI：用于鼠标操作录制和回放
- Newtonsoft.Json：用于JSON格式的脚本序列化/反序列化
- Python.NET：实现C#与Python的交互
- NLua：用于Lua脚本解析和本地化支持
- Encoding.UTF8：确保中文等非ASCII字符正确显示

### 3.3 系统要求
- Windows 10或更高版本操作系统
- .NET运行时环境
- Python 3.8+运行环境
- 至少4GB内存
- 支持1920×1080或更高分辨率的显示器

### 3.4 目录结构
```
aotuVisualJump/
├── .gitignore
├── .vscode/
├── bin/          # 编译输出目录（可删除以清理缓存）
├── obj/          # 编译中间文件目录（可删除以清理缓存）
│   ├── launch.json
│   └── tasks.json
├── LICENSE
├── README.md
├── docs/
│   ├── compilation_guide.md
│   ├── requirements.md
│   └── user_manual.md
├── scripts/
├── src/
│   ├── ImageRecognitionApp/
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── AssemblyInfo.cs
│   │   ├── ImageRecognitionApp.csproj
│   │   ├── Localization/
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── Resources/
│   │   ├── bin/
│   │   ├── obj/
│   │   ├── packages.config
│   │   └── unit/
│   │       └── LuaLocalizationHelper.cs
│   ├── PythonScripts/
│   │   ├── image_recognition.py
│   │   ├── requirements.txt
│   │   └── venv/
│   └── scripts/
└── tests/
```

## 4. 接口设计

### 4.1 C#与Python交互接口
- 录制脚本：通过进程调用执行Python录制命令
- 执行脚本：传递脚本名称参数给Python进程
- 获取执行状态：读取Python进程输出流
- 错误处理：捕获Python进程错误输出

### 4.2 脚本文件格式(JSON)
```json
[
  {
    "action": "click",
    "button": "left",
    "x": 500,
    "y": 300,
    "delay": 0.5,
    "timestamp": "14:30:25.123456"
  },
  // ...更多操作
]
```

### 4.3 本地化接口
- 使用Lua脚本存储多语言文本
- 通过LuaLocalizationHelper类实现语言切换和文本获取
- 支持语言代码标准化处理
- 确保所有文本使用UTF-8编码

## 5. 质量要求
- 界面响应迅速，无明显卡顿
- 脚本录制和执行准确，误差不超过5像素
- 程序稳定性：连续运行24小时无崩溃
- 兼容性：支持Windows 10及以上操作系统
- 可维护性：代码注释完整，遵循命名规范
- 国际化支持：确保中文等非ASCII字符正确显示

## 6. 交付物
- 可执行程序及源代码
- 需求文档
- 用户使用手册
- 开发文档
- 测试报告