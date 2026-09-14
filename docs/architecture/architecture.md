# 工程架构

## 当前形态

```text
WPF 自包含桌面外壳
├── MainWindow：连接状态、重试、浏览器替代入口
├── WebView2：承载 192.168.31.24 的权威工作台
└── WorkbenchEndpoint：默认地址与部署覆盖
        ↓ HTTP
FastAPI / PostgreSQL / 媒体服务 / AI 模型配置（192.168.31.24）
```

客户端不再维护本地“当前项目”、内置文案、情绪猜测音乐或伪草稿预览。相关旧模型、服务、ViewModel 和适配器已删除，避免业务事实分叉。

## 选择连接式客户端的原因

- `.24` 的运行中 Web 模块是用户指定的产品权威，已经包含六个页面和完整业务规则。
- 产品、标签、内容、音色、音乐、草稿和模型配置必须共享同一数据库。
- 在 WPF 中重新实现接口会造成双重 UI 和持续漂移；WebView2 可以在保持 Windows 应用入口的同时直接复用当前实现。
- 浏览器文件上传在 WebView2 中调用 Windows 文件选择器，满足桌面多文件导入。

## 运行边界

- WPF 使用 .NET 10，发布为 `win-x64` self-contained；.NET Desktop Runtime 随应用携带。
- WebView2 SDK 锁定稳定版 `1.0.4191.47`；目标 Windows 机器 `.29` 已安装 Evergreen WebView2 Runtime `153.0.4234.32`。
- 会话数据存放在 `%LocalAppData%\JianyingVideoAssistant\WebView2`。
- 默认 URL 为 `http://192.168.31.24:8000/workbench/`；合法的 HTTP/HTTPS `JVA_WORKBENCH_URL` 可覆盖，非法值回退默认地址。
- Windows 11 继续尝试 DWM 系统背景，Windows 10 或不支持时使用主题实色；Web 内容区由权威前端自身控制颜色。

## 故障处理

- 导航失败：显示连接失败、重试、在浏览器打开。
- WebView2 运行时缺失：显示安装提示并保留浏览器入口。
- WebView2 显示进程崩溃：保留窗口并允许重新载入。
- 外域新窗口：交给系统默认浏览器，避免未知页面接管客户端窗口。
- 登录凭据不由 WPF 存储；Web 会话由 WebView2 用户数据目录持久化。

## 部署

发布目录必须整体复制，不能只复制 exe。桌面快捷方式应指向当前版本目录中的 `JianyingVideoAssistant.exe`。发布前验证服务健康、目标机 WebView2 Runtime、Release 构建、入口配置测试、实际加载、登录和至少一个主要页面导航。
