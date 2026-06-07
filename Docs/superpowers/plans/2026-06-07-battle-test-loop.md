# 战斗测试闭环开发计划

> **给后续执行的 Agent：** 实施本计划时，优先使用 `superpowers:subagent-driven-development` 或 `superpowers:executing-plans`。所有任务使用 checkbox (`- [ ]`) 跟踪进度。

**目标：** 把当前已有的调试能力串成一个可重复验证战斗的闭环：保存/加载测试场景，从当前自定义局面一键开战，使用固定随机种子生成确定性战斗日志，并且能在战斗后重置回战前快照反复跑。

**架构原则：** `TavernTrainerView` 继续只负责界面和交互，所有会改变对局状态的动作继续走 `GameCommand` 和 `MatchService`。测试场景不要直接序列化活的 `MatchState` 引用，而是保存一组稳定 DTO。战斗模拟仍然由 `CombatEngine` 克隆双方战场后执行，但 `MatchService` 需要持有一份战前快照，用来支持重跑同一个局面。

**技术栈：** Unity C#、UGUI、NUnit EditMode tests、现有 `MatchService`、`GameCommand`、`CombatEngine`、`CombatLogEntry`、`TavernTrainerView`、JSON 卡牌目录和 `CodexEditModeTestRunner`。

---

## 范围

本版要做：

- 把当前玩家战场、玩家手牌、对手战场、酒馆商店、酒馆等级、金币、最大金币、升本费用、回合、随机种子、生命/护甲和当前阶段保存成命名测试场景。
- 从命名测试场景恢复当前 `MatchService.State`。
- 从当前自定义局面直接运行战斗测试，支持固定随机种子。
- 记录战斗种子、胜负、双方最终战场、步数、安全中止标记和有序战斗日志。
- 战斗后可以重置回战前快照，方便同一局面多次回放。
- 在右侧调试面板增加场景名、保存、加载、运行、种子、重置和最近场景列表。
- 增加测试证明场景保存/加载正确，且同一场景同一种子可稳定复现。

本版暂不做：

- 完整亡语、复生、复仇等战斗事件队列。
- 场景导入/导出 UI。
- 战斗动画时间线。
- 云端同步或跨设备共享。

---

## 数据模型

新增场景 DTO，不要直接保存 `MatchState`。这样可以避免活引用串联、`Dictionary` 序列化坑，也方便未来字段演进。

创建 `Assets/LearnHearthstone/Runtime/Domain/Models/TestScenarioModels.cs`。

核心类型：

```csharp
[Serializable]
public sealed class TestScenarioDefinition
{
    public string Version = "battle-test-loop-v1";
    public string Name;
    public int SavedAtRound;
    public int Seed;
    public MatchPhase Phase;
    public PlayerScenarioState Player = new PlayerScenarioState();
    public OpponentScenarioState Opponent = new OpponentScenarioState();
    public ScenarioTavernState Tavern = new ScenarioTavernState();
    public List<ScenarioCardState> Shop = new List<ScenarioCardState>();
    public List<ScenarioCardState> Hand = new List<ScenarioCardState>();
    public List<ScenarioCardState> PlayerBoard = new List<ScenarioCardState>();
    public List<ScenarioCardState> OpponentBoard = new List<ScenarioCardState>();
}
```

`ScenarioCardState` 保存调试编辑器可能改到的字段：

- 身份：`DefinitionId`、`CardId`、`CardKind`、`Name`
- 实例：`InstanceId`、`Owner`、`PoolSource`、`OriginPoolSource`
- 身材：`Attack`、`Health`、`MaxHealth`、`BaseAttack`、`BaseHealth`
- 标记：`Golden`、`Keywords`、`Tribes`、`EffectIds`、`RuntimeCounters`

`CombatTestOptions`：

```csharp
[Serializable]
public sealed class CombatTestOptions
{
    public int Seed;
    public bool ResetBeforeRun;
    public int SafetyLimit = 200;
}
```

`CombatTestSnapshot`：

```csharp
[Serializable]
public sealed class CombatTestSnapshot
{
    public TestScenarioDefinition BeforeCombat;
    public CombatTestOptions Options;
    public CombatOutput Result;
}
```

实现注意：

- 如果后面需要保存随从池，用 `List<PoolCountState>`，不要直接放 `Dictionary<string,int>`。
- 加载场景时尽量保留 `InstanceId`，这样日志的 `ActorId` 和 `TargetId` 才容易比较。
- 保存 DTO 时必须复制值，不要把 `MinionInstance` 原对象塞进去。

---

## 文件结构

- 新增 `Assets/LearnHearthstone/Runtime/Domain/Models/TestScenarioModels.cs`
  - 定义场景卡牌、玩家、对手、酒馆、战斗测试选项和战前快照 DTO。
- 新增 `Assets/LearnHearthstone/Runtime/Domain/Engine/TestScenarioMapper.cs`
  - 负责 `MatchState -> TestScenarioDefinition` 和 `TestScenarioDefinition -> MatchState`。
- 新增 `Assets/LearnHearthstone/Runtime/Application/Services/TestScenarioRepository.cs`
  - 定义 `ITestScenarioRepository`、`FileTestScenarioRepository`、`InMemoryTestScenarioRepository`。
- 修改 `Assets/LearnHearthstone/Runtime/Application/Commands/GameCommand.cs`
  - 增加保存、加载、运行战斗测试、重置战前快照的命令和 payload。
- 修改 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
  - 注入场景仓库，实现保存/加载、固定种子战斗测试、重置快照。
- 修改 `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs`
  - 确认能从调用方传入 `safetyLimit`，并让日志序号稳定。
- 修改 `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/TavernTrainerView.cs`
  - 增加 `BattleTest` 右侧页签或在现有信息页中加入紧凑战斗测试面板。
- 新增 `Assets/LearnHearthstone/Tests/EditMode/TestScenarioMapperTests.cs`
- 新增 `Assets/LearnHearthstone/Tests/EditMode/MatchServiceBattleTestTests.cs`
- 修改 `Assets/LearnHearthstone/Tests/EditMode/TavernTrainerViewTests.cs`

---

## 任务 1：场景 DTO 和映射器

**文件：**

- 新增：`Assets/LearnHearthstone/Runtime/Domain/Models/TestScenarioModels.cs`
- 新增：`Assets/LearnHearthstone/Runtime/Domain/Engine/TestScenarioMapper.cs`
- 新增：`Assets/LearnHearthstone/Tests/EditMode/TestScenarioMapperTests.cs`

- [ ] **Step 1：先写失败测试**

覆盖这些场景：

- 玩家战场保存后再加载，顺序、身材、金色、关键词、种族不变。
- 玩家手牌同时支持普通随从和酒馆法术。
- 对手战场加载后 owner 仍然是 `BoardSide.Opponent`。
- 酒馆等级、金币、最大金币、升本费用、回合、随机种子和阶段能 round-trip。
- 加载后的随从是新对象，不和保存前状态共享引用。

- [ ] **Step 2：实现保存快照**

在 `TestScenarioMapper` 中加入：

```csharp
public static TestScenarioDefinition Capture(MatchState state, string name)
```

规则：

- 只复制值到 DTO。
- 列表顺序必须完全保留。
- 保存 `State.Player.Tavern.Shop`，虽然首版 UI 重点是战场和手牌。
- 首版可以不保存 `CombatLog`，加载场景后应该从干净战斗日志开始。

- [ ] **Step 3：实现应用快照**

加入：

```csharp
public static void ApplyTo(MatchState target, TestScenarioDefinition scenario)
```

规则：

- 清空并替换玩家战场、玩家手牌、商店、对手战场。
- 如果保存的 owner 缺失或不可信，按目标列表修正 owner。
- 血量和最大血量沿用 `MinionPatch` 的 clamp 规则。
- 加载后清空 `LastResult` 和 `CombatLog`。

- [ ] **Step 4：跑绿测试**

```powershell
& "D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\unity project\Learn Heartstone" -executeMethod CodexEditModeTestRunner.Run -logFile "D:\unity project\Learn Heartstone\UnityScenarioMapperGreen.log"
```

---

## 任务 2：测试场景仓库

**文件：**

- 新增：`Assets/LearnHearthstone/Runtime/Application/Services/TestScenarioRepository.cs`
- 新增或修改：`Assets/LearnHearthstone/Tests/EditMode/MatchServiceBattleTestTests.cs`

- [ ] **Step 1：定义仓库接口**

```csharp
public interface ITestScenarioRepository
{
    IReadOnlyList<string> ListScenarioNames();
    void Save(TestScenarioDefinition scenario);
    TestScenarioDefinition Load(string name);
    bool Exists(string name);
}
```

- [ ] **Step 2：实现测试用内存仓库**

`InMemoryTestScenarioRepository` 用在 EditMode 测试里。保存和读取时要深拷贝，避免测试误判共享引用。

- [ ] **Step 3：实现运行时文件仓库**

运行时保存到：

```csharp
Path.Combine(Application.persistentDataPath, "TestScenarios")
```

文件规则：

- 场景名要转成安全文件名。
- 保存为可读 JSON。
- 场景不存在或 JSON 损坏时，抛出用户能看懂的 `InvalidOperationException`。
- `Version` 固定写入 `battle-test-loop-v1`。

实现建议：

- 优先让 DTO 保持简单，能被 `JsonUtility` 正常处理。
- 如果 `JsonUtility` 对嵌套列表不够用，只在项目已有 JSON 依赖时再使用其它 JSON 包。

---

## 任务 3：命令和 MatchService 流程

**文件：**

- 修改：`Assets/LearnHearthstone/Runtime/Application/Commands/GameCommand.cs`
- 修改：`Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- 新增或修改：`Assets/LearnHearthstone/Tests/EditMode/MatchServiceBattleTestTests.cs`

- [ ] **Step 1：增加命令类型**

在 `GameCommandType` 中增加：

- `SaveTestScenario`
- `LoadTestScenario`
- `RunCombatTest`
- `ResetCombatTestSnapshot`

给 `GameCommand` 增加 payload：

- `string ScenarioName`
- `CombatTestOptions CombatTestOptions`

推荐显式构造函数：

```csharp
public GameCommand(GameCommandType type, string scenarioName, CombatTestOptions options = null)
```

- [ ] **Step 2：注入场景仓库**

扩展默认创建方法：

```csharp
public static MatchService CreateWithDefaultCatalog(int seed = 12345, ITestScenarioRepository scenarios = null)
```

测试里传 `InMemoryTestScenarioRepository`。运行时默认使用 `FileTestScenarioRepository`。

- [ ] **Step 3：实现保存/加载**

处理器：

- `SaveTestScenario(name)`：捕获当前 state 并保存。
- `LoadTestScenario(name)`：读取场景并应用到当前 state。

校验：

- 场景名必填。
- 加载不存在的场景时抛出清晰错误。
- 加载后清空战斗结果和日志。

- [ ] **Step 4：实现固定种子战斗测试**

处理器：

```csharp
private void RunCombatTest(CombatTestOptions options)
```

规则：

- 开战前先保存 `combatTestSnapshot = TestScenarioMapper.Capture(State, "__before_combat__")`。
- 如果 `options.Seed` 有值，精确使用它；否则沿用当前逻辑 `State.Seed + State.Round`。
- 把 `options.SafetyLimit` 传给 `CombatEngine.SimulateBasicCombat`。
- 设置 `State.Phase = MatchPhase.Result`。
- 写入 `State.CombatLog` 和 `State.LastResult`。
- 将结果也放进最近一次 `CombatTestSnapshot`，方便 UI 展示。

- [ ] **Step 5：实现重置**

处理器：

```csharp
private void ResetCombatTestSnapshot()
```

规则：

- 没有战前快照时，首版建议 no-op，方便 UI 使用。
- 有快照时应用回当前 state。
- 首版重置后可以清空 `LastResult` 和 `CombatLog`，让下一次回放从干净状态开始。

---

## 任务 4：确定性战斗日志

**文件：**

- 修改：`Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs`
- 新增或修改：`Assets/LearnHearthstone/Tests/EditMode/MatchServiceBattleTestTests.cs`

- [ ] **Step 1：增加确定性回放测试**

同一个保存场景、同一个种子：

- 第一次和第二次运行胜负一致。
- 双方最终战场的 card id、血量一致。
- `CombatLogEntry.Seq`、`Title`、`ActorId`、`TargetId`、`Detail` 完全一致。

不同种子：

- 不强行断言胜负不同，因为小局面可能结果相同。
- 但要能证明本次战斗确实记录或使用了指定种子。

- [ ] **Step 2：确认 safety limit 从调用方传入**

`CombatEngine.SimulateBasicCombat` 当前已有 `safetyLimit = 200` 参数。`MatchService.RunCombatTest` 必须传入 options 里的值。

- [ ] **Step 3：补充开战和结算日志**

战斗开始前加入：

- `Title = "CombatStarted"`
- `Detail` 包含 seed 和双方战场数量。

战斗结束后加入：

- `Title = "CombatEnded"`
- `Detail` 包含 winner、steps、safety stop。

注意：亡语和复生目前已经会插入日志，所有日志的 `Seq` 必须保持单调递增。

---

## 任务 5：TavernTrainer UI

**文件：**

- 修改：`Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/TavernTrainerView.cs`
- 修改：`Assets/LearnHearthstone/Tests/EditMode/TavernTrainerViewTests.cs`

- [ ] **Step 1：增加 UI 构造测试**

`Build()` 后断言这些对象存在：

- `Tab-BattleTest`
- `BattleTestPanel`
- `ScenarioNameInput`
- `SaveScenarioButton`
- `LoadScenarioButton`
- `CombatSeedInput`
- `RunCombatTestButton`
- `ResetCombatSnapshotButton`
- `ScenarioList`

- [ ] **Step 2：增加右侧页签**

给 `RightInspectorTab` 增加 `BattleTest`。

面板内容：

- 顶部：场景名和当前阶段。
- 场景行：输入框、保存按钮、加载按钮。
- 种子行：数字输入、随机种子按钮、运行按钮。
- 重置行：重置到战前快照按钮。
- 最近场景：从 repository 读取场景名列表。
- 日志预览：展示结果摘要和前 8 条战斗日志。

- [ ] **Step 3：绑定命令**

按钮对应：

- 保存：`GameCommandType.SaveTestScenario`
- 加载：`GameCommandType.LoadTestScenario`
- 运行：`GameCommandType.RunCombatTest`
- 重置：`GameCommandType.ResetCombatTestSnapshot`

UI 规则：

- 默认场景名：`round-{Round}-battle-test`。
- 默认种子：`State.Seed + State.Round`。
- 如果当前 UI helper 支持禁用态，没有快照时置灰重置按钮；否则让 service no-op。
- 每次命令后 `Rebuild()`，异常写入 `lastError`。

---

## 任务 6：回归测试场景

**文件：**

- 新增：`Assets/LearnHearthstone/Tests/EditMode/MatchServiceBattleTestTests.cs`

- [ ] **场景 1：圣盾和剧毒回放**

构造：

- 玩家：一个带 `Keyword.Venomous` 的攻击者。
- 对手：一个带 `Keyword.DivineShield` 和嘲讽的随从。

断言：

- 第一次攻击只打掉圣盾。
- 剧毒只在真正造成战斗伤害时消耗，符合当前关键词规则。
- 重置后重跑，日志完全一致。

- [ ] **场景 2：复生回放**

构造：

- 一个会死亡且带 `Keyword.Reborn` 的随从。

断言：

- 战斗结果里出现 1 血复生复制体。
- 重置后原始随从仍带 `Keyword.Reborn`。

- [ ] **场景 3：满战场自定义局面**

构造：

- 玩家 7 个随从。
- 对手 7 个随从。
- 多个随从改过攻击、血量、关键词。

断言：

- 保存/加载保留全部 14 个随从的顺序。
- 运行战斗不会污染战前快照。

---

## 任务 7：为战斗事件队列预留接口

本版不急着把亡语、复生、复仇全部做完，但要给下一步留好口子。

- [ ] 确认或补齐 `MechanicEventType`：

```csharp
CombatStarted,
BeforeAttack,
AfterAttack,
DamageDealt,
DivineShieldPopped,
MinionDied,
DeathrattleQueued,
DeathrattleResolved,
RebornResolved,
AvengeCounterChanged,
CombatEnded
```

- [ ] 先记下后续事件 payload 形状：

```csharp
public sealed class CombatEvent
{
    public MechanicEventType Type;
    public BoardSide SourceSide;
    public string SourceId;
    public string TargetId;
    public int Amount;
    public int Sequence;
}
```

- [ ] 除非确定性回放测试必须修一个小点，否则本版不实现完整事件队列。下一份计划再把 `CombatEngine.ResolveDeaths` 里的直接结算替换成显式队列。

---

## 最终验证

运行 EditMode 测试：

```powershell
Remove-Item -LiteralPath "D:\unity project\Learn Heartstone\CodexEditModeResults.xml" -ErrorAction SilentlyContinue
& "D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\unity project\Learn Heartstone" -executeMethod CodexEditModeTestRunner.Run -logFile "D:\unity project\Learn Heartstone\UnityBattleTestLoopGreen.log"
```

预期：

- 场景 mapper 测试通过。
- repository 测试通过。
- `MatchService` 保存/加载/运行/重置测试通过。
- UI 构造测试通过。
- 现有战斗、机制、对手自定义、任意拿牌、酒馆和 catalog 测试仍然通过。

提交信息：

```text
Add battle test loop plan
```
