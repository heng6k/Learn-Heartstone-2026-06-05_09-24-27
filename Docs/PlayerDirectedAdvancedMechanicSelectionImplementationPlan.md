# 玩家自由搭配高级机制选择实现方案

更新日期：2026-07-06

## 背景

当前任务、饰品、第二英雄技能等高级机制已经有随机候选和选择流程，但为了测试和玩家自定义搭配，还需要在选择界面提供一个“自由选择”入口。

这个入口不是仅供调试使用。它应该是玩家可见、玩家可用的正式训练器功能：玩家可以在合法范围内主动选择自己想玩的任务、任务奖励、饰品和第二英雄技能，从而搭出想验证或想体验的组合。

核心原则：

- 玩家可以自由搭配，但不能绕过当前 ban 选和当前池规则。
- UI 只负责展示和发起选择，最终合法性由服务层判断。
- 所有结果必须走现有激活、装备、发现、日志和状态更新路径，不能只改 UI 文本或直接改字段。

## 目标

1. 在任务选择、饰品选择、第二英雄技能选择界面右侧增加一个小按钮。
2. 点击按钮后打开统一的“自由选择器”。
3. 玩家可以从当前合法候选池中精确挑选目标。
4. 任务场景支持直接选择“任务 + 任务奖励”组合。
5. 饰品场景支持按 Lesser / Greater 槽位选择目标饰品。
6. 第二英雄技能场景支持选择任意当前合法第二英雄技能。
7. 所有候选项必须满足本局种族 ban、启用开关、池版本、实现状态、槽位和拥有状态限制。
8. 选择过程要写入日志，便于测试复现和回放。

## 非目标

- 不允许选择 `Disabled` 或未实现内容作为玩家正常选择，除非以后单独做“开发者实验模式”。
- 不把 `DebugOnly` 当作普通玩家池内容。
- 不绕过 `ChooseMechanicOption`、`ChooseDiscover`、饰品装备、任务完成、英雄技能持有等服务层流程。
- 不在第一阶段重做整套 UI 视觉，只补必要入口、选择器和验证闭环。

## 模式定义

### 普通随机模式

保持当前随机三选一或普通候选逻辑。适合模拟正常对局。

### 玩家自由搭配模式

在选择界面显示“自由选择”按钮。玩家可以打开完整合法候选列表，从中指定自己想要的结果。

建议在开局设置里增加：

```csharp
public bool EnablePlayerDirectedChoices = true;
```

训练器默认可以开启；如果以后需要严格随机测试，可以在开局设置里关闭。

### 开发者实验模式

可后续再做。它可以显示 `DebugOnly`、`HiddenEffectOnly`、`Disabled` 或池外内容，但必须有明显标识和二次确认。本文第一阶段不实现这个模式。

## 合法性规则

自由选择不是无限制作弊选择，而是“当前合法候选池内的手动指定”。

通用过滤规则：

- 必须是 `Implemented`。
- 必须满足当前 `ActiveTribes` 和 ban 选。
- 必须满足当前卡池版本和系统启用开关。
- 普通玩家模式只允许正常可玩池。
- `Disabled` 不进入玩家自由选择。
- `DebugOnly` 不进入玩家自由选择。
- `HiddenEffectOnly` 默认不进入玩家自由选择；如果某类内容本来就是隐藏触发型，只能通过它对应的正常机制生效。
- 候选项如果被过滤，选择器可以显示“不可选原因”，但不能让玩家直接选择。

## 交互设计

### 按钮位置

在现有选择面板右侧增加一个小按钮：

- 文案：`自由选择`
- 紧凑场景可用 `自选`
- 鼠标悬停提示：`从当前合法池中指定一个结果`

按钮出现位置：

- 任务选择面板右侧。
- Lesser / Greater 饰品选择面板右侧。
- 第二英雄技能 Discover 或选择面板右侧。

按钮行为：

1. 打开统一自由选择器。
2. 默认带入当前选择上下文。
3. 显示当前上下文可选的合法候选项。
4. 玩家选择后调用服务层命令。
5. 服务层再次验证合法性。
6. 验证通过后执行选择并写日志。

### 选择器基础能力

统一选择器应支持：

- 名称搜索。
- CardId 搜索。
- 类型筛选。
- 状态筛选。
- 种族筛选。
- 槽位筛选。
- 费用 / 强度 / 触发时机筛选。
- 显示不可选原因。
- 显示卡牌图片、名称、文本、ID、备注。

## 任务自由选择

### 使用场景

当系统弹出任务选择时，玩家点击右侧 `自由选择`，可以手动挑选自己想玩的任务和奖励组合。

### 选择内容

任务自由选择必须支持组合选择：

- 左侧：任务列表。
- 右侧：任务奖励列表。
- 底部：当前组合预览。

玩家最终确认的是一组：

```text
QuestCardId + QuestRewardId
```

### 过滤规则

任务过滤：

- `QuestImplementationStatus=Implemented`。
- 当前模式允许任务。
- 不包含已明确禁用的任务。

任务奖励过滤：

- `QuestRewardImplementationStatus=Implemented`。
- `OfferPoolStatus` 在普通可玩范围内。
- 不包含 `Disabled`、`DebugOnly`。
- 如果奖励依赖特定系统，必须确认该系统在当前局启用。

### 服务层建议

新增候选查询：

```csharp
GetPlayerSelectableQuestPairs(PlayerDirectedChoiceContext context)
```

返回：

```csharp
public sealed class PlayerSelectableQuestPair
{
    public QuestDefinition Quest;
    public QuestRewardDefinition Reward;
    public bool IsSelectable;
    public string DisabledReason;
}
```

新增命令：

```csharp
GameCommandType.ChoosePlayerDirectedQuestPair
```

命令参数：

- `QuestCardId`
- `QuestRewardId`
- `Slot`

执行时不要直接写 `ActiveQuestState`。应复用或封装现有 `CreateQuestChoiceOption` / `ChooseMechanicOption` 路径，保证任务绑定、难度、日志、进度和奖励状态一致。

### 验收标准

- 玩家可以从合法任务和合法奖励中组成任意组合。
- 被 ban 或禁用的奖励不会出现在可选列表中。
- 选择后当前任务面板显示正确任务、奖励、图片和文本。
- 后续任务进度和奖励触发走现有逻辑。
- 日志记录 `Player directed Quest selection: <quest> + <reward>`。

## 饰品自由选择

### 使用场景

当系统弹出 Lesser 或 Greater 饰品选择时，玩家点击右侧 `自由选择`，可以手动指定本次要购买或装备的饰品。

### 选择内容

选择器需要区分：

- Lesser Trinket
- Greater Trinket

默认只显示当前槽位对应的饰品。可以显示另一个槽位的标签页，但不能把 Lesser 装进 Greater 槽，或把 Greater 装进 Lesser 槽，除非后续专门做规则变体。

### 过滤规则

饰品必须满足：

- `ImplementationStatus=Implemented`。
- `OfferPoolStatus=Offerable` 或等价普通可玩状态。
- `SlotKind` 匹配当前目标槽位。
- `associatedRaces` 满足当前 `ActiveTribes`。
- `requires` 条件满足当前局状态。
- 当前槽位允许装备或替换。

种族 ban 规则：

- `associatedRaces` 为空：视为中立，可进入候选。
- `associatedRaces` 非空：至少一个关联种族在当前 `ActiveTribes` 中才可进入候选。
- 关联种族全部被 ban：不进入玩家可选列表。

### 服务层建议

新增候选查询：

```csharp
GetPlayerSelectableTrinkets(
    TrinketSlotKind slotKind,
    PlayerDirectedChoiceContext context)
```

新增命令：

```csharp
GameCommandType.ChoosePlayerDirectedTrinket
```

命令参数：

- `TrinketCardId`
- `TargetSlotKind`
- `CostOverride` 可选

执行时必须走统一装备逻辑：

1. 校验目标饰品是否合法。
2. 校验目标槽位是否合法。
3. 扣费或使用当前选择流程的价格规则。
4. 装备饰品。
5. 触发 on-equip 效果。
6. 更新 UI 状态。
7. 写入日志。

不要只改 `LesserTrinketId` 或 `GreaterTrinketId`。

### 验收标准

- Lesser 选择只装备 Lesser。
- Greater 选择只装备 Greater。
- 种族 ban 后，纯关联被 ban 种族的饰品不会出现。
- 中立饰品仍可出现。
- 选择后饰品效果、状态、日志和 UI 一致。
- 日志记录 `Player directed Trinket selection: <slot> <trinket>`。

## 第二英雄技能自由选择

### 使用场景

在“双重宇宙”或类似机制触发第二英雄技能选择时，玩家点击右侧 `自由选择`，可以从当前合法英雄技能中指定第二英雄技能。

当前代码里相近流程包括：

- `StartSecondHeroPowerDiscover`
- `ExtraHeroPowerCardIds`
- `GetOfferableDiscoverableHeroPowers`
- `ChooseDiscover`

### 过滤规则

第二英雄技能必须满足：

- 英雄技能已实现。
- 不等于当前主英雄技能。
- 不在 `ExtraHeroPowerCardIds` 已拥有列表中。
- 不属于禁用或不可发现英雄技能。
- 满足当前模式、池版本和特殊规则。
- 如果某英雄技能依赖未实现目标选择或特殊 UI，必须显示不可选原因。

### 服务层建议

新增候选查询：

```csharp
GetPlayerSelectableSecondHeroPowers(PlayerDirectedChoiceContext context)
```

新增命令：

```csharp
GameCommandType.ChoosePlayerDirectedSecondHeroPower
```

命令参数：

- `HeroPowerCardId`
- `Source`

执行时应复用现有第二英雄技能获得路径：

1. 校验候选合法。
2. 写入 `ExtraHeroPowerCardIds`。
3. 清理当前 Discover 或 PendingChoice。
4. 写入日志。
5. 刷新英雄技能 UI。

如果当前第二英雄技能来源本来是 Discover，推荐内部复用 `DiscoverState` 的完成流程或提供一个统一的 `ResolveSecondHeroPowerSelection` 方法，避免 UI 直接改列表。

### 验收标准

- 当前主英雄技能不会出现在可选列表中。
- 已拥有的额外英雄技能不会重复出现。
- 不可用英雄技能显示不可选原因。
- 选择后额外英雄技能入口可用。
- 日志记录 `Player directed second Hero Power selection: <hero power>`。

## 统一数据结构建议

新增上下文对象：

```csharp
public sealed class PlayerDirectedChoiceContext
{
    public PlayerDirectedChoiceKind Kind;
    public string Source;
    public string Slot;
    public int Round;
    public IReadOnlyList<Tribe> ActiveTribes;
    public bool IncludeDebugOnly;
    public bool IncludeHiddenEffectOnly;
    public bool IncludeDisabled;
}
```

玩家模式下：

```csharp
IncludeDebugOnly = false;
IncludeHiddenEffectOnly = false;
IncludeDisabled = false;
```

新增候选项对象：

```csharp
public sealed class PlayerDirectedChoiceOption
{
    public PlayerDirectedChoiceKind Kind;
    public string CardId;
    public string SecondaryCardId;
    public string DisplayName;
    public string Text;
    public string ImagePath;
    public string Status;
    public string DisabledReason;
    public bool IsSelectable;
}
```

其中 `SecondaryCardId` 可用于任务奖励组合：

```text
CardId = QuestCardId
SecondaryCardId = QuestRewardId
```

## UI 组件建议

新增统一弹窗：

```text
UnityTavernPlayerDirectedChoiceModalComponent
```

它不负责判断最终合法性，只负责：

- 请求候选列表。
- 展示候选项。
- 搜索和筛选。
- 显示不可选原因。
- 发送选择命令。

可复用现有卡牌展示组件：

- `UnityTavernCardComponent`
- `UnityTavernCardDetailModalComponent`
- `UnityTavernDiscoverModalComponent` 的布局经验

## 日志与回放

所有自由选择都需要写入 recruit log：

- 选择来源。
- 选择类型。
- 选择的 CardId。
- 如果是任务组合，同时记录 QuestId 和 RewardId。
- 如果候选曾被过滤但玩家尝试选择，记录失败原因。

日志示例：

```text
Player directed Quest selection: BG24_Quest_112 + BG24_Reward_350.
Player directed Trinket selection: Lesser / ABC_123.
Player directed second Hero Power selection: HERO_XXbp.
```

测试场景保存时，应把这些选择后的实际状态保存下来。后续如果要做完整回放，可以再把自由选择命令纳入 command history。

## 实施阶段

### 阶段 1：服务层候选查询

目标：

- 建立统一候选查询入口。
- 查询任务组合、饰品、第二英雄技能。
- 每个候选都给出 `IsSelectable` 和 `DisabledReason`。

验收：

- EditMode 测试覆盖合法候选、ban 后过滤、禁用内容过滤。

### 阶段 2：服务层选择命令

目标：

- 新增玩家自由选择命令。
- 选择结果走现有任务、饰品、英雄技能流程。

验收：

- 任务组合选择后能正常推进任务和奖励。
- 饰品选择后装备和效果生效。
- 第二英雄技能选择后进入 `ExtraHeroPowerCardIds`。

### 阶段 3：统一自由选择器 UI

目标：

- 新建统一弹窗。
- 支持搜索、筛选、详情和不可选原因。

验收：

- 三类选择共用同一个弹窗基础组件。
- UI 不直接改状态。

### 阶段 4：接入任务、饰品、第二英雄技能界面

目标：

- 在对应选择界面右侧增加 `自由选择` 小按钮。
- 根据当前上下文打开对应候选列表。

验收：

- 任务选择界面能打开任务组合自由选择。
- 饰品选择界面能打开对应槽位自由选择。
- 第二英雄技能选择界面能打开英雄技能自由选择。

### 阶段 5：测试与回归

目标：

- 补齐服务层测试。
- 补齐 Unity UI 测试。
- 补齐 ban 选和不可选原因测试。

建议测试：

- `PlayerDirectedQuestChoice_RespectsRewardOfferPool`
- `PlayerDirectedQuestChoice_SelectsQuestAndRewardPair`
- `PlayerDirectedTrinketChoice_RespectsActiveTribes`
- `PlayerDirectedTrinketChoice_EquipsThroughServicePath`
- `PlayerDirectedSecondHeroPower_ExcludesOwnedAndCurrentPowers`
- `PlayerDirectedChoiceModal_ShowsButtonOnSupportedChoiceScreens`

## 风险与注意事项

### 不要破坏随机模式

自由选择是额外入口，不应改变原本随机三选一逻辑。关闭 `EnablePlayerDirectedChoices` 后，界面和行为应回到当前模式。

### 不要让 UI 绕过服务层

UI 不直接写：

- `PendingChoice`
- `ActiveQuestState`
- `LesserTrinketId`
- `GreaterTrinketId`
- `ExtraHeroPowerCardIds`

UI 只发命令，服务层负责验证和执行。

### 不要让玩家模式混入开发者池

玩家可用不等于所有内容可用。普通玩家自由选择只在合法普通池内自由搭配。

### 要清楚显示不可选原因

如果玩家搜索到某个内容但不能选，显示原因比直接隐藏更利于理解：

- 被当前种族 ban 过滤。
- 不是当前槽位。
- 已拥有。
- 当前池版本不可用。
- 未实现或禁用。

## 最终验收清单

- 任务选择右侧有 `自由选择` 按钮。
- 饰品选择右侧有 `自由选择` 按钮。
- 第二英雄技能选择右侧有 `自由选择` 按钮。
- 玩家可以选择合法任务 + 合法任务奖励组合。
- 玩家可以选择合法 Lesser / Greater 饰品。
- 玩家可以选择合法第二英雄技能。
- 所有候选满足当前 ban 选。
- `Disabled` 和未实现内容不会进入玩家可选结果。
- 选择后走服务层流程，效果真实生效。
- 选择行为写入日志。
- 有服务层和 UI 回归测试覆盖。

## 和测试目标的关系

这个功能本身是玩家可用功能，同时也能显著提升测试质量。

测试收益：

- 不再依赖随机刷出目标任务、饰品或英雄技能。
- 可以稳定复现指定组合。
- 可以快速验证边界组合。
- 可以验证 ban 选过滤是否真实生效。
- 可以更快构造任务、饰品、双英雄技能之间的交互场景。

因此文档里的实现口径是：玩家自由搭配优先，测试稳定性自然受益。
