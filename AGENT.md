# 剪映视频助手 Agent 入口

本仓库是“剪映视频助手”的唯一开发工作区。

## 开始工作前

1. 阅读 `docs/agent/project-context.md`。
2. 阅读 `docs/agent/memory-notes.md`。
3. 涉及产品流程时，同时阅读 `docs/product/product-design.md`。

## 技术边界

- 客户端：C#、WinUI 3、Windows App SDK。
- 目标平台：Windows 10 版本 1809 及以上、Windows 11。
- 默认使用 MVVM，业务状态不得直接堆叠在页面代码后置文件中。
- 剪映草稿写入必须通过适配器层完成；在格式得到样本验证前，只允许生成到应用工作目录，不覆盖用户原草稿。
- 素材、音频、草稿和生成缓存不得提交到 Git。

## 验证要求

- 在 Windows 环境运行 `dotnet build`。
- 核心流程变更需同步更新 `docs/product/product-design.md`。
- 无 Windows 构建环境时，必须明确记录未验证项。
