# Learn Hearthstone 测试套件总览

> 更新日期：2026-07-13
> 范围：`Assets/LearnHearthstone/Tests` 下全部 53 个 EditMode 与 4 个 PlayMode 测试文件。

## 1. 目录结构

```text
Assets/LearnHearthstone/Tests/
├─ EditMode/
│  ├─ Acceptance/
│  ├─ Catalogs/
│  ├─ Combat/
│  ├─ Core/
│  ├─ Heroes/
│  ├─ Match/
│  ├─ Mechanics/
│  ├─ Opponent/
│  ├─ Reliability/
│  ├─ UI/
│  └─ LearnHearthstone.Tests.asmdef
└─ PlayMode/
   ├─ Journeys/
   ├─ Opponent/
   ├─ Timewarped/
   └─ LearnHearthstone.PlayModeTests.asmdef
```

物理目录只表示测试职责。现有 namespace、测试类完整名称和程序集名称保持不变，所以历史命令仍按 `LearnHearthstone.Tests.EditMode.<ClassName>` 或 `LearnHearthstone.Tests.PlayMode.<ClassName>` 运行。

## 2. 分类职责

| 目录 | 职责 |
| --- | --- |
| `EditMode/Acceptance` | 按酒馆等级验证随从实现完整性与实际对局行为。 |
| `EditMode/Catalogs` | 验证卡牌、英雄、法术和效果数据目录。 |
| `EditMode/Combat` | 验证战斗引擎、战斗命令、敌方战斗和事件顺序。 |
| `EditMode/Core` | 验证不依赖 UI 的领域规则、事件分发和全局修正。 |
| `EditMode/Heroes` | 验证英雄、英雄技能和伙伴。 |
| `EditMode/Match` | 验证 MatchService、回合流程、选择阻塞和调试命令。 |
| `EditMode/Mechanics` | 验证畸变、任务、饰品、时空酒馆、奖品和酒馆法术。 |
| `EditMode/Opponent` | 验证对手配置和敌方机制状态。 |
| `EditMode/Reliability` | 验证边界、压力和长流程稳定性。 |
| `EditMode/UI` | 验证 Unity/训练器视图、布局、截图和交互。 |
| `PlayMode/Journeys` | 使用 EventSystem 和射线点击验证核心用户旅程。 |
| `PlayMode/Opponent` | 验证对手配置的真实输入路径。 |
| `PlayMode/Timewarped` | 验证时空酒馆独立 UI 的真实输入路径。 |

## 3. 运行方式

### Unity Test Runner

1. 打开 `Window > General > Test Runner`。
2. EditMode 页签运行领域、服务和 UI 构建测试。
3. PlayMode 页签运行真实输入、EventSystem 和射线点击测试。

### 项目测试脚本

全量 EditMode：

```powershell
& '.planning/scripts/run-unity-tests.ps1' \
  -TestName 'LearnHearthstone.Tests.EditMode' \
  -TimeoutMs 120000 \
  -MaxPolls 800
```

单个测试类：

```powershell
& '.planning/scripts/run-unity-tests.ps1' \
  -TestName 'LearnHearthstone.Tests.EditMode.MatchServiceTests' \
  -TimeoutMs 120000 \
  -MaxPolls 400
```

时空酒馆 PlayMode：

```powershell
& '.planning/scripts/run-unity-tests.ps1' \
  -Mode 'PlayMode' \
  -TestName 'LearnHearthstone.Tests.PlayMode.TimewarpedTavernInputTests' \
  -TimeoutMs 120000 \
  -MaxPolls 400
```

## 4. 计数说明

- “Test/UnityTest”统计源码中的 `[Test]` 与 `[UnityTest]` 声明。
- “TestCase”统计显式 `[TestCase]` 数据行。
- “TestCaseSource”只统计数据源声明；一个数据源可能在运行时展开成大量测试，因此表内声明数不等于 NUnit 最终发现总数。
- `UI` 中的截图验收需要可用图形设备，并依赖仓库内对应的 `.planning` 验收资料。
- `Reliability/StressTests.cs` 运行时间较长，日常定向开发可以先跑相关分类，提交前再跑全量。

## 5. 全部测试索引

| 模式 | 分类 | 文件 | 测试类 | Test/UnityTest | TestCase | TestCaseSource | 主要覆盖 |
| --- | --- | --- | --- | ---: | ---: | ---: | --- |
| EditMode | Acceptance | [`TierFiveAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierFiveAcceptanceTests.cs) | `TierFiveAcceptanceTests` | 13 | 0 | 0 | 五本随从的目录、效果与对局验收。 |
| EditMode | Acceptance | [`TierFourAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierFourAcceptanceTests.cs) | `TierFourAcceptanceTests` | 15 | 0 | 0 | 四本随从的目录、效果与交互验收。 |
| EditMode | Acceptance | [`TierOneTwoThreeCatalogAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierOneTwoThreeCatalogAcceptanceTests.cs) | `TierOneTwoThreeCatalogAcceptanceTests` | 2 | 0 | 0 | 一至三本随从目录完整性与数据一致性。 |
| EditMode | Acceptance | [`TierOneTwoThreeSinglePlayerAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierOneTwoThreeSinglePlayerAcceptanceTests.cs) | `TierOneTwoThreeSinglePlayerAcceptanceTests` | 11 | 0 | 0 | 一至三本单人战棋范围和核心效果验收。 |
| EditMode | Acceptance | [`TierSixSevenAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierSixSevenAcceptanceTests.cs) | `TierSixSevenAcceptanceTests` | 9 | 0 | 0 | 六至七本高等级随从实现验收。 |
| EditMode | Acceptance | [`TierThreeAllMinionsImplementationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierThreeAllMinionsImplementationTests.cs) | `TierThreeAllMinionsImplementationTests` | 11 | 0 | 0 | 三本随从实现注册表与覆盖完整性。 |
| EditMode | Acceptance | [`TierThreeDeathrattleSummonTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierThreeDeathrattleSummonTests.cs) | `TierThreeDeathrattleSummonTests` | 7 | 0 | 0 | 三本亡语和召唤链路验收。 |
| EditMode | Acceptance | [`TierThreeReactiveMechanicTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierThreeReactiveMechanicTests.cs) | `TierThreeReactiveMechanicTests` | 11 | 0 | 0 | 三本响应式、条件式机制验收。 |
| EditMode | Catalogs | [`DesignValidationToolingTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/DesignValidationToolingTests.cs) | `DesignValidationToolingTests` | 8 | 0 | 0 | 设计校验工具、完整回合与时空酒馆流程校验。 |
| EditMode | Catalogs | [`EffectCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/EffectCatalogTests.cs) | `EffectCatalogTests` | 3 | 0 | 0 | 效果目录加载、标识与数据约束。 |
| EditMode | Catalogs | [`HeroCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/HeroCatalogTests.cs) | `HeroCatalogTests` | 8 | 0 | 0 | 英雄和英雄技能目录完整性。 |
| EditMode | Catalogs | [`MinionCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/MinionCatalogTests.cs) | `MinionCatalogTests` | 7 | 0 | 0 | 随从目录加载、卡池与字段约束。 |
| EditMode | Catalogs | [`OfficialSoloTavernSpellCoverageTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/OfficialSoloTavernSpellCoverageTests.cs) | `OfficialSoloTavernSpellCoverageTests` | 2 | 0 | 0 | 官方单人酒馆法术覆盖率。 |
| EditMode | Catalogs | [`SpellCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/SpellCatalogTests.cs) | `SpellCatalogTests` | 1 | 0 | 0 | 法术目录基础加载与可用性。 |
| EditMode | Combat | [`CombatMechanicTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/CombatMechanicTests.cs) | `CombatMechanicTests` | 2 | 0 | 0 | 战斗机制的基础黑盒行为。 |
| EditMode | Combat | [`MatchServiceBattleTestTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/MatchServiceBattleTestTests.cs) | `MatchServiceBattleTestTests` | 6 | 21 | 0 | 战斗测试快照、命令阶段权限与重放入口。 |
| EditMode | Combat | [`OpponentCombatMechanicBlackBoxTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/OpponentCombatMechanicBlackBoxTests.cs) | `OpponentCombatMechanicBlackBoxTests` | 8 | 4 | 1 | 敌方战斗机制文档矩阵的黑盒执行。 |
| EditMode | Combat | [`StartOfCombatTavernSpellOrderingBlackBoxTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/StartOfCombatTavernSpellOrderingBlackBoxTests.cs) | `StartOfCombatTavernSpellOrderingBlackBoxTests` | 2 | 2 | 0 | 战斗开始法术与事件先后顺序。 |
| EditMode | Core | [`DomainEngineTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/DomainEngineTests.cs) | `DomainEngineTests` | 26 | 18 | 0 | 领域引擎、战斗、光环和动态条件基础行为。 |
| EditMode | Core | [`EffectDispatcherTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/EffectDispatcherTests.cs) | `EffectDispatcherTests` | 2 | 0 | 0 | 效果事件分发和订阅调用。 |
| EditMode | Core | [`GlobalSideModifierConsistencyTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/GlobalSideModifierConsistencyTests.cs) | `GlobalSideModifierConsistencyTests` | 13 | 0 | 0 | 敌我双方全局修正的一致性。 |
| EditMode | Core | [`MechanicEngineTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/MechanicEngineTests.cs) | `MechanicEngineTests` | 5 | 0 | 0 | 通用机制引擎的状态与事件处理。 |
| EditMode | Core | [`TavernRulesTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/TavernRulesTests.cs) | `TavernRulesTests` | 1 | 0 | 0 | 酒馆等级、金币等基础规则。 |
| EditMode | Core | [`TribeAvailabilityRulesTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/TribeAvailabilityRulesTests.cs) | `TribeAvailabilityRulesTests` | 4 | 0 | 0 | 种族启用、禁用和卡池可用规则。 |
| EditMode | Heroes | [`HeroEffectImplementationRegistryTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Heroes/HeroEffectImplementationRegistryTests.cs) | `HeroEffectImplementationRegistryTests` | 10 | 0 | 0 | 英雄效果实现状态和注册完整性。 |
| EditMode | Heroes | [`HeroPowerBuddyEffectTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Heroes/HeroPowerBuddyEffectTests.cs) | `HeroPowerBuddyEffectTests` | 144 | 4 | 0 | 英雄技能、伙伴及其跨回合效果。 |
| EditMode | Heroes | [`HeroSetupAndUnmaskedIdentityTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Heroes/HeroSetupAndUnmaskedIdentityTests.cs) | `HeroSetupAndUnmaskedIdentityTests` | 6 | 0 | 0 | 英雄选择、初始化与真实身份状态。 |
| EditMode | Match | [`MatchServiceDebugCardTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceDebugCardTests.cs) | `MatchServiceDebugCardTests` | 8 | 0 | 0 | 调试加牌、施法和工具命令。 |
| EditMode | Match | [`MatchServiceMechanicTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceMechanicTests.cs) | `MatchServiceMechanicTests` | 8 | 0 | 0 | MatchService 通用机制集成。 |
| EditMode | Match | [`MatchServiceSpellTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceSpellTests.cs) | `MatchServiceSpellTests` | 11 | 0 | 0 | MatchService 法术购买、施放和结算。 |
| EditMode | Match | [`MatchServiceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceTests.cs) | `MatchServiceTests` | 182 | 0 | 0 | 整局招募、回合、卡牌和状态服务的主回归套件。 |
| EditMode | Match | [`PlayerChoiceBlockingTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/PlayerChoiceBlockingTests.cs) | `PlayerChoiceBlockingTests` | 6 | 0 | 0 | 必须选择事件对其他命令的阻塞。 |
| EditMode | Match | [`PlayerDirectedAdvancedMechanicSelectionTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/PlayerDirectedAdvancedMechanicSelectionTests.cs) | `PlayerDirectedAdvancedMechanicSelectionTests` | 5 | 0 | 0 | 玩家定向选择任务、饰品和第二技能。 |
| EditMode | Match | [`TestScenarioMapperTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/TestScenarioMapperTests.cs) | `TestScenarioMapperTests` | 2 | 0 | 0 | 测试场景数据到对局状态的映射。 |
| EditMode | Mechanics | [`AnomalySystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/AnomalySystemTests.cs) | `AnomalySystemTests` | 72 | 3 | 0 | 畸变选择、回合调度和效果实现。 |
| EditMode | Mechanics | [`DarkmoonPrizeSystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/DarkmoonPrizeSystemTests.cs) | `DarkmoonPrizeSystemTests` | 13 | 0 | 0 | 暗月奖品发放、选择与消费。 |
| EditMode | Mechanics | [`MechanicTemplateTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/MechanicTemplateTests.cs) | `MechanicTemplateTests` | 4 | 0 | 0 | 机制模板和高级机制状态框架。 |
| EditMode | Mechanics | [`QuestSystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/QuestSystemTests.cs) | `QuestSystemTests` | 66 | 0 | 0 | 任务条件、进度、奖励和回合事件。 |
| EditMode | Mechanics | [`QuestTrinketInteractionTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/QuestTrinketInteractionTests.cs) | `QuestTrinketInteractionTests` | 15 | 0 | 0 | 任务、饰品、亡语与时空效果的组合。 |
| EditMode | Mechanics | [`TavernSpellEngineTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/TavernSpellEngineTests.cs) | `TavernSpellEngineTests` | 7 | 0 | 0 | 酒馆法术引擎和目标结算。 |
| EditMode | Mechanics | [`TimewarpedHistoricalImplementationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/TimewarpedHistoricalImplementationTests.cs) | `TimewarpedHistoricalImplementationTests` | 14 | 0 | 0 | 历史时空酒馆卡池与卡牌实现。 |
| EditMode | Mechanics | [`TrinketSystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/TrinketSystemTests.cs) | `TrinketSystemTests` | 225 | 0 | 0 | 饰品系统的目录、触发、光环与组合回归。 |
| EditMode | Opponent | [`OpponentCustomizationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Opponent/OpponentCustomizationTests.cs) | `OpponentCustomizationTests` | 14 | 0 | 0 | 对手阵容、技能和回合战斗定制。 |
| EditMode | Opponent | [`OpponentMechanicConfigurationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Opponent/OpponentMechanicConfigurationTests.cs) | `OpponentMechanicConfigurationTests` | 9 | 0 | 0 | 敌方任务、饰品、变量和机制配置。 |
| EditMode | Reliability | [`RobustnessEdgeTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Reliability/RobustnessEdgeTests.cs) | `RobustnessEdgeTests` | 5 | 0 | 0 | 极值、空状态、长链路和边界稳定性。 |
| EditMode | Reliability | [`StressTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Reliability/StressTests.cs) | `StressTests` | 6 | 0 | 0 | 多种子长回合和高负载压力测试。 |
| EditMode | UI | [`CombatReplayAndOpponentEditorTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/CombatReplayAndOpponentEditorTests.cs) | `CombatReplayAndOpponentEditorTests` | 14 | 0 | 0 | 战斗重放、对手编辑器和 UI 控制。 |
| EditMode | UI | [`MainHubViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/MainHubViewTests.cs) | `MainHubViewTests` | 1 | 0 | 0 | 主入口界面构建。 |
| EditMode | UI | [`RealisticTavernTrainerViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/RealisticTavernTrainerViewTests.cs) | `RealisticTavernTrainerViewTests` | 10 | 0 | 0 | 现实酒馆训练器视图布局与操作。 |
| EditMode | UI | [`TavernTrainerViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/TavernTrainerViewTests.cs) | `TavernTrainerViewTests` | 20 | 0 | 0 | 通用酒馆训练器视图模型和交互。 |
| EditMode | UI | [`UnityCombatReplayPanelAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/UnityCombatReplayPanelAcceptanceTests.cs) | `UnityCombatReplayPanelAcceptanceTests` | 4 | 0 | 0 | Unity 战斗重放全屏面板和截图验收。 |
| EditMode | UI | [`UnityTavernLargeStatDisplayTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/UnityTavernLargeStatDisplayTests.cs) | `UnityTavernLargeStatDisplayTests` | 6 | 6 | 0 | 超大数值显示、缩放和排版。 |
| EditMode | UI | [`UnityTavernTrainerViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/UnityTavernTrainerViewTests.cs) | `UnityTavernTrainerViewTests` | 112 | 0 | 0 | Unity 主酒馆 UI、工具、弹窗和完整用户旅程。 |
| PlayMode | Journeys | [`CorePlayerJourneyInputTests.cs`](../../Assets/LearnHearthstone/Tests/PlayMode/Journeys/CorePlayerJourneyInputTests.cs) | `CorePlayerJourneyInputTests` | 11 | 0 | 0 | 核心玩家旅程的真实输入与射线点击。 |
| PlayMode | Journeys | [`KeywordPlayerJourneyInputTests.cs`](../../Assets/LearnHearthstone/Tests/PlayMode/Journeys/KeywordPlayerJourneyInputTests.cs) | `KeywordPlayerJourneyInputTests` | 5 | 0 | 0 | 关键字展示和交互的玩家旅程。 |
| PlayMode | Opponent | [`OpponentConfigurationPlayerJourneyInputTests.cs`](../../Assets/LearnHearthstone/Tests/PlayMode/Opponent/OpponentConfigurationPlayerJourneyInputTests.cs) | `OpponentConfigurationPlayerJourneyInputTests` | 1 | 0 | 0 | 对手配置界面的真实输入旅程。 |
| PlayMode | Timewarped | [`TimewarpedTavernInputTests.cs`](../../Assets/LearnHearthstone/Tests/PlayMode/Timewarped/TimewarpedTavernInputTests.cs) | `TimewarpedTavernInputTests` | 2 | 0 | 0 | 时空酒馆购买、退出和战斗返回输入链路。 |

## 6. 新增和维护规则

1. 优先把测试放进现有职责目录；只有出现明确的新测试领域时才新增目录。
2. 新文件必须放在对应 asmdef 的子目录中，不为单个分类新增 asmdef。
3. 不因物理目录修改 namespace；EditMode 继续使用 `LearnHearthstone.Tests.EditMode`，PlayMode 继续使用 `LearnHearthstone.Tests.PlayMode`。
4. 移动测试时必须同时移动 `.cs.meta`，保持 Unity GUID 不变。
5. 新增、删除或移动测试文件后，同步更新本文“全部测试索引”。
6. 修复跨领域缺陷时，至少运行直接相关测试类；涉及共享状态、回合、战斗或 UI 控制器时运行全量 EditMode。

## 7. 最近验证基线

目录整理前的功能基线：

- EditMode：1336 项，1335 通过，0 失败，1 跳过。
- PlayMode `TimewarpedTavernInputTests`：2/2 通过。

目录整理后在全新 Unity Library 中重新导入并验证：

- EditMode：1336 项，1335 通过，0 失败，1 跳过。
- 全部 PlayMode：19/19 通过。
- 测试发现数量与整理前一致，程序集、namespace 和 Unity GUID 均未改变。
