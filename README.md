# 剪映视频助手

一款以“当前视频项目”为中心的 Windows 桌面助手，把素材整理、文案选择、背景音乐和草稿预检收敛到同一个工作台。

## 当前功能

- 素材：选择文件夹，递归扫描常见图片和视频，自动建议分类并允许直接纠正；扫描只读取源文件。
- 文案：按用途和关键词筛选本地片段，一键加入当前项目脚本，并可撤销上一段。
- 音乐：导入本地音频，按情绪筛选、试听、应用和撤销；不会复制或修改音频文件。
- 草稿：集中检查素材路径、文案和音乐，向应用工作目录生成包含 `project-preview.json` 的安全预览副本。

草稿输出目前是结构化项目预览，不是剪映可直接打开的原生草稿。必须取得真实剪映草稿样本并完成版本适配后，才能安全生成原生副本；应用始终禁止覆盖用户已有草稿。

## 技术栈

- C# / .NET 10
- WPF
- MVVM（项目内轻量实现，无第三方 MVVM 依赖）
- Windows 11 使用 DWM 系统背景；不支持时自动保留深色半透明实色背景

界面已从 WinUI 3 迁移到 WPF，因为目标 Windows 机器连空白 WinUI 程序都会在原生 XAML/Input 组件中崩溃。模型、ViewModel 和服务边界保持不变。

## Windows 开发环境

```powershell
dotnet restore src/JianyingVideoAssistant/JianyingVideoAssistant.csproj
dotnet build JianyingVideoAssistant.sln -c Release -p:Platform=x64
dotnet run --project tests/JianyingVideoAssistant.CoreSmokeTests -c Release -p:Platform=x64
```

## Windows 自包含发布

```powershell
dotnet publish src/JianyingVideoAssistant/JianyingVideoAssistant.csproj `
  -p:PublishProfile=win-x64-self-contained
```

发布目录携带 .NET Desktop 运行时，目标机不需要另行安装 .NET。必须整体分发 `publish` 目录，不能只复制其中的 exe。

## 产品与工程文档

- [产品流程](docs/product/product-design.md)
- [项目上下文](docs/agent/project-context.md)
- [工程决策](docs/architecture/architecture.md)
