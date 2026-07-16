# 准备阶段死亡与亡语补全规范

## 文档状态

- 状态：首批实现已完成；剩余争议项继续保留为后续范围。
- 日期：2026-07-15。
- 受众：玩法、领域模型和测试实现人员。
- 目标：让“死亡、消灭、亡语及其后续响应”成为跨准备阶段与战斗阶段的统一机制，同时保留出售、移除、吞食等不同语义。
- 结论置信度：
  - 卡牌文本和当前代码现状：高。
  - `Destroy`/`Dies` 应进入死亡与亡语结算：高。
  - 亡语召唤、Reborn 与七格上限的关系：高；通用规则资料与当前战斗实现一致。
  - Archlich exact copy 位于死亡链与 Reborn 之后：中高；按通用强制死亡检查点确定为项目契约，官方卡牌页未单独解释该互动。
  - 金色 Archlich 双目标、回合结束总队列和部分战斗专属亡语：中或低，仍单列讨论。

## 结论

当前项目的亡语核心只存在于 `CombatEngine`。准备阶段已经实现多种 `Destroy` 或 `Dies` 效果，但这些路径通常直接从 `State.Player.Board` 移除随从，没有进入死亡队列，也不会触发亡语、死亡观察者、额外亡语次数或“触发亡语后”效果。

这不是单一卡牌缺失，而是阶段模型缺口。正确补全方向不是让 `MatchService` 临时调用 `CombatEngine.SimulateBasicCombat`，也不是把所有 `Board.Remove` 都改成死亡；应当建立阶段中立的死亡与亡语结算入口，由战斗和准备阶段分别提供上下文。

首批明确需要进入准备阶段死亡结算的来源包括：

- `Butchering`：酒馆法术，消灭一个友方亡灵。
- `Jailer Sticker`：塑造法术，消灭一个友方亡灵后获取亡灵牌。
- `Disguised Graverobber`：战吼，消灭一个友方亡灵后获取其原始版复制。
- `Tomb Turning`：本回合打出发现的亡灵时，该随从死亡。
- `Archlich Kel'Thuzad`：回合结束时消灭左侧或相邻亡灵，再召唤完全相同的复制。

准备阶段没有找到文本为“直接触发所有友方亡语”的当前卡牌。`Timewarped Hawkstrider` 和 `Herald Sticker` 的“触发所有友方亡语”均限定为战斗开始。`Timewarped Warghoul` 的实际文本是其亡语触发一个相邻随从的亡语；它可以在被 Archlich Kel'Thuzad 消灭后形成准备阶段亡语连锁，但不等于固定触发全部亡语。

## 已实现状态（2026-07-15）

- `CombatEngine` 已增加作用于真实棋盘的准备阶段死亡与召唤入口，不再克隆整块棋盘模拟战斗。
- 已接入 Butchering、Jailer Sticker、Disguised Graverobber、Tomb Turning 和 Archlich Kel'Thuzad。
- 准备阶段真实死亡按“移除 → 亡语及连锁死亡 → Reborn”结算；Archlich 在其后尝试召唤死亡前 exact copy。
- Reborn 作为普通召唤受七格限制：保留原卡名称、描述、亡语、金色、种族、当前攻击、最大生命、永久附魔和应保留计数器，只消耗 Reborn 并把当前生命设为 1；随后重新应用当前有效的全局 Buff/召唤光环且不得重复叠加。Archlich exact copy 则保留死亡前完整状态，包括未消耗 Reborn。
- Warghoul 在准备阶段仍只触发一个合法相邻随从亡语。
- Plaguerunner 战斗外真实死亡为普通 +4、金色 +8；出售不触发该效果。
- Deathly Phylactery 不在准备阶段生效，也不会被准备阶段亡语消费。
- Sacrificial Altar 的 Remove、普通出售和 Consume 仍与真实死亡隔离。
- Hawkstrider 与 Herald Sticker 的战斗开始亡语路径保持原实现，并已做回归验证。

当前实现对明确依赖战斗敌方、击杀者、战斗死亡历史或“本场战斗”手牌召唤的亡语子效果采用“不执行该战斗专属子效果”的策略。金色 Archlich 双目标顺序、完整回合结束触发队列、Avenge/通用死亡观察者和 Consume 规则仍属于后续讨论范围。

## 范围

### 包含

- 准备阶段主动消灭随从。
- 打出后立即死亡的随从。
- 回合结束时消灭随从。
- 被消灭随从的亡语、额外亡语次数和亡语后响应。
- 亡语造成的召唤、永久属性、手牌、经济和进一步死亡链。
- 战斗外死亡观察者和统计。
- 出售、移除、吞食、返回手牌、变形等非死亡语义的明确隔离。
- 与现有战斗亡语保持一致的确定性、七格上限、随机种子和安全上限。

### 不包含

- 本文不直接实现代码。
- 不要求一次性把所有硬编码亡语迁移成数据驱动效果。
- 不重写完整战斗引擎。
- 不把调试工具的 `RemoveOpponentMinion` 自动解释成死亡。
- 未确认的复杂规则不会在本文中伪装成确定结论。

## 术语与退出原因

底层都可能表现为 `Board.Remove`，但规则语义必须显式区分。

| 语义 | 是否死亡 | 默认触发亡语 | 示例 | 处理建议 |
|---|---:|---:|---|---|
| `Destroy` / `Dies` | 是 | 是 | Butchering、Jailer Sticker、Tomb Turning、Archlich Kel'Thuzad | 进入统一死亡队列 |
| 生命值降到 0 | 是 | 是 | 战斗伤害、未来可能出现的准备阶段伤害 | 进入统一死亡队列 |
| 主动触发亡语 | 否 | 是 | Warghoul 触发相邻亡语、Hawkstrider 战斗开始 | 只结算亡语，不移除目标 |
| Sell | 否 | 否 | 正常出售、Devour 英雄技能、Invoke the Devourer | 只派发 `MinionSold` |
| Remove | 否 | 否 | Sacrificial Altar、Trash for Treasure | 派发独立移除事件，不派发死亡 |
| 返回手牌/换位 | 否 | 否 | Kidnap Sack、MoveMinionToHand、站位调整 | 仅区域迁移 |
| Transform | 否 | 否 | 变形酒馆法术 | 替换实体，不进入死亡 |
| Consume | 待确认 | 默认否 | Timewarped Devourer、恶魔吞食酒馆随从 | 保持独立语义，见“仍待讨论问题” |
| Debug Remove | 否 | 否 | 对手编辑工具 | 调试操作不模拟死亡，除非增加显式 Destroy 命令 |

建议引入显式原因枚举，而不是从调用位置或 `Board.Remove` 推断：

```csharp
public enum MinionExitReason
{
    Sold,
    Destroyed,
    DiedByEffect,
    DiedFromDamage,
    Removed,
    ReturnedToHand,
    Transformed,
    Consumed,
    DebugRemoved,
    CombatSnapshotRemoved
}
```

`Destroyed`、`DiedByEffect` 和 `DiedFromDamage` 默认进入死亡与亡语结算。Tomb Turning 属于 `DiedByEffect`；`Consumed` 的最终规则待讨论。

## 实际卡牌规则证据

### 确认的准备阶段死亡来源

| 卡牌 | 卡牌 ID | 官方文本要点 | 触发时机 | 当前项目状态 |
|---|---|---|---|---|
| Butchering | `110412` | `Destroy a friendly Undead` | 准备阶段使用酒馆法术 | 直接 `Board.Remove`，无亡语 |
| Jailer Sticker | `BG35_MagicItem_733` / `BG35_MagicItem_306` | `Destroy a friendly Undead` | 准备阶段塑造法术 | 记录战斗外消灭并发奖励，无亡语 |
| Disguised Graverobber | `BG28_303` | 战吼消灭友方亡灵，获取原始版复制 | 出牌/战吼 | 直接移除并给复制，无亡语 |
| Tomb Turning | `126957` | `It dies if you play it this turn` | 打出发现随从后 | 直接移除，错误借用出售效果，无亡语 |
| Archlich Kel'Thuzad | `BG28_308` | 回合结束消灭左侧亡灵并重召完全相同复制 | 回合结束 | 替换为原始版复制，无死亡/亡语 |
| Plaguerunner | `BG34_690` | 亡语；战斗外死亡时成长数值更高 | 任意战斗外死亡入口 | 战斗内亡语存在；战斗外逻辑错误挂在出售路径 |

### 明确不应触发死亡的来源

| 卡牌/操作 | 文本语义 | 当前风险 |
|---|---|---|
| 正常出售 | Sell | 当前正确，不应接入死亡 |
| Sacrificial Altar | `Remove all your minions` | 当前却调用战斗外 Destroy 统计，可能错误触发画像奖励 |
| Hooktusk：Trash for Treasure | Remove | 不应触发亡语 |
| Devour 英雄技能 | Sell | 不应因函数名 Devour 被解释成死亡 |
| Invoke the Devourer | Sell | 不触发亡语 |
| Cascading Avalanche | Sell | 不触发亡语 |
| 返回手牌、换位、变形 | 区域变化/替换 | 不触发亡语 |

## 当前实现审计

### 战斗阶段亡语链

现有战斗路径是：

```text
ResolveAllDeaths
  -> ResolveDeaths
     -> 从战斗棋盘移除死亡随从
     -> 记录死亡与战斗奖励
     -> ResolveAvenge
     -> ResolveDeathrattleEffect
     -> Reborn
```

关键位置：

- `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs:1752`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs:1763`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs:1820`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs:2288`

`ResolveDeathrattleEffect` 同时承担以下职责：

- 写战斗日志和战斗回放帧。
- 计算 Titus、Timewarped Deios、任务和饰品的额外次数。
- 排队 `CombatReward`。
- 刷新战斗动态属性。
- 执行召唤、属性变化、敌方伤害、击杀者效果和战斗专属效果。
- 派发饰品、任务和扭曲时空的“亡语触发后”效果。

因此它不能原样用于真实酒馆战场。

### 准备阶段现有入口

| 入口 | 代码位置 | 当前行为 | 缺口 |
|---|---|---|---|
| Butchering | `TavernSpellEngine.cs:1933` | 移除目标并增加亡灵攻击 | 无死亡、亡语、观察者和回池统一处理 |
| Jailer Sticker | `MatchService.cs:27050` | 移除、回池、记录战斗外消灭、给奖励 | 无亡语和通用死亡事件 |
| Disguised Graverobber | `MatchService.cs:27564` | 移除目标并给原始版复制 | 无亡语；没有统一回池策略 |
| Tomb Turning | `MatchService.cs:24977` | 移除、调用 `ResolveTierFourSellEffect`、回池 | 把死亡错误混入出售；无亡语 |
| Archlich Kel'Thuzad | `MatchService.cs:29566` | 移除并插入原始版复制 | 官方要求 exact copy；无亡语和战斗外消灭事件 |
| Sacrificial Altar | `MatchService.cs:11299` | Remove 全场，却调用 Destroy 统计 | Remove/Destroy 语义混淆 |
| Timewarped Devourer | `MatchService.cs:26413` | Consume 右侧恶魔并回池 | 是否死亡未明确；当前无亡语 |

### 已有但未接通的通用骨架

`MechanicEventType` 已包含：

- `MinionDied`
- `DeathrattleQueued`
- `DeathrattleResolved`
- `RebornResolved`
- `AvengeCounterChanged`

位置：`Assets/LearnHearthstone/Runtime/Domain/Models/MechanicModels.cs:6`。

`EffectDispatcher` 可以在真实的 Board、Hand、Shop 和 Tavern 上应用简单效果，但运行时没有真正派发上述死亡/亡语事件，默认目录也只覆盖少量示例效果。它适合作为增量复用点，不足以独立替代战斗亡语实现。

## 准备阶段触发来源矩阵

| 类别 | 触发来源 | 是否移除来源 | 是否结算亡语 | 是否派发死亡观察者 |
|---|---|---:|---:|---:|
| 回合中主动消灭 | Butchering、Jailer Sticker | 是 | 是 | 是 |
| 战吼消灭 | Disguised Graverobber | 是 | 是 | 是 |
| 出牌后死亡 | Tomb Turning | 是 | 是 | 是 |
| 回合结束消灭 | Archlich Kel'Thuzad | 是 | 是 | 是 |
| 亡语嵌套触发 | 被消灭的 Timewarped Warghoul | Warghoul 已死亡；目标不死亡 | 是 | 仅 Warghoul 自身死亡派发 |
| 直接触发亡语 | 未来准备阶段卡牌 | 否 | 是 | 否 |
| 出售 | 所有 Sell 效果 | 是 | 否 | 否 |
| Remove | Sacrificial Altar 等 | 是 | 否 | 否 |
| Consume | 恶魔/扭曲时空吞食 | 是 | 待确认 | 待确认 |

## 阶段中立死亡上下文

建议建立准备阶段和战斗阶段共同使用的死亡上下文：

```csharp
public enum ResolutionPhase
{
    Recruit,
    Combat
}

public sealed class MinionDeathContext
{
    public ResolutionPhase Phase;
    public MinionExitReason Reason;
    public MinionInstance DeadSnapshot;
    public string CauseCardId;
    public string CauseInstanceId;
    public int OriginalBoardIndex;
    public BoardSide Side;
    public bool SourceWasRemoved;
    public int ChainDepth;
}
```

死亡快照至少需要保存：

- 卡牌 ID、实例 ID、金色状态、关键字、种族和当前/最大属性。
- 附魔、Counter、Tag 和动态获得的亡语。
- 原站位以及死亡前相邻关系。
- 击杀者或消灭来源；准备阶段通常没有击杀者。
- 是否为真实死亡、直接触发亡语或战斗副本死亡。

## 推荐结算流程

### 真实死亡/消灭

```text
1. 校验目标仍在对应战场。
2. 创建死亡快照并保存原站位；如来源效果要求 exact copy，同时保存死亡前完整复制快照。
3. 从真实战场移除目标。
4. 派发 MinionDied，包括战斗外消灭画像、全局死亡计数和死亡观察者。
5. 如目标有亡语，进入 DeathrattleQueued。
6. 按当前阶段计算额外亡语次数。
7. 逐次执行亡语；每次完成后派发 DeathrattleResolved 和亡语后响应。
8. 处理由亡语产生的新死亡，直到队列稳定。
9. 如死亡实体具有尚未消耗的 Reborn，按一次普通召唤尝试生成新实体：复制死亡前当前状态，移除两套 Reborn 标记，当前生命设为 1，重置攻击次数和击杀来源，分配新实例 ID，再应用当前有效的全局 Buff/召唤光环。
10. 继续执行来源卡牌的后续效果，例如 Archlich exact copy、获取复制或其他奖励。
```

顺序锁定为“亡语及其召唤完整结算 → Reborn → 来源卡牌后续召唤/奖励”。每次召唤都在实际发生时独立检查七格上限，不预留位置、不挤掉已有随从。Reborn 本身属于召唤，应触发适用的召唤事件。

### 直接触发亡语但不死亡

```text
1. 创建来源快照，但不移除随从。
2. 计算阶段适用的额外次数。
3. 逐次执行亡语，`SourceWasRemoved = false`。
4. 每次派发 DeathrattleResolved 和亡语后响应。
5. 处理由亡语产生的死亡链，直到稳定。
```

Warghoul 触发相邻亡语需要保留目标原站位和来源未移除语义，等价于战斗实现中的 `sourceRemoved = false`。

## 额外亡语次数的阶段规则

不能直接复用 `CombatEngine.GetDeathrattleRepeats`，应按文本作用域过滤。

| 来源 | 官方文本作用域 | 准备阶段建议 |
|---|---|---:|
| Titus Rivendare | 亡语额外触发一次 | 生效 |
| Turbulent Tombs | 亡语额外触发一次 | 生效 |
| Timewarped Deios | 战吼、亡语、进击触发两次 | 生效 |
| Echoes of Argus | 战吼和亡语额外触发一次 | 历史内容；若启用则生效 |
| Deathly Phylactery | 每场战斗的第一次亡语 | 不生效，也不得消费状态 |

建议接口：

```csharp
int GetDeathrattleRepeatCount(
    MatchState state,
    BoardSide side,
    ResolutionPhase phase,
    MinionInstance source);
```

所有一次性状态必须在确认适用于当前阶段后再消费。

## 亡语后响应

下列效果文本没有战斗限定，应在准备阶段亡语完成后响应：

- Blood Amulet。
- Unholy Sanctum。
- Timewarped Ghoul-acabra。
- Timewarped Saurolisk。
- Thornspike Pauldron。

它们当前位于 `CombatEngine.ResolveTrinketDeathrattleTriggered` 或 `ResolveTimewarpedDeathrattleTriggered`。应抽取阶段中立响应入口，战斗和准备阶段共同调用。

战斗专属响应仍保留在战斗适配器，例如：

- 明确写“each combat”的一次性效果。
- 依赖战斗回放、敌方棋盘、击杀者或战斗奖励的效果。
- 只对战斗副本生效的临时属性。

## 亡语效果的阶段能力分类

每个亡语实现至少需要回答以下问题：

| 能力 | 准备阶段默认策略 |
|---|---|
| 召唤友方衍生物 | 可执行；进入真实棋盘并受七格限制，除非文本写战斗专属 |
| 永久强化友方随从/全局池 | 可执行并直接写真实状态 |
| 获取手牌、金币、折扣或酒馆法术 | 可执行 |
| 触发另一个友方亡语 | 可执行，目标不死亡 |
| 伤害/消灭敌方随从 | 没有敌方准备战场上下文时不得臆造目标 |
| 处理“击杀本随从的随从” | 准备阶段通常没有击杀者；应无目标或不执行 |
| “本场战斗中最先死亡” | 仅战斗 |
| “仅限本场战斗”的召唤 | 待讨论；不得自动变成永久召唤 |
| 立即攻击、复仇、攻击后触发 | 通常仅战斗，除非卡牌文本另有说明 |

建议为复杂效果增加能力元数据，而不是在准备阶段静默执行错误逻辑：

```csharp
public enum DeathrattlePhaseSupport
{
    AnyPhase,
    CombatOnly,
    RecruitOnly,
    NeedsPolicyDecision
}
```

## 回合结束流程

当前 `BeginTurnTransition` 以固定函数顺序依次处理随从、英雄、饰品、任务和异常的回合结束效果，然后开始战斗。建议改为：

```text
收集一个回合结束触发项
  -> 执行该触发项
  -> 结算由它产生的死亡/亡语/召唤链直到稳定
  -> 再执行下一个回合结束触发项
  -> 全部完成后进入战斗
```

这样 Archlich Kel'Thuzad 的消灭不会被延迟到所有回合结束效果之后，也不会与下一场战斗交叉。

最终触发顺序是按站位、入场顺序还是现有处理器顺序，需要与实际游戏行为确认，见“仍待讨论问题”。

## 卡牌级预期补全

### Archlich Kel'Thuzad

- 普通版在回合结束时消灭左侧亡灵；金色版消灭相邻亡灵。
- 被消灭随从应进入准备阶段死亡/亡语队列。
- 官方文本要求重新召唤“完全相同的复制”。当前代码和测试却创建原始版复制并移除附魔，需要修正测试契约。
- 如果目标是 Timewarped Warghoul，Warghoul 的亡语应继续触发符合条件的相邻亡语。
- exact-copy 快照取自目标死亡前，保留其永久属性、附魔、计数器、金色状态和尚未消耗的 Reborn。
- 结算顺序锁定为：保存 exact-copy 快照 → Destroy → 亡语及其连锁召唤 → Reborn → 尝试召唤 exact copy。
- Reborn 与 exact copy 是两个不同的召唤来源；空间足够时可以同时生成两个实体。
- 如果亡语召唤或 Reborn 已占满七格，后到的 exact copy 失败；不预留槽位，也不替换先生成的随从。
- 金色双目标之间的先后顺序仍需讨论。

### Timewarped Warghoul

- Warghoul 自身必须先死亡或被直接触发亡语。
- 每次 Warghoul 亡语触发一个相邻的、带亡语且非 Warghoul 的随从亡语。
- 两侧都合法时当前项目使用确定性随机选择；额外亡语次数会重复选择流程。
- 准备阶段应复用相同的目标规则，但作用于真实棋盘。
- 它不是“直接触发所有友方亡语”的卡牌；本项目已确认保持官方和本地数据的“一个相邻随从”语义。

### Tomb Turning

- 打出本回合发现的亡灵后，该随从死亡。
- 不应调用任何 Sell 效果。
- 应触发亡语、战斗外消灭观察者和适用的死亡统计。
- 亡语与打出事件、战吼、三连奖励之间的精确检查点需要测试锁定。

### Butchering、Jailer Sticker、Disguised Graverobber

- 三者都明确使用 Destroy，应共享统一准备阶段消灭入口。
- Destroy 的死亡链和卡牌后续奖励应拆开，不允许各自复制一套亡语逻辑。
- 目标亡语、死亡观察者和“战斗外消灭”画像必须一致触发。
- Disguised Graverobber 的奖励是原始版复制，不是 exact copy。

### Plaguerunner

- 战斗内死亡使用普通数值。
- 战斗外真实死亡使用更高数值。
- 出售、Remove、返回手牌和 Consume 默认不能获得战斗外死亡奖励。
- 当前 `ResolveTierFourSellEffect` 必须与死亡效果拆分。

### Sacrificial Altar

- 官方文本为 Remove，不是 Destroy。
- 默认不触发亡语、死亡计数或战斗外消灭画像。
- 当前 `RecordOutsideCombatMinionDestroyed` 调用应被负向测试审计。

## 卡池与实体所有权

当前各条移除路径对卡池处理不一致：

- Jailer Sticker 和 Tomb Turning 会 `ReleaseMinionToPool`。
- Disguised Graverobber 没有统一回池。
- Archlich Kel'Thuzad 当前创建新原始实例。
- 出售通常回池。

实现前应规定：

- Destroy 后是否立即返还原卡池份数。
- Archlich 的 exact copy 是否继承原实体的卡池所有权，而不是先归还再重新占用。
- 由亡语召唤的衍生物或复制是否持有卡池份数。
- 一次死亡链中不得重复归还同一份卡池资源。

该问题属于模拟器资源模型，不应通过亡语效果临时修补。

## 推荐实施阶段

### Phase A：语义入口与负向保护

- 引入 `MinionExitReason` 和准备阶段 `DestroyMinion` 入口。
- 保持 Sell、Remove、Return、Transform 和 DebugRemove 不进入死亡。
- 修正 Sacrificial Altar 的 Remove/Destroy 混淆。
- 修正 Tomb Turning 不再调用出售效果。

### Phase B：最小准备阶段死亡队列

- 支持死亡快照、原站位、亡语队列和安全上限。
- 首先接入 Butchering、Jailer Sticker、Disguised Graverobber、Tomb Turning。
- 接入战斗外消灭画像、Plaguerunner 和通用死亡统计。

### Phase C：回合结束与 Warghoul 连锁

- 接入 Archlich Kel'Thuzad。
- 修正 exact copy。
- 支持 Warghoul 触发相邻亡语。
- 在每个回合结束触发项后把死亡链结算到稳定。

### Phase D：额外次数和亡语后响应

- 接入 Titus、Turbulent Tombs、Timewarped Deios。
- 排除 Deathly Phylactery 等 each-combat 状态。
- 接入 Blood Amulet、Unholy Sanctum、Ghoul-acabra、Saurolisk、Thornspike Pauldron。

### Phase E：复杂效果和历史内容

- 逐卡标注 `AnyPhase`、`CombatOnly` 或 `NeedsPolicyDecision`。
- 处理 Reborn、Avenge、满场、战斗专属召唤和历史异常。
- 不阻塞前四个阶段的确定性补全。

## 测试矩阵

### 核心语义

| 用例 | 预期 |
|---|---|
| 出售带亡语随从 | 不触发亡语，不增加死亡计数 |
| Sacrificial Altar Remove 带亡语随从 | 不触发亡语和战斗外 Destroy 画像 |
| 返回手牌/换位/变形 | 不触发亡语 |
| Butchering 消灭带亡语亡灵 | 亡语触发，随后应用法术成长 |
| Jailer Sticker 消灭带亡语亡灵 | 亡语触发，随后获取亡灵牌 |
| Graverobber 消灭带亡语亡灵 | 亡语触发，随后获得原始版复制 |
| Tomb Turning 打出带亡语亡灵 | 随从死亡，触发亡语，不触发 Sell 效果 |

### Archlich 与连锁

| 用例 | 预期 |
|---|---|
| 普通 Archlich + 左侧亡语亡灵 | 回合结束进入死亡和亡语链，再产生 exact copy |
| 金色 Archlich + 两侧亡灵 | 两个目标按确定顺序结算，结果可重放 |
| Archlich + Warghoul + 相邻亡语 | Warghoul 死亡后触发合法相邻亡语 |
| Warghoul 两侧均为亡语 | 选择规则与战斗一致且由种子稳定 |
| Warghoul + Titus/Deios | 重复次数正确，不消费战斗专属饰品状态 |
| Reborn 亡灵无亡语且场上原有 6 个随从 | 先产生 1 点生命值 Reborn 实体，再产生 exact copy，最终七格 |
| Reborn 亡灵无亡语且场上原有 7 个随从 | Reborn 占用死亡空位，exact copy 因满场失败 |
| 亡语召唤先填满战场 | Reborn 失败；其后的 exact copy 仍按当时空间独立检查 |
| Reborn 成功 | 保留原卡描述、当前攻击/最大生命、永久附魔和应保留计数器；生命为 1、实例 ID 更新、Reborn 被消耗；派发适用的友方召唤事件，当前全局 Buff 只应用一次 |
| Archlich exact copy 成功 | 复制死亡前完整状态，包括尚未消耗的 Reborn；作为独立新实体入场 |

### 亡语后响应

| 用例 | 预期 |
|---|---|
| 准备阶段亡语 + Blood Amulet | 永久鲜血宝石生效 |
| 准备阶段亡语 + Unholy Sanctum | 最右随从永久成长 |
| 准备阶段亡语 + Ghoul-acabra | 真实棋盘永久成长 |
| 准备阶段亡语 + Saurolisk | 自身永久成长 |
| 准备阶段亡语 + Thornspike Pauldron | 下一场战斗前宝石增益生效 |
| 准备阶段亡语 + Deathly Phylactery | 不重复、不消费每场战斗状态 |

### 回归保护

- 现有战斗亡语、复仇、战斗奖励和回放顺序保持不变；战斗 Reborn 改为新实体 ID，并与准备阶段共用同一状态构造规则。
- 玩家与敌方战斗奖励隔离保持不变。
- 准备阶段死亡不产生 `CombatReward`。
- 死亡链必须有深度/步骤安全上限，随机选择必须使用稳定种子。
- 调试 Remove 不得误触发真实玩法事件。

## 验收标准

- 所有明确 `Destroy`/`Dies` 的准备阶段入口走同一死亡 API。
- 所有 Sell/Remove/Return/Transform 路径都有负向亡语测试。
- Plaguerunner 能在战斗外真实死亡时使用更高数值，出售时不能触发。
- Archlich Kel'Thuzad 能触发准备阶段亡语，并按最终确认规则重召 exact copy。
- Warghoul 可在准备阶段死亡链中触发相邻亡语。
- 准备阶段真实死亡按“亡语及其召唤 → Reborn”结算；Reborn 作为召唤派发事件并受七格限制。
- Archlich 目标具有 Reborn 时，空间足够可同时得到 Reborn 实体与 exact copy；后到召唤在满场时失败。
- Titus、Turbulent Tombs、Deios 等跨阶段额外次数生效；Deathly Phylactery 不生效。
- “触发亡语后”永久效果能在准备阶段响应。
- 所有战斗专属亡语均有明确的准备阶段策略，不能因缺少敌方战场而崩溃。
- 完整 EditMode 回归和准备/战斗阶段专项回归通过。

## 已确认并关闭的问题

### Timewarped Warghoul

官方与项目数据都是“亡语：触发一个相邻随从的亡语”。此前列出的“第 3 种可能”只是用于排除实际指向 Timewarped Hawkstrider/Herald Sticker 的情况；用户已确认不是这两张卡，因此关闭该分歧。

### 亡语召唤与 Reborn

- 准备阶段的真实 `Destroy`/`Dies` 会结算 Reborn，关键词没有战斗阶段限定。
- Deathrattle 总是在 Reborn 前结算；Battlegrounds 的 Deathrattle 沿用通用模式规则。
- Reborn 属于 summon，会派发召唤事件，并在发生时检查七格上限。
- 亡语召唤先占满战场时，Reborn 失败；不预留位置、不挤随从。
- Reborn 不是基础白板复制，也不是 Archlich exact copy：它保留原卡身份、描述、亡语、当前攻击、最大生命、永久附魔和应保留计数器，只消耗 Reborn 并将当前生命设为 1。
- Reborn 是新实体，因此必须生成新的 `InstanceId`，重置 `AttacksThisCombat`、可攻击状态和击杀来源标签，并将卡池所有权设为 `Summon`，不能重复持有原卡池份数。
- 当前有效的亡灵成长、甲虫成长、动态属性和战斗内持续全局 Buff 在 Reborn 入场时重新核算；已有同源跟踪附魔通过 ID 去重，不能重复叠加。
- `battlecruiser_full_health_reborn` 等明确写明满血 Reborn 的特殊效果继续覆盖通用“1 点生命”规则。

### Archlich exact copy

项目契约采用强制死亡检查点语义：先保存死亡前 exact-copy 快照，再完整处理死亡、亡语和 Reborn，最后尝试召唤 exact copy。空间足够时 Reborn 与 exact copy 都会生成；空间不足时后到的召唤失败。

证据边界：Blizzard 官方页确认卡牌文本，但没有单独发布 Archlich × Reborn × 满场的顺序说明。这里依据通用 Reborn/死亡阶段规则、类似“消灭后再召唤”效果的强制死亡阶段，以及项目现有战斗顺序锁定测试契约。若未来客户端录像给出相反结果，应以客户端实测修正规范。

## 仍待讨论问题

以下问题没有足够的官方顺序资料，或会明显改变玩法。实现前需要逐条确认。

### 1. 金色 Archlich 的双目标顺序

需要确认相邻亡灵是同时标记死亡，还是左到右逐个完整结算。该顺序会影响：

- Warghoul 的相邻目标。
- 亡语召唤占位。
- 死亡观察者和额外次数。
- exact copy 的插入位置。

### 2. 回合结束触发顺序与 Drakkari

需要确认实际酒馆战棋按站位、入场顺序还是其他内部顺序执行回合结束效果；Drakkari 是让每个效果连续执行两次，还是完整队列执行第二轮。

### 3. Avenge 和通用“友方随从死亡”观察者

文字未限定战斗的古老之魂、永恒骑士、刺鬃废料铁匠、藤语野猪人等是否全部感知准备阶段死亡？Avenge 关键词是否也在准备阶段累计？

建议按文本作用域分类，但在实现前需要代表性客户端实测。

### 4. 战斗专属亡语在准备阶段被触发时的行为

例如：

- Bassgill 的“仅限本场战斗”手牌召唤。
- Leeroy 的“消灭击杀本随从的随从”。
- Kangor 的“本场战斗中最先死亡的机械”。
- 造成敌方伤害或立即攻击的亡语。

可选策略：

1. 缺少必要战斗上下文时该子效果不执行。
2. 整个亡语不执行。
3. 为准备阶段定义特定替代语义。

建议优先选择 1，并逐卡加测试；不能自动把“本场战斗”效果永久化。

### 5. Consume 是否属于死亡

当前恶魔和扭曲时空吞食都直接移除目标。需要确认官方规则中 Consume 是否：

- 不算死亡、不触发亡语；
- 算战斗外死亡；或
- 依具体卡牌而异。

建议在确认前保持独立 `Consumed` 原因，不接入亡语。

### 6. Destroy 后卡池份数

需要统一确认普通 Destroy、Tomb Turning、Graverobber、Jailer Sticker 和 Archlich exact copy 的卡池归还/持有规则。

### 7. 原卡牌后续效果与死亡队列的检查点

Destroy 目标后，是先把死亡/亡语链完整结算，再继续“获取复制/获得卡牌/增加成长”，还是先完成卡牌文本后再进行死亡检查？这会影响随机目标、手牌上限和战场空间。

## 来源

### Blizzard 官方卡牌库

- [Timewarped Warghoul](https://hearthstone.blizzard.com/en-us/cards/127443-timewarped-warghoul)
- [Butchering](https://hearthstone.blizzard.com/en-us/cards/110412)
- [Archlich Kel'Thuzad](https://hearthstone.blizzard.com/en-us/cards/105518)
- [Disguised Graverobber](https://hearthstone.blizzard.com/en-us/cards/104610)
- [Tomb Turning](https://hearthstone.blizzard.com/en-us/cards/126957)
- [Jailer Sticker — Greater](https://hearthstone.blizzard.com/en-us/cards/131133)
- [Jailer Sticker — Lesser](https://hearthstone.blizzard.com/en-us/cards/131135)
- [Sacrificial Altar](https://hearthstone.blizzard.com/en-us/cards/111109)
- [Plaguerunner](https://hearthstone.blizzard.com/en-us/cards/126451)
- [Titus Rivendare](https://hearthstone.blizzard.com/en-us/cards/97408)
- [Turbulent Tombs](https://hearthstone.blizzard.com/en-us/cards/104670)
- [Timewarped Deios](https://hearthstone.blizzard.com/en-us/cards/128213)
- [Deathly Phylactery](https://hearthstone.blizzard.com/en-us/cards/117794)
- [Blood Amulet](https://hearthstone.blizzard.com/en-us/cards/130906)
- [Unholy Sanctum](https://hearthstone.blizzard.com/en-us/cards/120930)
- [Timewarped Ghoul-acabra](https://hearthstone.blizzard.com/en-us/cards/128044)
- [Timewarped Saurolisk](https://hearthstone.blizzard.com/en-us/cards/127126)
- [Thornspike Pauldron](https://hearthstone.blizzard.com/en-us/cards/130902)

### 通用机制资料（社区维护）

- [Reborn 规则原文](https://hearthstone.fandom.com/api.php?action=parse&page=Reborn&prop=wikitext&format=json)：明确 Deathrattle 先于 Reborn，且 Reborn 属于 summon。
- [Battlegrounds/Deathrattle 规则原文](https://hearthstone.fandom.com/api.php?action=parse&page=Battlegrounds%2FDeathrattle&prop=wikitext&format=json)：说明酒馆战棋亡语与其他模式采用相同规则。
- [Advanced rulebook 规则原文](https://hearthstone.fandom.com/api.php?action=parse&page=Advanced_rulebook&prop=wikitext&format=json)：记录死亡阶段、强制死亡阶段，以及“消灭后再召唤”类效果先结算死亡后继续原效果的机制。

这些页面是社区维护资料，不等同于 Blizzard 官方规则书；本文只在官方卡牌页未描述底层时序时使用，并将置信度与证据边界单独标注。

### 本地证据

- `Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs`
- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/TavernSpellEngine.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Models/MechanicModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/EffectDispatcher.cs`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsSpells.json`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json`
- `Assets/LearnHearthstone/Resources/Data/timewarpedTavernCards.json`

## 下一步

首批准备阶段死亡管线已经实现并通过专项回归。后续按“仍待讨论问题”继续处理金色 Archlich、回合结束总队列、Avenge/死亡观察者、Consume 和卡池所有权，不在当前实现中提前假定规则。
