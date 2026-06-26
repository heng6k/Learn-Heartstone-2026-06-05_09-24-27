# 开局选英雄与局内换英雄显示方案

## 目标

把当前英雄能力从“卡牌库里的调试项”提升为玩家能自然理解和操作的核心流程：

- 开局进入酒馆前，可以明确选择本局英雄。
- 局内可以看见当前英雄、英雄技能和关键状态。
- 局内换英雄入口清楚，但不挤占酒馆操作区。
- 英雄图标不能太大，保持直观识别即可。
- 第一版复用现有英雄数据、图片加载和换英雄逻辑，不新增英雄、不重做大 UI。

这个方案服务于 `v0.1.x` 后续可玩性提升。它优先解决体验和显示，不试图一次性补齐所有英雄技能精确实现。

## 当前代码现状

### 已有数据和运行时能力

- `Assets/LearnHearthstone/Runtime/Domain/Models/HeroModels.cs`
  - `HeroDefinition` 已包含 `HeroCardId`、`Name`、`Health`、`Armor`、`ImagePath`、`HeroPower`、`Buddy`。
  - `HeroPowerDefinition` 已包含 `Name`、`Cost`、`Text`、`ImagePath`、`PrimaryCategory`、`ReplacementEligibility`。

- `Assets/LearnHearthstone/Runtime/Domain/Data/HeroCatalog.cs`
  - `GetInitialSelectableHeroes()` 已能返回开局可选英雄。
  - `GetDiscoverableHeroPowers(...)` 已能提供可替换英雄技能。

- `Assets/LearnHearthstone/Runtime/Adapters/Images/CardImageProvider.cs`
  - 已支持 `CardKind.Hero`、`CardKind.HeroPower`、`CardKind.HeroBuddy`。
  - 英雄图会走 `HeroBuddyImages/heroes/{cardId}` 候选路径，也支持整图加载。

- `Assets/LearnHearthstone/Runtime/Domain/Models/TavernMatchModels.cs`
  - `MatchSetupOptions` 已有 `SelectedHeroCardId`。

- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
  - 开局会通过 `SelectedHeroCardId` 解析初始英雄。
  - 当前局内 `AddCardToHand(cardId, CardKind.Hero)` 已实际执行换英雄：更新 `HeroId`、`HeroPowerCardId`、生命、护甲并写入日志。

### 现有 UI 问题

- 开局页 `UnityTavernTribeSelectionView` 目前主要处理种族选择和卡池版本，不显示“本局英雄”。
- 局内卡牌库已经有“英雄 / 英雄技能 / 英雄宝宝”标签，但更像调试库，不像玩家自然使用的换英雄入口。
- 局内当前英雄只在文本摘要中弱显示，缺少稳定的小头像和技能提示。
- 如果把英雄完整卡图直接放在常驻界面，会占用酒馆训练器最宝贵的操作空间。

## 设计原则

1. 小图标识别优先，不做大卡面常驻。
2. 开局选英雄和局内换英雄复用同一个弹窗组件。
3. 常驻信息只放“当前是谁”和“技能是什么”，详细信息进入弹窗看。
4. 列表要适合大量英雄，必须支持搜索和轻量筛选。
5. 换英雄是高影响操作，局内需要确认反馈和日志。
6. 第一版不新增复杂动画，不依赖悬停才能完成关键操作。

## 视觉规格

### 图标尺寸

| 场景 | 建议尺寸 | 说明 |
| --- | --- | --- |
| 开局当前英雄条 | `64 x 64` | 足够识别，不抢种族选择区域 |
| 英雄弹窗列表 | `52 x 52` 或 `56 x 56` | 大量英雄滚动时仍清楚 |
| 弹窗左侧预览 | `72 x 72` | 用于确认当前选中英雄 |
| 局内顶部英雄徽章 | `44 x 44` | 常驻显示，不压缩酒馆区 |

### 常驻显示内容

开局当前英雄条：

```text
[头像] 英雄名
       技能名  费用
       生命 / 护甲
       [选择英雄]
```

局内英雄徽章：

```text
[头像] 英雄名
       技能名
```

局内徽章只显示两行文字。英雄技能描述、宝宝、实现状态等信息放到弹窗预览区，避免顶部状态栏变长。

## 开局选英雄流程

### 入口位置

在 `UnityTavernTribeSelectionView` 中，将页面结构调整为：

```text
选择本局设置

[当前英雄条]
[种族选择网格]
[卡池版本条]
[随机5个] [全部10个种族] [进入酒馆]
```

如果小窗口高度不足，可以把“当前英雄条”压缩为 `56` 高：

```text
[头像] 英雄名 / 技能名                         [选择]
```

### 默认英雄

默认选择规则沿用 `MatchService.ResolveInitialHero()` 的现有倾向：

1. 优先选择 `Patchwerk`。
2. 找不到时选择 `HeroCatalog.GetInitialSelectableHeroes().FirstOrDefault()`。
3. 仍找不到时显示 `未设置英雄`，进入酒馆按钮保持可用，由 `MatchService` 兜底。

开局页需要维护 `selectedHeroCardId`，进入酒馆时写入：

```csharp
new MatchSetupOptions
{
    ActiveTribes = ...,
    SelectedHeroCardId = selectedHeroCardId,
    CardPoolVersionId = ...,
    ...
}
```

## 英雄选择弹窗

### 复用组件

新增一个可复用组件：

```text
Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityHeroSelectionModalComponent.cs
```

组件输入：

- `HeroCatalog heroCatalog`
- `string currentHeroCardId`
- `bool inMatch`
- `Action<HeroDefinition> onHeroSelected`
- `Action onClose`

组件不直接改 `MatchService`，只负责 UI 和回调。开局页和局内控制器分别决定选择后的行为。

### 弹窗布局

```text
┌────────────────────────────────────────────────────────────┐
│ 选择英雄                         [搜索英雄...] [关闭]       │
├───────────────┬────────────────────────────────────────────┤
│ 当前预览       │ 英雄网格                                   │
│ [72头像]      │ [52头像] 英雄名 / 技能短名                  │
│ 英雄名         │ [52头像] 英雄名 / 技能短名                  │
│ 生命/护甲      │ [52头像] 英雄名 / 技能短名                  │
│ 技能名 费用    │ ...                                        │
│ 技能描述       │                                            │
│ 实现状态       │                                            │
│ [选择此英雄]   │                                            │
└───────────────┴────────────────────────────────────────────┘
```

小窗口下改为上下布局：

```text
[搜索 / 关闭]
[当前预览压缩条]
[英雄滚动列表]
```

### 筛选

第一版建议支持三类筛选：

- 搜索：英雄名、技能名、`HeroCardId`。
- 实现状态：全部、已实现、未完全实现。
- 技能分类：经济、增益、战斗、随从、发现、生命、被动、换技能、其他。

实现状态来自 `HeroEffectImplementationRegistry.FindByHeroCardId(hero.HeroCardId)`。

### 列表项内容

每个英雄列表项只显示：

```text
[头像] 英雄名
       技能名 · 费用
```

补充状态使用小标签：

- `已实现`
- `代理`
- `未完成`
- `禁用`

不要把完整技能描述塞进列表项。描述只出现在左侧预览。

## 局内换英雄流程

### 常驻显示位置

在 `UnityTavernTrainerController` 顶部状态区加入英雄徽章：

```text
[头像] 英雄名
       技能名
```

建议固定宽度约 `180-220`，高度随现有顶栏，不新增独立大面板。点击徽章打开英雄选择弹窗。

如果当前顶部空间不足，优先放在“工具/日志/回放”旁边，而不是挤占商店、棋盘、手牌区域。

### 换英雄入口

局内提供两个入口：

1. 点击顶部英雄徽章。
2. 训练工具中新增 `换英雄` 按钮。

两个入口打开同一个英雄选择弹窗，标题显示 `更换英雄`。

### 换英雄行为

局内选择英雄后执行：

```csharp
Apply(new GameCommand(GameCommandType.AddCardToHand, hero.HeroCardId, CardKind.Hero));
```

现有 `MatchService.AddCardToHand(..., CardKind.Hero)` 已负责：

- 更新 `State.Player.HeroId`
- 更新 `State.Player.HeroPowerCardId`
- 更新生命、最大生命、护甲
- 写入招募日志

UI 层只需要：

- 关闭弹窗。
- 重建界面。
- 显示 toast：`已更换为 {hero.Name}`。

### 风险提示

换英雄会重置生命、最大生命和护甲。第一版不弹确认框，但需要在预览区写明：

```text
局内更换会刷新英雄、技能、生命和护甲。
```

如果后续用户反馈误点严重，再加确认弹窗。

## 英雄技能显示

第一版不单独做“换技能”主入口，但英雄选择弹窗预览区需要显示：

- 技能名
- 技能费用
- 技能描述
- 技能分类
- 替换资格

局内卡牌库原有“英雄技能”标签可以保留为高级调试入口，不作为普通玩家主要路径。

## 组件命名建议

新增或补充命名：

- `UnityHeroSelectionOverlay`
- `UnityHeroSelectionPanel`
- `UnityHeroSelectionSearchInput`
- `UnityHeroSelectionStatusAllButton`
- `UnityHeroSelectionImplementedButton`
- `UnityHeroSelectionIncompleteButton`
- `UnityHeroSelectionCategory{Category}Button`
- `UnityHeroSelectionPreview`
- `UnityHeroSelectionPreviewImage`
- `UnityHeroSelectionConfirmButton`
- `UnityHeroSelectionHeroButton-{HeroCardId}`
- `UnityHeroBadge`
- `UnityHeroBadgeImage`
- `UnityHeroBadgeName`
- `UnityHeroBadgePower`
- `UnityTribeSelectionHeroPanel`
- `UnityTribeSelectionChooseHeroButton`

这些名字方便 EditMode 测试稳定查找。

## 实施步骤

### 第 1 步：开局页英雄选择

1. 在 `UnityTavernTribeSelectionView` 中加载 `HeroCatalog`。
2. 增加 `selectedHeroCardId` 状态。
3. 增加 `BuildHeroSummaryStrip(...)`。
4. 进入酒馆时写入 `MatchSetupOptions.SelectedHeroCardId`。
5. 加测试确认选择英雄后 `MatchSetupOptions.SelectedHeroCardId` 正确传出。

### 第 2 步：复用英雄选择弹窗

1. 新建 `UnityHeroSelectionModalComponent`。
2. 支持搜索、实现状态筛选、技能分类筛选。
3. 支持小头像列表和左侧预览。
4. 使用 `CardImageProvider.LoadSprite(hero.ImagePath, hero.HeroCardId, CardKind.Hero)` 加载图片。
5. 无图时显示 `无图`，不要让布局塌陷。

### 第 3 步：局内英雄徽章

1. 在 `UnityTavernTrainerController` 顶部状态区加入 `BuildHeroBadge(...)`。
2. 徽章点击打开同一个英雄弹窗。
3. 当前英雄和技能通过 `service.State.Player.HeroId`、`HeroPowerCardId` 从 `service.HeroCatalog` 解析。
4. 无英雄时显示 `未设置`。

### 第 4 步：局内换英雄

1. 训练工具增加 `换英雄` 按钮。
2. 弹窗选择英雄后调用现有 `GameCommandType.AddCardToHand` + `CardKind.Hero`。
3. 成功后显示 toast，并在招募日志里保留现有 `Hero set: ...` 记录。

### 第 5 步：测试和小窗口检查

新增 EditMode 覆盖：

- 开局页默认显示当前英雄条。
- 点击选择英雄后，英雄条更新。
- 进入酒馆时 `SelectedHeroCardId` 被传给 `MatchSetupOptions`。
- 局内顶部存在 `UnityHeroBadge`。
- 局内换英雄后 `HeroId` 和 `HeroPowerCardId` 更新。
- 英雄弹窗搜索和分类筛选能减少列表项。
- `994x384` 小窗口下弹窗仍有关闭按钮和确认按钮。

手动冒烟覆盖：

1. 开局打开英雄选择弹窗。
2. 搜索一个英雄并选择。
3. 选择 5 个种族进入酒馆。
4. 检查顶部英雄徽章是否显示正确。
5. 局内点击徽章换英雄。
6. 检查血量、护甲、技能名和日志是否更新。
7. 切到 `994x384`，确认弹窗不溢出。

## 第一版不做

- 不新增英雄数据。
- 不补齐英雄技能精确实现。
- 不做大尺寸英雄卡面常驻展示。
- 不做复杂皮肤、收藏、收藏夹。
- 不做局内换英雄确认弹窗，除非后续测试证明误触频繁。
- 不把英雄选择和卡池版本控制合并成一个大设置面板。

## 验收标准

- 玩家开局前能明确看到当前英雄，并能打开弹窗选择其它英雄。
- 进入酒馆后实际使用所选英雄，而不是仍然默认 Patchwerk。
- 局内能一直看见当前英雄和技能名。
- 局内换英雄后，头像、英雄名、技能名、生命、护甲和日志同步更新。
- 英雄头像尺寸克制，不遮挡卡牌、棋盘、商店和手牌。
- 小窗口下弹窗仍然可关闭、可搜索、可选择。
- 现有卡牌库英雄调试能力不被破坏。

## 推荐执行顺序

建议先做第 1、2、3 步，形成完整可见闭环；再做局内工具入口和筛选增强。

最小可交付版本：

1. 开局当前英雄条。
2. 英雄选择弹窗。
3. `SelectedHeroCardId` 进入 `MatchService`。
4. 局内顶部英雄徽章。

这四项完成后，用户就能感知“我选了谁、我现在是谁”。局内换英雄按钮可以随后接入同一弹窗。
