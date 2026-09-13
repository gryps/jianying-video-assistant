# 剪映视频助手

一款以“当前视频项目”为中心的 Windows 桌面助手，把素材整理、文案选择、背景音乐和剪映草稿生成收敛到同一个工作台。

## 当前状态

仓库包含首版 WinUI 3 界面骨架和内存演示数据。所有按钮只更新工作台状态；媒体扫描、全文检索、音频试听和真实剪映草稿写入尚未接入。

当前是 unpackaged、self-contained 开发构建；正式发布前再加入 MSIX 安装包、图标与签名。

Windows x64 Debug 基线已验证：0 个警告、0 个错误，启动冒烟测试通过。Mica 外观和交互布局仍需在 Windows 桌面会话中目视验收。

## 技术栈

- C# / .NET 10
- WinUI 3
- Windows App SDK 2.4.0
- MVVM（项目内轻量实现，无额外 MVVM 依赖）

## Windows 开发环境

1. Windows 10 1809 或更高版本；建议 Windows 11。
2. 安装 .NET 10 SDK，并安装 WinUI 应用开发工具。
3. 在项目根目录运行：

```powershell
dotnet restore src/JianyingVideoAssistant/JianyingVideoAssistant.csproj
dotnet build src/JianyingVideoAssistant/JianyingVideoAssistant.csproj -p:Platform=x64
dotnet run --project src/JianyingVideoAssistant/JianyingVideoAssistant.csproj -p:Platform=x64
```

首次成功还原后，请评估并提交 `packages.lock.json`，以固定传递依赖。

## 产品与工程文档

- [产品流程](docs/product/product-design.md)
- [项目上下文](docs/agent/project-context.md)
- [工程决策](docs/architecture/architecture.md)
