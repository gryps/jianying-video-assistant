# 剪映视频助手

一款以“当前视频项目”为中心的 Windows 桌面助手，把素材整理、文案选择、背景音乐和剪映草稿生成收敛到同一个工作台。

## 当前状态

仓库包含首版 WinUI 3 工作台，以及第一个真实功能“素材文件夹导入与分类”：用户可选择本地文件夹，递归扫描常见图片和视频，按文件名或目录名获得“商品、人物、场景、口播、其他”分类建议，并在列表中直接纠正。扫描只读取源文件，按完整路径去重，不移动、不重命名也不修改素材。

内容检索、音频试听仍为交互原型；真实剪映草稿写入尚未接入。

当前提供 unpackaged 的 Windows x64 自包含发布配置：.NET 10 运行时和 Windows App SDK 组件都会随发布目录提供，目标机不需要另行下载安装 .NET。发布结果是一个完整目录，必须整体分发，不能只复制其中的 exe。MSIX 安装包、图标与签名仍留待正式发布阶段完成。

Windows x64 Debug 解决方案已验证：0 个警告、0 个错误；素材核心流程测试通过。当前构建机启动 WinUI 时在 `Microsoft.UI.Input.dll` 触发 `0xC0000602`，且初始提交可复现同一故障，因此最新界面启动、文件夹选择器、分类下拉框和 Mica 外观仍需在恢复 Windows 图形运行环境后验收。

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

## Windows 自包含发布

在 Windows 项目根目录运行：

```powershell
dotnet publish src/JianyingVideoAssistant/JianyingVideoAssistant.csproj `
  -p:PublishProfile=win-x64-self-contained `
  --no-restore
```

将命令输出所指向的 `publish` 文件夹整体压缩或复制给用户。用户解压后直接运行 `JianyingVideoAssistant.exe`，无需单独安装 .NET 10。该发布配置明确关闭单文件合并和裁剪，以避免 WinUI 3 原生组件或反射代码在首版部署中缺失。

## 产品与工程文档

- [产品流程](docs/product/product-design.md)
- [项目上下文](docs/agent/project-context.md)
- [工程决策](docs/architecture/architecture.md)
