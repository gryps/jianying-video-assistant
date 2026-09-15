# 工程架构

## 独立客户端

```text
JianyingVideoAssistant.exe（WPF / .NET 10 自包含）
├── 启动与监管 JianyingVideoAssistant.Server.exe
├── 仅连接 127.0.0.1 的随机端口
├── 固定版 WebView2 显示 React 工作台
└── 打开本机数据目录、处理刷新与启动错误
        ↓ loopback HTTP
本地 FastAPI 服务
├── SQLite：账号、产品、标签、内容、音乐、模型配置、草稿记录
├── FFmpeg / FFprobe：媒体校验、时长与音频处理
├── 本地文件工作区
└── 剪映草稿适配器
```

客户端不再连接 `192.168.31.24`。`.24` 继续作为 Web 产品的独立部署实例，用于功能对照，不是客户端运行依赖；两端数据互不自动同步。

## 启动与关闭

1. WPF 验证安装目录中的本地服务和前端资源。
2. 在回环地址选择随机空闲端口并启动本地服务。
3. 服务使用 `%LocalAppData%\JianyingVideoAssistant\Data`，全新数据库直接创建当前完整结构并标记迁移版本。
4. 健康检查成功后初始化随包固定版 WebView2 并导航到本机工作台。
5. 主窗口关闭时终止本地服务进程树；业务数据保留。

本地服务不监听局域网地址。首次使用仍需创建本机管理员密码，账号和会话只属于这台电脑。

## 打包边界

- .NET 10 Desktop：`win-x64 --self-contained`。
- Python 3.12 服务：PyInstaller one-directory，运行机不需要 Python。
- WebView2：固定版 `153.0.4234.32` 随应用发布，不依赖系统 Evergreen Runtime。
- FFmpeg/FFprobe：随本地服务发布，不依赖系统 PATH。
- React 静态资源：使用 `VITE_DESKTOP_MODE=1` 构建，只展示剪映视频助手六个入口。
- WebView2 用户数据：`%LocalAppData%\JianyingVideoAssistant\WebView2`。

## 安全与故障处理

- API 只绑定 `127.0.0.1`，随机端口不作为持久配置。
- 桌面端 API Key 使用 Windows DPAPI CurrentUser 加密后保存在本机 SQLite，返回界面时掩码处理；日志禁止记录明文 Key。发布包不包含用户 SQLite。
- DPAPI 防止复制数据库后在另一台电脑或另一个 Windows 用户下解密；它不防护已经控制当前登录账户的恶意程序。换机或重装用户配置后需重新填写 Key。
- 本地服务缺失、启动退出、超时或 WebView2 启动失败时，桌面窗口显示原因和重试入口。
- 服务启动异常写入本机日志；界面提供数据目录入口。
- 草稿生成仍通过唯一适配器，使用新目录且永不覆盖已有草稿。
