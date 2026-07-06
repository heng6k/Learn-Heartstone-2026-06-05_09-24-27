# 对手手牌、分阵营全局变量与完整回合推进实现方案

更新日期：2026-07-06

## 背景

当前战队配置更偏向直接编辑场上随从和少量全局状态，但很多酒馆战棋效果并不只依赖当前场面。

例如：

- 某些亡灵、复仇、召唤类效果会读取或生成手牌中的随从。
- 己方和对手可能分别拥有不同的法术释放次数、法术强度、鲜血宝石质量、亡灵攻击成长、永恒骑士死亡次数等历史变量。
- 点击“下一回合”如果只是简单跳到下一回合，会绕过回合结束、战斗、战斗结算、新回合开始等关键流程，导致测试场景和真实对局不一致。

因此需要把测试/训练器里的战队配置能力从“静态摆场”升级为“可复现的完整战斗前状态配置”。

## 目标

1. 为对手增加手牌机制。
2. 卡牌库可以把牌加入对手手牌，而不仅是对手场上。
3. 对手手牌中的牌可以直接删除。
4. 对手手牌要能影响实际战斗和召唤逻辑。
5. 全局变量调整拆分为己方和对手两套。
6. 支持配置常见历史计数和阵营成长量。
7. 点击下一回合后，按完整流程推进：回合结束 -> 战斗 -> 战斗结算 -> 下一回合开始。
8. 所有 UI 操作都通过服务层命令，不直接改核心状态。
9. 保存/加载测试场景时保留对手手牌和双方全局变量。

## 非目标

- 不在第一阶段重做整个战队编辑器 UI。
- 不把所有特殊机制一次性做成完整官方模拟，只先补稳定复现测试所需的状态入口。
- 不允许 UI 直接写入战斗引擎内部缓存。
- 不把“下一回合”做成可跳过战斗的默认行为；如需跳过，应单独提供明确按钮。

## 功能一：对手手牌机制

### 使用场景

配置对手战队时，玩家可以像配置己方手牌一样配置对手手牌。

典型用途：

- 验证从对手手牌召唤随从的效果。
- 验证 6 本亡灵、复仇 4、获得亡灵等机制对实际战队的影响。
- 构造战斗中会消耗、召唤、复制或读取手牌的复杂场景。

### UI 行为

卡牌库的添加目标从当前能力扩展为：

- 己方手牌。
- 己方场上。
- 对手手牌。
- 对手场上。

对手区域新增一个手牌展示区：

- 显示对手手牌中的卡牌。
- 支持直接删除单张手牌。
- 最好支持悬停或点击查看详情。
- 卡牌顺序应稳定保存，便于测试复现。

### 服务层建议

优先使用通用命令，而不是只为对手新增一次性命令：

```csharp
GameCommandType.AddCardToHand
GameCommandType.RemoveHandCard
```

命令参数建议包含：

```csharp
BoardSide Side;       // Player / Opponent
string CardId;
CardKind CardKind;
int Index;
```

如果现有 `AddCardToHand` 只支持己方，可以扩展为：

```csharp
AddCardToHand(BoardSide side, string cardId, CardKind kind)
RemoveHandCard(BoardSide side, int index)
```

### 战斗引擎要求

对手手牌不能只是 UI 数据。进入战斗快照时，双方都应携带各自手牌：

```csharp
CombatSideState.Player.Hand
CombatSideState.Opponent.Hand
```

任何“从手牌召唤”“读取手牌”“获得指定池随从到手牌再召唤”的效果，都应根据当前触发方读取对应阵营的手牌。

### 验收标准

- 可以从卡牌库把随从加入对手手牌。
- 可以删除对手手牌中的指定卡牌。
- 对手手牌会进入测试场景保存和加载。
- 战斗效果可以读取对手手牌。
- 从对手手牌召唤随从时，不会错误读取己方手牌。
- 删除对手手牌后，该牌不再参与战斗。

## 功能二：分阵营全局变量配置

### 问题

当前很多“全局量”如果只有一份，会导致测试不准确。己方和对手在真实对局中可能有完全不同的历史状态。

例如：

- 己方释放过 6 个法术，对手释放过 0 个法术。
- 己方鲜血宝石是 +3/+2，对手鲜血宝石是 +1/+1。
- 对手亡灵全局攻击力更高。
- 己方永恒骑士死亡次数和对手不同。

### 变量分组

建议新增或整理为：

```csharp
public sealed class SideCombatModifierState
{
    public int SpellsCastThisGame;
    public int SpellPower;
    public int BloodGemAttackBonus;
    public int BloodGemHealthBonus;
    public int UndeadAttackBonus;
    public int EternalKnightDeaths;
    public int AstralAutomatonSummons;
}
```

挂载位置建议：

```csharp
PlayerSideState.CombatModifiers
OpponentSideState.CombatModifiers
```

或：

```csharp
Dictionary<BoardSide, SideCombatModifierState>
```

### 首批建议支持的变量

| 变量 | 说明 |
| --- | --- |
| 已释放法术次数 | 影响依赖本局/本回合法术次数的随从、任务、饰品或英雄技能。 |
| 法术强度 | 影响部分法术或特殊效果数值。 |
| 鲜血宝石攻击加成 | 影响鲜血宝石给随从的攻击增益。 |
| 鲜血宝石生命加成 | 影响鲜血宝石给随从的生命增益。 |
| 亡灵全局攻击力 | 影响亡灵召唤物或亡灵战队成长。 |
| 永恒骑士死亡次数 | 影响永恒骑士相关成长效果。 |
| 星元机召唤次数 | 影响星元机类随从的成长。 |
| 复仇相关历史计数 | 用于稳定复现复仇类效果的边界状态。 |

后续可以扩展：

- 海盗购买次数。
- 元素刷新/使用次数。
- 野兽召唤次数。
- 龙类战斗开始成长计数。
- 任务进度快照。
- 饰品局内计数器。

### UI 行为

全局变量面板需要明确分为己方和对手。

推荐两种 UI：

1. 左右双栏：
   - 左：己方变量。
   - 右：对手变量。

2. 阵营切换：
   - `己方`
   - `对手`

每个变量应使用数字输入或加减按钮，并显示简短用途。

示例：

```text
己方 / 法术释放次数：6
对手 / 法术释放次数：0

己方 / 鲜血宝石：+3 / +2
对手 / 鲜血宝石：+1 / +1
```

### 服务层命令建议

```csharp
GameCommandType.SetSideCombatModifier
GameCommandType.AdjustSideCombatModifier
```

参数：

```csharp
BoardSide Side;
SideCombatModifierKind Kind;
int Value;
```

### 验收标准

- 己方和对手变量可以独立设置。
- 设置己方变量不会修改对手变量。
- 设置对手变量不会修改己方变量。
- 战斗引擎读取触发方对应的变量。
- 测试场景保存/加载后变量不丢失。

## 功能三：完整下一回合流程

### 当前问题

如果“下一回合”只是简单增加回合数并刷新商店，会跳过许多真实对局流程：

1. 回合结束触发。
2. 招募阶段结束状态结算。
3. 战斗开始触发。
4. 完整战斗流程。
5. 战斗结果结算。
6. 战斗后状态更新。
7. 下一回合开始触发。
8. 新回合商店、金币、冻结、任务/饰品/英雄技能等状态刷新。

这会让依赖战斗或回合边界的机制无法通过训练器稳定验证。

### 新行为

点击“下一回合”后，默认执行完整流程：

```text
Recruit Turn End
-> End-of-Turn Triggers
-> Combat Setup
-> Start-of-Combat Triggers
-> Combat Simulation
-> Combat Result Resolution
-> Post-Combat Triggers
-> Next Recruit Turn Start
-> Start-of-Turn Triggers
-> Shop/Gold/Offer Refresh
```

### 建议服务层入口

当前 `NextTurn` 命令应从“直接推进”改为“完整推进”。

建议拆出明确内部方法：

```csharp
ResolveRecruitTurnEnd();
RunFullCombatForTurn();
ResolveCombatResult();
StartNextRecruitTurn();
```

主入口：

```csharp
AdvanceToNextTurnWithCombat()
```

如果仍需要旧行为，建议改名为调试命令：

```csharp
GameCommandType.DebugSkipToNextTurn
```

不要让普通 `NextTurn` 默认跳过战斗。

### 状态要求

完整流程中需要保留并更新：

- 当前回合数。
- 己方/对手场面。
- 己方/对手手牌。
- 双方全局变量。
- 任务进度。
- 饰品计数器。
- 英雄技能状态。
- 战斗日志。
- 招募日志。
- 战斗快照。

### UI 行为

下一回合按钮文案可以保持 `下一回合`，但行为变成完整流程。

如果担心用户误解，可以增加一个辅助按钮：

- `下一回合`：完整流程。
- `跳过战斗`：仅调试使用，明显标记为调试。

### 验收标准

- 点击下一回合会先处理回合结束。
- 回合结束后会执行完整战斗。
- 战斗后才进入下一回合开始。
- 依赖战斗开始、战斗中、战斗后、回合开始的效果都能被触发。
- 对手手牌和双方全局变量会参与这次战斗。
- 战斗结果和日志可以复查。
- 如果战斗失败或无对手阵容，应有明确错误或 fallback，不应静默跳过。

## 数据与保存/加载

测试场景模型需要扩展保存：

```csharp
PlayerHand
OpponentHand
PlayerCombatModifiers
OpponentCombatModifiers
Round
PendingTurnPhase
LastCombatSnapshot
```

加载场景时应恢复：

- 己方场面。
- 己方手牌。
- 对手场面。
- 对手手牌。
- 双方全局变量。
- 当前回合与酒馆状态。

## 测试建议

### 对手手牌

- `OpponentHand_AddsCardFromLibrary`
- `OpponentHand_RemovesCardByIndex`
- `OpponentHand_SavesAndLoadsWithScenario`
- `OpponentHand_CombatEffectReadsOpponentHand`
- `OpponentHand_DoesNotReadPlayerHandForOpponentEffect`

### 分阵营变量

- `SideCombatModifiers_PlayerAndOpponentAreIndependent`
- `SideCombatModifiers_BloodGemQualityUsesTriggeringSide`
- `SideCombatModifiers_UndeadAttackUsesCorrectSide`
- `SideCombatModifiers_EternalKnightDeathsUseCorrectSide`
- `SideCombatModifiers_AstralAutomatonSummonsUseCorrectSide`

### 完整下一回合

- `NextTurn_RunsEndTurnBeforeCombat`
- `NextTurn_RunsCombatBeforeNextRecruitStart`
- `NextTurn_TriggersStartOfCombatEffects`
- `NextTurn_TriggersPostCombatAndStartTurnEffects`
- `NextTurn_UsesOpponentHandAndSideModifiers`

## 实施阶段

### 阶段 1：状态模型与命令

- 为对手增加手牌状态。
- 增加通用 `AddCardToHand(BoardSide)` 和 `RemoveHandCard(BoardSide)`。
- 增加分阵营变量模型。
- 增加设置/调整变量命令。

### 阶段 2：UI 接入

- 卡牌库目标增加对手手牌。
- 对手区域展示手牌。
- 对手手牌支持删除。
- 全局变量面板拆成己方/对手。

### 阶段 3：战斗引擎接入

- 战斗快照携带双方手牌。
- 从手牌召唤/读取手牌效果改为按触发方读取。
- 战斗逻辑读取触发方的分阵营变量。

### 阶段 4：完整下一回合流程

- 重构 `NextTurn` 为完整回合推进。
- 补齐回合结束、战斗、战斗结算、下一回合开始的顺序。
- 如保留旧跳过行为，改为明确调试命令。

### 阶段 5：测试与回归

- 覆盖对手手牌添加、删除、保存、加载。
- 覆盖对手手牌参与战斗。
- 覆盖双方变量互不污染。
- 覆盖下一回合完整流程顺序。

## 风险与注意事项

### 不要把对手手牌做成纯 UI

如果对手手牌不进入战斗快照，就无法验证真实机制。

### 不要共享双方全局变量实例

己方和对手必须是两份独立状态，避免编辑一边污染另一边。

### 不要让下一回合静默跳过战斗

如果没有可战斗的对手，应明确提示或使用当前测试对手，而不是假装完成完整流程。

### 注意旧测试兼容

如果旧测试依赖 `NextTurn` 直接跳过战斗，需要改为使用新的调试跳过命令，或更新测试期望。

## 最终验收清单

- 卡牌库可以添加卡牌到对手手牌。
- 对手手牌可以单张删除。
- 对手手牌会保存和加载。
- 对手手牌能参与实际战斗效果。
- 全局变量可以分别配置己方和对手。
- 法术次数、法强、宝石质量、亡灵攻击、永恒骑士死亡次数、星元机召唤次数等变量有明确入口。
- 战斗逻辑读取正确阵营变量。
- 点击下一回合会执行：回合结束 -> 完整战斗 -> 下一回合开始。
- 下一回合流程会写入可复查日志。
- UI 只发送命令，不直接改核心状态。
- 有 EditMode 测试覆盖核心行为。
