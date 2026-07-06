# 项目文档索引

## 使用方式

这个索引用来快速判断“现在该看哪篇文档”。项目文档已经分成几类：Skill/工作流、发行上线、英雄和宝宝实现、缺陷追踪、UI 改造、数据/API、Unity 架构和项目历史。

新接手时建议先按路线读，不要直接从最长的缺陷文档开始读。

## 推荐阅读路线

### 准备修改项目或向代理提问

每次要修改项目、修 bug、做实现方案或提出项目相关问题时，先看这些文档来选择合适的 skill 和工作方式：

1. [../AGENTS.md](../AGENTS.md)
2. [LocalSkillClassification.zh-CN.md](LocalSkillClassification.zh-CN.md)
3. [PonytailSkillRouting.md](PonytailSkillRouting.md)

### 想了解当前产品怎么上线

1. [AlphaReleaseRoadmap.md](AlphaReleaseRoadmap.md)
2. [OnlineServicesAndSharingArchitecturePlan.md](OnlineServicesAndSharingArchitecturePlan.md)
3. [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md)
4. [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md)

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

### 想改 UI 或做更好用的编辑器

1. [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md)
2. [HeroSelectionAndSwapDisplayPlan.md](HeroSelectionAndSwapDisplayPlan.md)
3. [UnityPrefabUiImplementationPlan.md](UnityPrefabUiImplementationPlan.md)
4. [UnityMinionRightClickEditorPlan.md](UnityMinionRightClickEditorPlan.md)
5. [TavernTribeBanSelectionDesign.md](TavernTribeBanSelectionDesign.md)

### 想查官方数据、API 和一致性

1. [OFFICIAL_APIS.md](OFFICIAL_APIS.md)
2. [OfficialConsistencyRoadmap.md](OfficialConsistencyRoadmap.md)
3. [HeroPowerNonReplaceableList.md](HeroPowerNonReplaceableList.md)
4. [TribeDistributionSystemDesign.md](TribeDistributionSystemDesign.md)

## 文档地图

| 分类 | 文档 | 主要用途 | 当前定位 |
| --- | --- | --- | --- |
| 索引 | [DocumentationIndex.md](DocumentationIndex.md) | 当前文档导航 | 入口文档 |
| Skill/工作流 | [../AGENTS.md](../AGENTS.md) | 项目级代理路由规则，说明不同任务应该先用哪些 skill | 每次项目修改和提问前的入口规则 |
| Skill/工作流 | [LocalSkillClassification.zh-CN.md](LocalSkillClassification.zh-CN.md) | 本机 57 个本地 skills 的分类、适用场景和冲突处理 | 全量 skill 路由索引 |
| Skill/工作流 | [PonytailSkillRouting.md](PonytailSkillRouting.md) | Ponytail 系列 skills 的使用场景、边界和模式说明 | 编码最小化与复杂度控制参考 |
| 发行上线 | [AlphaReleaseRoadmap.md](AlphaReleaseRoadmap.md) | Alpha 到 1.0 的版本节奏、渠道、发包检查和运营动作 | 当前上线主路线 |
| 发行上线 | [OnlineServicesAndSharingArchitecturePlan.md](OnlineServicesAndSharingArchitecturePlan.md) | 官网、服务器、API、分享码、微信小程序和后端路线 | 线上架构主路线 |
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
| 后续机制 | [TrinketSystemImplementationPlan.md](TrinketSystemImplementationPlan.md) | 小饰品/大饰品槽位、候选、购买、效果触发和 Marin/Buttons 接入 | 饰品系统实现方案 |
| 后续机制 | [QuestSystemImplementationPlan.md](QuestSystemImplementationPlan.md) | Quest/Reward 数据、任务进度、奖励激活和 Denathrius/Shady Aristocrat 接入 | 任务系统实现方案 |
| 后续机制 | [AnomalySystemImplementationPlan.md](AnomalySystemImplementationPlan.md) | 单局畸变选择、全局规则修正、UI 展示和低风险 MVP 畸变池 | 畸变系统实现方案 |
| 后续机制 | [TimewarpSystemImplementationPlan.md](TimewarpSystemImplementationPlan.md) | 回合级历史快照、时空奖励、Morchie/Murozond 接入和安全边界 | 扭曲时空实现方案 |
| 数据/API | [OFFICIAL_APIS.md](OFFICIAL_APIS.md) | 暴雪官方 API、认证、Metadata、Cards 查询说明 | 数据接入参考 |
| 数据/API | [OfficialConsistencyRoadmap.md](OfficialConsistencyRoadmap.md) | 官方一致性检查、差异和修复优先级 | 一致性路线 |
| 数据/API | [TribeDistributionSystemDesign.md](TribeDistributionSystemDesign.md) | 种族分布计算、平局规则和消费者 | 种族统计设计 |
| 酒馆规则 | [TavernTribeBanSelectionDesign.md](TavernTribeBanSelectionDesign.md) | 酒馆种族 Ban 选、随从池过滤、发现和法术过滤 | 酒馆池规则设计 |
| Unity 架构 | [UnityMigrationDesign.md](UnityMigrationDesign.md) | 从源项目迁移到 Unity 的目标结构、领域层、UI 和测试策略 | 迁移总设计 |
| Unity UI | [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md) | 当前 UI 审计、问题、原则和目标形态 | UI 重做主文档 |
| Unity UI | [HeroSelectionAndSwapDisplayPlan.md](HeroSelectionAndSwapDisplayPlan.md) | 开局选英雄、局内换英雄和小头像显示方案 | 英雄 UI 近期方案 |
| Unity UI | [UnityPrefabUiImplementationPlan.md](UnityPrefabUiImplementationPlan.md) | Prefab 化酒馆 UI 的目录、阶段和实现方案 | UI 工程化方案 |
| Unity UI | [UnityMinionRightClickEditorPlan.md](UnityMinionRightClickEditorPlan.md) | 随从右键编辑、关键词悬停、一键套用规则 | 编辑器交互方案 |
| 项目历史 | [ProjectProgress.md](ProjectProgress.md) | 项目进度、决策、已有文档和后续建议 | 早期进度快照 |

## 当前优先级判断

### 最高优先级

- [AlphaReleaseRoadmap.md](AlphaReleaseRoadmap.md)：决定先发什么版本、发到哪里、每次发行做什么。
- [OnlineServicesAndSharingArchitecturePlan.md](OnlineServicesAndSharingArchitecturePlan.md)：决定什么时候加服务器、分享码、小程序和后端能力。
- [HeroEffectImplementationGaps.md](HeroEffectImplementationGaps.md)：继续补机制或对外说明缺陷时必须看。

### 中高优先级

- [UnityUiComprehensiveImprovementPlan.md](UnityUiComprehensiveImprovementPlan.md)：当前 UI 不方便，后续重做需要靠它定方向。
- [HeroEffectRemainingCompletionOrder.md](HeroEffectRemainingCompletionOrder.md)：从当前剩余 Planned/FrameworkFirst/Deferred 项继续补齐时先看。
- [HeroPowerBuddyEffectsImplementationOrder.md](HeroPowerBuddyEffectsImplementationOrder.md)：继续按批次补英雄和宝宝时使用。
- [HeroAndBuddyImplementationProcess.md](HeroAndBuddyImplementationProcess.md)：避免新增英雄/宝宝时漏掉数据、注册、运行时和测试步骤。
- [SharedAdvancedMechanicsFoundationImplementationPlan.md](SharedAdvancedMechanicsFoundationImplementationPlan.md)：开始饰品、任务、畸变和扭曲时空前必须先看。

### 参考优先级

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
