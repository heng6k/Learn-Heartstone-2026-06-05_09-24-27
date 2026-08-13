# Review Log

## Review 2026-07-19-01

- 阶段：G0 适用性与项目事实审计
- 结果：PASS
- 证据：README、PROJECT_SCOPE、全局要求、玩家视角测试规范、测试套件索引、Alpha 使用说明与已知问题。
- 结论：适合使用真实录屏 + HyperFrames 信息包装；不允许宣称官方完整复刻、双打或真实八人大厅。

## Review 2026-07-19-02

- 阶段：G1 需求草案
- 结果：PENDING HUMAN APPROVAL
- 已确认：16:9 横向、无旁白、不交付独立音频，只使用画面文案；BGM/音效可内嵌。
- 默认待确认：1920×1080、30fps、60–75 秒。

## Review 2026-07-19-03

- 阶段：G2 素材覆盖
- 结果：FAIL / BLOCKED BY MISSING RECORDINGS
- 问题：现有工程只有静态截图动态剪辑版；SOP 要求的八段连续操作/战斗录屏尚未冻结。
- 退回：阶段 2 素材采集。
- 说明：禁止用静态截图替代招募循环、连续战斗与回放控制证据。

## Review 2026-07-19-04

- 阶段：G2 Windows 构建录制可行性
- 结果：BUILD PASS / AUTOMATION FAIL
- 证据：Windows Alpha 成功启动并显示干净玩家入口。
- 问题：系统高 DPI、窗口捕获像素与 Unity 输入坐标不一致，自动点击无法可靠复现操作。
- 决策：不使用盲点坐标生成宣传素材；继续等待/执行人工可观察录屏。

## Root Cause Analysis 2026-07-19

- Error：`Cannot find module 'playwright-core'`
- Expected：Playwright 启动 Chromium 并加载当前 WebGL 构建。
- Cause：bundled `playwright@1.61.1` 的必需依赖 `playwright-core@1.61.1` 未随运行时提供。
- Fix：在本任务隔离目录安装完全匹配的 `playwright` 与 `playwright-core`，脚本显式引用该副本。
- Prevention：浏览器录制前先验证 Playwright 包与 core 版本一致、浏览器可启动，再进入场景自动化。

## Root Cause Analysis 2026-07-19-02

- Error：`page.goto: net::ERR_EMPTY_RESPONSE`
- Expected：临时服务器正确返回 Unity WebGL 页面及 Brotli 构建文件。
- Cause：项目 WebGL 依赖 `vercel.json` 声明的 `.br` MIME 与 `Content-Encoding: br`；通用 Python 静态服务器不实现该部署契约。
- Fix：使用任务内 Node 静态服务器，严格复刻仓库的 Brotli/MIME 响应头。
- Prevention：Unity WebGL 侦察使用项目部署头配置，不使用无压缩头支持的通用服务器。

## Review 2026-07-19-05

- 阶段：G2 首批真实录屏
- 结果：PARTIAL PASS
- 通过素材：R01 种族选择、R02 英雄到酒馆、R03 购买与真实拖拽上场。
- 技术规格：1920×1080、H.264、25fps；最终成片转换为 30fps。
- 视觉抽查：R01/R02 中段、R03 拖拽状态均为真实 UI，无桌面、Console、路径或调试错误。
- 剩余：R03 刷新/站位、R04–R08。

## Review 2026-07-19-06

- 阶段：G2 三连素材
- 结果：PASS WITH RANGE RESTRICTION
- 证据：工具连续加入同一随从，三张依次打出后形成 4/6 金色版本并打开“发现奖励”。
- 限制：三连奖励卡存在短暂内部英文占位样式；最终分镜禁止使用该时间范围，只使用金色形成和发现弹窗。
- 文件：`assets/recordings/r04-triple-golden.mp4`，1920×1080、30fps、H.264。

## Review 2026-07-19-07

- 阶段：G2 对手配置侦察
- 结果：PATH VERIFIED
- 路径：工具 → 加对手 → 卡牌库“加入敌方” → 查看对手。
- 结果：三张真实随从进入对手战场，对手配置面板可见，适合录制 R05 并继续触发战斗。

## Root Cause Analysis 2026-07-19-03

- Error：首次战斗页面显示己方 `0/7`，战斗立即结束。
- Expected：己方至少一张随从与敌方阵容进入正常攻击结算。
- Cause：R05/R06 探测脚本使用拖拽坐标时未稳定命中手牌卡的可拖区域，己方随从仍留在手牌。
- Fix：使用卡牌自身可见的“上场”按钮完成玩家动作，再进入战斗。
- Prevention：进入战斗前截图或断言己方战场非空；不以拖拽调用完成作为状态成功依据。

## Root Cause Analysis 2026-07-19-04

- Error：酒馆截图明确显示己方 3/1 随从已上场，但进入战斗回放后仍显示 `我方 0/7`。
- Expected：己方酒馆战场状态进入战斗快照，并与三张敌方随从结算。
- Cause：当前 WebGL Build 的工具/完成下一回合路径未把该玩家棋盘状态带入战斗回放；三种上场方式均已验证，问题跨越输入方式。
- Fix：视频生产阶段不修复产品代码；将 R06 标记为构建状态阻塞，改用仓库内可复现战斗 fixture 或后续修复 Build 重新录制。
- Prevention：所有战斗宣传镜头必须同时保存战前棋盘与战斗首帧，核对双方数量一致后才批准。

## Review 2026-07-19-08

- 阶段：G2 双方战斗素材
- 结果：FAIL FOR R06 / R05 PATH PASS
- R05：对手卡库与三张敌方随从配置真实可见。
- R06：当前构建无法用该路径证明完整双方战斗，禁止使用空场战斗冒充实战。

## Review 2026-07-19-09

- 阶段：G2 对手配置正式录制
- 结果：PASS WITH RANGE RESTRICTION
- 文件：`assets/recordings/r05-opponent-and-combat.mp4`，1920×1080、30fps、H.264、50.17 秒。
- 证据：卡牌库连续执行三次“加入敌方”，最终“查看对手”面板显示三张真实随从。
- 限制：只批准对手配置操作和最终面板；不得据此声明完整双方战斗。

## Review 2026-07-19-10

- 阶段：G2 回放与卡牌库素材复核
- 结果：PARTIAL PASS
- R07：批准播放、前后步进、跳过、速度和日志控件范围；仅证明回放控制及逐步查看事件。
- R08：批准五本筛选、海盗类型筛选与滚动列表；动态搜索未通过，布莱恩搜索由 `advanced-filter.png` 与 `brann.png` 补充。
- 限制：R07 不得证明完整双方战斗；R08 不得伪造键盘搜索连续操作。

## Review 2026-07-19-11

- 阶段：G2 仓库 fixture 审计
- 结果：FIXTURE EXISTS / BUILD ENTRY NOT EXPOSED
- 证据：仓库包含 `DesignValidationScenarioCatalog`，其中 `FullNextTurnFlow` 明确保存双方棋盘；`TestScenarioMapper.ApplyTo` 也会恢复 `PlayerBoard` 与 `OpponentBoard`。
- 阻塞：当前 WebGL UI 只公开用户保存/加载场景入口，未公开内置设计验证场景的加载入口；现有 Build 无法直接调用该 fixture。
- 决策：视频生产阶段不修改产品代码或重建 Build 来制造素材，R06 继续保持 `blocked-build-state`。

## Review 2026-07-19-12

- 阶段：G9 实现置信度检查
- 结果：PASS / 100%
- 重复检查：旧版工程为静态截图原型，新版需要真实录屏重建，不属于重复实现。
- 架构检查：使用 HyperFrames 0.7.64 官方 scaffold、单一 seek-safe GSAP 时间线和本地冻结素材。
- 文档与参考：已核对 HyperFrames core、CLI、creative、animation、general-video 与 media-use 契约。
- 根因：R06 战斗快照丢失己方棋盘已明确，成片删除完整双方战斗声明。

## Review 2026-07-19-13

- 阶段：G10 composition 检查与快照审计
- 结果：PASS / WAITING HUMAN PREVIEW APPROVAL
- Composition：`hyperframes/index.html`，1920×1080、30fps 目标、70 秒、无旁白、无未授权音乐。
- `hyperframes check`：PASS；runtime/layout/contrast 均为 0 错误。
- 快照：11 个审计点已生成于 `hyperframes/snapshots-final/`。
- 定点修复：R04 原取段出现内部英文占位提示，已改为 4 秒批准动态范围 + 普通/金色真实卡图对比；最终快照无该占位提示。
- 非阻塞警告：单文件轨道元素较密；不影响运行和渲染正确性。
- 动画图工具：Windows 下临时依赖 bootstrap 触发 `spawnSync npm.cmd EINVAL`；composition 本身的 check 与快照均通过，记录为工具兼容性问题，不重复失败调用。
- Preview：`http://localhost:3002`，等待人工批准后才能渲染 draft/high。

## Review 2026-07-19-14

- 阶段：G4 用户定向修改
- 结果：APPLIED
- 修改：删除 S04 的三连 / 金色动态录屏与普通/金色对比卡图，改为已批准的铜须五本卡真实卡图。
- 文案：`铜须：先看清一张关键卡。` / `五本卡牌，真实属性与效果直接可读。`
- 审计边界：原 R04 素材仍保留在资产库与审计记录中，但不再进入 composition。

## Root Cause Analysis 2026-07-30-01

- Error：Voicebox profile 已创建，但 `http://127.0.0.1:17493` 无法连接，生成历史无法查询。
- Expected：桌面版后端监听 17493，并读取 `%APPDATA%/sh.voicebox.app` 中的 profile 与生成记录。
- Cause：直接运行 `voicebox-server.exe` 默认监听 8000，并使用 `D:/voicebox/data` 空数据库；同时 Qwen TTS 模型尚未下载。
- Fix：以 `--host 127.0.0.1 --port 17493 --data-dir C:/Users/wch/AppData/Roaming/sh.voicebox.app` 启动后端；优先下载 CPU 可用的 `qwen-tts-0.6B`。
- Prevention：以后在调用 API 前同时验证 `/health`、数据目录、profile 数量与 `/models/status`，不能只检查进程是否存在。

## Review 2026-07-30-02

- 阶段：G9 新版实现置信度检查
- 结果：PASS / 100%
- 重复检查：Voicebox “曼波” profile 已存在，但生成历史为 0；composition 中铜须场景只有一处完整实现。
- 架构检查：沿用现有单一 seek-safe GSAP 时间线、原媒体轨和稳定 ID；只删除铜须 DOM/CSS 并将后续时间整体前移。
- 官方文档与参考：已核对 HyperFrames core/CLI/media 规则和 Voicebox v0.5.0 OpenAPI。
- 根因：铜须场景仍占用 25–35 秒；Voicebox 后端参数和模型状态均已明确。

## Review 2026-07-30-03

- 阶段：G4 用户定向修改
- 结果：APPLIED / AUDIO PENDING
- 修改：完整删除 25–35 秒铜须场景，成片从 70 秒缩短为 60 秒；后续画面、功能序号、GSAP 入场和聚焦时间整体前移 10 秒。
- 文案：删除全部铜须旁白，建立 7 句 Voicebox 新版旁白文本。
- 依赖失效：旧版 preview 批准、draft/high 与 SHA256 全部失效；新版旁白接入并通过 check 后必须重新取得 G10 人工批准。

## Review 2026-07-30-04

- 阶段：G5/G6 Voicebox 旁白生成与母带审计
- 结果：PASS / READY FOR COMPOSITION
- 实际引擎：Voicebox v0.5.0、用户授权的“曼波”克隆 profile、Chatterbox Multilingual（`chatterbox-tts`）；历史 Qwen 下载故障记录仅保留为排障证据，不是最终方案。
- 生成：按新版 `SCRIPT.md` 生成 7 段中文旁白，并在 0.6、5.7、16.7、25.7、34.7、44.7、52.0 秒接入 60 秒母带。
- 母带：`assets/audio/voiceover-final.wav`，48 kHz、mono、PCM s16le、60.000 秒、-16.0 LUFS、-1.5 dBTP。
- SHA256：`D3208F0200733408EF5D33A1B76C7DD18CDA330ABAA2ECF441E0B94854203B24`。
- 音频边界：无 BGM、无官方游戏原声；WAV 只保留于工程审计，不作为独立交付物。

## Root Cause Analysis 2026-07-30-05

- Error：首次运行 HyperFrames `transcribe` 返回 `whisper_unavailable`；安装 whisper.cpp 后，默认 Flash Attention 又关闭 DTW，HyperFrames 中文归一化把连续 CJK token 合并成两条长句。
- Expected：获得可复核的中文词组级时间戳，用于字幕与音画审计。
- Cause：本机缺少 `whisper-cli`；whisper.cpp v1.9.1 的 Flash Attention 与 DTW token timestamps 不兼容；当前 HyperFrames CJK 合并策略不适合这条连续中文旁白。
- Fix：安装官方 whisper.cpp v1.9.1 与 `ggml-small.bin`，对 7 个旁白源段分别使用 `--dtw small --no-flash-attn`，过滤特殊 token 后按锁定文案合并原始 token span。
- Verification：`assets/audio/transcript.json` 为 `w0`–`w54` 共 55 条、0 重叠；`captions/voiceover-final.srt` 为 7 条完整句子。只校正文案并合并真实 token 起止，未插值或伪造时间戳；详见 `narration/transcription-audit.json`。

## Review 2026-07-30-06

- 阶段：G7 旁白接入与字幕
- 结果：APPLIED / CHECK PENDING
- Composition：在 `index.html` 根合成加入独立 `voiceover` audio track，0–60 秒、track 10、volume 1，由 HyperFrames 管理播放和 seek。
- 字幕/转写：生成规范 transcript、7 句 SRT 与 whisper.cpp/模型/DTW 审计记录。
- 审批：旧版无声渲染继续失效；新版 `check --snapshots` 和 Studio 有声预览批准前不得渲染。

## Review 2026-07-30-07

- 阶段：G10 人工快照定点修正
- 结果：APPLIED / RECHECK PENDING
- 发现：编号章节已正确删除并顺延，但 53.5 秒“名称检索”静态拼接仍显示铜须卡图；自动 layout/contrast 检查不会识别这类内容边界问题。
- 修改：用现有真实普通随从卡 `minion-normal.png` 替代 `brann.png`，并将镜头文案收敛为“名称筛选，继续定位目标卡牌”。
- 边界：A007 铜须截图改为 `approved-unused`，本版 composition 不再引用铜须素材；修改后重新执行 `check --snapshots`。

## Review 2026-07-30-08

- 阶段：G10 最终工程门与人工快照审计
- 结果：PASS / WAITING HUMAN AUDIO PREVIEW APPROVAL
- HyperFrames：0.7.83，升级元数据 `updateAvailable: false`。
- `check --snapshots --json`：0 lint/runtime/layout/motion/contrast 错误；19/19 文本对比度通过。
- 非阻塞项：track 2 的 6 个 timed elements 触发 1 条维护性 warning；不影响当前单文件 60 秒成片的运行、布局或渲染。
- 主快照：3.3、16.7、30.0、43.3、56.7 秒均通过人工检查；功能编号为 02、03、04、05，结尾页与测试版/非官方边界完整。
- 定点快照：`snapshots-audit/frame-at-53.5s-no-brann.png` 确认名称筛选镜头无铜须；`index.html` 已规范化为 UTF-8 no-BOM，异常字符不再出现。
- 下一门：启动 Studio 有声预览，用户明确批准后才能进入 draft/high 渲染。

## Review 2026-07-30-09

- 阶段：G10 Studio 有声预览交付
- 结果：RUNNING / WAITING HUMAN APPROVAL
- URL：`http://localhost:3017/#project/hyperframes`。
- 服务：HTTP 200，监听 PID 7896；stdout 确认 project `hyperframes`、Studio running。
- Studio context：0 error、1 条既有非阻塞轨道密度 warning。
- 操作：请在 Studio 播放 60 秒完整时间轴，重点试听 7 段中文旁白的音色、响度、断句和画面对齐；明确批准前不运行 render。

## Root Cause Analysis 2026-07-30-10

- Error：通过 `npm run render -- --quality high ...` 调用时，HyperFrames 在编码前报告 `Not a directory: ...\high`；改用等号后又把 `30` 当作目录。
- Expected：package.json 固定版本 wrapper 将 render 选项原样传给 HyperFrames。
- Cause：本机 Windows、npm 10.9.2 与当前脚本调用组合破坏了带值参数转发，只把选项值作为位置参数交给 CLI；底层 `hyperframes@0.7.83` 无效质量探针能正确进入参数校验，证明工程和 CLI 本身正常。
- Fix：直接调用 package.json 中同一固定版本 `npx --yes hyperframes@0.7.83 render`，并统一使用 `--quality=high`、`--fps=30`、`--output=...`。
- Prevention：本机最终渲染使用固定版本底层命令或先验证 wrapper 的 argv 转发；参数错误必须在编码前停止，不得把失败调用视为成片。

## Review 2026-07-30-11

- 阶段：G10 批准、G11/G12 最终渲染与交付审计
- 结果：PASS / FINAL HIGH RENDER COMPLETE
- 人工批准：用户在当前任务中明确回复“批准”。
- HyperFrames：固定版本 `0.7.83`，升级检查 `updateAvailable: false`。
- 成片：`renders/LearnHeartstoneCombatPromo16x9-final-voiceover-high-20260730.mp4`，13,480,733 bytes。
- 技术规格：60.000 秒、1920×1080、30fps、H.264；音轨为 AAC、48 kHz、双声道。
- 验证：FFprobe 通过，FFmpeg 全片解码通过；render 的 drawElement 自检关键帧均通过。唯一 lint 信息仍是既有的 track 2 密度维护性 warning，不影响交付。
- SHA256：`5A0C97B479537A3014168DA861693A43762F250807E3CFBDAB84E6C34DB26597`。
