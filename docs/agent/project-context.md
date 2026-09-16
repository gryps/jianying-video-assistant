# 项目上下文

## 项目

- 名称：剪映视频助手
- 仓库：`jianying-video-assistant`
- 状态：独立运行的 WPF 桌面客户端；2026-09-15 撤销连接式 Web 外壳

## 产品基线

- 客户端固定入口：素材归类、内容文库、背景音乐、剪映草稿、模型配置；启动后直接进入素材归类。
- `192.168.31.24:8000/workbench/` 是 Web 部署对照，不是客户端运行依赖。
- `.24` 的模型配置导航已恢复；Web 端保留其 AI 视频模块，桌面端不注册入口、不渲染页面，也不显示 AI 视频相关模型卡片。
- 客户端与 `.24` 各自使用独立数据库，不承诺自动同步。

## 当前技术决策

- C# + WPF + .NET 10 Desktop；保留 WPF 以规避目标机 WinUI 原生崩溃。
- WPF 启动随包 FastAPI/Python 服务，本地服务只绑定 `127.0.0.1` 随机端口。
- SQLite、素材、音频和草稿数据存放在 `%LocalAppData%\JianyingVideoAssistant\Data`。
- WebView2 SDK `1.0.4191.47`；固定运行时 `153.0.4234.32` 随包发布。
- FFmpeg/FFprobe 和 .NET 10 均随包发布。
- 模型供应商接口是用户配置后调用的业务集成，不是启动依赖。
- 桌面端模型 API Key 使用 Windows DPAPI CurrentUser 加密后写入 SQLite；发布目录不携带用户数据库。密钥不能跨电脑或跨 Windows 用户解密，换机时需重新填写。

## 产品分类逻辑

- 产品主数据由“产品分类 → 产品名称”两级组成。
- 产品名称列表使用服务端搜索、分类筛选和每页 20 条分页，不一次渲染全部历史名称。
- 既有未分类产品继续可见；新建产品时界面要求先选分类。
- 标签分类/标签名称仍是独立、跨产品共享的素材标签体系。

## 开发与部署环境

- 主源码：本 Git 仓库。
- Web 对照与部署：`work-ubuntu:/home/gryps/apps/ecommerce-ops-platform/ops-workbench`。
- Windows 构建/运行机：`gryps@192.168.31.21`（网卡手动地址）；不得部署到 `.31`。
- Windows 构建机当前只保留随应用发布的 .NET Runtime，没有常驻 .NET SDK；宿主需要重编译时再临时安装 SDK，完成后清理。
- 当前发布目录：`%LocalAppData%\JianyingVideoAssistant\App-V15-StatusSync`。
- 当前可搬运包：Windows 桌面 `JianyingVideoAssistant-V15-StatusSync.zip`；包内不含用户数据库和 API Key。

## 发布前验证

1. Python 编译、目标业务 pytest、前端默认/桌面双构建。
2. 新建 SQLite 数据库启动与迁移版本。
3. 本地服务打包后健康检查，确认 FFmpeg/FFprobe 可用。
4. Windows Release 构建、核心冒烟测试、自包含发布。
5. 登录桌面会话启动，确认主进程、本地服务和随包 WebView2 均来自发布目录。
6. 实际截图检查初始化页及登录后的五个页面、长产品名称和分页状态。
