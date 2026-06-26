# 扭曲时空系统实现文档

## 目标

扭曲时空用于实现 Morchie、Murozond, Unbounded 以及后续“从历史中复制、发现、恢复局部状态”的机制。

它比饰品、任务、畸变更危险，因为它涉及历史状态、深拷贝、实例 id、卡池合法性和潜在回滚。第一版只做安全的“历史快照”和“历史复制奖励”，不做通用撤销、不做整局回滚、不做战斗中回滚。

## 依赖

实现顺序放在畸变之后。

必须依赖：

- `AdvancedMechanicState`：承载 Timewarp 状态。
- `MechanicChoiceRequest`：承载 Minor Timewarp 或历史奖励三选一。
- `OpponentHistoryState`：复用已有对手战队历史。
- `MinionInstance.Clone()` 或等价深拷贝：所有快照必须独立于当前状态。
- `TavernShopSlots`：恢复或替换 shop 后必须同步。
- `CardPoolAvailability`：复制历史卡时仍要检查当前版本和种族可用性。
- `RecruitLog` 和 `CombatLog`：所有历史来源都要可追踪。

## 第一版范围

### 包含

- 回合开始、刷新前、战斗后、对手战队四类轻量快照。
- 快照数量上限和裁剪。
- 从历史棋盘、历史手牌、历史酒馆、历史对手战队中复制卡。
- Minor Timewarp 三选一奖励。
- Morchie 的可见 Timewarp 代理流程。
- Murozond, Unbounded 的历史对手战队复制代理流程。
- UI 显示最近快照数量和 pending timewarp 奖励。

### 不包含

- 任意操作撤销。
- 整局状态回滚。
- 战斗中时间回滚。
- 回滚金币、血量、护甲、任务进度、饰品购买等全局状态。
- 多人大厅时间线同步。
- 双打队友历史、传递或共享手牌。

## 数据模型

新增文件建议：

- `Assets/LearnHearthstone/Runtime/Domain/Models/TimewarpModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Data/TimewarpCatalog.cs`
- `Assets/LearnHearthstone/Runtime/Adapters/Data/TimewarpCatalogLoader.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/TimewarpEngine.cs`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsTimewarps.json`

核心模型：

```csharp
public enum TimewarpSnapshotKind
{
    TurnStart,
    BeforeShopRefresh,
    AfterCombat,
    OpponentWarband
}

public enum TimewarpRewardKind
{
    CopyHistoricalBoardMinion,
    CopyHistoricalHandCard,
    CopyHistoricalShopMinion,
    DiscoverHistoricalOpponentMinion,
    RefreshShopFromHistoricalShop,
    GainHistoricalTavernSpell
}

public sealed class TimewarpSnapshot
{
    public string SnapshotId;
    public TimewarpSnapshotKind Kind;
    public int Round;
    public int TavernTier;
    public int Gold;
    public int MaxGold;
    public List<MinionInstance> Board = new List<MinionInstance>();
    public List<MinionInstance> Hand = new List<MinionInstance>();
    public List<MinionInstance> Shop = new List<MinionInstance>();
    public List<OpponentWarbandSnapshot> OpponentWarbands = new List<OpponentWarbandSnapshot>();
    public string Source;
}

public sealed class TimewarpRewardDefinition
{
    public string Id;
    public string Name;
    public string Text;
    public TimewarpRewardKind Kind;
    public TimewarpSnapshotKind PreferredSnapshotKind;
    public int TargetCount;
    public bool RequiresLegalCurrentPool = true;
    public List<string> Tags = new List<string>();
    public string Notes;
}

public sealed class PlayerTimewarpState
{
    public List<TimewarpSnapshot> Snapshots = new List<TimewarpSnapshot>();
    public Dictionary<string, int> Counters = new Dictionary<string, int>();
    public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
    public List<string> PendingRewardIds = new List<string>();
}
```

`AdvancedMechanicState` 增加：

```csharp
public PlayerTimewarpState Timewarp = new PlayerTimewarpState();
```

## 快照原则

快照最重要的规则是：不允许引用当前可变对象。

必须满足：

- 快照中的 `Board/Hand/Shop` 全部深拷贝。
- 快照保存时生成新的 `SnapshotId`。
- 快照只保存第一版需要的字段，不保存完整 `MatchState`。
- 快照裁剪后旧对象不可再被奖励引用。
- 从快照复制到当前状态时必须生成新的 `InstanceId`。
- 快照不写回，不修改。

建议上限：

```csharp
public const int MaxTurnStartSnapshots = 3;
public const int MaxBeforeRefreshSnapshots = 1;
public const int MaxAfterCombatSnapshots = 2;
public const int MaxOpponentWarbandSnapshots = 3;
```

每次写入后按 `Kind + Round` 裁剪。

## 快照写入点

### 回合开始

在新回合 shop 刷新完成、金币设置完成、任务和饰品回合开始效果之前写入 `TurnStart` 快照。这样快照代表“本回合自然开局状态”。

保存字段：

- `Round`
- `Gold`
- `MaxGold`
- `TavernTier`
- `Board`
- `Hand`
- `Shop`

### 刷新前

在 `RerollShop` 消费费用之后、替换 shop 之前写入 `BeforeShopRefresh` 快照。

用途：

- “复制刷新前酒馆的一个随从”
- “把酒馆刷新回上一版 shop”

注意：恢复 shop 不恢复金币，不退刷新费用。

### 战斗后

在 combat result 应用奖励和死亡记录之后写入 `AfterCombat` 快照。

用途：

- 复制上场战斗相关的历史死亡随从。
- 后续扩展战斗后奖励。

第一版不保存战斗 replay，只保存当前酒馆相关轻量状态和最近死亡摘要。

### 对手战队

复用 `CaptureLastOpponentWarband` 和 `OpponentHistoryState`。在每次战斗前保存对手战队快照，Timewarp 只读取，不修改。

用途：

- Murozond 从历史对手战队发现或复制随从。

## 奖励池

第一版 Timewarp 奖励只做历史复制，不做全状态恢复。

| ID | 名称 | 类型 | 效果 | 风险 |
| --- | --- | --- | --- | --- |
| `LH_TIMEWARP_COPY_BOARD` | Echo of Your Board | `CopyHistoricalBoardMinion` | 从最近回合开始快照的友方棋盘复制 1 个随从到手牌 | 低 |
| `LH_TIMEWARP_COPY_HAND` | Echo of Your Hand | `CopyHistoricalHandCard` | 从最近回合开始快照的手牌复制 1 张牌到手牌 | 中，手牌上限 |
| `LH_TIMEWARP_COPY_SHOP` | Echo of the Tavern | `CopyHistoricalShopMinion` | 从最近刷新前或回合开始 shop 复制 1 个合法随从到手牌 | 低 |
| `LH_TIMEWARP_RESTORE_SHOP` | Rewound Tavern | `RefreshShopFromHistoricalShop` | 将当前 shop 替换为最近刷新前 shop 的合法拷贝 | 中，需要同步槽位 |
| `LH_TIMEWARP_OPPONENT_DISCOVER` | Echo of the Enemy | `DiscoverHistoricalOpponentMinion` | 从最近对手战队历史中发现 1 个随从复制到手牌 | 中，需要 discover |
| `LH_TIMEWARP_GAIN_SPELL` | Old Spellwork | `GainHistoricalTavernSpell` | 从历史手牌或 shop 中复制 1 张酒馆法术 | 中，需要 spell 合法性 |

如果没有可用历史，奖励 no-op 并写日志，不抛异常。

## Morchie 实现路径

Morchie 的第一版代理目标是“提供 Minor Timewarp 选择”，不是完整复刻所有官方行为。

建议流程：

1. 选择 Morchie 或启用 Timewarp 模式时初始化 `PlayerTimewarpState`。
2. 每回合开始写入 `TurnStart` 快照。
3. 到触发时机时创建 `MechanicChoiceRequest`：
   - `Kind = AdvancedMechanicKind.Timewarp`
   - `Source = "morchie-minor-timewarp"`
   - `Options = 3`
4. 三个选项从 Timewarp 奖励池按 seed 抽取。
5. 选项 UI 显示奖励名称、文本、来源快照回合。
6. 选择后执行对应历史复制奖励。
7. 写 recruit log。

触发时机第一版建议用调试按钮或固定回合，等英雄资料补齐后再改成官方触发。

## Murozond, Unbounded 实现路径

Murozond 第一版只实现“从历史对手战队复制”，不实现对手完整行为时间线。

流程：

1. 每次战斗开始前保存对手战队到 `OpponentHistoryState`。
2. Murozond 触发时读取最近 `OpponentWarband` 快照。
3. 过滤非法候选：
   - 空对象。
   - Tavern tier 不合法。
   - 当前卡池版本不允许。
   - 当前种族 ban。
4. 如果候选数量大于 3，启动 discover。
5. 如果候选数量 1 到 3，直接作为选择项。
6. 选择后复制到手牌，生成新 `InstanceId`。
7. 没有历史时 fallback 到当前可编辑对手棋盘，并写清楚这是调试代理。

## 执行细节

### 深拷贝

新增 helper：

```csharp
private static MinionInstance CloneForSnapshot(MinionInstance source)
private static MinionInstance CloneFromSnapshotToHand(MinionInstance source, string instanceIdPrefix)
```

快照拷贝保留：

- `CardId`
- `DefinitionId`
- `Name`
- `CardKind`
- `BaseAttack/BaseHealth`
- `Attack/Health/MaxHealth`
- `TavernTier`
- `Tribes`
- `Keywords`
- `Enchantments`
- `Counters`
- `Tags`
- `ImagePath`

复制到手牌时重置：

- `InstanceId`
- `Owner = BoardSide.Player`
- `AttacksThisCombat = 0`
- combat-only tags
- killed-by tags

### 合法性过滤

历史复制不是无条件复活旧卡。第一版默认 `RequiresLegalCurrentPool = true`。

过滤规则：

- `CardKind.Minion` 必须能在当前 `MinionCatalog` 找到定义。
- 卡池版本必须允许。
- 当前 active tribes 必须允许。
- 手牌满则不加入，并写日志。
- shop 恢复时只恢复合法卡，不合法位置置空或重新补合法随机卡。第一版建议置空后 `TavernShopSlots.Ensure`。

### 实例 id

所有从历史复制出的牌使用：

```text
timewarp-{rewardId}-{round}-{counter}
```

不要复用快照内旧实例 id，否则会破坏场上定位、战斗 replay 和后续拖拽。

### pending choice

Timewarp 选择不能覆盖任务或饰品选择。

如果当前 `AdvancedMechanics.PendingChoice != null`：

- 第一版：记录 pending 被阻塞，不生成新选择。
- 后续：升级为 `PendingChoices` 队列。

## UI

### 状态区

显示：

- Timewarp 已启用。
- 当前保存的快照数量。
- 最近快照来自第几回合。
- 是否有待选择 Timewarp 奖励。

### 选择弹窗

复用高级机制选择弹窗：

- 主标题：Minor Timewarp 或对应来源。
- 选项标题：奖励名称。
- 选项文本：奖励说明。
- 副文本：来源快照，如“来自第 5 回合开始”。
- 图片：可先用来源英雄或通用 Timewarp 图标，后续接官方图。

### 调试入口

建议在调试工具里增加：

- 保存当前 Timewarp 快照。
- 查看快照摘要。
- 触发 Minor Timewarp。
- 清空 Timewarp 快照。

## 测试计划

新增测试文件建议：

- `TimewarpSnapshotTests.cs`
- `TimewarpRewardTests.cs`
- `MorchieTimewarpTests.cs`
- `MurozondTimewarpTests.cs`
- `TimewarpSerializationTests.cs`

必须覆盖：

- 回合开始写入快照。
- 快照是深拷贝，修改当前对象不会影响历史。
- 快照数量裁剪生效。
- 从历史棋盘复制随从到手牌。
- 从历史手牌复制卡到手牌。
- 从历史 shop 复制随从到手牌。
- 恢复历史 shop 后同步 slot。
- 手牌满时奖励 no-op。
- 当前卡池不允许的历史卡不会被复制。
- 没有历史时不崩溃并写日志。
- Morchie 能产生三选一 Timewarp 奖励。
- Murozond 优先读取历史对手战队。
- pending choice 已存在时不会覆盖任务或饰品选择。
- 序列化缺少 Timewarp 状态时能回退空状态。

## 实施顺序

1. 建立 `TimewarpModels`、`TimewarpCatalog`、`TimewarpCatalogLoader`。
2. 在 `AdvancedMechanicState` 增加 `PlayerTimewarpState`。
3. 增加快照深拷贝 helper 和裁剪 helper。
4. 接入 `TurnStarted`、`BeforeShopRefresh`、`AfterCombat`、`OpponentWarband` 四类快照。
5. 实现历史合法性过滤。
6. 实现 `CopyHistoricalBoardMinion`、`CopyHistoricalHandCard`、`CopyHistoricalShopMinion`。
7. 实现 `RefreshShopFromHistoricalShop`，同步 `TavernShopSlots`。
8. 实现 Timewarp 三选一请求。
9. 接入 Morchie 代理触发。
10. 接入 Murozond 历史对手复制。
11. 增加 UI 状态区和调试入口。
12. 补自动测试和人工冒烟。

## 人工冒烟清单

- 无 Timewarp 对局正常开始、买卖、刷新、战斗。
- 开启 Timewarp 后回合开始写入快照。
- 修改当前棋盘后，历史快照不变。
- 触发 Minor Timewarp 后出现三选一。
- 复制历史棋盘随从到手牌，实例 id 是新的。
- 手牌满时选择奖励不崩。
- 恢复历史 shop 后槽位不乱。
- Murozond 从上一场对手战队中给出候选。
- 和任务开局选择同时存在时，Timewarp 不覆盖 pending choice。

## 完成标准

- Timewarp 状态能随 `MatchState` 保存和读取。
- 快照有上限且不会引用当前可变对象。
- 至少 4 个历史复制奖励可玩。
- Morchie 有可见 Minor Timewarp 代理流程。
- Murozond 能从历史对手战队复制或发现候选。
- 没有历史、手牌满、卡池不合法时都安全 no-op。
- 自动测试覆盖快照、奖励、英雄代理、pending choice、序列化。

## 风险和处理

| 风险 | 处理 |
| --- | --- |
| 快照引用当前对象导致状态串改 | 所有快照必须深拷贝，并用测试验证 |
| 复制历史卡生成非法当前卡 | 默认要求当前卡池合法，不合法则过滤 |
| 玩家误以为支持整局回滚 | UI 和文档明确第一版是历史复制，不是通用撤销 |
| pending choice 覆盖任务或饰品 | 第一版阻塞并写日志，后续升级队列 |
| 快照内存增长 | 按 kind 设置上限，每次写入后裁剪 |
| 恢复 shop 破坏槽位 | 恢复后统一调用 `TavernShopSlots.Ensure` |
