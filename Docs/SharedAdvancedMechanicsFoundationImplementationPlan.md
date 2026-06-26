# 后续机制共用底座实现文档

## 目标

为饰品、任务、畸变和扭曲时空建立一套共用运行时底座，避免后续系统分别硬编码自己的选择、奖励、状态、日志和测试工具。

这份文档只定义公共基础能力，不直接追求任何一个系统的完整官方复刻。完成后，后续机制应该能用同一套模型表达：

- 一次性或延迟选择。
- 长期状态和回合计数。
- 奖励定义和奖励解析。
- 全局或局部规则修正。
- 调试日志、已知限制和测试夹具。

## 当前背景

项目已经有比较清晰的分层：

- `MatchService` 负责命令、经济、酒馆、回合和战斗编排。
- `HeroEffectEngine` 负责按 `cardId` 处理英雄和宝宝效果。
- `TavernState.HeroEffectCounters` 已用于持久计数。
- `DiscoverState` 已能承载部分发现/选择流程。
- `HeroEffectImplementationRegistry` 和 `HeroEffectImplementationGaps.md` 已用于暴露未实现状态。

代码中还没有成型的 `Trinket`、`Quest`、`Timewarp`、`Anomaly` 或 `Aberration` 运行时模型。因此底座应作为新机制层引入，但要沿用当前项目风格。

## 范围

### 第一版包含

- 通用机制种类枚举。
- 通用奖励定义和奖励解析入口。
- 通用选择请求、候选项、确认、取消和 pending 队列。
- 通用机制状态容器，挂到 `TavernState` 或 `MatchState`。
- 通用机制事件派发点。
- 状态注册和已知限制输出。
- EditMode 测试夹具。

### 第一版不包含

- 完整饰品池。
- 完整任务池。
- 完整畸变池。
- 真正回滚整局的时空系统。
- 多人大厅、真实对手排程和 Duos 行为。
- 服务器同步或分享码格式升级。

## 推荐新增模型

建议新增文件：

- `Assets/LearnHearthstone/Runtime/Domain/Models/AdvancedMechanicModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/AdvancedMechanicEngine.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Data/AdvancedMechanicImplementationRegistry.cs`

核心模型：

```csharp
public enum AdvancedMechanicKind
{
    SharedReward,
    Trinket,
    Quest,
    Anomaly,
    Timewarp
}

public enum AdvancedMechanicTrigger
{
    MatchStarted,
    TurnStarted,
    TurnEnded,
    CardBought,
    CardPlayed,
    MinionSold,
    TavernSpellCast,
    ShopRefreshed,
    HeroPowerUsed,
    CombatStarted,
    CombatEnded,
    DiscoverChosen
}

public sealed class MechanicRewardDefinition
{
    public string Id;
    public string DisplayName;
    public AdvancedMechanicKind SourceKind;
    public List<MechanicRewardEffect> Effects = new List<MechanicRewardEffect>();
}

public sealed class MechanicChoiceRequest
{
    public string RequestId;
    public AdvancedMechanicKind SourceKind;
    public string SourceId;
    public int Turn;
    public bool Required;
    public List<MechanicChoiceOption> Options = new List<MechanicChoiceOption>();
}
```

`MechanicRewardEffect` 不要一开始设计成万能脚本语言。第一版只需要覆盖项目已有能力：

- 给金币或最大金币。
- 给 Tavern Coin 或生成法术。
- 给随机/指定随从到手牌、酒馆或棋盘。
- 给目标或随机随从属性和关键词。
- 修改刷新、升级、购买费用。
- 启动已有 `DiscoverState`。
- 写入计数器或标记。

## 状态容器

建议在 `TavernState` 中增加一个明确容器，而不是继续把所有内容塞进 `HeroEffectCounters`：

```csharp
public sealed class AdvancedMechanicState
{
    public List<MechanicChoiceRequest> PendingChoices = new List<MechanicChoiceRequest>();
    public List<string> ActiveMechanicIds = new List<string>();
    public Dictionary<string, int> Counters = new Dictionary<string, int>();
    public Dictionary<string, string> Flags = new Dictionary<string, string>();
}
```

后续系统可以继续拆专用状态：

- `TrinketState`
- `QuestState`
- `AnomalyState`
- `TimewarpState`

但所有系统的 pending choice 和通用奖励可以先经过 `AdvancedMechanicState`。

## 事件派发

`MatchService` 继续是唯一编排入口。新增 `AdvancedMechanicEngine` 后，由 `MatchService` 在已有生命周期点调用：

- 创建对局后：`MatchStarted`
- 回合开始和结束：`TurnStarted`、`TurnEnded`
- 买、卖、打出、施放法术、刷新：复用现有钩子
- 进入战斗和战斗结束：复用当前战斗奖励回写路径
- 选择确认：从 UI 命令进入 `DiscoverChosen` 或新的 `ChooseMechanicOption`

不要让 UI 直接改 `TavernState`。UI 只能发命令，命令由 `MatchService` 校验并交给机制引擎。

## 命令层扩展

建议扩展 `GameCommandType`：

- `ChooseMechanicOption`
- `CancelMechanicChoice`

后续饰品可再增加：

- `PurchaseTrinket`

第一版命令字段优先复用现有 `ChoiceId`、目标区域和目标索引。若不够，再增加：

```csharp
public string MechanicRequestId;
public string MechanicOptionId;
public string MechanicSourceId;
```

## UI 基础能力

共用底座只需要提供三种 UI 能力：

1. **选择弹窗**
   - 展示来源、候选项、费用或条件。
   - 支持确认、取消。
   - 必选项不能静默跳过。

2. **状态条**
   - 展示当前激活的饰品、任务、畸变或时空效果。
   - 允许点击查看简短说明。

3. **日志输出**
   - 记录机制来源、触发时机、奖励结果和代理限制。
   - 未实现或近似实现时写入清晰提示。

## 数据来源

第一版不要接网络。使用本地 JSON 或 C# 静态定义即可。

建议路径：

- `Assets/LearnHearthstone/Resources/Data/advancedMechanicRewards.json`
- `Assets/LearnHearthstone/Resources/Data/trinkets.json`
- `Assets/LearnHearthstone/Resources/Data/quests.json`
- `Assets/LearnHearthstone/Resources/Data/anomalies.json`
- `Assets/LearnHearthstone/Resources/Data/timewarps.json`

如果先用 C# 静态定义，也要把后续 JSON 化路径写在注释或文档里，避免硬编码长期扩散。

## 注册表和可见性

新增 `AdvancedMechanicImplementationRegistry`，状态沿用英雄注册表思路：

| 状态 | 含义 |
| --- | --- |
| Implemented | 已有可玩实现和测试 |
| FrameworkFirst | 有可见代理或部分框架 |
| Planned | 已排期但未实现 |
| Deferred | 依赖更大系统 |
| Unsupported | 当前版本明确不支持 |

所有机制定义进入数据源后，都必须有注册项。测试要覆盖“数据源里有，但注册表没有”的失败路径。

## 测试策略

新增测试建议：

- `AdvancedMechanicRegistryTests`
  - 数据源全覆盖。
  - duplicate id 失败。
  - unsupported 状态可见。

- `AdvancedMechanicChoiceTests`
  - 创建 pending choice。
  - 确认合法 option。
  - 拒绝非法 option。
  - 必选项未处理时主流程提示。

- `AdvancedMechanicRewardTests`
  - 奖励金币、卡牌、属性、关键词。
  - 奖励目标不存在时不崩溃。
  - 随机奖励使用 seeded rng。

- `AdvancedMechanicSerializationTests`
  - 状态能随 `MatchState` 保存。
  - 旧存档缺少 `AdvancedMechanicState` 时能回退默认值。

## 实施顺序

1. 增加模型和空引擎。
2. 增加状态容器和空注册表。
3. 增加选择命令和 UI 弹窗复用入口。
4. 增加通用奖励解析器，先覆盖金币、卡牌、属性、关键词。
5. 接入 `MatchService` 的生命周期派发。
6. 增加日志和实现状态可见性。
7. 跑注册、选择、奖励、序列化测试。

## 完成标准

- 后续系统不需要各自实现一套 pending choice。
- 后续系统不需要各自实现一套简单奖励解析。
- 未实现机制能在注册表、日志或 UI 中可见。
- 旧对局和无机制对局不受影响。
- 至少有一组测试证明共享奖励和选择流程能被后续系统复用。

## 风险

| 风险 | 处理 |
| --- | --- |
| 底座设计过大，拖慢可玩机制落地。 | 第一版只支持当前项目已有奖励类型，不做脚本语言。 |
| 与 `HeroEffectEngine` 职责重叠。 | 英雄/宝宝仍归 `HeroEffectEngine`；饰品、任务、畸变、时空归 `AdvancedMechanicEngine`。 |
| UI 选择状态和 `DiscoverState` 重复。 | 第一版可复用 `DiscoverState` 展示，但 pending choice 数据归共用机制状态。 |
| 机制状态破坏旧存档。 | 新字段必须默认空对象，加载缺失字段时回退。 |
