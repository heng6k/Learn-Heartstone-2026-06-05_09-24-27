# 高级机制 UI 与接口适配下一阶段实现计划

更新日期：2026-06-26

本文用于规划下一阶段的任务、饰品、任务奖励 UI 与接口适配。当前优先目标不是重新设计整套 UI，而是先把训练器可控能力做扎实：玩家/开发者能够决定任务、饰品、任务奖励是否出现，能够主动完成、替换和验证高级机制，并确保所有 PendingChoice、Discover、延迟选择都有可见闭环。

## 当前判断

饰品、任务、任务奖励的系统接入和可运行实现已经完成，但 UI 和控制入口还没有完全跟上。

当前已有基础：

- `MatchSetupOptions` 已经支持 `ActiveTribes`，可以作为开局配置扩展点。
- `UnityTavernTribeSelectionView` 已经有开局种族选择 UI。
- `GameCommandType` 已有 `DebugOfferLesserTrinkets`、`DebugOfferGreaterTrinkets`、`DebugOfferQuests`。
- `MatchService` 已有 `PendingChoice`、Quest choice、Trinket choice、Discover、Quest Reward dispatch 等基础流程。
- 饰品数据已有 `slotKind`、`associatedRaces`、`offerPoolStatus`、`proxyLevel`、`effectFamily`、`requires` 等字段。
- 任务奖励数据已有 `offerPoolStatus`、`powerLevel`、`effectKind`、`trigger` 等字段。

当前缺口：

- 开局还不能显式控制是否启用任务、饰品、任务奖励。
- UI 不能主动控制某类内容是否进入候选池。
- Quest / Trinket / Quest Reward 的替换入口不完整。
- DebugOnly、HiddenEffectOnly、Disabled、ProxySafe、Exact 等状态没有形成稳定 UI 展示规则。
- 饰品普通分发还没有把 `associatedRaces` 接入本局种族 ban 过滤。
- PendingChoice、Discover、延迟选择需要统一可见入口，避免出现“系统等玩家选择，但 UI 不知道该显示什么”的情况。

## 总体策略

先完成 1/2/3，再考虑镜像 UI。

这里的 1/2/3 指：

1. 任务控制：任务是否出现、一键完成、任务奖励替换。
2. 饰品控制：饰品是否出现、已选饰品替换、饰品 ban 跟随种族 ban。
3. 开局模式控制：任务、饰品、任务奖励的启用与候选池策略。

这三个阶段完成并验收后，再进入“镜像当前 UI 并重构高级机制区域”。不要先大规模重做 UI，否则机制入口、状态字段、筛选规则还没稳定时，UI 会反复返工。

## 阶段 1：开局高级机制开关

### 目标

开局时能够主动控制任务、饰品、任务奖励是否出现在本局中。

### 建议新增配置

在 `MatchSetupOptions` 中增加高级机制配置，例如：

```csharp
public bool EnableQuests = true;
public bool EnableTrinkets = true;
public bool EnableQuestRewards = true;
public bool ShowProxySafe = true;
public bool ShowDebugOnly = false;
public bool ShowHiddenEffectOnly = false;
public bool ShowDisabled = false;
```

如果后续需要更细，可以抽成：

```csharp
public sealed class AdvancedMechanicSetupOptions
{
    public bool EnableQuests = true;
    public bool EnableTrinkets = true;
    public bool EnableQuestRewards = true;
    public List<string> AllowedImplementationStatuses = new List<string>();
    public List<string> AllowedOfferPoolStatuses = new List<string>();
    public List<string> AllowedProxyLevels = new List<string>();
    public List<string> ForcedIncludedCardIds = new List<string>();
    public List<string> ForcedExcludedCardIds = new List<string>();
}
```

第一版建议先用简单 bool + 状态枚举过滤，避免配置层过度设计。

### UI 入口

在开局种族选择页面增加一个“高级机制”区域：

- 任务：开 / 关
- 饰品：开 / 关
- 任务奖励：开 / 关
- 代理实现：显示 / 隐藏
- 调试池：显示 / 隐藏
- 隐藏效果池：显示 / 隐藏
- 禁用池：默认隐藏，只允许调试模式显示

### 行为规则

- `EnableQuests=false`：不开任务选择，不触发任务进度，不显示任务面板。
- `EnableTrinkets=false`：不开 Lesser/Greater 饰品选择，不触发饰品效果，不显示饰品槽位或显示为关闭状态。
- `EnableQuestRewards=false`：任务可以出现，但奖励不从普通奖励池生成；UI 应提示“任务奖励已关闭”。如果任务必须有奖励，则任务也应自动关闭。
- `ShowProxySafe=false`：候选池排除 `proxyLevel=ProxySafe`。
- `ShowDebugOnly=false`：候选池排除 `offerPoolStatus=DebugOnly`。
- `ShowHiddenEffectOnly=false`：候选池排除 `offerPoolStatus=HiddenEffectOnly`。
- `ShowDisabled=false`：候选池排除 `offerPoolStatus=Disabled`。

### 接口改造点

- `MatchSetupOptions`
- `UnityTavernTribeSelectionView`
- `LearnHearthstoneBootstrap.StartUnityTrainer`
- `MatchService.CreateWithDefaultCatalog`
- `MatchService.OfferTrinketChoice`
- `MatchService.OfferQuestChoice`
- `QuestCatalog.OfferableRewards`
- `TrinketCatalog.GetOfferableBySlot`

### 验收标准

- 开局关闭任务后，整局不弹任务选择。
- 开局关闭饰品后，整局不弹 Lesser/Greater 饰品选择。
- 开局关闭任务奖励后，不会生成或触发任务奖励。
- 关闭 ProxySafe 后，普通候选池不出现 ProxySafe 条目。
- 默认普通模式不会出现 DebugOnly、HiddenEffectOnly、Disabled。
- 调试模式下可以主动打开这些状态并在 UI 上看到状态标签。

## 阶段 2：任务控制与任务奖励替换

### 目标

任务系统要能被训练器主动控制：可以正常展示，可以一键完成，可以从卡牌库替换任务奖励。

### UI 展示

任务面板需要显示：

- 任务名称
- 任务图片
- 任务文本
- 当前进度 / 需求进度
- 任务奖励名称
- 任务奖励图片
- 任务奖励文本
- 奖励状态：`Offerable`、`HiddenEffectOnly`、`DebugOnly`、`Disabled`
- 奖励强度：`Weak`、`Medium`、`Strong`、`Premium`
- 奖励触发：`OnComplete`、`TurnStarted`、`CardBought` 等
- 实现备注 `notes`

### 训练器按钮

任务面板增加：

- `完成任务`
- `替换任务`
- `替换奖励`
- `移除任务`

第一版优先实现：

1. `完成任务`
2. `替换奖励`

### 一键完成规则

`完成任务` 不应该直接绕过奖励系统，而应走现有完成流程：

- 将当前任务进度设为需求值。
- 调用现有 quest completion 路径。
- 触发 `QuestRewardTrigger.OnComplete`。
- 写入 recruit log。

这样可以验证真实奖励逻辑，而不是只改 UI 状态。

### 替换奖励规则

`替换奖励` 打开卡牌库选择器，过滤类型为 Quest Reward。

筛选项：

- 普通池
- 包含 Proxy / MVP
- 包含 HiddenEffectOnly
- 包含 DebugOnly
- 包含 Disabled
- 按 `effectKind` 搜索
- 按 `trigger` 搜索
- 按 `powerLevel` 搜索

替换后：

- 如果任务未完成，只替换当前任务绑定的 Reward。
- 如果任务已完成，询问或提供两个按钮：
  - `只替换显示`
  - `替换并激活奖励`

第一版建议只做“未完成任务奖励替换”和“替换并激活奖励”，避免出现显示和实际 active reward 不一致。

### 主动控制是否出现

任务奖励候选池必须受开局设置控制：

- `EnableQuestRewards`
- `ShowDebugOnly`
- `ShowHiddenEffectOnly`
- `ShowDisabled`

卡牌库替换可以绕过普通池，但 UI 必须明确显示“调试替换”。

### 建议新增命令

```csharp
DebugCompleteQuest
DebugReplaceQuest
DebugReplaceQuestReward
DebugRemoveQuest
```

也可以先只加：

```csharp
DebugCompleteQuest
DebugReplaceQuestReward
```

### 验收标准

- 当前任务能在 UI 中完整显示。
- 任务奖励能显示图片、文本、状态、触发和备注。
- 点击完成任务后，奖励按现有逻辑激活。
- 可以从 Quest Reward 卡牌库替换奖励。
- 关闭任务奖励出现后，普通任务流程不会发放奖励。
- DebugOnly / HiddenEffectOnly / Disabled 只有在对应开关打开时才可选。

## 阶段 3：饰品控制、替换与种族 ban 过滤

### 目标

饰品系统要能被训练器主动控制：可以决定饰品是否出现，可以替换已装备饰品，可以按本局种族 ban 过滤饰品池。

### UI 展示

饰品槽位需要显示：

- Lesser / Greater
- 饰品名称
- 饰品图片
- 费用
- 文本
- `Exact / ProxySafe / Blocked`
- `Offerable / HiddenEffectOnly / DebugOnly / Disabled`
- `effectFamily`
- `associatedRaces`
- `requires`
- `notes`

### 训练器按钮

每个饰品槽位增加：

- `选择`
- `替换`
- `移除`
- `查看详情`

第一版优先实现：

1. `替换`
2. `查看详情`

### 替换规则

点击 `替换` 打开卡牌库选择器：

- Lesser 槽默认只显示 Lesser。
- Greater 槽默认只显示 Greater。
- 可以手动切换槽位过滤。
- 默认只显示 `Offerable + Implemented`。
- 打开调试开关后可显示 DebugOnly / HiddenEffectOnly / Disabled。

替换时应走统一装备逻辑：

- 清掉原槽位的 equipped state。
- 装备新饰品。
- 触发新饰品 on-equip 效果。
- 写入 recruit log。

不要只改 `LesserTrinketId` / `GreaterTrinketId`，否则状态和效果会分裂。

### 饰品 ban 跟随种族 ban

数据层已有 `associatedRaces`，下一步应接入普通饰品分发。

建议规则：

- `associatedRaces` 为空：视为中立饰品，默认可出现。
- `associatedRaces` 非空：只要至少一个关联种族在本局 `ActiveTribes` 中，就可出现。
- 如果关联种族全部被 ban，则不进入普通候选池。
- Debug 替换卡牌库可以显示被种族过滤掉的饰品，但必须标记“当前种族池外”。

### 主动控制是否出现

饰品候选池必须受开局设置控制：

- `EnableTrinkets`
- `ShowProxySafe`
- `ShowDebugOnly`
- `ShowHiddenEffectOnly`
- `ShowDisabled`
- `ActiveTribes`

同时支持手动强制排除：

- 本局不出现某个饰品。
- 本局只从指定饰品集合中出现。

第一版可以先做 UI 不暴露的内部字段，后续再做可视化 ban list。

### 建议新增接口

在 `TrinketCatalog` 或 `MatchService` 增加统一过滤函数：

```csharp
GetEligibleTrinkets(
    TrinketSlotKind slotKind,
    IReadOnlyCollection<Tribe> activeTribes,
    AdvancedMechanicSetupOptions setup)
```

不要让 UI 自己拼过滤逻辑。UI 只传筛选条件，最终可出现池由服务层判断。

### 验收标准

- 开局关闭饰品后，不出现饰品选择。
- Lesser / Greater 能正常显示和替换。
- 替换饰品后，装备状态、UI、效果、日志一致。
- ban 掉某个种族后，只关联该种族的饰品不会进入普通池。
- 中立饰品仍可出现。
- Debug 卡牌库能看到被过滤原因。

## 阶段 4：统一卡牌库选择器

### 目标

为任务、任务奖励、饰品提供同一个可复用的卡牌库选择器。

### 支持类型

- Quest
- Quest Reward
- Lesser Trinket
- Greater Trinket

后续可扩展：

- Minion
- Tavern Spell
- Buddy
- Hero Power

### 筛选能力

基础筛选：

- 类型
- 名称搜索
- CardId 搜索
- 状态
- 实现质量
- 是否普通池
- 是否当前种族池可用

饰品筛选：

- Lesser / Greater
- 关联种族
- `effectFamily`
- `requires`
- `proxyLevel`

任务奖励筛选：

- `effectKind`
- `trigger`
- `powerLevel`
- `offerPoolStatus`

### 卡牌显示

每张卡至少显示：

- 图片
- 名称
- 文本
- 状态标签
- 费用或强度
- 简短备注

点击卡牌后显示详情：

- 完整 notes
- 关联种族
- requires
- effect ids
- 当前为什么可选或不可选

### 验收标准

- 任务奖励替换和饰品替换复用同一选择器。
- 选择器可以显示不可选原因。
- 选择器不会直接绕过服务层规则。
- 所有替换操作最终都通过 `GameCommand` 或 `MatchService` 明确接口执行。

## 阶段 5：PendingChoice、Discover、延迟选择闭环

### 目标

任何系统进入等待选择状态时，UI 都能明确显示，并能让玩家完成选择。

### 需要覆盖

- 任务选择 `AdvancedMechanicKind.Quest`
- 饰品选择 `AdvancedMechanicKind.Trinket`
- 任务奖励引发的额外选择
- 饰品引发的替换选择
- Discover
- 延迟到下一回合的 Lesser / Greater Trinket choice

### UI 规则

当存在 `PendingChoice`：

- 屏幕上出现统一选择面板。
- 面板标题根据类型变化。
- 选项显示图片、文本、状态、费用。
- 选择后调用 `ChooseMechanicOption`。
- 如果还有后续 pending choice，面板继续显示。

当存在 `DiscoverState`：

- 使用 Discover 面板。
- 选择后调用 `ChooseDiscover`。
- Discover 来源和奖励等级要显示。

当存在延迟选择：

- 当前回合 UI 显示“已安排”提示。
- 到期回合自动弹出选择面板。
- 如果已有 pending choice，则排队或明确提示等待当前选择完成。

### 验收标准

- 不会出现 PendingChoice 已存在但 UI 无入口的情况。
- 不会出现 Discover 已存在但 UI 无入口的情况。
- 连续选择可以顺序完成。
- 选择完成后 UI 状态清空。

## 阶段 6：完成 1/2/3 后再镜像 UI

### 镜像原则

在阶段 1-5 验收前，不做大规模 UI 重写。

验收后，创建一个新的 UI 分支或组件路径，例如：

- `UnityTavernTrainerAdvancedView`
- `UnityTavernAdvancedMechanicsPanel`
- `UnityTavernCardLibraryModal`
- `UnityTavernMechanicChoiceModal`

保留当前 UI 作为稳定回退。

### 为什么不直接另起炉灶

完全重做 UI 会同时引入机制风险和视觉风险。当前最需要确认的是：

- 任务、饰品、任务奖励是否按设置出现。
- 替换是否走正确服务层逻辑。
- 种族 ban 是否影响饰品池。
- PendingChoice / Discover 是否闭环。

这些规则稳定后，再镜像 UI，视觉层才不会反复返工。

### 镜像后目标

镜像 UI 应专门服务训练器：

- 左侧：酒馆和手牌。
- 中间：当前战局。
- 右侧：高级机制控制台。
- 弹窗：统一选择器、Discover、卡牌库、详情。

高级机制控制台显示：

- 本局开关
- 当前任务
- 当前任务奖励
- Lesser / Greater 饰品
- 待处理选择
- 调试替换入口
- 当前过滤策略

## 建议实施顺序

1. 扩展 `MatchSetupOptions`，加入任务、饰品、任务奖励开关。
2. 在开局种族选择页显示高级机制开关。
3. 让 `MatchService` 按开关决定是否生成任务、饰品、任务奖励。
4. 建立统一候选池过滤函数，先覆盖饰品和任务奖励。
5. 接入饰品种族 ban 过滤。
6. 增加任务一键完成命令。
7. 增加任务奖励替换命令。
8. 增加饰品替换命令。
9. 做统一卡牌库选择器。
10. 做 PendingChoice / Discover / 延迟选择的统一 UI 闭环。
11. 阶段验收。
12. 镜像当前 UI，开始高级机制版 UI 重构。

## 风险与注意事项

### 不要让 UI 直接改状态

UI 不应直接改：

- `LesserTrinketId`
- `GreaterTrinketId`
- `ActiveQuestState`
- `RewardCounters`
- `PendingChoice`

UI 应发命令，服务层统一处理状态和副作用。

### 不要把 DebugOnly 当普通内容

DebugOnly、HiddenEffectOnly、Disabled 可以在调试卡牌库出现，但普通候选池默认不能出现。

### 不要让任务奖励显示和实际 active reward 分裂

替换任务奖励时，必须明确是：

- 替换未完成任务绑定奖励；
- 还是替换并激活当前奖励。

不要只改 UI 文案。

### 不要让种族过滤散落在 UI

饰品是否因种族可出现，应由服务层判断。UI 只展示原因。

## 审查重点

请重点审查这些决策：

1. `EnableQuestRewards=false` 时，是自动关闭任务，还是允许无奖励任务？
2. ProxySafe 默认是否应该出现在普通训练模式？
3. HiddenEffectOnly 是否只允许调试卡牌库出现？
4. DebugOnly / Disabled 是否需要二次确认才能选择？
5. 饰品多种族关联时，是否采用“任一关联种族可用即可出现”？
6. 替换已完成任务奖励时，是否允许只替换显示，不激活效果？
7. 卡牌库第一版是否只覆盖 Quest / Quest Reward / Trinket，不扩展 Minion / Spell？

## 第一阶段交付物

第一阶段完成后应包含：

- 开局高级机制开关。
- 任务是否出现控制。
- 饰品是否出现控制。
- 任务奖励是否出现控制。
- 饰品按种族 ban 过滤。
- 任务一键完成。
- 任务奖励替换。
- 饰品替换。
- 状态标签显示。
- PendingChoice / Discover 基础闭环。
- 对应 EditMode 测试或可替代验证记录。

第一阶段验收后，再开始镜像 UI。
