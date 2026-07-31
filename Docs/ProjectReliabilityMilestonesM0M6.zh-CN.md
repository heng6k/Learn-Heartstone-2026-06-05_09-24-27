# M0–M6 可靠性与发布模块完成度说明

首次记录：2026-07-31

## 1. 文档目的

本文只回答四个问题：

1. M0–M6 每个模块完成到什么程度。
2. 每个模块具体通过哪些代码、工具和流程完成。
3. 当前可复核证据在哪里。
4. 日常修复需要复用哪些模块、哪些模块不应重复实施。

详细操作命令仍以 [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md) 为准；架构背景与剩余风险仍以 [ProjectReliabilityArchitectureCompletion.zh-CN.md](ProjectReliabilityArchitectureCompletion.zh-CN.md) 为准。本文不替代这两份文档。

## 2. 总体结论

`M0 → M1 → M2 → M3 → M4 → M5 Preview → M6 Production / 停止跟踪 WebDeploy` 主链已经完成并实际跑通。

- M0、M1、M2：既定范围完成。
- M3、M4：协议 v1 范围完成；协议 v1 当前只远程交付随从 JSON。
- M5、M6：发布能力和 2026-07-29 基线完成；每次新版本仍必须重新执行 Preview、Promote 和线上 smoke，这属于使用既有模块，不是重做模块建设。
- 普通 UI、玩法或 Bug 修复不得重新设计 M0–M4；只需通过既有门禁，生成新的 ReleaseCandidate，再走 M5–M6。

## 3. 完成度总表

| 模块 | 完成度 | 已完成范围 | 未包含或后续再做 |
| --- | --- | --- | --- |
| M0 可信测试基线 | 100% | 官方发现、固定分片、XML 对账、普通/Stress/Marathon 分离、PlayMode 旅程和可追溯证据已建立；历史红线已被当前绿线取代 | 每次修改仍运行与风险相称的测试；30 分钟 Marathon 不属于普通发布门禁 |
| M1 发布边界 | 100% | 真源、WebGL 输出、ReleaseCandidate、Vercel 配置和退役生成物边界已固定 | 新平台或新的托管商需要另立发布边界 |
| M2 Resources 会话快照 | 100% | 启动时生成中英双语 `GameCatalogSnapshot`，同一快照注入整次会话，不在运行中随处重读 Resources | 兼容构造路径仍可直接加载 Resources，但不得新增平行生产加载器 |
| M3 内容协议 | v1 范围 100% | manifest、客户端版本、内容版本、文件名、大小、SHA-256、严格 UTF-8 和随从 JSON 解析校验已完成 | 多文件/多内容类型、离线签名或兼容语义变化应设计协议 v2 |
| M4 Remote/LKG/Embedded 回退 | v1 范围 100% | 同源 Remote → 本地 LKG → 内置 Embedded 的确定性选择、校验和原子持久化已完成 | LKG 历史清理器、内容离线签名尚未实现，目前不阻断 Alpha |
| M5 Vercel Preview | 100% | 冻结 ReleaseCandidate、项目 dry-run、Preview、Remote/LKG/Embedded、Brotli/MIME、缓存、SPA、安全头和浏览器 smoke 已跑通 | 每次新 ReleaseCandidate 都要产生并重新验收一个 Preview |
| M6 Production / 停止跟踪 WebDeploy | 100% | 同一 Preview Promote、Production smoke、`jsoncool.com` DNS/HTTPS、WebDeploy 退役和 0 tracked 生成物已完成 | 每次上线仍需 Promote 同一已验 Preview；不能用另一目录重新 `deploy --prod` |

## 4. 模块如何连接

```text
M0 测试结果可信
  ↓
M1 只允许真源进入 Git，构建输出进入冻结 ReleaseCandidate
  ↓
M2 客户端启动时形成一个不可变会话快照
  ↓
M3 远程内容必须通过协议、版本、字节与哈希校验
  ↓
M4 按 Remote → LKG → Embedded 选择可用快照
  ↓
M5 用冻结候选部署 Preview 并完成线上 smoke
  ↓
M6 Promote 同一 Preview 到 Production，jsoncool.com 复验
```

这条链中，M0–M4 是长期基础设施，M5–M6 是每次发布调用的固定门禁。

## 5. 各模块具体如何完成

### M0：可信测试基线

实现方式：

- `BatchEditModeTestRunner` 使用 Unity/NUnit 实际发现的叶级 full name 建立 manifest。
- 普通 EditMode 使用固定分片执行，并将 manifest 与结果 XML 对账，检查缺失、额外和重复项。
- Stress 与唯一显式 Marathon 分开；PlayMode 以真实输入旅程执行。
- 大型同步 UI 分片若只出现 NUnit Timeout，先在干净域隔离精确失败项，不提高超时、不修改业务逻辑掩盖问题。

当前证据：

- M0 历史红线：普通 EditMode 1505、Stress 10、PlayMode 19 均完整发现并可复核。
- 2026-07-29 当前绿线：M2–M4 精确集 11/11、普通 EditMode 有效 1516/1516、Stress 10/10、PlayMode 19/19。
- 详细记录：[testing/test-suite-overview.zh-CN.md](testing/test-suite-overview.zh-CN.md)。

### M1：发布边界

实现方式：

- Unity、C#、Prefab、测试和 WebGL Template 保留在源码真源。
- `Assets/LearnHearthstone/Editor/WebGLReleaseBuild.cs` 负责生成 `Builds/WebGL/<版本>/`。
- `Tools/Release/assemble-release-candidate.mjs` 只接受 WebGL 输出，生成 `Builds/ReleaseCandidate/<版本>/`，写入 `release-meta.json`、内容包并复制唯一 Vercel 配置。
- `Deploy/Vercel/vercel.json` 是 MIME、Brotli、缓存、安全头和 SPA rewrite 的唯一配置真源。
- `.gitignore` 排除 `Builds/ReleaseCandidate/`、`Builds/ReleaseReceipts/`、`WebDeploy/` 和 `.vercel/` 等生成状态。

完成标志：Preview 与 Production 只能来自同一个已冻结 ReleaseCandidate，源码 push 与 Vercel 部署已经解耦。

### M2：Resources 会话快照

实现方式：

- `GameCatalogSet` 聚合随从、法术、英雄、饰品、任务、时空酒馆、畸变和暗月奖品目录。
- `GameCatalogSnapshot` 同时持有中英文目录，并记录内容版本、客户端版本、来源、源码提交和加载时间。
- `EmbeddedGameCatalogSnapshotLoader` 将内置 Resources 解析为快照。
- `LearnHearthstoneBootstrap` 在启动时解析一次，然后把同一快照注入 MatchService 和主要 UI 路径。

完成标志：快照选定后本次会话不热切换；切换语言使用同一快照中的另一个 `GameCatalogSet`，不重新建立一套内容来源。

### M3：内容协议 v1

manifest 当前包含：

- `protocolVersion`
- `contentVersion`
- `requiredClientVersion`
- `generatedAtUtc`
- 随从文件的 `fileName`、`bytes`、`sha256`

`ContentPackageValidator` 会拒绝：

- 不支持的协议或客户端版本。
- 不安全的版本号和文件名。
- 超过大小限制、字节数不一致或 SHA-256 不一致的内容。
- UTF-8 BOM、非法 UTF-8 或不能解析的随从 JSON。

`assemble-release-candidate.mjs` 使用同一规则生成版本化内容文件和 manifest，因此生产者与消费者的约束一致。

### M4：Remote → LKG → Embedded

实现方式：

1. WebGL 从当前页面同源下载 `content/content-manifest.json` 和版本化随从文件。
2. Remote 只有完整通过 M3 校验才成为当前会话快照。
3. 有效 Remote 会原子提升到 `Application.persistentDataPath/Content/LKG`。
4. Remote 不可用或被拒绝时，LKG 会再次经过完整校验后加载。
5. Remote 与 LKG 都不可用时，加载内置 Resources。
6. 相同 `contentVersion` 出现不同字节时保留原 LKG，不允许静默覆盖。

主要实现：

- `RemoteContentPackageDownloader`
- `ContentPackageValidator`
- `LastKnownGoodContentRepository`
- `GameCatalogSnapshotResolver`
- `EmbeddedGameCatalogSnapshotLoader`

### M5：Vercel Preview

实现方式：

- 先对冻结候选执行 Vercel dry-run，明确 `Team=heng6ks-projects`、`Project=hengheng`、项目根目录为 `null`，防止再次落到旧 `WebDeploy` 根目录或错误项目。
- 只创建 Preview，不直接部署 Production。
- Preview 必须验证 Remote、LKG、Embedded、Brotli/MIME、缓存、SPA 深链、五个安全头和浏览器 Console。

2026-07-29 已验 Preview：`dpl_Ps3FHzViFirA15L87jELqP82pbJR`。

### M6：Production 与 WebDeploy 退役

实现方式：

- 只使用 `vercel promote` 晋升已经验收的同一 Preview deployment。
- Production 后在稳定 Vercel 域名和 `https://jsoncool.com/` 重跑完整 smoke，并核对关键资源与 Preview 一致。
- Vercel Git Integration 已断开，Git push 不会自动部署错误项目。
- 提交 `69ce401` 删除全部已跟踪 `WebDeploy/**` 文件；当前 `git ls-files -- WebDeploy/** Builds/**` 无输出。
- `jsoncool.com` 使用 apex A 记录 `76.76.21.21`，Vercel 域名与 HTTPS 已验证。

2026-07-29 已验 Production：`dpl_GBFSeFEwnjN3XEYqaPeFkt92X6pV`，由上述 Preview Promote 得到。

## 6. 日常修复时不需要重做什么

普通 UI、玩法和 Bug 修复按以下方式使用现有模块：

1. 修改对应真源。
2. 使用 M0 的现有测试门禁验证。
3. 使用 M1 的现有构建与组装工具生成新候选。
4. M2–M4 没有协议或内容来源变化时不改代码，只做相关回归。
5. 执行 M5 Preview。
6. Preview 通过后执行 M6 Promote，并在 `jsoncool.com` 复验。

不要在每次发布时重复：

- 新建另一套快照、协议或回退器。
- 恢复 `WebDeploy` 为部署真源。
- 重新编写 M0–M6 阶段划分。
- 从另一个目录重新部署 Production。
- 仅凭当前文件夹名称判断 Vercel 项目。

## 7. 何时才需要重新打开模块设计

只有发生以下变化时，才重新设计对应模块：

- 增加第二类远程内容文件或改变兼容语义：重新设计 M3 协议版本。
- 改变会话内热更新策略：重新评估 M2 与 M4。
- 加入离线签名、密钥轮换或 LKG 清理策略：扩展 M3/M4。
- 更换 Vercel Team、Project、Root Directory、域名或托管商：重新核准 M1/M5/M6。
- 恢复 Git 自动部署：重新评估 M1 与 M6 的发布隔离。

## 8. 提交与证据索引

| 阶段 | 关键提交 |
| --- | --- |
| M0–M1 | `3979338 chore: establish M0-M1 release baseline` |
| M2 | `587e590 feat: add M2 session catalog snapshot` |
| M3 | `801abca feat: add M3 content package protocol` |
| M4 | `391e850 feat: add M4 remote content fallback` |
| M6 / WebDeploy 退役 | `69ce401 chore: retire legacy WebDeploy publishing` |
| M6.1 安全头 | `612e984 chore: add M6.1 WebGL security headers` |
| Phase 6 交接收尾 | `fc42087 docs: close Phase 6 delivery handoff` |

相关主文档：

- [testing/test-suite-overview.zh-CN.md](testing/test-suite-overview.zh-CN.md)
- [ProjectReliabilityArchitectureCompletion.zh-CN.md](ProjectReliabilityArchitectureCompletion.zh-CN.md)
- [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md)
