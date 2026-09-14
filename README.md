# 剪映视频助手

“剪映视频助手”是现有视频生产工作台的 Windows 桌面客户端。它直接连接 `192.168.31.24` 的权威服务，共享同一套账号、数据和业务流程，不再维护一套偏离 Web 产品的本地“当前项目”。

## 功能

- 生产总览：素材、文案、音乐和剪映草稿指标及五步生产流程。
- 素材归类：产品、标签分类、标签名称管理，多视频上传与事务化归类。
- 内容文库：AI 文案迭代、文案库、597 音色、旁白与字幕、音频转文案。
- 背景音乐：本地/链接入库、试听、重命名、多标签和引用保护。
- 剪映草稿：不写视频轨，组合文案、旁白字幕、音乐生成原生草稿。
- 模型配置：文案生成、语音识别、字幕配音独立配置与脱敏调用日志。

## 技术

- C# / .NET 10 / WPF
- Microsoft Edge WebView2
- Windows x64 self-contained 发布
- 默认连接 `http://192.168.31.24:8000/workbench/`

目标机无需另装 .NET；需要系统 Edge WebView2 Runtime。当前目标机已验证安装。登录状态保存在应用自己的 WebView2 用户数据目录。

## Windows 构建

```powershell
dotnet restore src/JianyingVideoAssistant/JianyingVideoAssistant.csproj
dotnet build JianyingVideoAssistant.sln -c Release -p:Platform=x64
dotnet run --project tests/JianyingVideoAssistant.CoreSmokeTests -c Release -p:Platform=x64
dotnet publish src/JianyingVideoAssistant/JianyingVideoAssistant.csproj `
  -p:PublishProfile=win-x64-self-contained
```

发布目录必须整体分发，不能只复制其中的 exe。服务地址需要变更时设置环境变量 `JVA_WORKBENCH_URL`。

## 文档

- [产品基线](docs/product/product-design.md)
- [工程架构](docs/architecture/architecture.md)
- [项目上下文](docs/agent/project-context.md)
