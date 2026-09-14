# 工程架构

## 分层

```text
Views (WPF XAML / code-behind only for window behavior and native pickers)
  ↓ binding
ViewModels (project state and commands)
  ↓ interfaces
Services (media scan, content search, local audio preview)
  ↓
Adapters (application working files and version-specific Jianying formats)
```

界面层于 2026-09-14 从 WinUI 3 迁移到 WPF。原因是目标 Windows 环境中的空白 WinUI 程序也会在原生 XAML/Input 组件中崩溃，而 WPF 探针能在同一登录桌面会话正常驻留。模型、ViewModel、服务接口和草稿安全边界保持不变。

## 素材与文案

素材导入通过 `IAssetImportService` 注入工作台 ViewModel；`LocalAssetImportService` 只读扫描文件，跳过重解析点目录并统计无权访问的目录，`MediaCategoryClassifier` 提供可人工覆盖的规则建议。WPF 的 `OpenFolderDialog` 只保留在窗口代码后置中。

文案通过 `IContentLibraryService` 注入；`LocalContentLibraryService` 提供本地片段和内存筛选。检索、采用、自定义录入、排序、删除、重复提示、撤销与进度均由 ViewModel 维护，项目脚本通过 `IProjectStore` 持久化。

## 背景音乐

`LocalMusicLibraryService` 过滤支持的本地音频并根据文件名/目录名建议情绪。`WpfMusicPreviewService` 是唯一依赖 WPF 媒体 API 的服务实现，ViewModel 只依赖 `IMusicPreviewService`。应用音乐只记录原路径，试听不会修改或复制源文件。

## 草稿安全边界

所有输出通过 `IDraftExporter` 及适配器层完成。当前 `JianyingDraftPreviewAdapter` 的工作根目录由组合根固定到 `%LocalAppData%\JianyingVideoAssistant\DraftPreviews`，每次生成独立目录，只包含 `project-preview.json` 和说明文件。

当前输出明确不是剪映原生草稿。没有完成格式版本探测、真实样本回归和副本打开测试前，禁止写入剪映用户草稿目录，也禁止生成看似真实的 `draft_content.json`。

## 毛玻璃与回退

- Windows 11 尝试通过 DWM 系统背景属性呈现窗口材质。
- 内容面板和卡片使用高对比度半透明表面。
- DWM API 不可用或透明效果关闭时，应用资源提供深色实色/半透明回退，不影响功能。

## 部署

首版发布 x64 自包含目录，携带 .NET 10 Desktop Runtime，目标机无需安装 .NET。发布目录必须整体分发；当前不启用单文件合并和裁剪。MSIX、签名和自动更新留待正式发布阶段。
