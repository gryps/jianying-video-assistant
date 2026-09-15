# 剪映视频助手

“剪映视频助手”是可独立安装和运行的 Windows 视频生产工作台。应用包内包含桌面客户端、本地业务服务、本地数据库、前端资源、FFmpeg/FFprobe、.NET 10 和固定版 WebView2；启动、素材管理、内容管理、音乐管理和剪映草稿不依赖局域网服务器。

## 功能

- 生产总览：素材、文案、音乐和草稿指标及生产路径。
- 素材归类：产品分类、可分页搜索的产品名称、标签体系、多视频导入与事务归类。
- 内容文库：AI 五候选文案迭代、597 音色、旁白与字幕、音频转文案。
- 背景音乐：本地/链接入库、试听、重命名、多标签和引用保护。
- 剪映草稿：组合文案、旁白字幕和音乐生成无视频轨的真实草稿。
- 模型配置：文案生成、音频转文案和字幕配音分别配置；桌面端 API Key 使用 Windows 当前用户 DPAPI 加密，并脱敏显示。

模型生成和语音服务在用户主动配置供应商接口后才访问该接口；它们不是客户端启动依赖。

## 架构

- C# / .NET 10 / WPF 桌面宿主
- 本机 `127.0.0.1` 随机端口上的 FastAPI 服务，只在应用运行期间存在
- `%LocalAppData%\JianyingVideoAssistant\Data` 中的 SQLite 数据库与业务文件
- React 工作台，由随包固定版 WebView2 显示
- Windows x64 self-contained 发布

## Windows 构建

前端需以桌面模式构建到 `src/LocalWorkbench/desktop-dist/static-workbench`，再生成本地服务：

```powershell
npm --prefix src/LocalWorkbench/frontend run build:desktop
./scripts/build-local-workbench.ps1

dotnet restore JianyingVideoAssistant.sln
dotnet build JianyingVideoAssistant.sln -c Release
dotnet publish src/JianyingVideoAssistant/JianyingVideoAssistant.csproj `
  -c Release -r win-x64 --self-contained true -o publish
```

发布目录必须整体分发，不能只复制主程序 exe。

桌面端用户数据不进入发布目录。模型 API Key 加密后位于
`%LocalAppData%\JianyingVideoAssistant\Data`，复制发布目录或 ZIP 不会携带 Key；
复制数据目录到另一台电脑或另一个 Windows 用户也无法解密，换机时需重新填写。

## 文档

- [产品基线](docs/product/product-design.md)
- [工程架构](docs/architecture/architecture.md)
- [配色系统](docs/product/color-system.md)
- [项目上下文](docs/agent/project-context.md)
