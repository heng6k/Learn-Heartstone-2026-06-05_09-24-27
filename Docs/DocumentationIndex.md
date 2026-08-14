# 项目文档索引

## 使用方式

这个索引用来快速判断“现在该看哪篇文档”。项目文档已经分成几类：Skill/工作流、发行上线、英雄和宝宝实现、缺陷追踪、UI 改造、数据/API、Unity 架构和项目历史。

新接手时建议先按路线读，不要直接从最长的缺陷文档开始读。

### 当前酒馆模拟器优化统一入口

1. [TavernSimulatorGlobalRequirements.zh-CN.md](TavernSimulatorGlobalRequirements.zh-CN.md)：所有新功能和 UI 修改必须遵守的全局产品与质量基线。
2. [TavernSimulatorOptimizationPlan.zh-CN.md](TavernSimulatorOptimizationPlan.zh-CN.md)：当前翻译、时空酒馆、UI 和关键词反馈优化的统一执行计划。
3. [TavernPlayerPerspectiveTestingStandard.zh-CN.md](TavernPlayerPerspectiveTestingStandard.zh-CN.md)：防止白盒假通过、以酒馆玩家旅程验证功能真实性的测试规范。

## 推荐阅读路线

### 准备修改项目或向代理提问

每次要修改项目、修 bug、做实现方案或提出项目相关问题时，先看这些文档来选择合适的 skill 和工作方式：

1. [../AGENTS.md](../AGENTS.md)
2. [LocalSkillClassification.zh-CN.md](LocalSkillClassification.zh-CN.md)
3. [PonytailSkillRouting.md](PonytailSkillRouting.md)

### 想了解当前产品怎么上线

1. [ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md)：现行上传总入口；统一拆分“手机版轻量网页、完整 Unity 网页版、网页下载包”，并固定 Git 先行、干净候选、Cloudflare Preview/Production、ZIP 与证据提交顺序。
2. [Releases/2026-08-14-mobile-picker-windows-web-release.md](Releases/2026-08-14-mobile-picker-windows-web-release.md)：当前最新完整发布基准；包含手机双重缩放修复、Windows D3D11/D3D12、R2 完整回读、最终 SHA、Preview/Production 与正式域名复验。
3. [PatchSubmissionAndReleasePolicy.zh-CN.md](PatchSubmissionAndReleasePolicy.zh-CN.md)
4. [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md)
5. [PostLaunchProductRoadmap.zh-CN.md](PostLaunchProductRoadmap.zh-CN.md)
6. [ProjectReliabilityArchitectureCompletion.zh-CN.md](ProjectReliabilityArchitectureCompletion.zh-CN.md)
7. [WebGLWebReleaseReadinessSpec.zh-CN.md](WebGLWebReleaseReadinessSpec.zh-CN.md)
8. [AlphaReleaseRoadmap.md](AlphaReleaseRoadmap.md)
9. [OnlineServicesAndSharingArchitecturePlan.md](OnlineServicesAndSharingArchitecturePlan.md)
10. [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md)
11. [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md)

### 想提交手机版网页或完整网页版

1. [ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md)：先确定本轮交付面，再按共享内容、手机版网页、完整网页版、打包配置和发布记录分块提交。
2. [Releases/2026-08-14-mobile-picker-windows-web-release.md](Releases/2026-08-14-mobile-picker-windows-web-release.md)：当前最新完整上线基准；记录最终 Git、Windows/R2、WebGL、手机横竖屏和 Cloudflare 身份，以及残留 Vite/Brotli 协商等经验。
3. [Releases/2026-08-12-windows-download-hotfix.md](Releases/2026-08-12-windows-download-hotfix.md)：上一版 Windows 可缩放/最大化与 R2 发布基准。
4. [Releases/2026-08-12-web-release.md](Releases/2026-08-12-web-release.md)：首轮手机版/完整 Unity 网页发布与回滚历史证据。
5. [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md)：执行 Unity ReleaseCandidate、Cloudflare Pages Preview、同一冻结 `dist` 的 Production 部署和正式域名复验。
6. [PatchSubmissionAndReleasePolicy.zh-CN.md](PatchSubmissionAndReleasePolicy.zh-CN.md)：判断变更类型、版本号、测试门禁与回滚级别。

### 想处理线上补丁或规划下一阶段

1. [ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md)：先确定本次交付面，并按固定块提交、验收和记录。
2. [PatchSubmissionAndReleasePolicy.zh-CN.md](PatchSubmissionAndReleasePolicy.zh-CN.md)：再判断是内容、Web、Unity 客户端、重大功能还是线上事故，并确定版本、门禁和回滚方式。
3. [Season14ProductizationImplementationPlan.zh-CN.md](Season14ProductizationImplementationPlan.zh-CN.md)：按玩法硬缺口、Vue3/Windows、R2 一图流、移动分发四阶段实施当前 36.2 产品化路线。
4. [PostLaunchProductRoadmap.zh-CN.md](PostLaunchProductRoadmap.zh-CN.md)：查看新规则、新手一图流、纯残局、肉鸽爬塔和在线 1v1 的依赖顺序。
5. [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md)：执行 ReleaseCandidate、本地 Pages 门禁、Preview、Production 和正式域名复验。
6. [ProjectReliabilityArchitectureCompletion.zh-CN.md](ProjectReliabilityArchitectureCompletion.zh-CN.md)：核对内容回退、生成物边界和新规则接入位置。
7. [OnlineServicesAndSharingArchitecturePlan.md](OnlineServicesAndSharingArchitecturePlan.md)：进入分享后端或在线 1v1 前核对服务端边界。

### 想继续补英雄、宝宝和英雄技能

1. [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md)
2. [HeroEffectRemainingCompletionOrder.md](HeroEffectRemainingCompletionOrder.md)
3. [HeroPowerBuddyEffectsImplementationOrder.md](HeroPowerBuddyEffectsImplementationOrder.md)
4. [HeroAndBuddyImplementationProcess.md](HeroAndBuddyImplementationProcess.md)
5. [HeroPowerBuddyEffectsImplementationPlan.md](HeroPowerBuddyEffectsImplementationPlan.md)

### 想开始做饰品、任务、畸变和扭曲时空

1. [SharedAdvancedMechanicsFoundationImplementationPlan.md](SharedAdvancedMechanicsFoundationImplementationPlan.md)
2. [IncompleteAndAmbiguousAdvancedMechanics.md](IncompleteAndAmbiguousAdvancedMechanics.md)
3. [TrinketSystemImplementationPlan.md](TrinketSystemImplementationPlan.md)
4. [QuestSystemImplementationPlan.md](QuestSystemImplementationPlan.md)
5. [AnomalySystemImplementationPlan.md](AnomalySystemImplementationPlan.md)
6. [TimewarpSystemImplementationPlan.md](TimewarpSystemImplementationPlan.md)
7. [PlayerDirectedAdvancedMechanicSelectionImplementationPlan.md](PlayerDirectedAdvancedMechanicSelectionImplementationPlan.md)
8. [PlayerChoiceBlockingImplementationPlan.md](PlayerChoiceBlockingImplementationPlan.md)

### 想改 UI 或做更好用的编辑器

1. [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md)
2. [BattlegroundsUiV2ResearchAndMcpPlan.md](BattlegroundsUiV2ResearchAndMcpPlan.md)
3. [TavernTrainerUiSkinAndComponentPlan.md](TavernTrainerUiSkinAndComponentPlan.md)
4. [HeroSelectionAndSwapDisplayPlan.md](HeroSelectionAndSwapDisplayPlan.md)
5. [UnityPrefabUiImplementationPlan.md](UnityPrefabUiImplementationPlan.md)
6. [UnityMinionRightClickEditorPlan.md](UnityMinionRightClickEditorPlan.md)
7. [OpponentHandAndSideStateConfigurationPlan.md](OpponentHandAndSideStateConfigurationPlan.md)
8. [TavernTribeBanSelectionDesign.md](TavernTribeBanSelectionDesign.md)

### 想查官方数据、API 和一致性

1. [OFFICIAL_APIS.md](OFFICIAL_APIS.md)
2. [OfficialConsistencyRoadmap.md](OfficialConsistencyRoadmap.md)
3. [HeroPowerNonReplaceableList.md](HeroPowerNonReplaceableList.md)
4. [TribeDistributionSystemDesign.md](TribeDistributionSystemDesign.md)

### 想查看、定位或运行测试

1. [testing/test-suite-overview.zh-CN.md](testing/test-suite-overview.zh-CN.md)：全部 EditMode/PlayMode 测试的分类目录、测试类索引、覆盖范围、运行命令和最近验证基线。
2. [TavernPlayerPerspectiveTestingStandard.zh-CN.md](TavernPlayerPerspectiveTestingStandard.zh-CN.md)：玩家视角验收标准、测试分层和发布门槛。
3. [Season14NewContentActivateAuditStandard.zh-CN.md](Season14NewContentActivateAuditStandard.zh-CN.md)：36.2 新随从、大小饰品、黑暗之赐、发动与高本内容的逐条证据基线，以及随从内嵌发动 UI 验收标准。

### 想制作宣传或玩法演示视频

1. [HyperframesAuditedVideoProductionSOP.zh-CN.md](HyperframesAuditedVideoProductionSOP.zh-CN.md)：从立项、真实素材、脚本、分镜和配音，到 HyperFrames 实现、G0–G12 审计、Draft/High 渲染及归档的完整标准流程。

## 文档地图

| 分类 | 文档 | 主要用途 | 当前定位 |
| --- | --- | --- | --- |
| 索引 | [DocumentationIndex.md](DocumentationIndex.md) | 当前文档导航 | 入口文档 |
| 全局要求 | [TavernSimulatorGlobalRequirements.zh-CN.md](TavernSimulatorGlobalRequirements.zh-CN.md) | 本地化、时空酒馆触发安全、UI、关键词反馈和测试基线 | 所有酒馆功能修改的强制要求 |
| 当前计划 | [TavernSimulatorOptimizationPlan.zh-CN.md](TavernSimulatorOptimizationPlan.zh-CN.md) | 合并当前五项需求和全流程审核建议的分阶段实施方案 | 当前酒馆模拟器优化统一入口 |
| 测试规范 | [TavernPlayerPerspectiveTestingStandard.zh-CN.md](TavernPlayerPerspectiveTestingStandard.zh-CN.md) | 测试分层、玩家旅程、实现真实性证据和发布门槛 | 所有酒馆功能验收的统一测试基线 |
| 测试索引 | [testing/test-suite-overview.zh-CN.md](testing/test-suite-overview.zh-CN.md) | 全部测试目录、测试类、覆盖范围、运行方式和验证结果 | 后续定位与运行测试的统一入口 |
| 测试基线 | [Season14NewContentActivateAuditStandard.zh-CN.md](Season14NewContentActivateAuditStandard.zh-CN.md) | 63 新随从、47 新饰品、43 黑暗之赐的证据模型，17 张发动与 22 张高本强化矩阵，随从内嵌发动 UI 方向 | Season 14 新内容排查与后续实现的默认基线 |
| Skill/工作流 | [../AGENTS.md](../AGENTS.md) | 项目级代理路由规则，说明不同任务应该先用哪些 skill | 每次项目修改和提问前的入口规则 |
| Skill/工作流 | [LocalSkillClassification.zh-CN.md](LocalSkillClassification.zh-CN.md) | 本机 57 个本地 skills 的分类、适用场景和冲突处理 | 全量 skill 路由索引 |
| Skill/工作流 | [PonytailSkillRouting.md](PonytailSkillRouting.md) | Ponytail 系列 skills 的使用场景、边界和模式说明 | 编码最小化与复杂度控制参考 |
| 视频制作/审计 | [HyperframesAuditedVideoProductionSOP.zh-CN.md](HyperframesAuditedVideoProductionSOP.zh-CN.md) | 宣传视频从策划、录制、脚本、分镜、声音和素材冻结，到 HyperFrames 制作、G0–G12 审计、渲染及归档的完整流程 | 后续视频生成与质量审计的默认执行规范 |
| 当前计划 | [PostLaunchProductRoadmap.zh-CN.md](PostLaunchProductRoadmap.zh-CN.md) | 新规则、新手一图流、纯残局、肉鸽爬塔和在线 1v1 的阶段依赖与退出条件 | 当前上线后产品主路线 |
| 当前计划 | [Season14ProductizationImplementationPlan.zh-CN.md](Season14ProductizationImplementationPlan.zh-CN.md) | 36.2 玩法收口、Vue3/Windows、R2 一图流和移动分发的工作包、规则契约与验收门 | 当前四阶段实施基线 |
| 发行上线 | [ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md) | 手机版轻量网页、完整 Unity 网页版、下载包与可选微信渠道的分块提交；Git 先行、Cloudflare Preview/Production、发布模板和回滚规则 | 后续所有网页上传的唯一总入口 |
| 发行记录 | [Releases/2026-08-14-mobile-picker-windows-web-release.md](Releases/2026-08-14-mobile-picker-windows-web-release.md) | 手机一图流双重缩放修复、最终 Git SHA、Windows/R2、WebGL、Cloudflare Preview/Production、正式域名与故障恢复经验 | 当前最新完整上线基准与下一轮发布实战参考 |
| 发行记录 | [Releases/2026-08-12-windows-download-hotfix.md](Releases/2026-08-12-windows-download-hotfix.md) | 玩家文案热修、Windows 可缩放/最大化、退出验收、R2 完整回读、新下载地址和 Cloudflare 部署证据 | 当前最新上线与后续上传基准 |
| 发行记录 | [Releases/2026-08-12-web-release.md](Releases/2026-08-12-web-release.md) | 手机版/完整 Unity 网页、全屏入口、干净 WebGL 候选、Cloudflare Preview/Production、下载包与回滚证据 | 2026-08-12 已上线版本的最终发布证据 |
| 发行上线 | [PatchSubmissionAndReleasePolicy.zh-CN.md](PatchSubmissionAndReleasePolicy.zh-CN.md) | 补丁分类、版本、测试门禁、不停服升级、Cloudflare Preview/Production 与事故回滚 | 所有线上变更的决策规范 |
| 发行上线 | [AlphaReleaseRoadmap.md](AlphaReleaseRoadmap.md) | Alpha 到 1.0 的早期版本节奏、渠道、发包检查和运营动作 | 早期渠道与产品假设历史参考 |
| 发行上线 | [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md) | Unity/UI 修改、ReleaseCandidate 组装、Cloudflare Pages 本地验收、Preview/Production 与 `jsoncool.com` 复验 | 网页版日常构建与发布手册 |
| 历史审计 | [WebGLWebReleaseReadinessSpec.zh-CN.md](WebGLWebReleaseReadinessSpec.zh-CN.md) | 2026-07-16 平台未定阶段的 WebGL 构建、托管和浏览器准入审计 | 仅作历史依据；当前上传使用三渠道规范与 Cloudflare Pages 指南 |
| 发行上线 | [OnlineServicesAndSharingArchitecturePlan.md](OnlineServicesAndSharingArchitecturePlan.md) | 官网、API、分享码、教程/题库和一般后端路线 | 分享与一般后端基础；实时权威 1v1 待 R5 前扩展 |
| 发行上线 | [../MiniProgram/README.md](../MiniProgram/README.md) | 原生微信小程序 MVP、本地运行、共享场景契约、API 适配和明确边界 | Phase 12 可运行交接入口 |
| 英雄/宝宝 | [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md) | 当前英雄、宝宝、公共系统缺陷和解决方向 | 最重要的缺陷追踪表 |
| 英雄/宝宝 | [HeroEffectRemainingCompletionOrder.md](HeroEffectRemainingCompletionOrder.md) | 当前剩余英雄、宝宝和公共机制的补齐顺序 | 当前剩余补齐路线 |
| 英雄/宝宝 | [HeroAndBuddyImplementationProcess.md](HeroAndBuddyImplementationProcess.md) | 将英雄和宝宝信息实现进项目的具体流程 | 开发流程说明 |
| 英雄/宝宝 | [HeroPowerBuddyEffectsImplementationOrder.md](HeroPowerBuddyEffectsImplementationOrder.md) | 英雄技能和宝宝实现顺序、分批计划、阶段状态 | 历史批次和阶段参考 |
| 英雄/宝宝 | [HeroPowerBuddyEffectsImplementationPlan.md](HeroPowerBuddyEffectsImplementationPlan.md) | 英雄技能/宝宝效果架构、派发点、测试策略 | 架构设计 |
| 英雄/宝宝 | [HeroBuddyHeroPowerDevelopmentPlan.md](HeroBuddyHeroPowerDevelopmentPlan.md) | 英雄、宝宝、英雄技能接入的早期开发文档 | 历史设计与数据目标 |
| 英雄/宝宝 | [HeroPowerNonReplaceableList.md](HeroPowerNonReplaceableList.md) | 当前不可替换、禁用或开局限定的英雄技能清单 | 机制边界参考 |
| 后续机制 | [SharedAdvancedMechanicsFoundationImplementationPlan.md](SharedAdvancedMechanicsFoundationImplementationPlan.md) | 饰品、任务、畸变和扭曲时空共用的选择、奖励、状态、注册和测试底座 | 下一批机制前置文档 |
| 后续机制 | [IncompleteAndAmbiguousAdvancedMechanics.md](IncompleteAndAmbiguousAdvancedMechanics.md) | 饰品、任务、任务奖励的不完整清单、补齐方案、原因可视化和所需决策 | 当前缺陷与补齐主文档 |
| 后续机制 | [PlayerDirectedAdvancedMechanicSelectionImplementationPlan.md](PlayerDirectedAdvancedMechanicSelectionImplementationPlan.md) | 玩家可见的任务、饰品、第二英雄技能自由搭配选择器实现方案 | 高级机制自选入口方案 |
| 后续机制 | [PlayerChoiceBlockingImplementationPlan.md](PlayerChoiceBlockingImplementationPlan.md) | 玩家未完成任务、饰品、畸变、扭曲或发现选择时，阻止下一回合、战斗和完整回合推进 | 强制玩家抉择守门规则 |
| 后续机制 | [TrinketSystemImplementationPlan.md](TrinketSystemImplementationPlan.md) | 小饰品/大饰品槽位、候选、购买、效果触发和 Marin/Buttons 接入 | 饰品系统实现方案 |
| 后续机制 | [QuestSystemImplementationPlan.md](QuestSystemImplementationPlan.md) | Quest/Reward 数据、任务进度、奖励激活和 Denathrius/Shady Aristocrat 接入 | 任务系统实现方案 |
| 后续机制 | [AnomalySystemImplementationPlan.md](AnomalySystemImplementationPlan.md) | 单局畸变选择、全局规则修正、UI 展示和低风险 MVP 畸变池 | 畸变系统实现方案 |
| 后续机制 | [TimewarpSystemImplementationPlan.md](TimewarpSystemImplementationPlan.md) | 回合级历史快照、时空奖励、Morchie/Murozond 接入和安全边界 | 扭曲时空实现方案 |
| 数据/API | [OFFICIAL_APIS.md](OFFICIAL_APIS.md) | 暴雪官方 API、认证、Metadata、Cards 查询说明 | 数据接入参考 |
| 数据/API | [OfficialConsistencyRoadmap.md](OfficialConsistencyRoadmap.md) | 官方一致性检查、差异和修复优先级 | 一致性路线 |
| 数据/API | [TribeDistributionSystemDesign.md](TribeDistributionSystemDesign.md) | 种族分布计算、平局规则和消费者 | 种族统计设计 |
| 酒馆规则 | [TavernTribeBanSelectionDesign.md](TavernTribeBanSelectionDesign.md) | 酒馆种族 Ban 选、随从池过滤、发现和法术过滤 | 酒馆池规则设计 |
| Unity 架构 | [UnityMigrationDesign.md](UnityMigrationDesign.md) | 从源项目迁移到 Unity 的目标结构、领域层、UI 和测试策略 | 迁移总设计 |
| Unity 架构 | [ProjectReliabilityArchitectureCompletion.zh-CN.md](ProjectReliabilityArchitectureCompletion.zh-CN.md) | 当前稳定性收敛、内容快照、Remote/LKG/Embedded、发布边界与下一版规则入口 | M2–M6 架构与运维交接 |
| 发行上线 | [ProjectReliabilityMilestonesM0M6.zh-CN.md](ProjectReliabilityMilestonesM0M6.zh-CN.md) | M0–M6 各模块完成度、具体实现、证据与日常复用边界 | 已完成发布主链的统一状态说明 |
| Unity UI | [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md) | 当前 UI 审计、问题、原则和目标形态 | UI 重做主文档 |
| Unity UI | [BattlegroundsUiV2ResearchAndMcpPlan.md](BattlegroundsUiV2ResearchAndMcpPlan.md) | Battlegrounds 风格 UGUI V2 的调研结论、参考来源、skill 组合和 MCP 实施路线 | V2 UI 开工参考 |
| Unity UI | [TavernTrainerUiSkinAndComponentPlan.md](TavernTrainerUiSkinAndComponentPlan.md) | Tavern Trainer 镜像入口、皮肤素材、开源 UI 组件候选、项目内 UGUI 组件和中文字体决策 | UI 皮肤与组件路线 |
| Unity UI | [HeroSelectionAndSwapDisplayPlan.md](HeroSelectionAndSwapDisplayPlan.md) | 开局选英雄、局内换英雄和小头像显示方案 | 英雄 UI 近期方案 |
| Unity UI | [UnityPrefabUiImplementationPlan.md](UnityPrefabUiImplementationPlan.md) | Prefab 化酒馆 UI 的目录、阶段和实现方案 | UI 工程化方案 |
| Unity UI | [UnityMinionRightClickEditorPlan.md](UnityMinionRightClickEditorPlan.md) | 随从右键编辑、关键词悬停、一键套用规则 | 编辑器交互方案 |
| Unity UI | [OpponentHandAndSideStateConfigurationPlan.md](OpponentHandAndSideStateConfigurationPlan.md) | 对手手牌、分阵营全局变量和完整下一回合流程的配置方案 | 战队配置和回合推进方案 |
| 项目历史 | [ProjectProgress.md](ProjectProgress.md) | 项目进度、决策、已有文档和后续建议 | 早期进度快照 |

## 当前优先级判断

### 最高优先级

- [ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](ThreeChannelReleaseSubmissionWorkflow.zh-CN.md)：提交手机版网页、完整网页版或下载包时，先按它确定交付面和证据。
- [Season14ProductizationImplementationPlan.zh-CN.md](Season14ProductizationImplementationPlan.zh-CN.md)：当前下一步编码顺序、玩法硬缺口、Cloudflare 产品壳与一图流场景底座的统一实施入口。
- [PostLaunchProductRoadmap.zh-CN.md](PostLaunchProductRoadmap.zh-CN.md)：决定社交发布后先更新规则，再依次推进新手、残局、肉鸽和联机。
- [PatchSubmissionAndReleasePolicy.zh-CN.md](PatchSubmissionAndReleasePolicy.zh-CN.md)：决定每个 Bug、内容更新或大版本如何分类、测试、上线和回滚。
- [WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md](WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md)：当前 Production、日常 Cloudflare Preview/Production、域名、安全头、慢网诊断与回滚的执行手册。
- [OnlineServicesAndSharingArchitecturePlan.md](OnlineServicesAndSharingArchitecturePlan.md)：决定什么时候加服务器、分享码、小程序和后端能力。
- [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md)：继续补机制或对外说明缺陷时必须看。

### 中高优先级

- [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md)：当前 UI 不方便，后续重做需要靠它定方向。
- [BattlegroundsUiV2ResearchAndMcpPlan.md](BattlegroundsUiV2ResearchAndMcpPlan.md)：开新的 UGUI V2 壳子、用 MCP 截图迭代前先看。
- [TavernTrainerUiSkinAndComponentPlan.md](TavernTrainerUiSkinAndComponentPlan.md)：按镜像入口改 UI、选择现成皮肤、决定是否引入开源 Unity UI 组件和统一中文字体前先看。
- [HeroEffectRemainingCompletionOrder.md](HeroEffectRemainingCompletionOrder.md)：从当前剩余 Planned/FrameworkFirst/Deferred 项继续补齐时先看。
- [HeroPowerBuddyEffectsImplementationOrder.md](HeroPowerBuddyEffectsImplementationOrder.md)：继续按批次补英雄和宝宝时使用。
- [HeroAndBuddyImplementationProcess.md](HeroAndBuddyImplementationProcess.md)：避免新增英雄/宝宝时漏掉数据、注册、运行时和测试步骤。
- [SharedAdvancedMechanicsFoundationImplementationPlan.md](SharedAdvancedMechanicsFoundationImplementationPlan.md)：开始饰品、任务、畸变和扭曲时空前必须先看。

### 参考优先级

- [AlphaReleaseRoadmap.md](AlphaReleaseRoadmap.md)：回看早期 Alpha 渠道、投放节奏和新手/残局产品假设时使用。
- [OFFICIAL_APIS.md](OFFICIAL_APIS.md)：查官方数据接入方式时使用。
- [UnityMigrationDesign.md](UnityMigrationDesign.md)：理解项目架构来源时使用。
- [ProjectProgress.md](ProjectProgress.md)：查早期项目背景时使用。

## 维护规则

以后新增文档时，建议同步做三件事：

1. 给新文档写清楚一级标题和目的。
2. 在本文档的“文档地图”中补一行。
3. 如果新文档改变了上线、实现顺序或缺陷状态，同步更新对应主文档。

如果文档内容过期，不要只改代码。需要在相关文档里写清楚：

- 哪个结论变了。
- 为什么变了。
- 新的执行顺序是什么。
- 还有什么没做到位。
