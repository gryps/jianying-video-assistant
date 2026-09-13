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

首版只实现工作台 ViewModel 和草稿适配器接口。后续服务都通过依赖注入进入 ViewModel，避免文件系统、播放器或草稿 JSON 逻辑进入页面代码。

## 毛玻璃策略

- 主窗口使用系统 `MicaBackdrop`，适合作为持久窗口底层。
- 卡片使用半透明主题资源，让 Mica 可见，同时保持文本对比度。
- Acrylic 只用于后续短暂浮层，不用于整页内容。
- Windows 10 或系统关闭透明效果时接受系统实色回退，不自行模拟高成本实时模糊。

## 首版部署形态

首个代码基线采用 unpackaged、self-contained 配置，便于先验证产品工作台和系统 Backdrop。正式分发前切换到官方 Packaged 模板，补齐 MSIX 清单、应用图标、签名和干净安装/卸载测试。这个选择不改变 View、ViewModel 或服务层结构。

Windows App SDK 虽可回溯运行到 Windows 10 1809，但该系统版本已不在常规支持范围。产品验收以 Windows 10 22H2 的实色回退和 Windows 11 的 Mica 效果为主；1809 只作为尽力兼容目标。

## 草稿安全边界

`IDraftExporter` 的输出根目录必须由应用控制。尚未完成格式版本探测、样本回归测试和原草稿备份前，禁止直接写入剪映用户草稿目录。
