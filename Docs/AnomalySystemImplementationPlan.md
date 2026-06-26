# 畸变系统实现文档

## 目标

畸变是对整局战场规则的全局修改。它和饰品、任务不同：饰品和任务主要挂在玩家身上，畸变应该挂在对局规则层，影响双方共享的经济、酒馆、卡池、战斗准备或回合流程。

第一版目标不是一次性补齐所有官方畸变，而是建立一个安全的畸变运行层，让对局可以在创建时选择“无畸变、指定畸变、随机畸变”，并保证畸变效果可见、可复现、可测试、可关闭。

## 依赖

实现顺序放在任务机制之后、扭曲时空之前。

必须依赖：

- `AdvancedMechanicState`：承载当前高级机制状态。
- `MatchSetupOptions.AdvancedMechanicMode`：允许玩家选择启用哪些机制。
- `MechanicChoiceRequest`：如果某个畸变需要开局选择，复用共用选择弹窗。
- `RecruitLog` 和 `CombatLog`：所有畸变触发都写日志。
- `CardPoolAvailability`：任何改动卡池或生成候选卡的畸变都必须经过卡池版本和种族可用性校验。
- `TavernShopSlots`：任何改动酒馆槽位的畸变都必须重新同步槽位。

## 第一版范围

### 包含

- 对局最多一个主畸变。
- 对局创建时可指定畸变、随机畸变或禁用畸变。
- 畸变状态持久化到 `MatchState`。
- 支持经济、酒馆、回合、卡池轻量过滤、战斗开始这五类效果。
- UI 显示当前畸变名称、文本和触发日志。
- 畸变和饰品、任务可以同时存在，但按固定事件顺序结算。

### 不包含

- 多畸变叠加。
- 官方全量畸变池。
- 双打、队友、传递、共享资源类畸变。
- 战斗中复杂事件改写，例如攻击顺序重排、战斗中购买、战斗回滚。
- 网络同步和分享码兼容。

## 数据模型

新增文件建议：

- `Assets/LearnHearthstone/Runtime/Domain/Models/AnomalyModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Data/AnomalyCatalog.cs`
- `Assets/LearnHearthstone/Runtime/Adapters/Data/AnomalyCatalogLoader.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/AnomalyEngine.cs`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsAnomalies.json`

核心模型：

```csharp
public enum AnomalyOfferPoolStatus
{
    Offerable,
    DebugOnly,
    Disabled
}

public enum AnomalyTrigger
{
    MatchStarted,
    TurnStarted,
    TurnEnded,
    BeforeShopRefresh,
    AfterShopRefresh,
    CardBought,
    MinionPlayed,
    TavernSpellCast,
    BeforeCombat,
    AfterCombat
}

public enum AnomalyEffectKind
{
    None,
    StartingGoldBonus,
    TurnStartGoldBonus,
    RefreshCostModifier,
    UpgradeCostModifier,
    ShopSizeModifier,
    TavernTierCapModifier,
    TavernSpellShopBias,
    MinionPoolIncludeTribe,
    MinionPoolExcludeTribe,
    StartCombatBoardBuff,
    EndTurnRandomFriendlyBuff,
    TurnStartDiscoverFromTier,
    ShopMinionsHaveStats,
    FirstBuyEachTurnDiscount
}

public sealed class AnomalyDefinition
{
    public string Id;
    public string CardId;
    public int DbfId;
    public string Name;
    public string Text;
    public string ImagePath;
    public string ImageUrl;
    public AnomalyTrigger Trigger;
    public AnomalyEffectKind EffectKind;
    public int AttackBonus;
    public int HealthBonus;
    public int GoldAmount;
    public int CostDelta;
    public int TargetCount;
    public int TavernTier;
    public Tribe Tribe;
    public AnomalyOfferPoolStatus OfferPoolStatus = AnomalyOfferPoolStatus.Offerable;
    public List<string> Tags = new List<string>();
    public string Notes;
}

public sealed class PlayerAnomalyState
{
    public string ActiveAnomalyId;
    public string ActiveAnomalyName;
    public string ActiveAnomalyText;
    public string ActiveAnomalyImagePath;
    public Dictionary<string, int> Counters = new Dictionary<string, int>();
    public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
}
```

`TavernState.AdvancedMechanics` 中增加：

```csharp
public PlayerAnomalyState Anomalies = new PlayerAnomalyState();
```

如果后续要支持“大厅共享畸变”，再把 `PlayerAnomalyState` 提升为 `MatchAnomalyState`。第一版仍放在本地玩家 tavern 状态里，和当前单人训练器结构一致。

## 对局创建流程

`MatchSetupOptions` 增加：

```csharp
public string SelectedAnomalyId;
public bool RandomizeAnomaly;
public bool DisableAnomaly = true;
```

流程：

1. 创建对局时读取 `AdvancedMechanicMode`。
2. 如果模式是 `Anomalies` 或 `Mixed`，并且 `DisableAnomaly == false`，进入畸变选择。
3. `SelectedAnomalyId` 非空时直接挂载指定畸变。
4. `RandomizeAnomaly == true` 时从 `OfferableAnomalies` 按 seed 抽一个。
5. 没有指定也没有随机时默认无畸变。
6. 成功挂载后写入 `AdvancedMechanics.Anomalies`，并写 recruit log。
7. 触发 `AnomalyTrigger.MatchStarted`。

同一时刻若已有任务或饰品开局选择，畸变不应该覆盖 `PendingChoice`。若第一版仍只有单个 `PendingChoice`，采用固定顺序串行：

1. 畸变选择或挂载。
2. 任务开局选择。
3. 饰品回合选择。

如果畸变本身需要玩家选择，必须等前一个 pending choice 处理完再弹出。

## 事件接入顺序

畸变是全局规则，应尽量早于玩家个人奖励结算，但晚于基础规则初始化。

推荐顺序：

### 对局开始

1. 创建基础 `MatchState`。
2. 应用英雄初始属性。
3. 挂载畸变。
4. 触发畸变 `MatchStarted`。
5. 触发英雄 `MatchStarted`。
6. 触发任务、饰品等开局选择。

### 回合开始

1. 基础金币、酒馆刷新、升级费用递减。
2. 畸变 `TurnStarted`。
3. 英雄 `TurnStarted`。
4. 饰品 `TurnStarted`。
5. 任务奖励 `TurnStarted`。
6. 随从回合开始效果。

### 刷新酒馆

1. 计算刷新费用：基础费用、英雄修正、畸变修正、免费刷新。
2. 畸变 `BeforeShopRefresh`。
3. 正常刷新 shop。
4. 应用卡池版本和种族限制。
5. 畸变 `AfterShopRefresh`。
6. 饰品和任务的 shop refresh 效果。
7. 同步 `TavernShopSlots`。

### 战斗开始

1. 从酒馆棋盘复制战斗棋盘。
2. 英雄战斗开始修正。
3. 饰品战斗开始修正。
4. 任务战斗开始修正。
5. 畸变 `BeforeCombat`。
6. 调用 `CombatEngine`。

若某个畸变是“永久修改酒馆棋盘”，必须在酒馆阶段完成，不要在战斗副本内悄悄回写。

## 首批 MVP 畸变

第一批建议只做 8 个低风险畸变，覆盖各类接入点。

| ID | 名称 | 触发 | 效果 | 备注 |
| --- | --- | --- | --- | --- |
| `LH_ANOMALY_EXTRA_START_GOLD` | Overflowing Pockets | `MatchStarted` | 开局额外获得 1 金币，最大金币不变 | 经济最小闭环 |
| `LH_ANOMALY_RICH_TURNS` | Rich Turns | `TurnStarted` | 每回合开始额外获得 1 金币 | 要受最大金币上限保护 |
| `LH_ANOMALY_CHEAP_REFRESH` | Restless Tavern | 刷新费用计算 | 刷新费用 -1，最低 0 | 与免费刷新、英雄刷新费用叠加顺序要测 |
| `LH_ANOMALY_BIG_SHOP` | Crowded Tavern | `MatchStarted` | 酒馆槽位 +1 | 要同步 `TavernShopSlots` |
| `LH_ANOMALY_SMALL_SHOP` | Quiet Tavern | `MatchStarted` | 酒馆槽位 -1，最低 1 | 只进 Debug 池 |
| `LH_ANOMALY_COMBAT_DRILL` | Combat Drill | `BeforeCombat` | 战斗开始友方随从 +1/+1，仅战斗副本 | 不回写酒馆 |
| `LH_ANOMALY_END_TURN_TRAINING` | End Turn Training | `TurnEnded` | 回合结束随机友方随从 +1/+1 | 永久 buff |
| `LH_ANOMALY_SPELL_MARKET` | Spell Market | `AfterShopRefresh` | 刷新后至少保留 1 张酒馆法术，若当前 tier 有可用法术 | 必须经过 `AvailableTavernSpells` |

这些 ID 是本项目代理畸变，不冒充官方 ID。等官方畸变池整理完后，再把 `cardId/dbfId/imageUrl` 接入 HearthstoneJSON 或营地图片。

## 效果实现细节

### 金币类

使用 `GrantGold` 风格的 helper，遵守：

- 不允许金币为负。
- 不突破 `StatMath.MaxStat`。
- 如果效果描述是“获得金币”，可以超过当前最大金币；如果是“每回合金币上限”，再改 `MaxGold`。
- 日志写清楚来源。

### 费用类

不要在每次使用处硬编码。新增统一费用修正 helper：

```csharp
private int ModifyRefreshCostByAnomaly(int baseCost)
private int ModifyUpgradeCostByAnomaly(int baseCost)
```

结算顺序建议：

1. 基础费用。
2. 英雄费用修正。
3. 畸变费用修正。
4. 饰品、任务等个人机制修正。
5. 免费刷新或健康支付等特殊规则覆盖。

### 酒馆槽位类

在 `TavernState` 增加：

```csharp
public int AnomalyShopSizeDelta;
```

`TavernRules.GetShopSize(tier)` 后统一叠加 delta，再 clamp 到 `1..TavernRules.MaxShopSize`。任何改变后都调用 `TavernShopSlots.Ensure(tavern)`。

### 卡池类

卡池类畸变只能修改候选过滤条件，不直接塞非法卡。

新增 helper：

```csharp
private IEnumerable<MinionDefinition> ApplyAnomalyMinionPoolRules(IEnumerable<MinionDefinition> candidates)
private IEnumerable<TavernSpellDefinition> ApplyAnomalySpellPoolRules(IEnumerable<TavernSpellDefinition> candidates)
```

处理顺序：

1. 卡池版本过滤。
2. 种族可用性过滤。
3. 畸变 include/exclude。
4. tier 和来源过滤。

如果畸变要求“额外启用某种族”，也不能绕过版本不可用卡。

### 战斗类

战斗类第一版只允许两种：

- 修改战斗副本。
- 写入 `TavernState.NextCombat...` 这种已有临时战斗字段。

战斗结束后必须重置畸变临时字段，防止串到下一场。

## UI

### 对局设置

在高级机制设置区增加：

- 关闭畸变。
- 随机畸变。
- 指定畸变。

如果当前 `AdvancedMechanicMode` 不是 `Anomalies` 或 `Mixed`，畸变设置不可用或折叠。

### 局内展示

在现有高级机制状态区显示：

- 畸变名称。
- 简短文本。
- 图标或卡图。
- “代理实现”标记，如果不是官方完整复刻。

不要用大段说明占据主界面。详细说明放到 hover 或点击展开面板。

### 日志

每次触发写入：

```text
Anomaly: Combat Drill gave combat board +1/+1.
```

如果没有合法目标：

```text
Anomaly: Spell Market found no legal Tavern spell candidates.
```

## 测试计划

新增测试文件建议：

- `AnomalyCatalogTests.cs`
- `AnomalySetupTests.cs`
- `AnomalyEconomyTests.cs`
- `AnomalyShopTests.cs`
- `AnomalyCombatTests.cs`

必须覆盖：

- JSON 能加载，ID 不重复。
- `Offerable`、`DebugOnly`、`Disabled` 池拆分正确。
- 默认无畸变不改变现有对局。
- 指定畸变可复现。
- 随机畸变同 seed 可复现。
- 金币类不产生负数或非法上限。
- 刷新费用类和免费刷新不冲突。
- 酒馆槽位变化后 shop slot 同步。
- 卡池类不会生成版本外或 ban 种族卡。
- 战斗类不回写酒馆棋盘。
- 和任务、饰品同时开启时，pending choice 不互相覆盖。

## 实施顺序

1. 建立 `AnomalyModels`、`AnomalyCatalog`、`AnomalyCatalogLoader`。
2. 在 `AdvancedMechanicState` 增加 `PlayerAnomalyState`。
3. 在 `MatchSetupOptions` 增加畸变配置字段。
4. 实现 `AnomalyEngine` 空分发和日志。
5. 接入 `MatchStarted`、`TurnStarted`、`TurnEnded`、`BeforeShopRefresh`、`AfterShopRefresh`、`BeforeCombat`。
6. 实现 8 个 MVP 畸变。
7. 增加 UI 状态展示和高级设置入口。
8. 增加测试。
9. 做人工冒烟：无畸变、指定畸变、随机畸变、畸变+任务、畸变+饰品。

## 完成标准

- 对局可以无畸变、指定畸变、随机畸变启动。
- 当前畸变在 UI 和日志中可见。
- 至少 8 个 MVP 畸变可玩。
- 畸变不破坏无畸变默认流程。
- 畸变和任务、饰品同时开启时不会覆盖 pending choice。
- 所有卡池和 shop 修改都经过合法性过滤。
- 自动测试覆盖 catalog、setup、economy、shop、combat 和 mixed mode。

## 风险和处理

| 风险 | 处理 |
| --- | --- |
| 全局规则影响面太大 | 第一版只做低风险经济、酒馆、轻量战斗效果 |
| 与英雄费用规则冲突 | 建立统一费用计算顺序并为已实现英雄加回归测试 |
| 卡池畸变生成非法卡 | 所有候选必须经过 `CardPoolAvailability` 和种族限制 |
| 与任务、饰品选择互相覆盖 | pending choice 必须串行或升级为队列 |
| 玩家看不懂当前规则 | 状态区常驻显示畸变名称和文本 |
