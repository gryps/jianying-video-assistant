# 项目上下文

## 项目

- 名称：剪映视频助手
- 仓库：`jianying-video-assistant`
- 状态：连接式 WPF 桌面客户端；产品基线已于 2026-09-15 纠偏

## 产品权威

- 运行权威：`work-ubuntu:/home/gryps/apps/ecommerce-ops-platform`
- 工作台：`http://192.168.31.24:8000/workbench/`
- 服务：`product-video-automation.service`
- Windows 客户端是该工作台的桌面入口，不是独立的“四步项目助手”。
- 业务页面为生产总览、素材归类、内容文库、背景音乐、剪映草稿、模型配置。
- 完整规则见 `docs/product/product-design.md`。

## 当前技术决策

- C# + WPF + .NET 10 Desktop；目标 Windows 的空白 WinUI 程序曾发生原生崩溃，因此保留 WPF。
- 使用 Microsoft Edge WebView2 承载权威工作台，避免客户端复制服务端数据和流程。
- WebView2 SDK 锁定 `1.0.4191.47`；目标机 Evergreen Runtime 为 `152.0.4191.66`。
- 默认 URL 为 `http://192.168.31.24:8000/workbench/`，可用 `JVA_WORKBENCH_URL` 覆盖。
- Web 会话目录为 `%LocalAppData%\JianyingVideoAssistant\WebView2`，WPF 不保存账号密码。
- Windows 11 尝试使用 DWM 系统背景；内容区颜色由 Web 权威前端控制。

## 已废止实现

以下本地逻辑因偏离 Web 产品而删除：

- 当前视频项目、项目名和四张任务卡
- 文件名猜测“商品、人物、场景、口播、其他”
- 内置示例文案和本地项目脚本
- 文件名猜测音乐情绪
- `project-preview.json` 伪草稿预览

旧版本的 `%LocalAppData%\JianyingVideoAssistant\current-project.json` 不再读取，也不会主动删除。

## 开发与验证环境

- 主源码：本 Git 仓库。
- Web 运行权威：`work-ubuntu`（192.168.31.24）。
- Windows 构建/运行机：`gryps@192.168.31.29`。`.31` 是另一台宿主机，不得在其上保留本项目包。
- Windows 用户级 .NET SDK：10.0.401。
- 发布：`win-x64` self-contained，目标机无需安装 .NET。
- Windows 机到 Web 工作台的 HTTP 连通性已验证为 200。
- 目标机 Edge WebView2 Evergreen Runtime：153.0.4234.32。

## 发布前验证

1. `.24` 健康检查与服务状态。
2. Windows Release 构建和入口配置测试。
3. self-contained publish 文件完整。
4. 登录桌面会话实际启动，检查 WebView2 加载与登录。
5. 生产总览及五个业务页面导航。
6. 更新桌面快捷方式到新发布目录。
