# Task Plan: Learn Heartstone 16:9 宣发视频

## Goal
完成一支 60 秒、16:9 的 Learn Heartstone 战斗训练宣发视频，内嵌经 Voicebox Chatterbox 克隆生成的中文旁白，并保留可复核的音频、转写、字幕和 HyperFrames 检查证据。

## Current Phase
Phase 6

## Phases

### Phase 1: 项目理解与脚本/分镜
- [x] 了解项目定位与战斗训练能力
- [x] 完成 60 秒脚本、设计和分镜
- [x] 删除“3：5 本卡铜须”段落并顺延编号
- **Status:** complete

### Phase 2: 画面工程
- [x] 完成 16:9 HyperFrames 合成
- [x] 将总时长收敛为 60 秒
- [x] 保持 Alpha/测试版、非官方训练工具的准确表述
- **Status:** complete

### Phase 3: Voicebox 旁白母带
- [x] 从用户提供视频建立 30 秒参考音频
- [x] 使用 Voicebox Chatterbox Multilingual 生成七段中文旁白
- [x] 合成并审计 48 kHz 单声道母带
- **Status:** complete

### Phase 4: 音频接入、真实时间戳与字幕
- [x] 用七段 Whisper DTW 原始 token 覆盖错误 transcript
- [x] 生成最终 SRT 与转写审计记录
- [x] 在 HyperFrames 合成中接入旁白音轨
- [x] 同步 BRIEF、ASSET_MANIFEST、REVIEW_LOG 镜像文档
- **Status:** complete

### Phase 5: 工程检查与新版预览
- [x] 执行 HyperFrames check 和 snapshots
- [x] 人工检查关键快照与音画时长
- [x] 启动/提供新版 Studio 预览供试听
- **Status:** complete

### Phase 6: 批准后最终渲染
- [x] 获得新版有声预览的明确批准
- [x] 渲染最终成片并完成最终审计
- **Status:** complete

## Constraints
- 无 BGM、无官方游戏原声。
- 旁白只内嵌成片，不把 WAV 作为独立交付物。
- 仅称 Alpha/测试版、非官方单人训练/模拟工具。
- 不声称官方完整复刻、100% 还原、真实八人大厅或完整双方战斗。
- 旧 70 秒预览、draft/high 渲染及其哈希均已失效。
- 不修改仓库根目录或 `.planning` 下其他任务的规划文件。

## Errors Encountered
| Error | Attempt | Resolution |
|---|---:|---|
| HyperFrames `transcribe` 返回 `whisper_unavailable` | 1 | 安装并验证官方 whisper.cpp v1.9.1 二进制与 small 模型 |
| whisper.cpp Flash Attention 关闭 DTW token 时间戳 | 1 | 使用 `--no-flash-attn` |
| HyperFrames 中文归一化把连续 CJK token 合并为两条长句 | 1 | 分别转写七段，保留真实 DTW token span，只校正文案并合并原始时间范围 |
| Windows 下 `npm run check -- --snapshots --json` 未把参数转发到固定版本脚本，结果显示 `Snapshots disabled` | 1 | 不重复该调用；改用同一固定版本 `npx --yes hyperframes@0.7.83 check --snapshots --json` |
| 第二次独立 `snapshot` 调用重建了 `snapshots/`，先前的 50 秒专项帧随目录刷新消失 | 1 | 不依赖临时专项帧留存；完成最后视觉修正后重新运行 `check --snapshots` 生成最终证据集 |
| PowerShell 字符扫描脚本在 `foreach` 后直接接管道，触发 `An empty pipe element is not allowed` | 1 | 改为先收集 `$rows` 再输出；确认 HTML 文本内无 U+FEFF/`ï»¿`，异常来自物理 BOM 候选 |
| `apply_patch` 无法匹配带物理 BOM 的 HTML 首行 | 1 | 不重复首行补丁；只做一次无内容变化的 UTF-8 no-BOM 编码规范化并立即核对字节与快照 |
| 当前环境执行 `rg.exe` 搜索 Voicebox 证据时返回 `Access is denied` | 1 | 不重复同一调用；改用受限范围的 PowerShell `Get-ChildItem` + `Select-String` |
| `whisper-cli.exe --help` 正常打印帮助后返回退出码 1，导致包装工具标记失败 | 1 | 已取得所需参数，不重复执行；按帮助内容核对 `--output-json-full`、`--dtw` 与 `--no-flash-attn` |
| 事实断言将数值入点格式化后按字符串比较，`52` 与文档展示值 `52.0` 被误判不一致 | 1 | 改为逐项数值比较，展示格式单独检查，不重复字符串等值判断 |
| 直接动态执行从 Markdown 提取的 PowerShell mix 脚本被终端安全策略拒绝 | 1 | 不重复动态执行；改用 `apply_patch` 创建受控临时 `.ps1`，实跑后删除 |
| mix filter 首次实跑生成 `[]aresample`，FFmpeg 拒绝空输入标签 | 1 | PowerShell 将 `$i:a` 误解析为作用域变量；改为 `${i}:a` 后重跑 |
| 文档调用示例使用 `pwsh`，当前已验证 Windows 环境未安装 PowerShell 7 | 1 | 改为系统自带 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File` |
| stale 文本扫描在 HyperFrames 子目录误用仓库相对 `Docs/...` 路径 | 1 | 改回仓库根目录执行绝对范围扫描，不重复错误工作目录 |
| 事实复核脚本省略 PowerShell 比较运算符两侧空格，误报 transcript ID 不匹配 | 1 | 保留数据不变，修正为显式 ` -ne ` / ` -lt ` 后重跑 |
| Windows PowerShell 5.1 中用 `@(...)` 包裹 `ConvertFrom-Json` 的顶层数组后得到单个嵌套对象 | 1 | 不再额外包裹，直接使用 `ConvertFrom-Json` 返回的 `Object[]` |
| 对 `D:\voicebox` 直接运行 `git -C` 返回“not a git repository”，并使并行置信度检查提前终止 | 1 | 不重复 Git 假设；改为检查发布包元数据、本地上游快照和 GitHub 官方仓库/Release |
| `tar -tf | Select-String` 在找到结果后仍使并行包装调用以退出码 1 结束，其他并行结果未返回 | 1 | 不重复该管道；后续命令单独执行并显式捕获网络/筛选结果，避免一个非零退出吞掉其余证据 |
| PowerShell 源码搜索把 `$matches` 当普通数组使用，撞到内置自动变量并触发 hashtable 加法错误 | 1 | 改用非保留变量名 `$foundFiles`，后续不复用 PowerShell 自动变量 |
| Windows/npm 通过 `npm run render --` 转发带值参数时把 `high`/`30` 当作项目目录 | 2 | 用无效质量探针确认底层 CLI 正常；直接调用 package.json 固定的 `hyperframes@0.7.83` 并使用等号参数完成最终渲染 |

## Success Criteria
- 60 秒合成内有完整且可听的中文旁白，无 BGM。
- transcript 为 55 条真实 DTW 时间跨度，SRT 可复核。
- HyperFrames check 通过，关键快照无明显布局/编号问题。
- 用户在新版 Studio 预览确认后，最终渲染成功。

## Documentation Workstream: SOP 音频方法整合
- [x] 审阅现有 SOP 的 G6/G7、工程资料包、音轨接入、预览和归档章节
- [x] 核对 Voicebox v0.5.0、Chatterbox、参考音频、生成清单、母带和 DTW 审计证据
- [x] 写入可复用的授权声音克隆旁白分支与项目实证
- [x] 核对目录、编号、交叉引用、UTF-8 和 Markdown 结构
- [x] 完成首轮无上下文读者检查与独立一致性审阅
- [x] 修复 profile 样本身份、批次参数一致性、分段完整性与哈希门
- [x] 补齐响度自动审计、DTW 全局校验和唯一真值约束
- [x] 修复内外部/源码协作交付边界、白名单与递归归档风险
- [x] 完成必要的结构与示例语法核对
- **Status:** complete
