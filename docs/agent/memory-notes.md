# 记忆说明

## 2026-09-13

- 正式建立独立项目工作区。
- 用户认为素材分类、内容文库、背景音乐、剪映草稿原流程偏繁琐。
- 首版产品改为以“当前视频项目”为中心的统一工作台。
- 技术栈确定为 C#、WinUI 3、Windows App SDK。
- 当前稳定依赖基线采用 Windows App SDK 2.4.0；首次 Windows 构建后提交 NuGet 锁文件。
- Windows 构建机 `gryps@192.168.31.36` 已配置用户级 .NET 10.0.401，远程镜像只用于构建和测试，本地 Git 根仍是源码权威。
- Windows x64 Debug 构建与 5 秒启动冒烟测试通过；构建机未安装剪映。
- 首个真实功能“素材文件夹导入与分类”已实现：系统选择文件夹、后台递归扫描、格式过滤、路径去重、规则分类和逐项人工纠错。
- 素材版本通过 Windows x64 解决方案构建（0 警告、0 错误）和无第三方测试框架的核心流程冒烟测试；文件选择与视觉交互仍需人工验收。
- 日常开发构建仍为 .NET 10 框架依赖；新增 `win-x64-self-contained` 发布配置，使交付目录同时携带 .NET 10 和 Windows App SDK，目标机无需另装运行时。发布目录必须整体分发，未启用单文件或裁剪。
- 素材版本启动验证时，构建机在 `Microsoft.UI.Input.dll` 触发 `0xC0000602`；独立重建初始提交也复现，属于该机当前的 WinUI 原生运行故障，不能归因于本次功能变更。

## 2026-09-14

- 用户确认原首页“找一句开头”“查看推荐”等入口含义不清。新版首页移除所有只改变演示状态的假交互。
- 四张任务卡改为明确描述输入、打开形式和结果；素材真实可用，文案、音乐、草稿在实现前标注“后续版本”并禁用。
- 移除重复的“项目建议”区，未实现的独立资料库导航明确标为后续开放。
- 第二个真实功能“添加视频文案”已实现：右侧覆盖面板、内置本地片段、关键词与用途筛选、完整句子预览、加入项目脚本和撤销最近一次加入。
- 首页素材与文案两步标为“现在可用”；音乐和草稿保持禁用，文案与项目脚本当前只保存在内存中。
- 目标 Windows 机器重启后，正式应用与全新空白 WinUI 程序仍会在 `Microsoft.UI.Xaml.dll` / `Microsoft.UI.Input.dll` 原生崩溃；DISM 组件存储和 SFC 系统文件校验均通过。
- 用户确认改用 WPF 兼容外壳。现有 MVVM 业务层保留，自包含 WPF 探针及正式应用均能在登录桌面会话驻留。
- 背景音乐首版实现本地多选导入、情绪建议与筛选、试听、应用和撤销。
- 草稿首版实现路径预检和应用工作目录内的结构化项目预览副本；没有真实剪映样本前仍不生成或覆盖原生草稿。
- 用户确认界面改为默认浅色并跟随 Windows 应用主题；深色模式从纯黑调整为蓝灰色，标题栏、主窗口和侧面板同步切换。
- 用户随后否决 V5 的具体配色，并要求检索成熟 UI 设计体系、将方法沉淀到设计技能。审计发现 V5 浅色卡片上的半透明次要文字合成后约 3.12:1，未达到普通文字 WCAG AA 4.5:1；现有色值仅为失败方案，下一版不得直接沿用。
- 用户批准按成熟体系修正。V6 改用 Fluent 中性灰分层和 Microsoft Blue 单一强调色，补齐悬停、按下、禁用、焦点与高对比度映射；主要文字、次要文字、按钮文字、控件边框和焦点组合均通过目标对比度。
- V6 已在 Windows 登录桌面以 1245×960 截图验证浅色首页。首次截图发现 WPF 系统按钮模板覆盖主按钮文字色，随后改用应用级按钮模板并再次截图，确认主按钮为白字蓝底、内容层级清晰。

## 2026-09-15

- 用户认为首页右下角“安全原则”及类似自我说明无需持续告知。首页移除该说明卡、“现在可用”徽标和未开放资料库列表；底层只读与不覆盖约束保持不变。界面只保留能指导下一步、解释错误、说明重要输出限制或确认结果的提示。
- 用户指出在素材、文案和音乐尚未形成前无法合理预先定义项目名称。项目名改为非前置项：首次导入素材时自动使用文件夹名称，之后可随时修改并自动保存。
- 下一步实现单项目自动保存与恢复：使用版本化 JSON 原子写入项目名、素材分类、脚本、音乐选择和最近预览位置；暂不为单一小快照引入 SQLite。
- V7 补齐本地工作流：文件/文件夹拖放导入、批量处理“其他”素材、失效素材清理、自定义文案、脚本排序与删除、音乐移除和失效音乐清理。
- V7 实机检查发现三个右侧面板被根元素本地 `Visibility="Collapsed"` 压住，样式触发器无法覆盖；移除本地值后，文案、音乐和草稿面板均已在 Windows 登录桌面实际打开并截图验证。
- 全部下拉框的已选文字和展开选项统一采用水平、垂直居中；该规则由应用级 `ComboBox` 与 `ComboBoxItem` 样式管理，新增下拉框无需逐项重复设置。
- 用户再次指出产品方向偏离，并要求以 `192.168.31.24` 的现有 Web 应用为准。审计确认权威模块是“生产总览、素材归类、内容文库、背景音乐、剪映草稿、模型配置”，不存在必须预先命名的当前项目。
- 原本地四步模型与实际规则冲突：素材应使用产品和全局标签并事务化移动重命名；内容应包含 AI 五候选审核、597 音色和旁白字幕；音乐不做情绪猜测或节拍分析；草稿至少选择文案/旁白/音乐之一且不写视频轨。
- Windows 客户端改为连接式 WPF 外壳，以 WebView2 直接承载 `.24` 的权威工作台并共享服务端数据。旧的当前项目、分类猜测、内置文案、音乐情绪和伪草稿代码不再作为产品实现。
- 曾误把 `work-windows`（192.168.31.31）当作目标机并部署 V8；用户纠正后已立即删除该机的 V8 源码、发布目录、快捷方式、临时任务、截图和本轮安装的用户级 SDK。正确 Windows 目标机为 `gryps@192.168.31.29`。
- `.29` 已有 .NET SDK 10.0.401 和 WebView2 Runtime 153.0.4234.32，到 `.24` 工作台 HTTP 请求返回 200。
- 用户随后明确要求客户端完全独立，不得依赖 `.24` 或其他应用服务；V8 连接式 Web 外壳因此废止。
- V9 将经验证的 FastAPI/React 工作台纳入本仓库，WPF 启动只绑定 `127.0.0.1` 随机端口的随包本地服务；SQLite、FFmpeg/FFprobe、.NET 10 和固定版 WebView2 全部随客户端或保存在本机。
- `.24` 模型配置缺失的根因是后端与页面仍在，但导航和页面挂载丢失；已恢复导航，且保留 `.24` 自己的 AI 视频入口。
- 素材主数据新增“产品分类 → 产品名称”层级；产品名称使用服务端关键词搜索、分类筛选和每页 20 条分页。标签分类继续作为独立的跨产品素材标签体系。
- 前端改为 Fluent 浅色中性层级、Microsoft Blue 单一强调色和六个直接入口；客户端不显示 `.24` 的额外 AI 视频模块。主要/次要文字和主按钮对比度通过 WCAG AA。
- 桌面客户端模型 API Key 改用 Windows DPAPI CurrentUser 加密存储；复制安装目录不包含用户数据，复制 SQLite 到其他电脑或 Windows 用户也无法解密，换机需重新填写。
- Windows 构建/运行机地址由 DHCP 的 `.29` 变为 `.21`，现已把 `Ethernet0` 固定为 `192.168.31.21/24`，网关 `192.168.31.1`，DNS `221.12.33.227`、`223.5.5.5`。
- V10 安全独立版已发布到 `%LocalAppData%\JianyingVideoAssistant\App-V10-SecureStandalone`，桌面 ZIP 为 `JianyingVideoAssistant-V10-SecureStandalone.zip`（418,761,459 字节，SHA-256 `E8CB853574C50E432D7DE145182CE99FA9A9175187148DD89558A8C999961DD4`）。Windows DPAPI 16 项测试、Release 构建、打包服务密钥落盘检查、结构冒烟和登录页实机截图通过；桌面上的既有助手快捷方式均指向 V10。
- V11 修正桌面构建标志缺失：升级后会清理无效的 AI 视频导航状态，客户端不再显示 AI 视频、生产总览和 AI 视频相关模型卡；启动页改为素材归类，通用 404 不再显示英文 `Not Found`。
- V11 菜单改用通过 WCAG AA 的 Microsoft Blue 状态色；产品/标签管理改为纵向区块，标签分类和名称合并为同一卡片且分类可直接筛选标签；旁白页限制横向溢出，背景音乐两个导入按钮底部对齐。
- 用户指定的人物云服务 PNG 已用作 EXE、窗口标题栏、登录页和快捷方式图标。V11 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V11-BlueUI`；Windows 自包含发布、登录会话启动和实机截图均通过。
- 素材归类入口默认子页调整为“产品与标签管理”；背景音乐的链接提取与上传卡片改用网格行拉伸和内部弹性布局，确保两张卡片外框和底部主按钮对齐。
- 产品、产品分类筛选、标签分类等选择控件统一复用组合下拉样式但保持原业务状态；模型配置卡标题区固定同高；侧栏取消横向溢出；桌面端只保留 Windows 标题栏的一套品牌，顶部删除可见刷新按钮。
- “选择视频”改用与音频转文案、上传音乐相同的本地文件选择控件并保留视频多选。桌面模式不再创建 Web 专属 `ai-video`，Windows 数据目录已清理为 `databases`、`logs`、`workspace`。
- 最新桌面包 `JianyingVideoAssistant-V11-BlueUI.zip` 为 495,456,980 字节，SHA-256 `DD8577DCE5EC79DA43D6D9C4442E117BFA97D041757AAECB03B7333C7A805551`。

## 2026-09-16

- V12 将素材打标改为逐视频单选：用户从视频列表选择一个当前视频，再按“标签分类 → 标签名称”的分组层级直接点选标签；同分类仍只允许一个标签，再次点击可取消。归类页不再提供标签分类和标签名称输入框，标签维护统一留在“产品与标签管理”。
- 待归类视频新增删除操作；后端只允许删除 `runtime/video-imports` 下的单个暂存视频，拒绝任何暂存区外路径，删除最后一个视频后同步移除空批次目录，不触碰用户原始文件。
- Windows 后端素材测试 16 项通过，前端桌面构建通过，Windows 解决方案恢复依赖后以 0 警告、0 错误完成 Release 构建。
- Windows `C:\Users\gryps\source` 中 V8/V9/V10、WinUI/WPF 探针旧镜像及当前构建缓存已删除，只保留约 260 MB 当前源码镜像；发布目录和用户数据未删除。
- V12 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V12-SingleTagging`，桌面快捷方式已指向 V12；最新桌面包 `JianyingVideoAssistant-V12-SingleTagging.zip` 为 419,821,640 字节，SHA-256 `666B9F0DB3BC3D960EF7B714165E6F73059C6E85390FD26522F61359761CD602`。
- 用户指出归类结果不能继续藏在应用数据目录。V13 新增 WebView2 → WPF 原生文件选择桥接：桌面端直接取得同一原始目录内视频的绝对路径，归类后在原目录内创建产品文件夹；从待归类列表删除只移除当前选择，不删除原文件。归类成功提示展示完整目录并可直接打开。
- 用户指出图标失真。应用 ICO 从单一 256px 图层改为保留原图比例的 16/20/24/32/40/48/64/128/256px 九层资源，WPF 窗口改为直接使用 ICO，V13 新路径也避免复用旧快捷方式图标缓存。
- `C:\Users\gryps\source` 最终只保留 272,976,330 字节当前源码镜像，C 盘剩余约 12.9 GB；旧镜像、探针与所有本轮构建缓存均已清理。桌面只保留 V13 ZIP，V11/V12 ZIP 已删除，已发布程序和用户数据未删除。
- V13 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V13-OriginalFolder` 并已运行，桌面快捷方式已切换；最新包 `JianyingVideoAssistant-V13-OriginalFolder.zip` 为 420,233,453 字节，SHA-256 `5AACC8EC089635FDF56EF85820D766DF392FE256E29199C956D1AF460D890477`。
- 用户实测 V13 确认归类时遇到 HTTP 500。隔离 Windows 数据库、真实 MP4、中文产品/标签与原目录绝对路径复现均成功，定位到认证依赖会在每个并行 API 请求中更新 `last_seen_at`，令所有读取请求变成 SQLite 写入并放大写锁冲突。
- V14 将认证校验改为只读，SQLite 连接与 `busy_timeout` 统一提高到 30 秒；归类的意外异常改为 409 可读原因并写入 `Data/logs/server.log`，不再只显示无信息的 500。Windows 素材与接口回归 17 项通过，隔离数据的打包后端真实视频归类通过。
- V14 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V14-ClassificationFix`，桌面快捷方式已切换并在交互会话启动；桌面包 `JianyingVideoAssistant-V14-ClassificationFix.zip` 为 420,497,565 字节，SHA-256 `6FC05DFFF43042C57C71CCBAB3510202470E07FCC030C5B615A0EAE7610CD132`。
- V15 修复顶部“操作状态”滞留：成功消息会清除旧错误，错误会清除旧成功；文案生成、继续迭代和音频转文案也会把结果同步到顶部状态。桌面前端类型检查与生产构建、打包本地服务隔离健康检查、实际 V15 进程树和新静态资源检查通过。
- V15 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V15-StatusSync`，桌面快捷方式和交互启动任务均已切换；桌面包 `JianyingVideoAssistant-V15-StatusSync.zip` 为 420,801,731 字节，SHA-256 `943C1F123D5E1FEEDDCE727FD26A259E314D40A40AA24D3F256D451179CCD6C4`。本次 WPF 宿主未变，沿用 V14 已验证的自包含宿主，仅重建并替换本地服务与前端资源。

## 2026-09-17

- 抖音链接回退解析原本只检查 WSL 浏览器路径，并把原生 Windows `.exe` 错误送入 `wslpath`，导致已安装 Edge 仍提示缺少 Chrome。V16 按 Windows 的 Program Files、LocalAppData 路径优先发现 Microsoft Edge，也兼容 Chrome/Chromium；临时浏览器配置目录继续按任务隔离并在完成后删除。
- Windows 音乐/草稿相关测试 9 项通过，目标机真实 Edge 无界面 DOM 测试、V16 隔离服务健康检查以及实际交互会话进程树验证通过。V16 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V16-EdgeDouyin`；桌面包 `JianyingVideoAssistant-V16-EdgeDouyin.zip` 为 420,803,098 字节，SHA-256 `87660D3ABADCA60DBB058718A8F47F9E7C196AEC5C173288C9717392CB191647`。
- V16 对真实抖音页面仍会失败，因为页面 `<video>` 使用 `blob:`，真实媒体地址只出现在浏览器网络请求中。V17 使用隔离 Edge 会话的临时 NetLog，仅接受抖音可信媒体域名并在任务后删除日志；用户测试链接成功提取 67.709 秒、11,943,968 字节音频，音乐/草稿测试 10 项通过，V17 健康检查通过。V17 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V17-DouyinNetLog`；桌面包 `JianyingVideoAssistant-V17-DouyinNetLog.zip` 为 420,806,572 字节，SHA-256 `2660AE6F23FF10E09C0141A17809886ADB0F643980E2519D7731E9EDFDEA3884`。
- 完整音频转文案验证已越过抖音下载阶段，但当前 `token-plan` 百炼业务空间的模型列表没有任何 ASR 模型；`qwen3-asr-flash-2026-02-10` 和稳定版 `qwen3-asr-flash` 均由服务端返回 `model_not_found`。客户端配置已改为官方稳定模型名，仍需在该业务空间开通 ASR，或换用已开通 ASR 的同地域 Workspace ID/API Key。
- 后续确认该业务空间实际可调用 `qwen-audio-3.0-asr-flash`，但兼容模式 `/models` 不返回这一使用百炼多模态接口的模型，先前“没有任何 ASR 模型”的判断不完整。V18 新增对应同步适配器、把该模型补入百炼工作空间下拉列表且不自动选中，并在模型卡内显示读取结果。真实抖音链接在不改写用户配置的临时验证中返回 207 个字符；相关 Windows 测试 26 项通过，桌面前端构建与 V18 健康检查通过。V18 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V18-QwenAudioAsr`；桌面包 `JianyingVideoAssistant-V18-QwenAudioAsr.zip` 为 422,406,772 字节，SHA-256 `3C8461ED9E554AF60056DFCD3C8403B976E3F5380424A716ABC63BC282BCFAD7`。
- V19 的“读取模型列表”不再替语音识别筛选供应商结果，三张业务卡在读取成功后按用途显示选择建议，并从完整列表中标出匹配候选。建议使用 `qwenN.x-max` 等家族占位规则，可覆盖 `qwen3.9-max`、`qwen4.x-max` 及后续代际，而不是锁死当前版本；语音识别卡原有的固定版本提示已删除。Windows 后端相关测试 26 项和桌面前端构建通过，V19 进程树、`/api/health`、桌面快捷方式及实机模型配置页验证通过。发布目录为 `%LocalAppData%\JianyingVideoAssistant\App-V19-FullModelList`；桌面包 `JianyingVideoAssistant-V19-FullModelList.zip` 为 420,632,556 字节，SHA-256 `3B60D9C7C392A9FD2AF1C58DFBEE0521193D65D1F17863FDB67D63693E2C7417`。
- V20 为剪映草稿三个物料库增加全文/详情查看，长文案按标点和最多约 28 字的可读片段依次写入时间线，单段按字符数分配 2–5 秒；完整原文继续保存在创建快照中。客户端左侧导航新增“关于”，署名开发工具 Codex（OpenAI）、项目协助 Gryps 和微信 `gryps_zhang`，联系信息只存在于静态界面。Windows 草稿回归 11 项、桌面前端构建、V20 `/api/health`、进程树、快捷方式和实机关于页截图均通过。发布目录为 `%LocalAppData%\JianyingVideoAssistant\App-V20-LongContentAbout`；桌面包 `JianyingVideoAssistant-V20-LongContentAbout.zip` 为 420,628,428 字节，SHA-256 `DF7BA0F472EF3918392166D3E982A731D38353628B683840EEAF33254D5533E1`。
- V21 取消素材归类中独立的“产品与标签管理”；产品分类和产品名称改为可直接输入的历史下拉框，空输入显示最近 5 条，输入后可模糊查询全部历史，只在归类成功后保存新记录。视频标签改为与背景音乐一致的逗号分隔自由标签弹窗，旧产品、标签和素材关系不删除。Windows 素材与接口回归 18 项、桌面前端构建、V21 打包后健康检查、进程路径和桌面快捷方式均通过。发布目录为 `%LocalAppData%\JianyingVideoAssistant\App-V21-SimpleClassification`；桌面包 `JianyingVideoAssistant-V21-SimpleClassification.zip` 为 420,631,969 字节，SHA-256 `BF8532C3E05C8E92A365300DB9CD35DFA28B06BC8EAC90C27D82680530D61047`。
- V22 将产品分类和产品名称统一为完全相同的历史输入交互：两者均始终可输入，均展示最近 5 条并可模糊查询全部历史；选择已有产品名称时自动带出它的分类。自由标签弹窗明确说明逗号只用于分隔标签，归类文件名仍使用短横线连接。桌面前端生产构建、V22 静态资源校验、打包后健康检查和进程路径通过。发布目录为 `%LocalAppData%\JianyingVideoAssistant\App-V22-ConsistentHistoryInputs`；桌面包 `JianyingVideoAssistant-V22-ConsistentHistoryInputs.zip` 为 420,631,939 字节，SHA-256 `8FE2E444395543222B26F6A579BE09CFF8D139B044058BE3A95EC1306F444826`。

## 2026-09-18

- V22 背景音乐库的 67.734 秒音频“Failed to fetch”排查确认为当时客户端和本地服务均未运行，不是音频损坏；原文件为 11,948,404 字节且 FFprobe 可完整读取。V23 的音频请求会短暂重试一次，服务仍无法连接时改为中文提示重新打开客户端。
- V23 按用户要求将当前模型信息和 API Key 以 AES-256-GCM 认证加密种子放入交付 ZIP；新电脑首次启动时仅在本机无模型配置时导入，改用当前 Windows 用户 DPAPI 加密后删除解压目录中的种子。完整 ZIP 仍可被他人复制和使用，访问限制依赖百炼云端配置。首次运行主程序会自动在当前用户桌面创建或更新快捷方式。
- Windows 模型、密钥和音乐回归 30 项通过；隔离打包服务 DPAPI 测试、全新数据目录加密种子导入/删除/再导出、.NET 10 自包含发布、V23 健康检查、ZIP 种子存在性、自动快捷方式目标和进程路径均通过。发布目录为 `%LocalAppData%\JianyingVideoAssistant\App-V23-PortableModels`；桌面包 `JianyingVideoAssistant-V23-PortableModels.zip` 为 425,971,338 字节，SHA-256 `14E1BD3E93D7559B358BA26E8DD7C2D47B8BB70CA41262D14D95B2A2888CF5B8`。

## 2026-09-19

- 背景音乐试听的真实故障并非音频损坏或服务离线：WebView2 收到 200 响应头后会中断 11,948,404 字节的整段回环响应并报 `net::ERR_FAILED`，但 1 MiB Range 响应稳定。V24 将 GET 文件读取改为逐段 Range 下载并在浏览器中合并；真实 WebView2 验证 12 段完整合计 11,948,404 字节，界面进入“停止试听”且无错误。桌面前端生产构建、V24 本地服务健康检查、进程路径和快捷方式目标均通过。发布目录为 `%LocalAppData%\JianyingVideoAssistant\App-V24-MusicPreview`；桌面包 `JianyingVideoAssistant-V24-MusicPreview.zip` 为 426,136,988 字节，SHA-256 `4D7086F4852376A48B9EC6122D2B0AA611B04719C8826E7EBE6E59FD8E019BA8`，ZIP 保留加密模型种子。
- V25 修复等待型操作切页丢状态：主导航页面改为隐藏而非卸载，素材归类、文案生成、音频转文案、旁白、音乐和草稿等页面切换后继续保留运行状态。音频转文案额外把处理中、成功、失败及结果持久化到本地数据库，刷新后也能恢复，只有保存到内容文库后才清除。用户此前成功但未显示的 343 字识别结果已从调用日志恢复为待确认文案。
- Windows 音频转文案与接口回归 25 项、桌面前端生产构建、Python 编译检查通过；真实浏览器完成“内容文库 → 背景音乐 → 内容文库”导航验证，返回后的 343 字结果完全一致，顶部操作状态同步显示恢复成功。V25 发布到 `%LocalAppData%\JianyingVideoAssistant\App-V25-PersistentOperations` 并已运行，桌面快捷方式指向 V25；桌面包 `JianyingVideoAssistant-V25-PersistentOperations.zip` 为 424,201,341 字节，SHA-256 `2B13BDE232D5CC181AEED853356F80038EB845315BEE65336144C504B90ABC03`，ZIP 保留加密模型种子。
- V26 统一为 FFmpeg、FFprobe、yt-dlp、临时 Edge/Chrome 及兼容 AI 媒体工具添加 Windows `CREATE_NO_WINDOW` 启动标志，素材探测、音频提取、静音检测、转写预处理和草稿时长读取不再闪现黑色控制台。Windows 相关回归 37 项通过；登录桌面会话中用持续约 4 秒的命令行子进程实测，新增可见控制台窗口为 0。V26 健康检查确认随包 FFmpeg/FFprobe 可用，发布到 `%LocalAppData%\JianyingVideoAssistant\App-V26-NoConsoleFlash` 并已运行，桌面快捷方式已切换；桌面包 `JianyingVideoAssistant-V26-NoConsoleFlash.zip` 为 424,204,343 字节，SHA-256 `B704CFA212963C7DD8F6710B0707A261CBB92044E62ACAF76973D11D40A2E068`，ZIP 保留加密模型种子。
