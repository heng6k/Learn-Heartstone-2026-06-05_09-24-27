# Learn Hearthstone 测试套件总览

> 更新日期：2026-07-29
> 范围：`Assets/LearnHearthstone/Tests` 下全部 58 个 EditMode 与 4 个 PlayMode 测试文件。

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

物理目录只表示测试职责。多数既有 EditMode 类继续使用 `LearnHearthstone.Tests.EditMode.<ClassName>`，M2–M4 内容链测试使用 `LearnHearthstone.Tests.Catalogs.<ClassName>`，PlayMode 使用 `LearnHearthstone.Tests.PlayMode.<ClassName>`。批量门禁应以 Unity 当前发现的 full name 为准，不根据物理目录猜 namespace。

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

### 正式批量入口

以下示例使用本轮验证版本；其他机器只需调整 `$Unity`：

```powershell
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
$Project = (Get-Location).Path
```

普通 EditMode（始终排除 `Stress` 与 `Marathon`）：

```powershell
& $Unity -batchmode -nographics -projectPath $Project `
  -executeMethod LearnHearthstone.Editor.BatchEditModeTestRunner.RunEditMode `
  -batchTestResults 'Logs/EditMode.xml' `
  -batchTestManifest 'Logs/EditMode.manifest.txt' `
  -logFile 'Logs/EditMode.log'
```

单个测试类：

```powershell
& $Unity -batchmode -nographics -projectPath $Project `
  -executeMethod LearnHearthstone.Editor.BatchEditModeTestRunner.RunEditMode `
  -batchTestName 'LearnHearthstone.Tests.EditMode.MatchServiceTests' `
  -batchTestResults 'Logs/MatchService.xml' `
  -batchTestManifest 'Logs/MatchService.manifest.txt' `
  -logFile 'Logs/MatchService.log'
```

Stress（包含 `Stress`，始终排除 `Marathon`）：

```powershell
& $Unity -batchmode -nographics -projectPath $Project `
  -executeMethod LearnHearthstone.Editor.BatchEditModeTestRunner.RunStressEditMode `
  -batchTestResults 'Logs/Stress.xml' `
  -batchTestManifest 'Logs/Stress.manifest.txt' `
  -logFile 'Logs/Stress.log'
```

全部 PlayMode：

```powershell
& $Unity -batchmode -nographics -projectPath $Project `
  -runTests -testPlatform PlayMode `
  -testResults 'Logs/PlayMode.xml' `
  -logFile 'Logs/PlayMode.log'
```

`BatchEditModeTestRunner` 还支持 `-batchTestNameFile`、`-batchTestShardIndex` 与 `-batchTestShardCount`。manifest 中以 `#` 开头的行为元数据，其余叶级名称可直接作为下一次 `-batchTestNameFile` 输入。普通 EditMode 只有在至少执行 1 项且 0 失败时返回 0；0 用例或任意失败都返回 1。标准 PlayMode CLI 在测试失败且 XML 已正常写出时可能返回 2。

## 4. 计数说明

- “Test/UnityTest”统计源码中的 `[Test]` 与 `[UnityTest]` 声明。
- “TestCase”统计显式 `[TestCase]` 数据行。
- “TestCaseSource”只统计数据源声明；一个数据源可能在运行时展开成大量测试，因此表内声明数不等于 NUnit 最终发现总数。
- 当前源码声明总计为 EditMode `[Test]` 1321、`[TestCase]` 85、`[TestCaseSource]` 3，以及 PlayMode `[UnityTest]` 19；权威运行总数以 Unity/NUnit 实际叶级测试树为准。
- `UI` 中的截图验收依赖图形环境；`-nographics` 下的尺寸与渲染结果不能直接代表浏览器或桌面图形环境。
- `Reliability/StressTests.cs` 及其他 `Stress` 分类用例运行时间较长，日常普通 EditMode 不会误跑；发布门禁应单独执行 Stress 入口。

## 5. 全部测试索引

| 模式 | 分类 | 文件 | 测试类 | Test/UnityTest | TestCase | TestCaseSource | 主要覆盖 |
| --- | --- | --- | --- | ---: | ---: | ---: | --- |
| EditMode | Acceptance | [`TierFiveAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierFiveAcceptanceTests.cs) | `TierFiveAcceptanceTests` | 17 | 0 | 0 | 五本随从的目录、效果与对局验收。 |
| EditMode | Acceptance | [`TierFourAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierFourAcceptanceTests.cs) | `TierFourAcceptanceTests` | 17 | 0 | 0 | 四本随从的目录、效果与交互验收。 |
| EditMode | Acceptance | [`TierOneTwoThreeCatalogAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierOneTwoThreeCatalogAcceptanceTests.cs) | `TierOneTwoThreeCatalogAcceptanceTests` | 2 | 0 | 0 | 一至三本随从目录完整性与数据一致性。 |
| EditMode | Acceptance | [`TierOneTwoThreeSinglePlayerAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierOneTwoThreeSinglePlayerAcceptanceTests.cs) | `TierOneTwoThreeSinglePlayerAcceptanceTests` | 15 | 2 | 0 | 一至三本单人战棋范围和核心效果验收。 |
| EditMode | Acceptance | [`TierSixSevenAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierSixSevenAcceptanceTests.cs) | `TierSixSevenAcceptanceTests` | 15 | 11 | 2 | 六至七本高等级随从实现验收。 |
| EditMode | Acceptance | [`TierThreeAllMinionsImplementationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierThreeAllMinionsImplementationTests.cs) | `TierThreeAllMinionsImplementationTests` | 12 | 0 | 0 | 三本随从实现注册表与覆盖完整性。 |
| EditMode | Acceptance | [`TierThreeDeathrattleSummonTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierThreeDeathrattleSummonTests.cs) | `TierThreeDeathrattleSummonTests` | 7 | 0 | 0 | 三本亡语和召唤链路验收。 |
| EditMode | Acceptance | [`TierThreeReactiveMechanicTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Acceptance/TierThreeReactiveMechanicTests.cs) | `TierThreeReactiveMechanicTests` | 11 | 0 | 0 | 三本响应式、条件式机制验收。 |
| EditMode | Catalogs | [`ContentPackageProtocolTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/ContentPackageProtocolTests.cs) | `ContentPackageProtocolTests` | 5 | 0 | 0 | manifest、版本、路径、字节数、SHA-256 与严格 UTF-8 协议。 |
| EditMode | Catalogs | [`ContentSnapshotFallbackTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/ContentSnapshotFallbackTests.cs) | `ContentSnapshotFallbackTests` | 4 | 0 | 0 | Remote、LKG、Embedded 回退和原子提升。 |
| EditMode | Catalogs | [`GameCatalogSnapshotTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/GameCatalogSnapshotTests.cs) | `GameCatalogSnapshotTests` | 2 | 0 | 0 | 中英双语会话快照、注入与会话稳定性。 |
| EditMode | Catalogs | [`DesignValidationToolingTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/DesignValidationToolingTests.cs) | `DesignValidationToolingTests` | 8 | 0 | 0 | 设计校验工具、完整回合与时空酒馆流程校验。 |
| EditMode | Catalogs | [`EffectCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/EffectCatalogTests.cs) | `EffectCatalogTests` | 3 | 0 | 0 | 效果目录加载、标识与数据约束。 |
| EditMode | Catalogs | [`GoldenMinionEffectContractTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/GoldenMinionEffectContractTests.cs) | `GoldenMinionEffectContractTests` | 1 | 0 | 0 | 金色随从效果契约与普通版本映射。 |
| EditMode | Catalogs | [`HeroCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/HeroCatalogTests.cs) | `HeroCatalogTests` | 9 | 0 | 0 | 英雄和英雄技能目录完整性。 |
| EditMode | Catalogs | [`MinionCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/MinionCatalogTests.cs) | `MinionCatalogTests` | 12 | 0 | 0 | 随从目录加载、卡池与字段约束。 |
| EditMode | Catalogs | [`OfficialSoloTavernSpellCoverageTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/OfficialSoloTavernSpellCoverageTests.cs) | `OfficialSoloTavernSpellCoverageTests` | 4 | 0 | 0 | 官方单人酒馆法术覆盖率。 |
| EditMode | Catalogs | [`SpellCatalogTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Catalogs/SpellCatalogTests.cs) | `SpellCatalogTests` | 2 | 0 | 0 | 法术目录基础加载与可用性。 |
| EditMode | Combat | [`CombatMechanicTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/CombatMechanicTests.cs) | `CombatMechanicTests` | 3 | 0 | 0 | 战斗机制的基础黑盒行为。 |
| EditMode | Combat | [`MatchServiceBattleTestTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/MatchServiceBattleTestTests.cs) | `MatchServiceBattleTestTests` | 6 | 21 | 0 | 战斗测试快照、命令阶段权限与重放入口。 |
| EditMode | Combat | [`OpponentCombatMechanicBlackBoxTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/OpponentCombatMechanicBlackBoxTests.cs) | `OpponentCombatMechanicBlackBoxTests` | 10 | 7 | 1 | 敌方战斗机制文档矩阵的黑盒执行。 |
| EditMode | Combat | [`StartOfCombatTavernSpellOrderingBlackBoxTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Combat/StartOfCombatTavernSpellOrderingBlackBoxTests.cs) | `StartOfCombatTavernSpellOrderingBlackBoxTests` | 2 | 2 | 0 | 战斗开始法术与事件先后顺序。 |
| EditMode | Core | [`DomainEngineTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/DomainEngineTests.cs) | `DomainEngineTests` | 38 | 20 | 0 | 领域引擎、战斗、光环和动态条件基础行为。 |
| EditMode | Core | [`EffectDispatcherTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/EffectDispatcherTests.cs) | `EffectDispatcherTests` | 2 | 0 | 0 | 效果事件分发和订阅调用。 |
| EditMode | Core | [`GlobalSideModifierConsistencyTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/GlobalSideModifierConsistencyTests.cs) | `GlobalSideModifierConsistencyTests` | 17 | 0 | 0 | 敌我双方全局修正的一致性。 |
| EditMode | Core | [`MechanicEngineTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/MechanicEngineTests.cs) | `MechanicEngineTests` | 5 | 0 | 0 | 通用机制引擎的状态与事件处理。 |
| EditMode | Core | [`TavernRulesTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/TavernRulesTests.cs) | `TavernRulesTests` | 2 | 0 | 0 | 酒馆等级、金币等基础规则。 |
| EditMode | Core | [`TribeAvailabilityRulesTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Core/TribeAvailabilityRulesTests.cs) | `TribeAvailabilityRulesTests` | 4 | 0 | 0 | 种族启用、禁用和卡池可用规则。 |
| EditMode | Heroes | [`HeroEffectImplementationRegistryTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Heroes/HeroEffectImplementationRegistryTests.cs) | `HeroEffectImplementationRegistryTests` | 10 | 0 | 0 | 英雄效果实现状态和注册完整性。 |
| EditMode | Heroes | [`HeroPowerBuddyEffectTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Heroes/HeroPowerBuddyEffectTests.cs) | `HeroPowerBuddyEffectTests` | 144 | 6 | 0 | 英雄技能、伙伴及其跨回合效果。 |
| EditMode | Heroes | [`HeroSetupAndUnmaskedIdentityTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Heroes/HeroSetupAndUnmaskedIdentityTests.cs) | `HeroSetupAndUnmaskedIdentityTests` | 6 | 0 | 0 | 英雄选择、初始化与真实身份状态。 |
| EditMode | Match | [`CardAcquisitionTierBoundaryTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/CardAcquisitionTierBoundaryTests.cs) | `CardAcquisitionTierBoundaryTests` | 8 | 7 | 0 | 卡牌获取方式与酒馆等级边界。 |
| EditMode | Match | [`MatchServiceDebugCardTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceDebugCardTests.cs) | `MatchServiceDebugCardTests` | 8 | 0 | 0 | 调试加牌、施法和工具命令。 |
| EditMode | Match | [`MatchServiceMechanicTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceMechanicTests.cs) | `MatchServiceMechanicTests` | 8 | 0 | 0 | MatchService 通用机制集成。 |
| EditMode | Match | [`MatchServiceSpellTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceSpellTests.cs) | `MatchServiceSpellTests` | 35 | 0 | 0 | MatchService 法术购买、施放和结算。 |
| EditMode | Match | [`MatchServiceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/MatchServiceTests.cs) | `MatchServiceTests` | 194 | 0 | 0 | 整局招募、回合、卡牌和状态服务的主回归套件。 |
| EditMode | Match | [`PlayerChoiceBlockingTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/PlayerChoiceBlockingTests.cs) | `PlayerChoiceBlockingTests` | 6 | 0 | 0 | 必须选择事件对其他命令的阻塞。 |
| EditMode | Match | [`PlayerDirectedAdvancedMechanicSelectionTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/PlayerDirectedAdvancedMechanicSelectionTests.cs) | `PlayerDirectedAdvancedMechanicSelectionTests` | 5 | 0 | 0 | 玩家定向选择任务、饰品和第二技能。 |
| EditMode | Match | [`TestScenarioMapperTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Match/TestScenarioMapperTests.cs) | `TestScenarioMapperTests` | 2 | 0 | 0 | 测试场景数据到对局状态的映射。 |
| EditMode | Mechanics | [`AnomalySystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/AnomalySystemTests.cs) | `AnomalySystemTests` | 73 | 3 | 0 | 畸变选择、回合调度和效果实现。 |
| EditMode | Mechanics | [`DarkmoonPrizeSystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/DarkmoonPrizeSystemTests.cs) | `DarkmoonPrizeSystemTests` | 13 | 0 | 0 | 暗月奖品发放、选择与消费。 |
| EditMode | Mechanics | [`MechanicTemplateTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/MechanicTemplateTests.cs) | `MechanicTemplateTests` | 4 | 0 | 0 | 机制模板和高级机制状态框架。 |
| EditMode | Mechanics | [`QuestSystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/QuestSystemTests.cs) | `QuestSystemTests` | 69 | 0 | 0 | 任务条件、进度、奖励和回合事件。 |
| EditMode | Mechanics | [`QuestTrinketInteractionTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/QuestTrinketInteractionTests.cs) | `QuestTrinketInteractionTests` | 15 | 0 | 0 | 任务、饰品、亡语与时空效果的组合。 |
| EditMode | Mechanics | [`TavernSpellEngineTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/TavernSpellEngineTests.cs) | `TavernSpellEngineTests` | 21 | 0 | 0 | 酒馆法术引擎和目标结算。 |
| EditMode | Mechanics | [`TimewarpedHistoricalImplementationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/TimewarpedHistoricalImplementationTests.cs) | `TimewarpedHistoricalImplementationTests` | 14 | 0 | 0 | 历史时空酒馆卡池与卡牌实现。 |
| EditMode | Mechanics | [`TrinketSystemTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Mechanics/TrinketSystemTests.cs) | `TrinketSystemTests` | 231 | 0 | 0 | 饰品系统的目录、触发、光环与组合回归。 |
| EditMode | Opponent | [`OpponentCustomizationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Opponent/OpponentCustomizationTests.cs) | `OpponentCustomizationTests` | 14 | 0 | 0 | 对手阵容、技能和回合战斗定制。 |
| EditMode | Opponent | [`OpponentMechanicConfigurationTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Opponent/OpponentMechanicConfigurationTests.cs) | `OpponentMechanicConfigurationTests` | 9 | 0 | 0 | 敌方任务、饰品、变量和机制配置。 |
| EditMode | Reliability | [`RobustnessEdgeTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Reliability/RobustnessEdgeTests.cs) | `RobustnessEdgeTests` | 5 | 0 | 0 | 极值、空状态、长链路和边界稳定性。 |
| EditMode | Reliability | [`StressTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/Reliability/StressTests.cs) | `StressTests` | 6 | 0 | 0 | 多种子长回合和高负载压力测试。 |
| EditMode | UI | [`CombatReplayAndOpponentEditorTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/CombatReplayAndOpponentEditorTests.cs) | `CombatReplayAndOpponentEditorTests` | 14 | 0 | 0 | 战斗重放、对手编辑器和 UI 控制。 |
| EditMode | UI | [`MainHubViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/MainHubViewTests.cs) | `MainHubViewTests` | 3 | 0 | 0 | 主入口界面构建。 |
| EditMode | UI | [`RealisticTavernTrainerViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/RealisticTavernTrainerViewTests.cs) | `RealisticTavernTrainerViewTests` | 11 | 0 | 0 | 现实酒馆训练器视图布局与操作。 |
| EditMode | UI | [`TavernTrainerViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/TavernTrainerViewTests.cs) | `TavernTrainerViewTests` | 21 | 0 | 0 | 通用酒馆训练器视图模型和交互。 |
| EditMode | UI | [`UnityCombatReplayPanelAcceptanceTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/UnityCombatReplayPanelAcceptanceTests.cs) | `UnityCombatReplayPanelAcceptanceTests` | 4 | 0 | 0 | Unity 战斗重放全屏面板和截图验收。 |
| EditMode | UI | [`UnityTavernLargeStatDisplayTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/UnityTavernLargeStatDisplayTests.cs) | `UnityTavernLargeStatDisplayTests` | 6 | 6 | 0 | 超大数值显示、缩放和排版。 |
| EditMode | UI | [`UnityTavernTrainerViewTests.cs`](../../Assets/LearnHearthstone/Tests/EditMode/UI/UnityTavernTrainerViewTests.cs) | `UnityTavernTrainerViewTests` | 127 | 0 | 0 | Unity 主酒馆 UI、工具、弹窗和完整用户旅程。 |
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

## 7. 2026-07-28 M0 历史红线基线

环境与可追溯信息：

- Unity：`6000.4.10f1`。
- Source commit：`239adcfd06b780d5e4fa11ccd9ffb3ec72f5a10b`。
- 测试时工作树包含尚未提交的 M0 运行器与 Tribe Selection UI 修复；这两份源码 diff 的 Git blob 指纹为 `acbc872a948aab31255c3a192552eb6eea172e00`。
- 证据目录：`Logs/M0-Official-NonStress-Sharded8-PostFix-20260728`、`Logs/M0-Official-Stress-20260728`、`Logs/M0-Official-PlayMode-20260728`、`Logs/M0-PlayMode-Isolation-20260728`。

普通 EditMode：

- 两次独立 Unity/NUnit 原生发现均得到相同的 1505 个非压力叶级测试。
- 8 个固定分片的 manifest 与 XML 逐片完全一致；合并后缺失 0、额外 0、重复 0。
- 执行 1505：通过 1310、失败 195、跳过 0、Inconclusive 0。
- 失败主要集中于 `OpponentCombatMechanicBlackBoxTests` 103、`AnomalySystemTests` 20、`MatchServiceTests` 19、`TrinketSystemTests` 17、`HeroPowerBuddyEffectTests` 12、`QuestTrinketInteractionTests` 7、`UnityTavernTrainerViewTests` 4、`QuestSystemTests` 3，其余 fixture 合计 10。
- 两个 headless 截图用例在 `994x384` 下生成 7530 字节 PNG，低于现有 10000 字节阈值，并伴随 URP `EndRenderPass: Not inside a Renderpass` 日志；保持红灯，不通过放宽断言掩盖。

Stress：

- 发现并执行 10：通过 8、失败 2、跳过 0；确认包含 `Stress` 且排除 `Marathon`。
- 两项失败都是压力驱动未向需要目标的卡牌提供友方棋盘目标，分别发生在“废铁残械”和 `BG28_303`“变装盗墓贼”。

PlayMode：

- 执行 19：通过 17、失败 2、跳过 0；Unity 正常写出 XML 后以退出码 2 表示测试失败。
- `PlayMode_PlayerDirectedChoice_SearchCloseAndSelectCompleteThroughRaycast`：选择按钮无法被 `GraphicRaycaster` 命中，与历史失败一致。
- `PlayMode_PJ05_RebornTooltipCombatReplayAndConsumptionCompleteThroughRaycast`：Reborn 回放帧中找不到原 `InstanceId`。
- 两项分别隔离复跑仍失败，已排除套件顺序污染。

结论：M0 已建立“发现完整、执行可复核”的可信基线，但当前基线是红色而非全绿。上述失败继续阻断发布绿灯；它们不阻止 M1 的离线发布边界和候选包工程工作。

## 8. 2026-07-29 Phase 6 当前绿线基线

环境与边界：

- Unity：`6000.4.10f1`；单一 Editor PID 30312 与项目内 Unity-MCP 6401 bridge。
- Source commit：`612e9842ade9a517b85a0333b359d07caacf6561`。
- Unity 完整分页发现：EditMode 1527/1527 unique、PlayMode 19/19 unique。
- EditMode 精确分区：普通 1516、Stress 10、唯一显式 Marathon 1；Marathon 本轮排除。

验证结果：

- M2–M4 内容链精确集：11/11 通过，job `b887376f3c924d2599dbf79edbe2ef5d`。
- 普通 EditMode：shard 0–7 共 1360/1360 通过；shard 8 的 156 项中 152 项直接通过，4 项只触发 NUnit 180 秒 Timeout。
- 四个 UI Timeout 在正常编译/域重载后的精确联合隔离 job `9b4686dad7a04badb35937841ad0c3cf` 中 4/4 通过；因此普通 EditMode 有效覆盖为 1516/1516。没有提高 Timeout，也没有修改生产代码。
- Stress：10/10 通过，job `65fbe22d19fb42adb6ec22faff4653da`；唯一 30 分钟 Marathon 未运行。
- PlayMode：19/19 通过，job `b3c952e0c0ff4c06a2ddcd1f5a800ff8`。

UI 超时根因是大型同步 UI 集合在同一 Editor 域内的累计性能退化，失败仅为 Timeout、无断言或业务异常。后续门禁应让重 UI 分片在干净域运行；若仍只有 Timeout，隔离精确失败项验证，不重复整片、不提高超时。

本节取代 M0 红线作为当前发布绿灯；M0 记录继续保留，用于追溯可靠性收敛前后的差异。
