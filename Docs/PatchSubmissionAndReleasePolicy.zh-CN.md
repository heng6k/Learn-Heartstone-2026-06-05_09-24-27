# 补丁提交与发布处理规范

> 状态：当前执行规范
> 生效日期：2026-07-30；Cloudflare 基准更新：2026-08-12
> 适用范围：Learn Hearthstone 的内容数据、Unity 客户端、WebGL 外壳、Cloudflare Pages 配置、重大功能版本与线上故障处理

## 1. 文档目的

本文规定“一个修改应该按什么类型提交、需要走哪些门禁、怎样上线以及怎样回滚”。具体构建和 Cloudflare Pages 命令以 [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md) 为准，三类交付面与分块提交以 [ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md) 为准，本文不复制操作手册。

本文主要解决四个问题：

1. 大版本是否需要先关闭网站。
2. 小 Bug 是否可以直接修改 Production。
3. 哪些修改需要重新构建 Unity WebGL。
4. 提交补丁需求时必须给出哪些信息和证据。

## 2. 核心结论

- 默认不停服。旧 Production 在新版本开发、构建和 Preview 验收期间继续服务。
- Production 不允许手工改文件；任何会影响线上行为的修改都必须先形成可审计真源和 ReleaseCandidate。
- 所有线上变更至少经过 Preview；重大版本必须经过完整测试和线上 smoke 后才能部署 Production。
- Cloudflare Pages 直传会创建独立的 Preview 和 Production deployment；两者必须使用同一最终源 SHA 和同一份冻结 `WebApp/dist`，不能为 Production 重新构建一个“看起来相同”的包。
- 每次发布都保留上一已知良好 deployment 作为回滚点。
- 同一个 `contentVersion` 不得对应不同字节；内容修复必须使用新版本号。
- 已经打开游戏的会话保持自己的内容快照；新内容或新客户端只在刷新或新会话中生效。
- Git 提交、push、tag 与线上部署是不同授权动作，不因完成其中一个而自动获得其他动作的权限。

## 3. “热补丁”的准确含义

本项目可以为用户提供近似无停机更新，但不采用运行中注入代码的传统热修复。

| 说法 | 本项目中的真实含义 |
| --- | --- |
| 内容热更新 | 发布新的 manifest 和版本化 JSON；当前会话不切换，下次启动读取新内容 |
| Unity 小补丁 | 重新构建 WebGL，部署 Preview；放行后从同一冻结 `dist` 创建 Production，旧 Production 在切换前一直可用 |
| Web 配置热修 | 复用已验 WebGL 输出组装新候选，先验 Preview，再从同一冻结 `dist` 部署 Production |
| 直接热改 Production | 禁止；这会破坏真源、审计、复现和回滚 |

因此，“小补丁可以快速上线”是正确的；“小补丁可以跳过 Preview 或直接改线上文件”是不正确的。

## 4. 变更分类

提交需求时先选择一个主类型。如果一个补丁同时命中多个类型，按风险最高的类型执行，或拆成相互独立的补丁。

| 类型 | 典型内容 | 是否重建 Unity | 最低发布路径 |
| --- | --- | --- | --- |
| `DOC` 文档 | 说明、计划、索引、测试记录 | 否 | Markdown/链接检查；通常不部署游戏 |
| `CONTENT` 兼容内容补丁 | 文字、翻译、兼容 JSON、随从数值 | 否 | 新 `contentVersion` → 候选 → Preview → 内容/回退 smoke → Production |
| `WEB` 网页或部署配置 | WebGL Template、Cloudflare headers/Functions、缓存、MIME、SPA 外壳 | 视情况而定 | 候选 → Preview → 浏览器完整 smoke → Production |
| `CLIENT` Unity 客户端补丁 | C#、Prefab、场景、Unity UI、序列化资源 | 是 | 相关测试 → WebGL 构建 → 候选 → Preview → smoke → Production |
| `FEATURE` 规则大更新或新功能 | 新机制、新模式、协议变化、跨模块行为 | 通常是 | 独立版本计划 → 全量门禁 → 新候选 → Preview → 完整验收 → Production |
| `INCIDENT` 线上事故 | 白屏、启动失败、数据破坏、安全问题、错误 deployment | 先回滚，后判断 | 回滚已知良好 deployment → 恢复服务 → 按上述类型修复 |

### 4.1 内容补丁的进一步划分

- 纯文字或描述：不改变规则语义时，可以缩小到解析、内容协议、本地化和浏览器内容链测试。
- 数值、随从池或规则数据：必须补相关 Domain/Data 测试；若可能改变战斗、经济或发现结果，升级到完整普通 EditMode 和相关 Stress 门禁。
- 需要新 C# 才能理解的数据：不再是纯 `CONTENT`，应按 `CLIENT` 或 `FEATURE` 处理。
- 新增第二类远程文件、改变 manifest 兼容语义或旧客户端可能误读：需要单独设计协议版本，禁止把它伪装成普通 JSON 补丁。

## 5. 版本规则

### 5.1 客户端版本

当前仍处于 Alpha 时使用语义化版本：

本文口语中的“大版本”是指重大功能或规则发布，不等同于语义化版本中的 `MAJOR`。

- `PATCH`：兼容的小修复，例如 `0.1.0-alpha` → `0.1.1-alpha`。
- `MINOR`：新规则、新模式或显著能力，例如 `0.1.0-alpha` → `0.2.0-alpha`。
- `MAJOR`：正式 1.0 之后才用于明显不兼容的产品级变化。

除非当前稳定版本已经是 `1.0.0`，否则不要把一次大更新命名为 `1.0.1`；`1.0.1` 通常表示 1.0 正式版的小补丁。

### 5.2 内容版本

- `contentVersion` 使用新的、单调可区分的版本值，例如日期版本 `20260730`。
- 已发布版本号永不覆盖；即使只改一个字，也必须产生新版本。
- manifest 的字节数和 SHA-256 必须与版本化内容文件一致。
- 不兼容内容必须通过 `requiredClientVersion` 阻止旧客户端接受。
- 内容真源仍为 `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json`；版本化文件只属于发布候选和线上生成物。

## 6. 标准处理流程

```text
需求分类与复现
    ↓
冻结范围、版本和禁止改动
    ↓
修改唯一真源并补直接测试
    ↓
按风险执行扩展门禁
    ↓
创建干净源码检查点
    ↓
构建或复用 WebGL，组装 ReleaseCandidate
    ↓
部署 Cloudflare Pages Preview
    ↓
功能、内容、浏览器与安全 smoke
    ↓
从同一冻结 dist 创建 main Production deployment
    ↓
Production 复验并保留上一回滚点
```

### 6.1 提交前

- 写清问题、期望结果、复现步骤和用户影响。
- 确认主类型、目标客户端版本、目标 `contentVersion` 和是否需要协议升级。
- 列出允许修改与禁止修改的目录。
- 检查工作树中的其他改动，禁止把无关文件混入补丁。
- 先确认根因；不能因为某项测试超时就直接提高 Timeout 或改业务逻辑。

### 6.2 实施和测试

- 优先修改已有真源和复用已有实现，不新增平行加载器、发布脚本或第二套配置。
- 先运行最直接的测试，再按共享范围升级门禁。
- Unity 测试使用当前单一 Editor 和既有测试入口，不启动同项目第二实例。
- 任何候选都必须来自干净检查点，`sourceDirty=false`。
- `Builds/**`、`WebDeploy/**`、manifest 和版本化内容不得进入源码 Git。

### 6.3 Preview 与 Production

- Preview 必须验证修改本身，并复验它可能影响的 Remote → LKG → Embedded、缓存、Brotli/MIME、SPA 和安全头。
- 重大版本和 Unity 客户端补丁必须实际启动 WebGL，不能只检查 HTTP 200。
- Preview 失败时修真源、创建新检查点并生成新候选；禁止原地修改冻结候选。
- Production 只能使用已通过 Preview 的同一源 SHA 和同一冻结 `dist`。
- Production 部署后立即验证正式域名、版本身份和关键资源响应头/长度。

## 7. 最低测试门禁矩阵

下表给出最低要求。修改共享状态、战斗、经济、回合、内容协议或 Bootstrap 时必须向更高门禁升级。

| 门禁 | DOC | CONTENT | WEB | CLIENT | FEATURE |
| --- | --- | --- | --- | --- | --- |
| Markdown/配置解析与 `git diff --check` | 必须 | 必须 | 必须 | 必须 | 必须 |
| 直接相关 EditMode | 不适用 | 必须 | 视配置而定 | 必须 | 必须 |
| 内容协议与 Remote/LKG/Embedded | 不适用 | 必须 | 影响内容路径时必须 | 影响内容路径时必须 | 必须 |
| 普通 EditMode | 不适用 | 规则数据变化时必须 | 通常不需要 | 按影响范围，核心改动全量 | 必须全量 |
| Stress（排除 Marathon） | 不适用 | 战斗/经济风险时 | 不需要 | 战斗/性能风险时 | 必须 |
| PlayMode | 不适用 | 影响用户旅程时 | 外壳交互变化时 | 必须 | 必须 |
| Unity WebGL 重建 | 否 | 否 | Template 改动时 | 必须 | 通常必须 |
| Preview 浏览器 smoke | 不适用 | 必须 | 必须 | 必须 | 必须完整执行 |
| Production smoke | 不适用 | 必须 | 必须 | 必须 | 必须完整执行 |

当前测试基线和 full name 以 [testing/test-suite-overview.zh-CN.md](testing/test-suite-overview.zh-CN.md) 为准。

## 8. 重大功能版本处理

大版本、新机制和新模式默认不停服：

1. 旧 Production 保持在线并继续承担用户访问。
2. 新版本在源码分支和 ReleaseCandidate 中独立开发。
3. Preview 完成规则、内容、UI、WebGL 和回退验收。
4. 从已验的同一冻结 `dist` 创建 `main` Production deployment，旧 Production 在新部署就绪前继续在线。
5. 已打开的旧会话继续使用会话快照；刷新或新进入的用户获得新版本。
6. 新版本发生 P0 时立即回滚上一 deployment。

只有以下情况才考虑维护页面或临时关闭入口：

- 后端数据库迁移无法同时兼容新旧客户端。
- 共享在线状态需要短时间排他迁移。
- 已发生安全泄露、数据破坏或法律合规事件。
- 所有已知良好 deployment 都不可安全使用。

当前纯 WebGL + 静态内容架构通常不满足这些条件，因此大更新不应默认停服。

## 9. 小补丁处理

- 小补丁可以缩小测试范围，但不能跳过直接测试、Preview 和 Production 复验。
- 内容补丁不重建 Unity，但必须使用新 `contentVersion`。
- C#、Prefab、场景或 Unity UI 补丁必须重新构建 WebGL。
- Cloudflare Pages 配置、Function 或安全头补丁通常可以复用已验 WebGL 输出，但必须重新组装候选、生成新 `dist` 并做浏览器 smoke。
- 纯视觉问题如果不影响主流程可进入计划补丁；白屏、无法启动、规则错误或数据破坏应升级为事故处理。

## 10. 事故与回滚

| 严重度 | 示例 | 第一动作 |
| --- | --- | --- |
| P0 | 网站打不开、WebGL 无法启动、数据破坏、安全泄露 | 立即回滚已知良好 deployment，恢复访问后再排查 |
| P1 | 核心规则错误、主要流程阻断、大面积浏览器不兼容 | 冻结发布，评估回滚；不能快速证明安全时优先回滚 |
| P2 | 文案、轻微布局、非阻断体验问题 | 进入正常补丁流程，不直接改 Production |

回滚原则：

- 线上恢复优先于立刻修代码。
- Deployment 回滚与源码修复分开进行。
- 远程内容损坏时客户端会自动走 LKG/Embedded，但仍应停止错误内容继续传播并发布新版本。
- DNS、证书、代理、Cloudflare Edge 或客户端到 Pages 的网络故障先分层诊断，不通过重复构建 Unity 解决。
- 不删除仍承担回滚职责的历史 deployment。

## 11. 补丁需求提交模板

以后提交补丁、修 Bug 或版本更新需求时，尽量提供以下信息。未知项可以写“待排查”，但不能省略影响发布决策的边界。

```markdown
# 补丁名称

- 主类型：DOC / CONTENT / WEB / CLIENT / FEATURE / INCIDENT
- 优先级：P0 / P1 / P2
- 当前客户端版本：
- 目标客户端版本：
- 当前内容版本：
- 目标内容版本：

## 问题与期望

- 当前表现：
- 期望表现：
- 用户影响：
- 复现步骤或证据：

## 范围

- 允许修改：
- 明确禁止修改：
- 是否允许重建 Unity：
- 是否允许部署 Preview：
- 是否允许部署 Production：
- 是否允许 commit / push / tag：

## 验收

- 必跑直接测试：
- 扩展门禁：
- 浏览器/设备要求：
- 已知风险：
- 回滚 deployment 或源码检查点：
```

## 12. 禁止事项

- 不直接修改 Production 文件或 Dashboard 中的非真源副本。
- 不用同一个版本号覆盖不同内容。
- 不把 Preview 通过后的另一份重新构建产物当作 Production。
- 不从脏工作树组装正式候选。
- 不把无关用户改动混入补丁。
- 不用提高 Timeout、删除测试或弱化断言掩盖失败。
- 不在未授权时 push、tag、部署 Production 或删除历史 deployment。
- 不在同一项目已有 Unity Editor 时启动第二实例。

## 13. 快速决策表

| 问题 | 决策 |
| --- | --- |
| 只改 JSON 文本或兼容数据？ | 新 `contentVersion`，不重建 Unity，仍走 Preview |
| 改 C#、Prefab、场景或 Unity UI？ | 重建 WebGL，走 CLIENT 流程 |
| 改 Cloudflare headers、Functions、缓存或 SPA？ | 不一定重建 Unity，但必须组装新候选、生成新 `dist` 并验 Preview |
| 加新机制或新模式？ | 独立 FEATURE 版本，全量门禁，不与无关补丁混发 |
| 线上已经严重故障？ | 先回滚，再按根因类型修复 |
| 重大功能版本是否先关网站？ | 默认不关；完整例外见第 8 节，包括排他后端/共享状态迁移、安全/数据/合规事故或无可安全回滚版本 |

## 14. 相关文档

- [ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md)：手机版、完整 Unity 网页版、下载包的分块提交和统一上传顺序。
- [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md)：具体构建、候选、Cloudflare Preview、Production、正式域名与回滚命令。
- [WebGLWebReleaseReadinessSpec.zh-CN.md](WebGLWebReleaseReadinessSpec.zh-CN.md)：WebGL 发布准入和浏览器验收标准。
- [ProjectReliabilityArchitectureCompletion.zh-CN.md](ProjectReliabilityArchitectureCompletion.zh-CN.md)：Remote → LKG → Embedded、内容真源和下一版规则接入边界。
- [testing/test-suite-overview.zh-CN.md](testing/test-suite-overview.zh-CN.md)：当前 Unity 测试集合与绿线基线。
- [PostLaunchProductRoadmap.zh-CN.md](PostLaunchProductRoadmap.zh-CN.md)：版本更新和后续新功能的阶段顺序。
