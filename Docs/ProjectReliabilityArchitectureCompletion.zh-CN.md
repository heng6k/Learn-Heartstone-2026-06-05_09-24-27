# 项目稳定性与架构阶段完成记录

首次记录：2026-07-11
最新收尾：2026-07-29

## 1. UI 测试稳定性

### Error

- `BoardCard_HoverShowsAndHidesKeywordTooltip` 抛出 `Sequence contains no matching element`。
- `TribeSelectionView_AdvancedMechanicPoolEditorPassesSelectedPools` 超过 NUnit 180 秒限制。

### Expected

- 悬停测试稳定构造商店随从并验证提示框。
- 高级机制池测试只验证 UI 选择结果传递，不受完整资源规模和整页重复重建影响。

### Cause

- 悬停夹具先清空商店，再调用依赖商店随从的 `CreateBoardMinion`。
- 高级池测试重复加载完整目录、逐个选择五个种族并多次重建整页；早期排查中编译失败又导致 Unity 继续运行旧测试程序集，放大了误判。

### Fix

- 在创建商店随从后再清空商店。
- 高级池测试改用最小英雄、任务、奖励、饰品、畸变、随从和法术目录。
- 使用“全部种族”一次进入高级页，避免六次无关整页重建。
- 高级池筛选和切页只重建覆盖层。
- 测试通过 `UiFactory.SetFontOverride` 使用内置字体，避免扩大共享动态字体图集。

### Prevention

- UI 行为测试使用最小目录；完整目录只用于资源完整性测试。
- 不通过提高超时掩盖资源解析、重建和夹具问题。
- Unity 编译失败时不得采信旧程序集测试结果。

结果：目标高级池测试 1/1，通过耗时 1.05 秒；完整 `UnityTavernTrainerViewTests` 103/103 通过。

## 2. 机制覆盖注册表

- 新增 `MechanicCoverageRegistry`，每项机制拥有稳定键、配置性、战斗消费、UI 可见性、测试覆盖、可信度和说明。
- `MechanicCoverageReportService` 只从注册表生成独立报告行，不再维护第二份硬编码事实。
- 校验稳定键和系统名称唯一、必填说明完整、报告行与注册表隔离。

## 3. SideModifierService

- 抽离字段读取/写入、Tavern 同步、历史附魔回算和支持的战斗奖励累积。
- `MatchService` 保留双方选择、调用时机和日志编排。
- 敌方新增/编辑、复制/镜像、工具调整和战斗奖励继续使用同一回算语义。
- 数值写入采用非负与饱和计算，避免增长溢出。

## 4. 场景版本迁移

- 当前版本升级为 `battle-test-loop-v2`。
- 新增有序 `v1 → v2` 迁移，将旧 Tavern 历史字段一次性写入权威 `PlayerCombatModifiers`。
- 当前版本重复迁移保持幂等；未知未来版本明确拒绝。
- Capture、内存仓库和文件仓库统一输出/读取当前版本。

## 5. 后续内容准入

新增 `Docs/ContentAndOpponentMechanicEntryChecklist.zh-CN.md`，覆盖数据、图片、机制注册、敌我一致性、场景迁移、黑盒顺序和回归要求。后续批量补卡及高级敌方机制应以该清单为合入门槛。

## 6. 验证结果

- UI 两项 + 注册表 + 全局参数/迁移：22/22 通过，2.55 秒。
- SideModifier、场景映射、MatchService 战斗测试、设计验证：25/25 通过，1.75 秒。
- 完整 Unity UI 组：103/103 通过，506.52 秒。完整组仍偏慢，但不再有单项 180 秒超时；后续可单独开展全组字体/截图测试性能治理。
- `git diff --check`：通过，仅有既有 CRLF/LF 提示。

## 7. M2–M6 内容交付架构收尾

内容交付已经从“运行时随处读取 Resources”收敛为“启动时解析一次、会话持有一个不可变快照”。当前职责边界如下：

```text
Assets/LearnHearthstone/
├─ Runtime/Application/Content/
│  ├─ GameCatalogSet.cs            一种语言的一组 Catalog
│  ├─ GameCatalogSnapshot.cs       中英双语会话快照、来源与版本信息
│  └─ ContentPackageManifest.cs    最小内容协议模型
├─ Runtime/Adapters/Content/
│  ├─ EmbeddedGameCatalogSnapshotLoader.cs  内置 Resources 快照
│  ├─ ContentPackageValidator.cs            UTF-8、协议、版本、大小、SHA-256 校验
│  ├─ RemoteContentPackageDownloader.cs     WebGL 同源下载
│  ├─ LastKnownGoodContentRepository.cs     persistentDataPath 原子持久化
│  └─ GameCatalogSnapshotResolver.cs        Remote → LKG → Embedded 选择
├─ Runtime/Presentation/LearnHearthstoneBootstrap.cs
│                                         启动编排并把同一快照注入整次会话
├─ Resources/Data/battlegroundsMinions.json
│                                         唯一人工编辑内容真源与最终内置回退
└─ Tests/EditMode/Catalogs/                快照、协议与回退测试

Tools/Release/assemble-release-candidate.mjs
                                          生成版本化内容包和 ReleaseCandidate
Deploy/Vercel/vercel.json                  部署、安全头、缓存、MIME 与 SPA 真源
```

运行时固定顺序：

1. WebGL 从当前页面同源的 `content/content-manifest.json` 下载 manifest 与版本化随从文件；Editor/非 WebGL 不主动联网。
2. 只有协议版本、客户端版本、文件名、字节数、SHA-256、严格 UTF-8 和中英双语目录解析全部通过，Remote 才能成为本次会话快照并提升为 LKG。
3. Remote 不可用或被拒绝时读取 `Application.persistentDataPath/Content/LKG`；LKG 仍需再次完整校验。
4. Remote 与 LKG 都不可用时加载内置 Resources。
5. 快照选定后本次会话不热切换；新内容只在下次启动/新会话生效。

保留的兼容入口仍可在测试或旧构造路径中直接加载 Resources，但生产 Bootstrap、MatchService 和主要 UI 路径应优先消费注入的 `GameCatalogSnapshot` / `GameCatalogSet`，后续不得再新增平行内容加载器。

## 8. 发布与运维闭环

- 源码 Git 只保存源码、测试、`Assets/WebGLTemplates`、`Deploy/Vercel`、`Tools/Release` 与唯一内容真源。
- `Builds/WebGL/**`、`Builds/ReleaseCandidate/**`、版本化远程内容和旧 `WebDeploy/**` 都是生成物；当前跟踪数为 0。
- Preview 与 Production 必须来自同一个冻结 ReleaseCandidate；Production 只能 Promote 已验 Preview，不能重新构建或重新组装。
- 当前 Production deployment：`dpl_GBFSeFEwnjN3XEYqaPeFkt92X6pV`，由 Preview `dpl_Ps3FHzViFirA15L87jELqP82pbJR` Promote。
- 正式入口：<https://jsoncool.com/>；备用稳定入口：<https://hengheng-one.vercel.app/>。
- 当前内容版本：`20260727`；当前安全基线包含最小 CSP、Permissions Policy、Referrer Policy、`nosniff` 与 `SAMEORIGIN`。

故障处理顺序：

1. 页面或资源故障：先在 Vercel 回滚/重新 Promote 最近已知良好 deployment，再复跑完整 Production smoke。
2. 远程内容故障：客户端自动走 LKG/Embedded；修复时发布新 `contentVersion`，禁止用相同版本号覆盖不同字节。
3. 源码故障：使用可审计的 revert 提交；不要用破坏性 reset 代替线上回滚。
4. DNS/证书故障：核对公共 DNS、Vercel 域名状态和证书，不通过重复部署解决 DNS 问题。

## 9. 下一版规则接入边界

酒馆战棋新机制应在本架构之上增量进入，不修改已验证的交付主链：

- 规则、模型和效果进入 `Runtime/Domain`；对局流程和用例编排进入 `Runtime/Application`。
- JSON 解析、远程/LKG、持久化与图片继续留在各自 `Adapters`；UI 只消费 Application 暴露的快照和用例。
- 新内容先修改 `Resources/Data` 真源并增加新的 `contentVersion`。如果只是随从数据变化，沿用协议 v1；只有需要发布第二类文件或改变兼容语义时，才设计协议 v2。
- 若新机制需要新的客户端代码，必须同步提升客户端版本，并用 `requiredClientVersion` 阻止旧客户端接受不兼容内容。
- 新规则测试先补直接相关 Domain/Application/Adapter 用例，再进入普通 EditMode、Stress（排除 Marathon）与 PlayMode 门禁。

剩余已知风险：协议 v1 目前只远程交付随从 JSON；内容真实性依赖 HTTPS 同源和部署权限，尚未增加离线签名；LKG 保留旧版本文件但没有清理器；大型 Unity UI 测试仍需分片和域重载控制累计退化。这些均不阻断当前 Alpha，上述边界变化时再单独立项。
