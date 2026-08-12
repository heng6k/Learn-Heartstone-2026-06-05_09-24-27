# Unity WebGL 网页版上线专项规范

## 文档状态

- 状态：历史发布前审计；其中“平台未定、构建待执行”的结论已于 2026-08-12 关闭，不再作为当前上传操作规范。
- 日期：2026-07-16。
- 受众：项目负责人、Unity 工程、网页托管和测试人员。
- 目标：明确当前项目从 Unity 运行时代码到浏览器可访问作品之间还缺少什么，并建立可重复的 WebGL 发布门禁。
- 关联代码提交：`614fdd3 feat: complete recruit deathrattle and reborn pipeline`。

> 当前上传必须先阅读 [三渠道统一上传要求](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md) 和 [Cloudflare Pages WebGL 发布指南](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md)。现行平台为 Cloudflare Pages 项目 `learn-heartstone`，正式域名为 [https://jsoncool.com](https://jsoncool.com)；本文件保留 2026-07-16 当时的审计上下文，不得用其中“托管未确定”“压缩关闭”“浏览器验收未执行”等历史状态覆盖 2026-08-12 已上线基准。

## 结论摘要

本项目的死亡、亡语和 Reborn 运行时代码已经具备 WebGL 使用条件，且已通过 Unity EditMode 相关回归。当前提交不是网页版构建产物，也没有完成浏览器加载和托管验证，因此不能直接视为“网页版已上线”。

当前判断：

- 运行时功能：可进入 WebGL 构建阶段。
- Unity WebGL 工具链：已安装。
- WebGL 构建产物：未生成或未纳入当前仓库。
- 网页托管：尚未确定。
- 浏览器验收：未执行。
- 全仓库测试：仍有既有目标缺失失败，不能宣称全绿发布。

## 目标与非目标

### 本专项包含

- Unity WebGL 构建设置和工具链检查。
- 网页静态资源、缓存、压缩和响应头要求。
- 浏览器加载、窗口缩放、刷新恢复和核心玩法验收。
- 准备阶段死亡、亡语、Reborn 在 WebGL 中的专项回归。
- 发布前阻断项、已知基线问题和上线后回滚要求。

### 本专项不包含

- 不把 Windows 可执行包当作网页版交付物。
- 不在没有确定托管平台的情况下擅自选择 Vercel、GitHub Pages 或其他平台。
- 不在本专项中修复与死亡/亡语无关的目标缺失测试。
- 不修改当前已提交的玩法语义，仅验证其能否进入 WebGL 发布链路。

## 当前环境证据

| 检查项 | 当前结果 | 结论 |
|---|---|---|
| Unity 版本 | `6000.4.10f1` | 固定构建版本，发布环境必须一致 |
| WebGL 模块 | `Editor/Data/PlaybackEngines/WebGLSupport` 存在 | 工具链已安装 |
| 构建场景 | `Assets/Scenes/SampleScene.unity` | 当前唯一启用场景，引用有效 |
| WebGL 模板 | `APPLICATION:Default` | 可构建，但没有项目专用网页外壳 |
| 链接目标 | WebAssembly | 浏览器目标正确 |
| WebGL 线程 | 关闭 | 兼容性较好，避免跨源隔离要求 |
| 数据缓存 | 开启 | 适合重复访问，但必须处理版本更新 |
| 文件哈希 | 关闭 | 有旧资源缓存风险 |
| 压缩 | 关闭 | 兼容性简单，但首屏体积较大 |
| WebGL 构建产物 | 未发现 | 还没有可部署的网页包 |
| 部署配置 | 未发现 | 需要根据托管平台补充 |

对应配置位于：`ProjectSettings/ProjectSettings.asset` 的 `webGL*` 字段。

## 已确认的玩法基线

以下内容已经由 Unity 运行时测试锁定，不应因切换 WebGL 而改变：

- Reborn 与战斗阶段、准备阶段共用同一实体构造语义。
- Reborn 保留原卡描述、当前攻击、最大生命、永久附魔和应保留计数器。
- Reborn 消耗 Reborn 关键词、生成新 `InstanceId`、当前生命通常为 1，并重置瞬时战斗状态。
- 亡语及连锁召唤先结算，随后 Reborn，最后才尝试 Archlich exact copy。
- 每次召唤独立检查七格上限，不预留位置、不挤占已有随从。
- Warghoul 只触发一个合法相邻随从亡语。
- `Butchering`、`Jailer Sticker`、`Disguised Graverobber`、`Tomb Turning` 和 Archlich 已接入准备阶段死亡管线。

最近验证的重点回归包括：

- `DomainEngineTests`：49/49。
- `CombatMechanicTests`：3/3。
- `TierFiveAcceptanceTests`：15/15。
- `MatchServiceTests`：189/189。
- `TrinketSystemTests`：225/225。
- 回放、Tomb Turning、Disguised Graverobber 和其他死亡链专项通过。

## WebGL 当前风险

### P0：没有网页构建产物

仓库当前只有 Windows 构建包，没有 `Builds/WebGL` 或等价目录。没有产物就无法验证：

- Unity loader 是否能启动。
- `.wasm`、`.data`、`.framework.js` 是否完整。
- 浏览器是否能正确解压和执行。
- 资源路径是否区分大小写。
- 首次加载和刷新后缓存是否正常。

发布门禁：必须先生成可独立托管的 WebGL 输出目录，并保存构建日志。

### P1：文件哈希关闭

当前 `webGLNameFilesAsHashes: 0`。网页部署后，浏览器或 CDN 可能继续使用上一版 `.data`、`.wasm` 或 framework 文件。

建议：正式发布启用文件哈希，或由托管层提供严格的版本化目录和缓存失效策略。二者必须选定一个，不能依赖手工清缓存。

### P1：压缩策略尚未确定

当前 `webGLCompressionFormat: 0`，即不压缩。这样最容易兼容静态服务器，但下载体积会明显增大。

若改用 Gzip 或 Brotli，托管层必须正确返回：

- `.wasm`：`application/wasm`。
- `.js`：`application/javascript`。
- `.data`：`application/octet-stream`。
- 压缩文件：对应的 `Content-Encoding`。
- 静态资源：正确的跨域和缓存策略。

在平台未确定前，不应只修改 Unity 压缩选项而不准备服务器响应头。

### P1：没有网页外壳

默认模板缺少项目专用的：

- 响应式画布尺寸处理。
- 加载进度和加载失败提示。
- 浏览器标签页标题和 favicon。
- 移动端 viewport 设置。
- 版本号显示和反馈入口。

默认模板可以用于内部验收，不建议直接作为公开发布页面。

### P1：完整测试仍有既有基线失败

完整回归中仍存在目标缺失问题：

- `Volcanic Visitor Attack needs a target`。
- `防御者的仪式 needs a target`。
- `鲜血宝石 needs a target`。

这些失败不是本次死亡/亡语/Reborn 改动引入，但在发布报告中必须单独列出，不能把“功能专项通过”写成“全项目测试全绿”。

### P2：没有浏览器运行证据

Unity 编辑器内的 EditMode 测试不能替代浏览器验收。WebGL 特有问题包括资源加载、浏览器缓存、WebAssembly 内存、输入焦点、窗口缩放和移动端触控。

## 待确定项（不应由代码审计代替决策）

以下事项目前没有足够的项目配置或用户要求可以直接定案，必须在生成正式网页包前明确记录：

| 事项 | 当前状态 | 需要确认的决定 |
|---|---|---|
| 托管平台 | 未指定 | 选择静态托管平台，并确认是否支持自定义 MIME、`Content-Encoding`、缓存头和单页入口回退 |
| 版本与缓存 | 尚未定案 | 选择 Unity 文件哈希，或选择版本化目录；同时定义 `index.html` 与构建资源的缓存时长 |
| 压缩方式 | 当前关闭 | 维持不压缩，或由托管平台配合 Gzip/Brotli 响应头后再启用 |
| 公开访问范围 | 未指定 | 确认是内部预览、测试链接还是公开发布，并据此确定访问控制和错误反馈 |
| 浏览器/设备范围 | 未指定 | 至少确认桌面浏览器版本、是否支持移动端，以及最低可接受的 WebAssembly 内存和加载时间 |
| 发布回滚 | 未指定 | 确认保留的上一版本目录、回滚入口和构建元数据保存位置 |

在这些决定完成前，本专项文档只能作为“发布前审计规范”，不能作为某个平台的部署配置说明。

## 推荐构建契约

### 构建前

1. 使用固定 Unity 版本 `6000.4.10f1`。
2. 确认当前分支包含目标提交 `614fdd3`。
3. 确认 `Assets/Scenes/SampleScene.unity` 仍是启用场景。
4. 清理旧的 WebGL 输出目录，避免残留文件误部署。
5. 记录构建时间、Git commit、Unity 版本和 WebGL 设置。

### 输出目录

推荐目录：

```text
Builds/WebGL/LearnHeartstone_<version>/
├── index.html
├── Build/
│   ├── *.loader.js
│   ├── *.framework.js
│   ├── *.wasm
│   └── *.data
└── TemplateData/
```

实际文件名以 Unity 生成结果为准；部署脚本不能硬编码旧版本文件名。

### 版本与缓存

- 正式发布推荐启用文件哈希，或使用版本化 URL 目录。
- `index.html` 可以短缓存或不缓存。
- `.wasm`、`.data`、`.framework.js` 可以长期缓存，但必须随版本变化。
- 不允许把旧版本和新版本的 `Build/` 文件混合上传。

### 服务端

静态服务器至少应正确处理：

```text
application/wasm              *.wasm
application/javascript        *.js
application/octet-stream     *.data
```

如果启用 Gzip/Brotli，必须同时返回正确的 `Content-Encoding`。若托管平台无法自定义响应头，应保持 Unity 压缩关闭，或使用平台明确支持的压缩方案。

## 浏览器验收矩阵

### 加载与资源

| 用例 | 预期 |
|---|---|
| 首次打开页面 | 显示加载状态，最终进入主界面，无白屏 |
| 刷新页面 | 能重新加载，不出现旧 loader 与新 data 混用 |
| 强缓存后发布新版本 | 新版本资源能生效 |
| 资源 404 或响应头错误 | 显示可理解的失败提示，而不是永久卡在加载中 |
| 浏览器控制台 | 无 WebGL 初始化、CORS、MIME 或解压错误 |

### 玩法核心

| 用例 | 预期 |
|---|---|
| 普通购买、出售、刷新、下一回合 | 与 Unity 编辑器行为一致 |
| 准备阶段消灭带亡语随从 | 亡语、连锁死亡和 Reborn 顺序正确 |
| Reborn 带永久附魔 | 附魔保留，生命为 1，Reborn 消耗 |
| 全局亡灵/甲虫 Buff | Reborn 只重新应用一次，不重复叠加 |
| Warghoul 相邻亡语 | 只触发合法相邻目标 |
| Archlich + Reborn | Reborn 与 exact copy 按空间和顺序结算 |
| 回放和日志 | 新实体 ID、事件顺序和棋盘快照可显示 |

### 设备与交互

- Chrome、Edge、Firefox 最新稳定版至少各验收一次。
- 桌面 1920×1080、1366×768 和窄窗口各验收一次。
- 移动端或窄屏至少验证画布不溢出、按钮可点击、文字不遮挡。
- 鼠标悬停、点击、滚轮和键盘输入不能被页面外壳吞掉。
- 页面切后台再回来后，游戏不能卡死或重复初始化。

## 发布门禁

### 必须通过

- WebGL 构建成功，无编译错误。
- 构建目录中 loader、framework、wasm、data 文件完整。
- 静态服务器返回正确 MIME 和压缩头。
- 首次加载、刷新、缓存更新通过。
- Reborn/亡语核心浏览器用例通过。
- 浏览器控制台无未解释的 Error、CORS、MIME 或 WebAssembly 错误。
- 发布包记录 Git commit 和 Unity 版本。

### 可以带已知问题发布，但必须显式标注

- 目标缺失测试失败，但不影响网页核心路径。
- 未实现的非核心卡牌或战斗专属细节。
- 低端设备加载较慢，但能够完成加载并进入玩法。

### 不得发布

- WebGL 构建失败或产物不完整。
- 页面白屏、永久加载或刷新后资源错配。
- Reborn/亡语在浏览器中与 Unity 编辑器结果不一致。
- 控制台出现未定位的运行时异常。
- 依赖本地文件路径或 Unity 编辑器才能工作的功能。

## 当前发布结论

当前可以给出“运行时代码已具备 WebGL 兼容基础”的结论，不能给出“网页版已上线”的结论。

下一步顺序固定为：

1. 确定托管平台。
2. 决定文件哈希和压缩策略。
3. 生成 WebGL 构建产物。
4. 使用平台静态服务器部署临时预览。
5. 执行浏览器验收矩阵。
6. 单独记录并标注既有目标缺失测试。
7. 通过发布门禁后再生成正式网页版本号和发布说明。

## 相关文件

- [准备阶段死亡与亡语补全规范](</D:/unity project/Learn Heartstone/Docs/RecruitPhaseDeathAndDeathrattleCompletionSpec.zh-CN.md>)
- [Unity WebGL 项目设置](</D:/unity project/Learn Heartstone/ProjectSettings/ProjectSettings.asset:793>)
- [构建场景设置](</D:/unity project/Learn Heartstone/ProjectSettings/EditorBuildSettings.asset:1>)
- [功能提交 614fdd3](</D:/unity project/Learn Heartstone/Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs:2029>)
