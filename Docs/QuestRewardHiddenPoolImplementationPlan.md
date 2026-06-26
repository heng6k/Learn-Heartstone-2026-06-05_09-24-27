# 任务奖励隐藏池实现细案

## 目标

这篇文档只处理任务版本中的 Quest Reward：玩家在酒馆开局选择任务，三选一，每个选项同时显示“要做的任务”和“完成后的奖励”。本批 20 个奖励效果需要实现，但因为强度偏低，不进入普通随机三选奖励池。

结论先定死两件事：

1. `ImplementationStatus=Implemented` 只表示效果可执行，不再等同于“可以被随机开出”。
2. 任务版本由玩家选择启用，和饰品、畸变、扭曲时空一样走共用高级机制底座；同一局只能按配置启用一种或多种高级机制，选择 UI 复用 `AdvancedMechanics.PendingChoice`。

## 资料来源与可信度

官方英文文本以 HearthstoneJSON 为准，查询地址为 `https://api.hearthstonejson.com/v1/latest/enUS/cards.json`。HSReplay 和营地页面用于人工比对池子、图片和排序：`https://hsreplay.net/battlegrounds/trinkets/lesser/`、`https://www.iyingdi.com/tz/tool/general/battlegrounds`。

可信度：高。20 个主奖励 ID 都能在 HearthstoneJSON 中查到，类型都是 `BATTLEGROUND_QUEST_REWARD`。其中 `BG33_Reward_006t`、`BG24_Reward_715t` 到 `BG24_Reward_715t4` 也能查到子牌。`BG28_Reward_505` 的 “Improve this permanently” 没有明示每次提升数值，文档中按 +1/+1 实现，并标为需实测确认。

## 必须先改的底座

### 1. 奖励池状态拆分

当前 `QuestCatalog.ImplementedRewards` 被 `ResolveQuestReward` 直接用作可选池。需要新增独立字段：

```csharp
public enum QuestOfferPoolStatus
{
    Offerable,
    HiddenEffectOnly,
    DebugOnly,
    Disabled
}

public sealed class QuestRewardDefinition
{
    public QuestOfferPoolStatus OfferPoolStatus;
}
```

JSON 增加：

```json
"offerPoolStatus": "HiddenEffectOnly"
```

选择奖励时只用：

```csharp
catalog.Rewards.Where(reward =>
    reward.ImplementationStatus == QuestImplementationStatus.Implemented &&
    reward.OfferPoolStatus == QuestOfferPoolStatus.Offerable)
```

本文件列出的 20 个奖励统一登记为 `Implemented + HiddenEffectOnly`。调试命令可以通过指定 reward id 强制挂载，方便测效果。

### 2. 开局任务三选一

新增高级机制配置：

```csharp
public enum AdvancedMechanicMode
{
    None,
    Trinkets,
    Quests,
    Anomalies,
    Timewarp,
    Distortion,
    Mixed
}
```

对局创建时如果模式包含 `Quests`：

1. `MatchStarted` 或酒馆第一回合初始化后调用 `OfferQuestChoice(3, "quest-mode-opening", "Main", null)`。
2. 每个选项使用 `MechanicChoiceOption` 的 Quest 字段：`DisplayName/Text/ImagePath` 显示任务，`RewardName/RewardText/RewardImagePath` 显示奖励。
3. UI 上一张选择卡必须左右或上下同时展示任务牌和奖励牌；选择按钮只选整组组合，不允许只换奖励。
4. 选择后写入 `PlayerQuestState.MainQuest`，并在进度条显示任务名、奖励名、进度、完成状态。
5. 如果玩家配置同时启用饰品和任务，所有开局待选项进入队列，不覆盖已有 `PendingChoice`。当前只有单个 `PendingChoice`，因此要补 `PendingChoices: Queue<MechanicChoiceRequest>` 或先按机制顺序串行弹出。

### 3. 触发点补齐

当前任务奖励只覆盖 `OnComplete`、`TurnStarted`、`TurnEnded`、`CardBought`、`ShopRefreshed`。本批奖励需要新增：

```csharp
public enum QuestRewardTrigger
{
    OnComplete,
    TurnStarted,
    TurnEnded,
    CardBought,
    CardPlayed,
    MinionPlayed,
    ShopRefreshed,
    StartOfCombat,
    CombatMinionSummoned,
    CombatFriendlyMinionDied,
    CombatAfterAttack,
    SpellcraftGenerated
}
```

战斗内奖励不要直接改酒馆棋盘。战斗开始时把已完成奖励转成 `CombatQuestEffect` 注入 `CombatInput`，由 `CombatEngine` 在战斗克隆里执行，战斗结束只回传“永久提升”或“下回合资源”等明确需要落回酒馆的结果。

### 4. 持久状态

`PlayerQuestState` 需要新增通用计数，而不是为每个奖励硬写字段：

```csharp
public Dictionary<string, int> RewardCounters;
public Dictionary<string, bool> RewardFlags;
```

建议 key 统一为 `rewardId + ":" + name`，例如：

- `BG24_Reward_123:usedRound`
- `BG24_Reward_321:parity`
- `BG28_Reward_505:attack`
- `BG28_Reward_505:health`
- `BG28_Reward_505:avenge`

## 20 个隐藏奖励实现方案

| ID | 奖励 | 触发 | 实现方案 | 待确认 |
| --- | --- | --- | --- | --- |
| `BG24_Reward_115` | Theotar's Parasol | `TurnEnded` | 找到己方最右随从；永久 +0/+8；添加 `Keyword.Stealth` 和来源为本奖励的临时 enchantment；下个 `TurnStarted` 移除这个来源的 Stealth。 | 需要确认“下回合”是否到自己下次回合开始就移除；按官方常规先这样做。 |
| `BG24_Reward_123` | Exquisite Conch | 首个 Battlecry | 在 `GetBattlecryRepeats` 或 Battlecry 解析入口检查奖励已激活且本回合未用；本回合第一个 `Keyword.Battlecry` 随从额外 +2 次；写 `usedRound=State.Round`。 | 与 Brann 的叠加顺序需实测；先按“现有重复次数 + 2”实现。 |
| `BG24_Reward_125` | The Smoking Gun | Aura | 新增玩家随从攻击光环 `QuestAttackAura += 4`；作用于己方棋盘、战斗克隆、召唤物和后续打出的随从；不要把 +4 写成永久 enchantment。 | UI 是否显示 aura 后攻击值要同步。 |
| `BG24_Reward_128` | Mirror Shield | `ShopRefreshed` | 现有逻辑继续使用：刷新后随机酒馆随从 +6/+6 并加 `Keyword.DivineShield`；JSON 备注要改成已实现。 | 无。 |
| `BG24_Reward_131` | Red Hand | `TurnStarted` | 从手牌随从中随机 1 个，永久 +12/+12；手牌为空则 no-op 并写日志。 | 无。 |
| `BG24_Reward_312` | Staff of Origination | `StartOfCombat` | 战斗开始注入 combat-only buff，己方所有战斗克隆 +12/+12；不回写酒馆棋盘。 | 官方未写永久，按战斗临时处理。 |
| `BG24_Reward_321` | Alter Ego | `TurnStarted`、`ShopRefreshed` | 完成时 `parity=Even`；酒馆中符合偶数星的随从获得 +7/+7。每个自己回合开始 parity 在 Even/Odd 间切换；刷新酒馆后重新给当前 parity 的随从加临时酒馆 enchantment。购买到手牌后保留已获得的 +7/+7。 | 锁定酒馆跨回合时旧 parity buff 是否移除需实测；先在酒馆区域移除旧来源后重算。 |
| `BG24_Reward_331` | Menagerie Mayhem | `TurnEnded` | 统计己方场上不同随从类型数量，给所有己方随从永久 +N/+N。普通无类型随从不计数。 | `Tribe.All` 是否算所有类型需统一项目内 helper；先按“贡献当前可见全部类型”设计。 |
| `BG24_Reward_364` | Volatile Venom | Aura + `CombatAfterAttack` | 新增战斗/棋盘 +7/+7 光环；战斗中己方随从完成攻击结算后，若仍存活，立即造成致死/标记死亡，正常触发死亡相关流程。 | 是否影响酒馆阶段展示需和 Smoking Gun 共用 aura UI。 |
| `BG24_Reward_708` | Blood Goblet | `TurnEnded` | 找最右随从，计算 `missingHealth = MaxHeroHealth - Health`，给它永久 +missingHealth/+0。 | 项目若没有 `MaxHeroHealth` 字段，先使用开局生命上限快照；护甲不计入 missing health。 |
| `BG24_Reward_712` | Sinfall Medallion | `MinionPlayed` | 打出随从后，在其他己方随从中筛同星级，随机最多 2 个永久 +4/+4；不足 2 个则全 buff。 | 随从入场后再触发，排除被打出的那个随从。 |
| `BG24_Reward_715` | Enhance-a-matic | `TurnStarted` | 随机给 1 张 Enhanced Part 入手牌：`BG24_Reward_715t` Mega Horn、`715t2` Blazing Blades、`715t3` Bunker Plating、`715t4` Death Rewinder。施放时给目标 +5/+5 和对应关键词。 | 手牌满时按现有生成牌规则处理；要补 SpellCatalog 数据和目标校验。 |
| `BG27_Reward_502` | Boom Squad | `CombatFriendlyMinionDied` | 战斗内 Avenge 计数友方死亡；每满 3 次，对敌方最高生命随从造成 10 点伤害，处理圣盾、死亡和死亡结算。 | 最高生命平手先按最左/稳定顺序选，需实测官方 tie-break。 |
| `BG27_Reward_804` | Sturdy Shard | `TurnEnded` | 统计己方嘲讽随从数量 T；给所有非嘲讽己方随从永久 +T/+2T。T=0 no-op。 | 无。 |
| `BG27_Reward_810` | Map of the Unknown | `MinionPlayed` | 打出随从前记录己方已控制类型；如果新随从至少有一种此前未控制的类型，则触发。触发后按场上每种类型各选 1 个友方随从，永久 +2/+2。 | 多类型随从和 `Tribe.All` 的官方处理需实测；先复用 Menagerie 类型 helper。 |
| `BG27_Reward_815` | Endless Blood Moon | `OnComplete` + `TurnStarted` | 完成时 `BloodGemBonusAttack += 1`、`BloodGemBonusHealth += 1`。每回合开始添加 2 张 `BG20_GEM` 到手牌。 | 如果项目已有血宝石玩家 enchant，优先写入现有字段，不另造状态。 |
| `BG28_Reward_505` | Tumbling Disaster | `CombatMinionSummoned` + Avenge 4 | 初始 combat summon buff 为 +4/+4。战斗中己方召唤随从时给它当前数值。友方死亡每满 4 次，永久提升该奖励计数，战斗结束回写 `attack += 1`、`health += 1`。 | 官方未写提升幅度；需实测确认是不是每次 +1/+1。 |
| `BG33_Reward_003` | Righteous Charge | `StartOfCombat` | 战斗开始给最左己方随从 `DivineShield`，然后插入一次立即攻击动作；攻击目标按当前战斗引擎正常选敌方目标。 | 如果 CombatEngine 没有插队攻击动作，先实现 `QueuedImmediateAttack`。 |
| `BG33_Reward_004` | Grim Freshener | `CombatFriendlyMinionDied` | 战斗内 Avenge 计数友方死亡；每满 2 次产生 `CombatRewardType.GainFreeRefresh`，战斗结束回写 `Tavern.FreeRefreshes += amount`。 | 无。 |
| `BG33_Reward_006` | Rushing Winds | `TurnStarted` / Spellcraft | 每回合开始生成 1 张临时法术 `BG33_Reward_006t` 到手牌，持续一回合或按项目 Spellcraft 规则回合结束移除；施放给目标 `Windfury` 和 `DivineShield`。 | 项目目前需要补 Spellcraft 临时卡过期清理。 |

## 代码落点

### 数据与加载

- `Assets/LearnHearthstone/Runtime/Domain/Models/QuestModels.cs`：新增 `QuestOfferPoolStatus`、更多 `QuestRewardTrigger`、更多 `QuestRewardEffectKind`、通用 counters。
- `Assets/LearnHearthstone/Runtime/Adapters/Data/QuestCatalogLoader.cs`：解析 `offerPoolStatus`。
- `Assets/LearnHearthstone/Runtime/Domain/Data/QuestCatalog.cs`：新增 `OfferableRewards`、`HiddenEffectRewards`。
- `Assets/LearnHearthstone/Resources/Data/battlegroundsQuests.json`：登记这 20 个奖励，全部 `offerPoolStatus=HiddenEffectOnly`。

### 任务选择

- `MatchService.OfferQuestChoice`：任务模式开局传 `count=3`。
- `MatchService.ResolveQuestReward`：只从 `OfferableRewards` 自动抽。调试和指定奖励可以拿隐藏奖励。
- `UnityAdvancedMechanicChoiceOverlay`：Quest option 必须同时渲染任务卡和奖励卡，奖励名、奖励描述、奖励图片不可省略。

### 酒馆阶段事件

- `DispatchQuestRewardTurnStarted`：处理 Red Hand、Enhance-a-matic、Endless Blood Moon、Rushing Winds、Alter Ego parity。
- `DispatchQuestRewardTurnEnded`：处理 Theotar's Parasol、Menagerie Mayhem、Blood Goblet、Sturdy Shard。
- `PlayMinion` 完成落场后：派发 `MinionPlayed` 给 Sinfall Medallion、Map of the Unknown。
- `ResolveMinionBattlecry`：给 Exquisite Conch 一个“本回合首个战吼额外 2 次”的入口。
- `RefreshShop` 后：Mirror Shield、Alter Ego 当前 parity buff。

### 战斗阶段事件

新增 `CombatQuestEffect`，从 active quest rewards 编译而来：

```csharp
public sealed class CombatQuestEffect
{
    public string RewardId;
    public QuestRewardEffectKind EffectKind;
    public int Attack;
    public int Health;
    public int AvengeRequired;
}
```

`CombatEngine` 需要支持：

- `StartOfCombat`：Staff of Origination、Righteous Charge。
- `MinionSummoned`：Tumbling Disaster。
- `MinionDied`：Boom Squad、Grim Freshener、Tumbling Disaster avenge。
- `AfterAttack`：Volatile Venom。
- 战斗结束回写：free refresh、Tumbling Disaster 改善后的永久计数。

## 测试清单

每个隐藏奖励至少 1 个直接单测，另加 5 个系统级测试：

1. `QuestRewardPoolTests.HiddenRewardsDoNotAppearInOpeningThreeChoice`：隐藏奖励不出现在普通三选。
2. `QuestRewardPoolTests.DebugCanAttachHiddenRewardById`：调试指定 ID 可以挂载并触发。
3. `QuestOpeningChoiceTests.QuestModeOffersThreeQuestRewardPairs`：任务模式开局三选一，选项同时有任务和奖励图片。
4. `QuestOpeningChoiceTests.AdvancedMechanicQueueDoesNotOverwriteTrinketChoice`：任务和饰品同时启用时 pending choice 串行。
5. `QuestCombatRewardTests.CombatEffectsDoNotMutateTavernBoardUnlessPermanent`：Staff、Volatile、Righteous Charge 不错误永久写回。

逐项奖励单测重点：

- Theotar's Parasol：回合结束加生命和潜行，下回合移除潜行。
- Exquisite Conch：每回合只有第一个战吼额外触发 2 次。
- The Smoking Gun / Volatile Venom：aura 影响后续召唤/打出的随从。
- Alter Ego：偶数/奇数切换，刷新后只 buff 当前 parity。
- Enhance-a-matic / Rushing Winds：生成真实子牌，施放后关键词正确。
- Boom Squad / Grim Freshener / Tumbling Disaster：Avenge 计数跨一次战斗内正确归零和多次触发。

## 待确认重点

- Exquisite Conch 与 Brann、其他战吼翻倍效果的叠加顺序。
- Alter Ego 锁定酒馆跨回合时，旧 parity 的 +7/+7 是否从仍在酒馆的随从身上移除。
- Menagerie Mayhem、Map of the Unknown 对 `All` 类型随从的官方计数方式。
- Blood Goblet 在有护甲或最大生命被修改时的 missing health 口径。
- Tumbling Disaster 的 “Improve this permanently” 每次提升是否确认为 +1/+1。
- Rushing Winds 的 Spellcraft 临时牌是否必须在回合结束移除；当前项目需要补临时生成牌过期机制。

## 实施顺序

1. 先做数据层：`offerPoolStatus`、20 个奖励 JSON、子牌数据。
2. 再做任务模式开局三选一：玩家可选机制配置、三选一任务奖励对、UI 显示。
3. 做酒馆阶段低风险奖励：Red Hand、Sturdy Shard、Blood Goblet、Menagerie Mayhem、Sinfall、Map、Enhance-a-matic、Rushing Winds。
4. 做 aura 和临时状态：Smoking Gun、Volatile Venom、Theotar's Parasol、Alter Ego。
5. 做战斗注入：Staff、Boom Squad、Tumbling Disaster、Righteous Charge、Grim Freshener。
6. 最后补完整测试和人工冒烟：任务三选、奖励隐藏、每个触发点、战斗回写。
