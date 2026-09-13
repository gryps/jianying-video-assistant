# 工程架构

## 分层

```text
Views (XAML / code-behind only for window behavior)
  ↓ binding
ViewModels (screen state and commands)
  ↓ interfaces
Services (media scan, content search, audio preview, draft generation)
  ↓
Adapters (filesystem and version-specific Jianying draft formats)
```

素材导入通过 `IAssetImportService` 注入工作台 ViewModel，`LocalAssetImportService` 负责只读文件扫描，`MediaCategoryClassifier` 负责可替换的规则分类。系统 `FolderPicker` 及 HWND 初始化只保留在窗口代码后置中。后续播放器或草稿 JSON 逻辑同样不得进入页面代码。

素材扫描在后台线程执行，跳过重解析点目录以避免目录环；结果回到 UI 线程后再写入 `ObservableCollection`。项目当前只保留素材路径与内存分类状态，尚未把用户纠错持久化到磁盘。

## 毛玻璃策略

- 主窗口使用系统 `MicaBackdrop`，适合作为持久窗口底层。
- 卡片使用半透明主题资源，让 Mica 可见，同时保持文本对比度。
- Acrylic 只用于后续短暂浮层，不用于整页内容。
- Windows 10 或系统关闭透明效果时接受系统实色回退，不自行模拟高成本实时模糊。

## 首版部署形态

首版继续采用 unpackaged 配置，同时提供 `win-x64-self-contained` 发布配置，让 .NET 10 运行时和 Windows App SDK 组件一起进入发布目录。用户无需另装 .NET，但必须保留完整目录，不能只分发 exe。该配置不启用单文件合并或裁剪，以降低 WinUI 3 原生组件和反射代码在部署时缺失的风险。

开发构建保持框架依赖，避免扩大日常构建和核心测试的输出体积。完整自包含只在 `dotnet publish` 时启用。Packaged/MSIX 形态仍留到正式发布阶段验证，届时补齐清单、应用图标、签名和干净安装/卸载测试。这个选择不改变 View、ViewModel 或服务层结构。

Windows App SDK 虽可回溯运行到 Windows 10 1809，但该系统版本已不在常规支持范围。产品验收以 Windows 10 22H2 的实色回退和 Windows 11 的 Mica 效果为主；1809 只作为尽力兼容目标。

## 草稿安全边界

`IDraftExporter` 的输出根目录必须由应用控制。尚未完成格式版本探测、样本回归测试和原草稿备份前，禁止直接写入剪映用户草稿目录。
