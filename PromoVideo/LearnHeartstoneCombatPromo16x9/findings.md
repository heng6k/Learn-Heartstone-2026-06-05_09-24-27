# Findings and Decisions

## Current Artifacts
- HyperFrames 工程：`hyperframes/`
- 旁白母带：`hyperframes/assets/audio/voiceover-final.wav`
- 母带规格：48 kHz、mono、PCM s16le、-16.0 LUFS、-1.5 dBTP。
- 母带 SHA256：`D3208F0200733408EF5D33A1B76C7DD18CDA330ABAA2ECF441E0B94854203B24`。
- 七段 DTW 原始转写：`hyperframes/narration/whisper-segments/01.json` 至 `07.json`。
- 全长原始证据：`hyperframes/narration/whisper-small-raw.json` 与 `whisper-small-dtw-raw.json`。

## Transcription Findings
- whisper.cpp v1.9.1 已安装在 HyperFrames 缓存目录。
- 使用模型 `ggml-small.bin`，SHA256 为 `1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b`。
- `--no-flash-attn` 是保留 DTW token 时间戳的必要参数。
- 规范 transcript 应包含 `w0` 至 `w54` 共 55 条；首条约 `0.640–1.090`，末条约 `56.350–56.610`。
- 文案校正不得插值或伪造时间戳，只能按既定 token 索引合并原始 span。
- HyperFrames 规范输入是扁平 word 数组：`[{id,text,start,end}]`；必须在生成字幕前人工检查文字、空项、特殊 token 与异常短时间跨度。
- 当前 `hyperframes/assets/audio/transcript.json` 只有两条乱码长句，是 HyperFrames CJK 合并失败产物，必须整体覆盖，不能复用。

## Composition Findings
- `hyperframes/index.html` 的根合成是 `learn-heartstone-promo`，时长 60 秒，已接入 0–60 秒独立旁白 `<audio>` 轨道。
- `hyperframes/package.json` 的所有脚本固定 `hyperframes@0.7.83`；此前升级探针确认该版本为最新版。
- `check --snapshots` 通过后人工查看主快照：02 招募、03 对手配置、04 回放、05 卡牌库的编号顺序正确，56.7 秒最终页完整。
- 额外抓取 53.5 秒发现“名称检索”镜头仍显示铜须卡图。虽然它不是已删除的编号章节，但为严格满足用户删除“5 本卡铜须”内容的要求，最终预览前应改为不含铜须的批准素材。
- `assets/stills/minion-normal.png` 是现有真实普通随从卡素材，可在不新增素材、不伪造连续操作的前提下替代 `brann.png`；镜头文案同步收敛为“名称筛选，继续定位目标卡牌”。
- 53.5 秒替换后专项快照的左上角出现极小的 `ï»¿` 样异常痕迹；HTML 文本内没有这些字符，但文件物理头为 UTF-8 BOM（`EF BB BF`），需要去 BOM 后复核。
- 将 `index.html` 规范化为 UTF-8 no-BOM 后，文件首字节变为 `3C 21 64 6F...`，53.5 秒专项快照中的异常字符消失；普通随从卡、名称筛选文案和测试版标识均正常。

## Decisions
- 旁白方案采用 Voicebox 的 Chatterbox Multilingual / `chatterbox-tts`，不再使用历史 Qwen 方案。
- 无 BGM，旁白以独立 HyperFrames audio track 内嵌。
- 音频播放与 seek 由 HyperFrames 框架管理；HTML 只声明媒体轨道，不添加手动播放脚本。
- 音轨使用单独 `<audio>` 元素、显式 `data-start`/`data-duration`/`data-track-index`/`data-volume`，不在时间轴脚本中调用 play、pause 或 seek。
- 接入后必须依次通过 `check`、快照人工检查和 Studio 预览。
- 最终门使用一次 `check --snapshots`（它已包含 lint），人工打开生成的 PNG；不要在它前面重复跑独立 lint。
- Studio 必须从工程目录后台启动，并交付包含 `#project/LearnHeartstoneCombatPromo16x9` 的真实 URL；确认服务仍在运行后再交给用户。
- 在新版有声预览得到明确批准前，不生成新的 draft/high 最终渲染。
- 仓库根部 planning 文件属于其他任务，本视频只使用本目录的规划记录。

## SOP Audio Integration Findings
- 目标文档为 `Docs/HyperframesAuditedVideoProductionSOP.zh-CN.md`，现有第 11 节已覆盖真人录音、外部 TTS、HyperFrames 本地 TTS和统一响度，第 12 节已覆盖 SRT/transcript，但尚未记录授权声音克隆、Voicebox 清单、分场景 DTW 和中文 CJK 合并问题。
- 新内容应作为第 11 节内的可复用“授权声音克隆旁白分支”，并在第 12 节补充真实 DTW 时间戳分支；不另起一套与 G6/G7 冲突的流程。
- 推荐实现固定为本次已验证的 Voicebox v0.5.0 + Chatterbox Multilingual（`chatterbox-tts`）；历史 Qwen 路径不得作为推荐方案。
- 声音克隆必须先登记权利人授权、参考音视频来源、允许用途和保留期限；未获授权不得提取或克隆。
- 本项目实证参数与哈希应放在独立示例/证据小节，通用流程不硬编码项目特例。
- 现有 SOP 还应同步补充工程资料包、文件职责、HyperFrames `<audio>` 声明式接入、G10 有声预览、归档和最终检查清单，确保新方法不是孤立说明。
- 视频子项目保留了 Voicebox v0.5.0 上游源码与实际生成辅助脚本：`voicebox-work/upstream-main-generation.py`；可据此写出真实的本地 HTTP 生成接口，而无需猜测 GUI 步骤。
- Voicebox 安装目录 `D:\voicebox` 为打包二进制与数据目录；可复现文档应记录版本、服务地址/profile/engine/seed 和输出清单，不依赖机器特定安装路径。
- 已有 `ASSET_MANIFEST.md` 和 `REVIEW_LOG.md` 明确确认“曼波”参考声音已获用户授权，最终引擎是 Chatterbox Multilingual；该授权事实只作为项目示例，通用 SOP 仍要求每次重新登记。
- Voicebox v0.5.0 默认 REST 地址为 `http://127.0.0.1:17493`；`POST /generate` 支持 `profile_id`、`text`、`language`、`seed`、`engine`、`normalize` 等字段，完成状态可由生成记录/状态接口确认。
- 克隆 profile 的样本端点要求音频文件和准确 `reference_text`；因此 SOP 必须规定参考音频对应文字，不可只上传无文本样本。
- 本地源码确认 `chatterbox` 对应 `chatterbox-tts`，适合多语言零样本克隆；Qwen 是接口默认回退值，所以调用时必须显式传 `engine: "chatterbox"`，避免静默走错引擎。
- 视频子项目还保存了实际自动化脚本 `voicebox-work/generate-voicebox-narration.mjs`，下一步以它为项目示例命令依据。
- 实际生成脚本从纯文本逐行读取 7 句旁白，显式固定 `profile_id`、`language: zh`、`seed: 42917`、`engine: chatterbox`、`personality: false`、`normalize: true`；轮询历史状态，复制每段 WAV，并逐段写入时长、生成 ID、来源路径和 SHA256。
- 脚本支持只重做单段并复用已完成段，这与 G6“只重录问题句”一致；通用 SOP 应要求 manifest 支持增量重生成而不是整段覆盖。
- 实际 30 秒参考音频为 48 kHz、mono、PCM s16le；最终母带也为 48 kHz、mono、PCM s16le、60 秒。
- 没有找到保存下来的单条 FFmpeg 合成命令；SOP 可提供经过约束的通用 `adelay + amix + 两遍 loudnorm` 模板，并以 `audio-audit.json` 作为结果真值，不声称该模板是唯一实现。
- 实际 HyperFrames 接入为根合成内独立 `<audio id="voiceover" ... data-track-index="10" data-volume="1">`；必须由框架管理播放和 seek，禁止另写 `play()`/`pause()`/手动跳转脚本。
- 工程资料包应新增 `voicebox-generation.json`、`audio-audit.json`、`transcription-audit.json`、参考音频和分段 DTW 原始证据；这些是工程审计资料，不等同于对外单独交付 WAV。
- 文档内 JSON 示例 3/3 可解析，FFmpeg `aresample + adelay + amix + apad + atrim` 示例已用现有分段 WAV 实跑通过。
- Voicebox `/health`、`/health/filesystem`、`/profiles`、`/models/status` 路由均有本地源码/运行证据；当前 `chatterbox-tts` 状态为 downloaded + loaded，可在 SOP 中显式检查 `model_name` 和 `downloaded`。
- 目标 SOP 仍保持 1–27 顶层章节顺序，UTF-8 no-BOM，Markdown 围栏成对，`git diff --check` 通过。
- 最终事实断言已通过：7 个入点、Voicebox engine/model/seed、母带文件/hash、whisper 模型文件/hash、55 条 transcript/0 重叠均与文档一致。
- 首轮无上下文读者检查发现交付定义冲突：必须把“内部完整审计包”和“脱敏外发包”拆开，外发包采用白名单，且扫描 `ASSET_MANIFEST.md`、`REVIEW_LOG.md`、profile ID、本地路径、参考/分段音频与授权身份信息。
- 独立一致性审阅确认 Voicebox 示例仍缺少端到端落盘、请求/轮询超时、失败状态、音频路径解析及即时 duration/SHA256；这些属于阻塞项，必须补成可单独运行的脚本模板。
- 参考文字需要唯一真值：固定为 `narration/reference/reference.txt`，并在生成清单记录 SHA256；profile 样本或参考文字变化时，G6 及下游 G7–G12 全部失效。
- FFmpeg 合成必须提供可归档的 filter 生成器和 `mix-filter.txt`，同时检查分段重叠与叠加峰值；中间母带使用浮点 PCM，避免 `amix=normalize=0` 写入 s16 时不可逆削波。
- 母带生成后必须再次用 FFprobe、loudnorm 测量和 SHA256 验证实际产物，不能只记录预期规格。
- DTW 流程必须归档固定版本转换脚本及其输入/输出哈希，并在无有效 token span、无法单调映射或识别增删冲突时阻断自动生成，转人工复核或重生成音频。
- HyperFrames Core 明确规定 `class="clip"` 只用于可视定时元素；`<video>` 与无视觉的 `<audio>` 是例外。SOP 应写明该例外，不能给 `<audio>` 误加 `class="clip"`。
- Learn Heartstone 的历史任务内授权不能冒充当前通用门禁通过；应补建独立 `AUTHORIZATION.md`，或将该实例明确标记为 legacy 非完整合规证据。
- 最终无上下文读者测试仍判定 FAIL：profile 预检必须证明 Voicebox 实际 sample ID、音频哈希和参考文字哈希与项目证据完全一致，不能只检查 `sample_count`。
- 同一 `voicebox-generation.json` 不得混入不同 profile、reference、Voicebox 版本、engine/model/language/seed；旧批次参数不一致时必须停止并新建批次，每段也要复制保存这些参数。
- 混音前必须核对锁定脚本的完整段数/索引集合、逐段文本、实际 WAV SHA256 和媒体规格；缺段、替换文件、零时长或非预期 PCM 均应阻断。
- 两遍 loudnorm 不能依赖手填测量值；应由脚本解析第一遍 JSON、执行第二遍、复测最终 LUFS/true peak、应用数值容差并生成 `audio-audit.json`。
- DTW 转换器必须同时读取 generation manifest、锁定脚本和母带时长，校验段全集、唯一索引、`scene_start_s` 一致、全文一致和最终不越界，避免“只校验已列 map”造成漏段静默通过。
- `PUBLIC_ALLOWLIST.txt` 必须是外发文件唯一真值，BRIEF 只记录授权政策；源码协作包需独立 allowlist、空目录构建和同等级扫描门。
- 内部审计包不得在被复制的工程根内递归构建；交付根应位于工程根之外，或复制时显式排除所有 deliverables 目录并验证边界。
- 通用安装段仍需补充 Voicebox 与 whisper.cpp 的固定版本来源、安装/获取步骤及校验策略；实例证据路径要明确标注路径基准。
- 从正文 261–265 行发现 Learn Heartstone 录制示例仍含“三连/金色描述”和“5本→搜索布莱恩”；这与当前成片已删除铜须/三连章节的批准版本不一致，应改为当前仍保留的功能证明并顺延素材命名。
- 稳定段 WAV 必须先在 `.partial` 上完成媒体规格、时长和哈希验证，再替换正式文件；失败时旧有效 WAV 与旧 manifest 记录必须保持一致。Voicebox 返回路径还要做目录分隔符边界校验。
- 生成脚本需固定为串行写 manifest，并使用临时 JSON 原子替换；POST 后至少查询一次 history，`generation_id` 以已确认存在的 job ID 为准。
- 外发 allowlist 还必须拒绝空清单、绝对路径、`..`、根外路径和空包，强制包含最终 MP4 与 SHA256；任何 narration 工作目录、归档文件和独立音频都应按绑定授权记录的策略阻断。
- 当前 loudnorm 段仍含 `<I>/<LRA>/<TP>/<THRESH>/<OFFSET>` 手填占位符，且 PowerShell 5.1 重定向可能产生非 UTF-8 证据；应由一个固定 PS1 在内存解析 FFmpeg stderr 并写 UTF-8 no-BOM JSON。
- 当前 `dtw-map.json` 复制 `scene_start_s`，形成第二真值；map 应只引用段 index/raw/items，转换器从 generation manifest 取入点并 FFprobe 母带上界。
- 第 1059–1062 行素材命名示例也残留 `minion-golden` 与 `brann-tier5-search`，应与已批准的当前视频功能集合同步移除。
- composition 总时长当前分散在 BRIEF、混音参数默认 60、调用示例和 HTML `data-duration`；规范应指定 BRIEF/工程根属性的校验关系，并要求命令显式传入，不保留静默默认值。
- Confidence Check 通过：未发现另一套可复用的完整审计脚本；当前方案沿用既有 PowerShell/Python/FFmpeg/HyperFrames 合同；Voicebox/whisper.cpp 官方发布和本地源代码均已核对；根因明确，实施置信度 100%。
- Voicebox 官方 v0.5.0 Windows setup 来源为 `jamiepine/voicebox` Release，GitHub digest `eaf5410e...b3b8`；本机 `voicebox.exe` ProductVersion/FileVersion 均为 0.5.0。
- whisper.cpp 官方 v1.9.1 `whisper-bin-x64.zip` digest 为 `7d8be46e...3539`；官方 Hugging Face `ggml-small.bin` LFS SHA256 为 `1be3a9b2...987b`，与本项目实证一致。
- Voicebox v0.5.0 提供 `GET /profiles/{profile_id}/samples`，响应含 sample `id`、`audio_path`、`reference_text`。上传后 Voicebox 会用 librosa 转 24 kHz、去 DC、裁边和峰值限制，因此源 WAV 文件哈希不能与内部 sample WAV 直接比较；严格做法是项目专用 profile 上传回执 + sample ID + 源/存储双哈希 + reference text 规范化哈希。
- SOP 最终采用 `profile-binding.json`、generation context hash、不可变分段 WAV、自动两遍 loudnorm/audio audit、manifest 驱动 DTW 和独立交付 allowlist，形成可复用的 Voicebox/Chatterbox 审计链。
- 公开包中的 ISO BMFF 文件改由 FFprobe 判断流类型；普通同时含视频和音频流的 MP4 不再被误判为独立音频。
- 内部审计包、公开包和按需源码协作包必须在工程根外物理隔离；授权到期/撤回时保存不含声音样本的最小化处置回执。
