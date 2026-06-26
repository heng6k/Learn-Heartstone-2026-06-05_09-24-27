# 任务系统实现文档

## 目标

实现 Battlegrounds 任务与任务奖励系统，让德纳修斯大帝（Sire Denathrius）和阴暗贵族（Shady Aristocrat）等机制可以从文档缺口进入可玩状态。

这里的“任务”指对局内 Quest/Reward 机制，不是 `AlphaReleaseRoadmap.md` 中的新手教程任务。

## 依赖

任务系统依赖：

- [SharedAdvancedMechanicsFoundationImplementationPlan.md](SharedAdvancedMechanicsFoundationImplementationPlan.md)
- 饰品系统中沉淀出的 `MechanicRewardDefinition` 和奖励解析能力

任务比饰品多两层：

- 任务目标进度。
- 完成后奖励激活。

因此推荐在饰品 MVP 之后实现。

## 第一版范围

### 包含

- 对局开始或事件触发时发现任务。
- 任务目标定义。
- 任务进度追踪。
- 任务完成后激活奖励。
- 奖励可复用通用奖励定义。
- 德纳修斯大帝和阴暗贵族最小可玩路径。

### 不包含

- 完整官方任务池。
- 完整官方任务难度动态调整。
- 复杂多人条件。
- 所有奖励的官方精确行为。

## 数据模型

建议新增：

- `Assets/LearnHearthstone/Runtime/Domain/Models/QuestModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Data/QuestCatalog.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/QuestEngine.cs`

核心模型：

```csharp
public enum QuestObjectiveKind
{
    BuyCards,
    BuyMinions,
    BuyTavernSpells,
    SellMinions,
    SpendGold,
    RefreshShop,
    CastTavernSpells,
    TriggerBattlecry,
    FriendlyMinionsDie,
    WinOrTieCombat
}

public sealed class QuestDefinition
{
    public string Id;
    public string CardId;
    public string Name;
    public string Text;
    public QuestObjectiveDefinition Objective;
    public string RewardId;
    public List<string> Tags = new List<string>();
}

public sealed class QuestObjectiveDefinition
{
    public QuestObjectiveKind Kind;
    public int RequiredAmount;
    public string RequiredTag;
}

public sealed class ActiveQuestState
{
    public string QuestId;
    public string RewardId;
    public int Progress;
    public int RequiredAmount;
    public bool Completed;
    public bool RewardActive;
}
```

第一版允许一个玩家同时只有一个主任务。阴暗贵族带来的额外任务如果需要并行，先进入队列或替换为“发现并激活一个额外任务槽”的后续扩展。

## 任务选择流程

### 开局任务

德纳修斯大帝需要开局二选一任务：

1. `MatchStarted` 检查英雄。
2. 生成两个可用任务。
3. 写入 `MechanicChoiceRequest`。
4. 玩家选择一个任务。
5. 写入 `ActiveQuestState`。
6. 招募日志记录任务和奖励。

### 事件任务

阴暗贵族出售时发现一个任务：

1. `MinionSold` 检查卖出的宝宝。
2. 生成任务候选。
3. 写入 pending choice。
4. 玩家选择任务。
5. 任务完成后获得 8 金币钱袋奖励。

如果当前 UI 还不能承载多个 pending choice，出售阴暗贵族时可以先阻止重复触发，并写清日志。

## 任务进度派发

`QuestEngine` 订阅共用底座事件：

- `CardBought`
- `MinionSold`
- `TavernSpellCast`
- `ShopRefreshed`
- `CombatEnded`
- `BattlecryTriggered`
- `FriendlyMinionDied`

每次事件只做三件事：

1. 判断当前是否有未完成任务。
2. 判断事件是否匹配目标。
3. 增加进度并在完成时激活奖励。

不要把奖励效果写在进度更新函数里。奖励激活后由奖励解析器在对应触发点执行。

## 奖励设计

任务奖励第一版优先复用以下类别：

- 回合开始给金币或卡牌。
- 回合结束给属性。
- 买牌后奖励属性或 Tavern Coin。
- 刷新时修改酒馆。
- 战斗后奖励卡牌。

暂缓：

- 需要完整 Secret 系统的奖励。
- 需要真实多人大厅的奖励。
- 需要复杂战斗中实时派发的奖励。
- 需要完整官方智能选择的奖励。

## UI 设计

### 任务选择弹窗

显示：

- 任务名。
- 完成条件。
- 当前奖励。
- 选择按钮。

### 任务进度条

局内状态区域显示：

- 任务名称。
- 进度：`3/10`。
- 奖励摘要。
- 完成后显示“奖励已激活”。

### 日志

招募日志写：

- 选择了哪个任务。
- 进度变化。
- 完成时机。
- 奖励触发结果。

## MVP 任务池

第一批建议 6 到 8 个任务目标，全部用已有事件实现：

1. 购买若干张牌。
2. 购买若干个随从。
3. 购买若干个 Tavern spell。
4. 出售若干个随从。
5. 消耗若干金币。
6. 刷新若干次酒馆。
7. 施放若干个 Tavern spell。
8. 触发若干次战吼。

第一批奖励建议：

1. 回合开始获得 Tavern Coin。
2. 每次买牌给随机友方随从属性。
3. 每次刷新给酒馆随从属性。
4. 回合结束给最低攻击随从属性。
5. 战斗后获得随机当前等级随从。

## 关联英雄和宝宝

### Sire Denathrius

第一版：

- 开局发现两个任务。
- 选择后显示任务进度。
- 完成后激活奖励。

如果玩家不选择任务，主流程应提示存在必选项。

### Shady Aristocrat

第一版：

- 出售时发现一个任务。
- 该任务完成后给 8 金币钱袋。
- 如果当前已经有进行中的额外任务，先拒绝重复触发并写日志。

后续：

- 支持多个任务槽。
- 支持任务队列。
- 支持奖励替换和叠加。

## 测试计划

新增测试建议：

- `QuestCatalogTests`
  - 所有任务有目标和奖励。
  - 所有奖励 id 可解析。
  - duplicate id 失败。

- `QuestChoiceTests`
  - 德纳修斯开局生成两个任务。
  - 选择任务后写入状态。
  - 非法选项被拒绝。

- `QuestProgressTests`
  - 买牌进度增加。
  - 卖牌进度增加。
  - 刷新进度增加。
  - 不匹配事件不增加。

- `QuestRewardTests`
  - 完成后奖励激活。
  - 未完成不触发奖励。
  - 奖励只在对应触发点执行。

- `ShadyAristocratQuestTests`
  - 出售触发任务发现。
  - 完成后获得 8 金币钱袋。
  - 宝宝不在场或不是该宝宝时不触发。

## 实施顺序

1. 建立 `QuestDefinition`、`QuestCatalog`、`ActiveQuestState`。
2. 将任务状态挂入 `AdvancedMechanicState`。
3. 接入任务选择弹窗和进度 UI。
4. 实现进度派发。
5. 实现 6 到 8 个低风险任务目标。
6. 实现 5 个低风险奖励。
7. 接入德纳修斯。
8. 接入阴暗贵族。
9. 补注册表、日志、已知限制和测试。

## 完成标准

- 德纳修斯开局能选择任务。
- 任务进度能随操作变化。
- 完成后奖励能稳定触发。
- 阴暗贵族出售能进入任务发现流程。
- 任务和奖励都有注册状态。
- 自动测试覆盖选择、进度、完成、奖励和非法路径。

## 风险

| 风险 | 处理 |
| --- | --- |
| 任务和教程任务混淆。 | 文档、类型命名和 UI 文案统一使用 Quest/Reward，教程另走 TutorialTask。 |
| 任务目标过多导致事件监听膨胀。 | 第一版只做已有事件能覆盖的目标。 |
| 多任务并行复杂。 | 第一版一个主任务，一个可选额外任务槽，后续再扩。 |
| 奖励复刻不准。 | 每个奖励注册状态明确标为 Implemented、FrameworkFirst 或 Deferred。 |
