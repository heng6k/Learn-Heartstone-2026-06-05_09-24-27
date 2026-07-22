# 英文描述残留与金币／铸币上限专项问题分析

## 文档状态

- 日期：2026-07-19。
- 状态：问题分析与实施规格已完成，尚未修改运行时代码。
- 范围：英文模式仍显示中文；金币当前值、铸币上限与超过上限后的奖励金币显示。
- 不在本轮范围：提交、推送、部署、批量翻译落库、Unity 场景或 Prefab 修改。

## 执行摘要

当前两个问题都不是单个 UI 文本或单个数值字段造成的。

英文模式问题由两类根因叠加：

1. 语言选择已经传入 MatchService，但大量运行时界面没有消费 UseEnglish，直接写死中文。
2. 随从与酒馆法术的数据结构没有完整英文描述。即使 UI 正确选择英文，随从仍没有英文 Name/Text，酒馆法术也只有 EnglishName，没有 EnglishText。

金币问题的核心不是“UI 完全不刷新”。Unity 风格界面在每次普通 GameCommand 执行后会 Rebuild，并重新读取 TavernState.Gold。真正的问题是：

1. Gold 与 MaxGold 的语义没有被稳定定义。
2. 金币增加散落在 MatchService、HeroEffectEngine、MechanicEngine、TavernSpellEngine 中。
3. 有的增加路径允许超过 MaxGold，有的被 MaxGold 截断，有的还会把 MaxGold 强行提升到 Gold。
4. HUD 虽显示 Gold/MaxGold，但分母会被不同效果以不同方式污染，无法可靠代表“正常回合开始时的铸币上限”。

建议采用最小正确规则：

> Gold 保持“当前实际可花金币”；MaxGold 明确定义为“正常回合开始时的金币上限／补满值”，最高为 99。正常回合开始先把 Gold 设置为不超过 99 的 MaxGold；之后的下回合奖励、回合开始效果、铸币牌、出售、退款等显式金币收益继续叠加，允许 Gold 超过 99，但不得反向抬高 MaxGold。

例如：

- 无其它奖励时：99 / 99。
- 回合开始后额外获得 2 金币：101 / 99。
- 使用一张酒馆铸币：102 / 99。
- 下一回合没有可保留金币规则与额外奖励：重新回到 99 / 99。

这个“99 是正常补满软上限、实际金币可超过 99”的目标是本项目规则，不等同于资料中酒馆战棋的官方硬上限。

## 一、需求解释与边界

### 1. 英文模式

用户期望切换到英文后，当前主要可玩流程中的以下内容均使用英文：

- 主界面和设置界面。
- Unity 风格酒馆主界面。
- 顶栏、按钮、弹窗、提示、状态标签。
- 随从、英雄、英雄技能、酒馆法术、任务、任务奖励、饰品、畸变、暗月奖品、时空酒馆卡牌。
- 招募日志与用户可见错误信息。

允许保持不变的内容：

- CardId、EffectId、测试场景 Id 等技术标识。
- 官方没有英文名且产品明确允许保留的专有数据；当前没有证据表明主要卡牌属于这种情况。

缺失英文时不应静默回退中文。推荐开发环境显示明确占位：

    [Missing en-US: BGxx_xxx.name]

这样可以及时发现漏翻，而不是让英文模式表面可用、实际夹杂中文。

### 2. 金币与上限

结合用户描述，本文按以下规则解释：

- 正常酒馆回合的基础金币仍遵循项目现有 3、4、5……10 的常规增长。
- 英雄、随从、任务、饰品等“提高铸币上限”效果可以提高正常回合补满值。
- 正常补满值最终不能超过 99。
- 没有其它即时或排队奖励时，即使所有上限增益之和超过 99，回合开始后的实际金币也应是 99。
- 所有明确写着“获得 N 枚金币／铸币”的额外效果，在正常补满之后单独增加，可以使实际金币超过 99。
- “免费购买”“费用降低”“改用生命值支付”不是获得金币，不能伪造 Gold 增量。

### 3. 与实际酒馆战棋资料的差异

Hearthstone Wiki 的 Battlegrounds/Gold 页面说明：

- 基础金币从 3 开始，每回合增加 1，常规增长到 10。
- 普通金币不会在回合之间保留。
- 金币池的硬上限为 100，超过 100 的收益会丢失。

版本注意：该 Wiki 页面当前可访问内容的最新修订时间为 2023-12-13，因此“100 硬上限”适合作为社区规则参考，置信度为中等；实施仍以用户明确指定的 99 软上限规则为准。

Blizzard 32.2 官方补丁说明中的 Forest Lord Cenarius 英雄技能明确写着：

    Increase your maximum Gold by 1.

这说明“当前金币”和“最大金币”在正式规则中也是不同概念。

但是，用户要求的是：

- 正常／显示上限为 99。
- 额外方式可以让实际金币超过 99。

因此实施时必须明确：

> 99 是本项目指定的正常回合软上限，不应在文档、变量命名或测试中伪装成官方酒馆战棋的 100 硬上限。

## 二、问题一：英文模式仍显示中文

## 用户可见现象

切换到英文并进入 Unity 风格酒馆后，仍可看到大量中文，例如：

- “星灯秘法酒馆”。
- “回合”“金币”“酒馆”“生命”“种族”。
- “返回”“冻结”“完整下一回合”“工具”。
- 退出确认、英雄状态、任务状态、饰品状态、对手状态和提示文本。
- 随从名称与描述。
- 酒馆法术描述。

这不是语言切换失败，而是部分系统有英文、部分系统没有英文或没有选择英文。

## 当前语言链路

~~~mermaid
flowchart LR
    A["主界面选择 English"] --> B["LearnHearthstoneBootstrap.useEnglish"]
    B --> C["UnityTavernTribeSelectionView"]
    C --> D["MatchSetupOptions.UseEnglish"]
    D --> E["MatchService.useEnglish"]
    E --> F["支持双语的目录和 Localized(...)"]
    E --> G["UnityTavernTrainerController 中的硬编码中文"]
    E --> H["只有中文字段的随从／法术实例"]
    F --> I["部分界面正确显示英文"]
    G --> J["英文模式仍显示中文"]
    H --> J
~~~

关键证据：

| 文件 | 位置 | 当前行为 | 结论 |
|---|---:|---|---|
| Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTribeSelectionView.cs | 2843-2846 | 创建 MatchSetupOptions 并写入 UseEnglish | 语言标记成功进入开局设置 |
| Runtime/Application/Services/MatchService.cs | 1218 | useEnglish = setup?.UseEnglish ?? false | MatchService 收到语言标记 |
| Runtime/Application/Services/MatchService.cs | 5816-5819 | Localized 根据 useEnglish 返回中／英文 | 局部双语机制可用 |
| Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTrainerController.cs | 429-444 | 顶栏标题、资源标签、返回按钮直接写中文 | 主界面没有使用语言标记 |
| Runtime/Adapters/Data/MinionCatalogLoader.cs | 43-60 | 只读取 raw.name 与 raw.text | 随从没有英文选择入口 |
| Runtime/Domain/Models/MinionModels.cs | 19-31 | MinionDefinition 只有 Name 与 Text | 模型无法同时保存中英文描述 |

## 数据覆盖审计

本地 Resources/Data 审计结果：

| 内容类型 | 总数 | 英文名称 | 英文描述 | 中文名称／本地化 | 中文描述／本地化 | 结论 |
|---|---:|---:|---:|---:|---:|---|
| 随从 | 280 | 0 | 0 | 280 | 280 | 英文模式无法显示英文随从名称和描述 |
| 酒馆法术 | 73 | 73 | 0 | 73 | 73 | 名称可英文，描述必然中文 |
| 英雄 | 117 | 117 | 117 | 独立 zh-CN 记录 | 独立 zh-CN 记录 | 结构基本完整 |
| 饰品 | 330 | 330 | 327 | 330 | 330 | 主体完整，3 条英文基础描述需单独复核 |
| 时空酒馆卡牌 | 158 | 158 | 158 | 158 | 158 | 结构完整 |

英雄本地化记录数大于英雄数，是因为同一文件还包含英雄技能等关联卡牌。

## UI 硬编码规模

按“含 CJK 字符的代码行”做启发式统计：

| 文件 | 含中文行 | 明显受 T／UseEnglish／Localized 保护 | 明显未保护 |
|---|---:|---:|---:|
| UnityTavernTrainerController.cs | 477 | 25 | 452 |
| TavernTrainerView.cs | 120 | 0 | 120 |
| RealisticTavernTrainerView.cs | 101 | 0 | 101 |
| UnityTavernTribeSelectionView.cs | 167 | 165 | 2 |

这些是代码行数量，不等同于待翻译词条数量，但足以证明问题是系统性的。

UnityTavernTribeSelectionView 已经大量使用 T(chinese, english)，说明项目存在可复用经验；主酒馆控制器、Legacy 和 Realistic 界面尚未完成同等级覆盖。

## Root Cause Analysis

**Error**：切换为英文并进入游戏后，界面和卡牌描述仍出现中文。

**Expected**：英文模式下，所有主要用户可见 UI、卡牌名称、卡牌描述、提示和日志均使用英文；缺失翻译应被测试或明确占位暴露。

**Cause**：

1. 语言标记已传递，但 UnityTavernTrainerController 等界面直接硬编码中文，没有调用统一的 UI 文本解析。
2. 随从数据和模型只有中文 Name/Text，没有英文备用字段或独立英文目录。
3. 酒馆法术只有 EnglishName，没有 EnglishText。
4. 各目录采用四种不同本地化结构，缺少统一显示解析入口。
5. 当前测试主要验证 UseEnglish 传播和部分目录，不验证完整界面是否残留中文。

**Fix**：

1. 建立一个轻量、统一的 UI 文本目录，通过稳定 key 解析中英文。
2. 为随从补齐英文 Name/Text；为法术补齐 EnglishText。
3. 卡牌实例或展示层通过 locale 解析显示文本，不把语言切换写进玩法逻辑。
4. 将 Unity 主酒馆的硬编码中文分批迁移，优先覆盖玩家主流程。
5. 新增英文模式可见文本审计与代表性卡牌测试。

**Prevention**：

1. 禁止新增裸写的用户可见字符串；必须通过 UI key 或卡牌本地化解析器。
2. 数据导入测试要求每个受支持语言都有名称和描述。
3. 英文 UI 测试遍历主要 Text 组件，禁止未白名单的 CJK 文本。
4. UI GameObject 使用语言无关的稳定名称，不从显示文本拼接对象名。

## 推荐最小修复架构

### 1. UI 文本

不需要一开始引入大型第三方本地化框架。可先增加项目原生的小型目录，例如：

    UiText.Get(UiTextKey.Gold, locale)
    UiText.Get(UiTextKey.Round, locale)
    UiText.Get(UiTextKey.Back, locale)

要求：

- key 是稳定枚举或常量。
- zh-CN 与 en-US 都必须存在。
- 缺失项在开发环境报错并显示占位。
- GameObject 名称使用 key，不使用翻译后的 label。

ResourcePill 当前使用：

    "UnityResourcePill-" + label

导致测试依赖 “UnityResourcePill-金币”。建议改为分别传入：

    id: "Gold"
    label: UiText.Get(UiTextKey.Gold, locale)

测试查找 “UnityResourcePill-Gold”，显示文字则可为“金币”或“Gold”。

### 2. 卡牌文本

短期最小改动建议保留现有定义对象，增加明确的显示字段：

| 内容 | 建议 |
|---|---|
| MinionDefinition | 增加 EnglishName、EnglishText，保留当前中文 Name、Text，或迁移为统一 LocalizedCardText |
| GoldenMinionDefinition | 同步增加英文金色描述 |
| SpellDefinition | 保留 EnglishName，增加 EnglishText |
| Hero／Quest／Trinket／Anomaly／Darkmoon | 复用现有英文基础目录＋zh-CN overlay |
| Timewarped | 复用现有 Name/Text＋ZhName/ZhText |

展示层统一使用：

    CardTextResolver.DisplayName(definition, locale)
    CardTextResolver.DisplayText(definition, locale)

不要在切换语言时覆盖玩法层使用的字段。项目中已有按 Text 检查 Magnetic、Rally 等行为；将本地化文本直接写回玩法定义可能使语言改变玩法结果。

### 3. 优先级

P0：

- Unity 主酒馆顶栏、行动按钮、返回确认、选中卡牌区、状态标签、提示。
- 随从英文名称和描述。
- 酒馆法术英文描述。
- 金币 HUD 的 Gold/MaxGold 英文标签。

P1：

- 工具面板、对手配置、卡池浏览、任务／饰品详情、日志。
- Legacy 与 Realistic 路线；若产品已不再入口可先下线而不是重复翻译。

P2：

- 编辑器工具、调试提示、测试场景名称和低频诊断信息。

## 英文模式验收标准

### 功能验收

1. 主界面选择 English，进入设置和 Unity 酒馆后 UseEnglish 始终为 true。
2. 顶栏显示 Round、Gold、Tavern、Health、Tribes、Back。
3. 普通随从、金色随从、酒馆法术、英雄技能、任务、饰品各抽查至少 3 张，名称与描述均为英文。
4. 英文模式下主要流程的 Text 组件不包含未白名单 CJK 字符。
5. 缺失英文翻译显示 Missing en-US 占位并产生测试失败，不静默显示中文。
6. 切换语言不会改变 CardId、EffectId、玩法分派、关键词和数值。

### 建议测试

| 测试 | 预期 |
|---|---|
| Setup_EnglishPropagatesToMatch | MatchService.UseEnglish 为 true |
| UnityTopBar_EnglishContainsNoChinese | 顶栏所有标签为英文 |
| MinionCatalog_AllEntriesHaveEnglishNameAndText | 280/280 通过 |
| SpellCatalog_AllEntriesHaveEnglishText | 73/73 通过 |
| EnglishMainFlow_NoUnexpectedCjkText | 主要 UI 文本扫描通过 |
| ResourcePill_UsesStableSemanticObjectName | 查找 UnityResourcePill-Gold，不依赖“金币” |
| LocaleDoesNotChangeMechanicDispatch | 中英文运行相同 CardId 的状态结果一致 |

## 三、问题二：当前金币、铸币上限与实时显示

## 当前状态模型

TavernState 目前只有：

    public int Gold;
    public int MaxGold;
    public int NextTurnBonusGold;

当前主要含义：

- Gold：实际可花金币。
- MaxGold：常规回合补满值，同时也被部分效果当作即时钱包上限或显示分母。
- NextTurnBonusGold：下回合开始时额外加入 Gold。

TavernRules.GetMaxGoldForRound 当前返回：

    min(10, 2 + max(1, round))

对应测试明确要求第 1 回合为 3、后期常规上限为 10。

## 当前回合开始流程

MatchService.CompletePendingTurnStart 当前执行：

1. 计算基础回合金币。
2. 应用英雄的 ModifyTurnMaxGold。
3. 加上饰品 ExtraMaxGold。
4. 读取 NextTurnBonusGold。
5. 设置 Gold = maxGold + bonusGold。
6. 设置 MaxGold = maxGold。
7. 清空 NextTurnBonusGold。
8. 再依次触发饰品、棋盘、英雄、任务、随从、时空和畸变的回合开始效果。

这已经部分体现“正常补满＋额外奖励”的思路，但没有统一约束：

- maxGold 没有 99 软上限。
- 后续金币效果使用不同的加法与截断方式。
- 某些额外收益会错误抬高 MaxGold。

## 当前金币变更分散情况

| 位置 | 示例行为 | 问题 |
|---|---|---|
| MatchService.DebugAddGold | 增加 Gold 后把 MaxGold 提升到 Gold | 临时调试收益污染正常上限 |
| MatchService.GrantQuestGold | Gold 增加到 int.MaxValue | 不受 99 限制，但入口只覆盖任务 |
| MatchService.SellMinion | min(MaxGold, Gold + sellValue) | 出售在上限时完全丢失收益 |
| MatchService.HandleTurnStartedForTierThreeMinions | 加 Gold 后把 MaxGold 提升到 Gold | 回合开始额外金币变成永久上限 |
| MatchService.TimewarpedDevourer | 加 3/6 Gold 后把 MaxGold 提升到 Gold | 即时奖励污染未来回合 |
| HeroEffectEngine | 有 raw +=、int.MaxValue、min(MaxGold, ...) 等多种写法 | 同类效果结果不一致 |
| MechanicEngine | GainGold 直接 += | 绕过统一规则 |
| TavernSpellEngine | 有直接 +=、SaturatingAdd 和 MaxGold += | 法术规则各自实现 |

StatMath.MaxStat 当前是 int.MaxValue，因此“Math.Min(StatMath.MaxStat, ...)”只是整数溢出保护，不是 99 规则。

## 当前 UI 刷新行为

UnityTavernTrainerController.Apply 的顺序是：

1. service.Apply(command)。
2. Rebuild()。
3. BuildTopBar 重新读取 Tavern.Gold 与 Tavern.MaxGold。

所以普通点击命令后的状态会立即重新构建。当前问题更准确地说是：

- HUD 读取的是语义不稳定的 Gold/MaxGold。
- 测试只验证 service.State.Gold 已变化，没有验证 UnityResourceValue.text 已变化。
- 如果未来存在不经过 Controller.Apply 的异步／动画／外部状态变更，HUD 没有专门的金币变更订阅。

## Root Cause Analysis

**Error**：金币 HUD 不能可靠表示当前实际金币和铸币上限；不同奖励对上限与溢出金币的处理不一致。

**Expected**：

- HUD 实时显示当前实际金币。
- HUD 同时显示正常回合开始上限。
- 正常补满值最高 99。
- 明确的额外金币收益可让实际金币超过 99。
- 只增加金币的效果不能修改正常上限。
- 只减费用或免费购买的效果不能修改金币。

**Cause**：

1. Gold 和 MaxGold 没有稳定、不变量化的定义。
2. 所有引擎都可以直接写 Gold/MaxGold。
3. 增益、出售、退款、调试和回合补满使用不同截断规则。
4. HUD 用 MaxGold 作为分母，但 MaxGold 会被即时奖励污染。
5. UI 测试没有断言显示值随状态实时变化。

**Fix**：

1. 明确定义 Gold 为实际余额、MaxGold 为正常回合补满上限。
2. 增加 NormalGoldSoftCap = 99。
3. 所有金币变更通过统一方法，按类型区分正常补满、额外收益、支出、上限增加和费用替代。
4. 正常回合补满受 99 限制；额外收益允许超过 99。
5. 删除“额外收益后 MaxGold = max(MaxGold, Gold)”类型写法。
6. HUD 显示最新 Gold/MaxGold，并增加显示文本回归。

**Prevention**：

1. 禁止业务代码直接 Gold +=、Gold =、MaxGold +=。
2. 增加静态搜索或测试，确保新增效果调用统一经济 API。
3. 为每种金币来源建立跨上限测试。
4. 为 HUD 建立状态与显示文本一致性测试。

## 推荐金币不变量

### 变量语义

为了减少全仓重命名风险，短期可保留 Gold 和 MaxGold 字段名，但必须统一语义：

| 字段／概念 | 定义 |
|---|---|
| Gold | 当前实际可花金币；可高于 99 |
| MaxGold | 正常回合开始时的补满上限；范围 0..99 |
| BaseRoundGold | TavernRules 的基础 3..10 |
| PersistentMaxGoldBonus | 英雄、饰品、随从等持久上限增益的统一总和 |
| NextTurnBonusGold | 下回合正常补满后额外加入的金币 |
| NormalGoldSoftCap | 常量 99 |

长期如果允许结构重命名，MaxGold 更准确的名字是 TurnStartGoldLimit 或 NormalRefillGold。

### 核心公式

正常上限：

    MaxGold = min(99, BaseRoundGold + PersistentMaxGoldBonus)

回合开始：

    Gold = MaxGold
    Gold += NextTurnBonusGold
    Gold += TurnStartTriggeredBonusGold

额外收益：

    Gold += amount

支出：

    Gold -= amount

上限增加：

    PersistentMaxGoldBonus += amount
    MaxGold = min(99, BaseRoundGold + PersistentMaxGoldBonus)

注意：

- “上限提高 4，并获得 4 金币”必须分别调用 IncreaseMaxGold(4) 与 GainGold(4)。
- “只获得 4 金币”不能修改 MaxGold。
- “只提高 4 上限”是否立即增加当前 Gold，由卡牌文本决定；默认不增加。
- 金币不能小于 0，支出前必须检查余额或费用替代规则。

## 金币来源分类

| 来源类型 | 示例 | 是否可超过 99 | 是否修改 MaxGold |
|---|---|---:|---:|
| 正常回合补满 | 基础 3..10＋持久上限增益 | 否 | 设置到不超过 99 |
| 排队到下回合的奖励 | 南海卖艺者、谨慎投资 | 是 | 否 |
| 回合开始额外收益 | 手风琴机器人、伙伴／宝宝类经济效果 | 是 | 否 |
| 酒馆铸币／钱袋 | Tavern Coin、3-Gold Coin Pouch | 是 | 否 |
| 任务／饰品即时收益 | 获得 4、8、10、16 金币 | 是 | 否，除非文本同时写提高上限 |
| 出售／退款 | 普通出售、特殊出售、英雄技能退款 | 按用户“其它方式另加”解释为是 | 否 |
| 调试加金币 | DebugAddGold | 是 | 否 |
| 上限提升 | Wisdom of the Ancients、Goblin Wallet | 不直接产生金币 | 是，最高 99 |
| 上限提升＋即时金币 | Bob's Tip Jar、Timewarped Strike Oil | 即时金币部分可超过 | 是，最高 99 |
| 免费购买／费用降低 | Grifter Portrait、免费刷新 | 不适用 | 否 |

如果后续产品决定严格复刻官方“100 硬上限，超过丢失”，必须另立规则；不能把该规则与本文的“99 软上限、额外收益可超过”混合。

## 用户示例映射

### 诈骗犯肖像

本地数据：

- CardId：BG32_MagicItem_957。
- 英文：Grifter Portrait。
- 中文：诈骗犯肖像。
- EffectId：grifter_portrait。
- 效果：获得 Doubloon Grifter；每回合购买的第一个海盗免费。

该效果不应增加 Gold，也不应增加 MaxGold。正确实现是把符合条件的第一次海盗购买费用解析为 0。

### 回合开始经济效果

本地代表：

- BG26_147 手风琴机器人：在回合开始时获得 1 枚铸币。
- BGDUO_118 打劫共犯：回合开始时双方各获得 1 枚铸币。

这些效果应在正常回合补满完成后调用 GainGold，并允许从 99 / 99 变为 100 / 99。

用户所说“挖矿宝宝”未直接匹配到当前本地中文名称，实施前应以实际 CardId 或 EffectId 确认，不应依靠昵称字符串写分支。

## 推荐统一经济 API

为保持最小改动，可先把方法放在 MatchService 或一个小型 GoldRules／GoldEconomy 类中，不必立即建立复杂事件总线。

建议入口：

    SetTurnStartGold(baseRoundGold, persistentMaxBonus)
    GainGold(amount, source, gainKind)
    SpendGold(amount, source)
    IncreaseMaxGold(amount, source)
    SetPurchaseCostOverride(...)

建议 gainKind：

- TurnStartRefill。
- QueuedBonus。
- TriggeredBonus。
- Sale。
- Refund。
- Debug。

SetTurnStartGold 是唯一受 99 限制的余额设置入口。其它 GainGold 默认允许越过 99。

所有入口统一负责：

- 非负校验。
- 整数溢出保护。
- RecruitLog 的 before／after。
- GoldSpent 统计。
- Splinter of Aurum 等金币变化后触发。
- 测试可观察的变更结果。

## HUD 显示规格

推荐继续使用紧凑格式：

    104 / 99

含义固定为：

    当前实际金币 / 正常回合开始上限

要求：

- 当前值不截断。
- 当前值高于上限时仍完整显示。
- 中文 label 为“金币”，英文 label 为“Gold”。
- 可以增加 tooltip：“当前金币 / 正常回合上限”或“Current Gold / Normal turn-start limit”。
- 宽度至少支持 3 位数；如果未来允许更大数值，应使用自适应宽度或缩放。
- GameObject 名称固定为 UnityResourcePill-Gold。

如果希望更清晰，也可显示：

    Gold 104
    Limit 99

但当前 UI 空间较紧，104 / 99 是最小改动方案。

## 金币验收矩阵

| 场景 | 操作前 | 操作 | 预期显示 |
|---|---:|---|---:|
| 正常上限达到 99 | 任意 | 开始新回合，无额外收益 | 99 / 99 |
| 排队奖励越过上限 | 上限 99，NextTurnBonusGold=4 | 开始新回合 | 103 / 99 |
| 回合开始随从奖励 | 99 / 99 | 手风琴机器人触发 +1 | 100 / 99 |
| 酒馆铸币 | 99 / 99 | 使用 Tavern Coin +1 | 100 / 99 |
| 普通出售 | 99 / 99 | 出售普通随从 +1 | 100 / 99 |
| 特殊出售 | 98 / 99 | 出售价值 3 的随从 | 101 / 99 |
| 支出 | 104 / 99 | 购买 3 金币随从 | 101 / 99 |
| 免费海盗 | 99 / 99 | Grifter Portrait 令首个海盗免费 | 99 / 99 |
| 只提高上限 | 90 / 95 | 最大金币 +4 | 90 / 99 |
| 上限＋即时收益 | 99 / 99 | Bob's Tip Jar：+4 上限且 +4 金币 | 103 / 99 |
| 超额不保留 | 104 / 99 | 普通进入下一回合，无保留规则 | 99 / 99 |
| 允许保留异常 | 104 / 99 | 特殊规则明确保留未花金币 | 按异常规则单独计算，并有专项测试 |

## 建议测试

### 规则测试

1. TavernRules 或 GoldRules：正常上限永不超过 99。
2. GainGold：从 99 增加 1 得到 100，MaxGold 保持 99。
3. IncreaseMaxGold：只修改 MaxGold／持久 bonus，不修改 Gold。
4. GainAndIncreaseMaxGold：两个操作都发生，但 MaxGold 仍不超过 99。
5. SellMinion：在 99 时出售仍获得金币。
6. DebugAddGold：不再抬高 MaxGold。
7. 所有现有直接 Gold/MaxGold 赋值入口迁移后结果一致。

### 回合顺序测试

1. 正常补满先执行。
2. NextTurnBonusGold 后执行并可越过 99。
3. 饰品、棋盘、英雄、任务、随从、时空和畸变回合开始金币效果后执行并可越过 99。
4. 普通下一回合重新按 MaxGold 补满，不默认继承超额余额。

### UI 测试

1. 构建 UI 后记录 UnityResourcePill-Gold/UnityResourceValue.text。
2. 执行 DebugAddGold、Tavern Coin、出售和下一回合。
3. 每次断言 UI 文本等于最新 Gold + " / " + MaxGold。
4. 英文模式断言 label 为 Gold，中文模式断言 label 为 金币。
5. 断言 103 / 99 不被显示成 99 / 99、103 / 103 或 99。

## 四、建议修改文件与职责

| 文件 | 建议修改 |
|---|---|
| Domain/Models/TavernMatchModels.cs | 明确 Gold／MaxGold 语义；必要时增加持久 MaxGold bonus 的统一字段 |
| Domain/Engine/TavernRules.cs | 增加 NormalGoldSoftCap=99；提供正常上限计算 |
| Application/Services/MatchService.cs | 建立统一 Gain／Spend／IncreaseMaxGold／TurnStart 入口；迁移直接赋值 |
| Domain/Engine/HeroEffectEngine.cs | 禁止直接写 Gold／MaxGold，调用统一入口或返回明确经济结果 |
| Domain/Engine/MechanicEngine.cs | GainGold 走统一规则 |
| Domain/Engine/TavernSpellEngine.cs | 酒馆铸币、钱袋、上限法术走统一规则 |
| Runtime/Presentation/.../UnityTavernTrainerController.cs | 本地化顶栏与动作；使用稳定 Gold id；显示当前值／正常上限 |
| Runtime/Adapters/Data/MinionCatalogLoader.cs | 读取英文随从名称和描述 |
| Runtime/Adapters/Data/SpellCatalogLoader.cs | 读取英文法术描述 |
| Domain/Models/MinionModels.cs、SpellModels.cs | 支持双语显示字段或统一本地化对象 |
| Resources/Data/battlegroundsMinions.json | 补齐 280 张随从英文名称／描述及金色描述 |
| Resources/Data/battlegroundsSpells.json | 补齐 73 张法术英文描述 |
| Tests/EditMode/UI/UnityTavernTrainerViewTests.cs | 增加英文可见文本和金币显示实时断言；改用稳定对象名 |
| Tests/EditMode/Core/TavernRulesTests.cs | 增加 99 软上限测试 |
| Tests/EditMode/Match、Mechanics、Heroes | 增加所有金币来源跨 99 测试 |

## 五、实施顺序

### 阶段 A：金币规则先统一

1. 定义 Gold、MaxGold、99 软上限不变量。
2. 增加统一经济方法。
3. 迁移所有直接赋值入口。
4. 修复回合开始顺序与持久上限计算。
5. 增加规则测试。

原因：本地化 HUD 时必须先知道分子、分母的稳定语义。

### 阶段 B：金币 HUD

1. ResourcePill 使用稳定 id。
2. 显示 Gold/MaxGold 最新值。
3. 支持 3 位以上数值。
4. 增加命令后显示文本断言。

### 阶段 C：英文主流程

1. 顶栏和核心行动按钮。
2. 选中卡牌、商店、手牌、棋盘和常用提示。
3. 随从英文数据。
4. 法术英文描述。
5. 英文无 CJK 扫描。

### 阶段 D：扩展界面

1. 工具、对手、卡池、详情、日志。
2. Legacy／Realistic 路线按产品是否保留决定翻译或下线。

## 六、风险与注意事项

### 1. 不要把本地化文本当玩法数据

当前部分代码会检查 Text 中是否包含 Magnetic、Rally 等词。翻译结构调整时必须避免语言切换改变玩法判断。长期应依赖 Keyword／EffectId，而不是解析显示文本。

### 2. 不要把所有金币增加都截断到 99

这会破坏用户要求，也会破坏现有任务、饰品、法术和畸变测试中 Gold > MaxGold 的行为。

### 3. 不要让即时金币自动提高 MaxGold

DebugAddGold、回合开始额外金币、吞食奖励等当前存在这种问题。修复后只有明确写“提高铸币上限”的效果才能改变 MaxGold。

### 4. 不要把免费购买实现成先加金币再扣金币

Grifter Portrait 等费用效果应在费用解析阶段变为 0，否则会错误触发“获得金币”“花费金币”和相关计数。

### 5. 测试对象名不能依赖翻译

“UnityResourcePill-金币”切到英文后会失效。稳定 id 与显示 label 必须分离。

### 6. 现有脏工作区

开始实施时应继续保留当前与本任务无关的 Docs 和 PromoVideo 修改，不应覆盖或回滚。

## 七、结论

### 英文问题

置信度：高。

语言标记已经正确传到 MatchService。主要根因是 UI 硬编码中文，以及随从／法术缺失英文描述数据。仅修复一个语言参数或顶栏标签无法解决问题。

### 金币问题

置信度：高。

UI 的普通命令后重建存在，但 Gold/MaxGold 语义和所有金币入口不统一。必须先建立“正常补满最高 99、额外收益允许超过 99”的经济不变量，再修 HUD 和测试。

### 规则差异

置信度：项目目标为高；官方 100 硬上限参考为中等。

外部资料描述的是常规 3→10、金币池硬上限 100；其中 100 来自最新修订为 2023-12-13 的社区 Wiki 页面。Blizzard 2025-04-24 发布的 32.2 官方补丁可高置信确认“当前金币”和“最大金币”是不同概念。本项目要求是 99 正常软上限且额外收益可超过，实施应以用户要求为准，并在代码和测试中明确标注为项目规则。

## Sources

1. Hearthstone Wiki — Battlegrounds/Gold
   https://hearthstone.wiki.gg/wiki/Battlegrounds/Gold
   最新修订：2023-12-13。用于基础 3→10、回合间不保留、金币池硬上限 100 的社区规则参考。

2. Blizzard News — 32.2 Patch Notes
   https://news.blizzard.com/en-us/article/24198086/32-2-patch-notes
   发布日期：2025-04-24。官方补丁说明，包含 “Increase your maximum Gold by 1” 的明确用词。

3. Hearthstone Wiki — Bob's Tip Jar
   https://hearthstone.wiki.gg/wiki/Battlegrounds/Bob%27s_Tip_Jar
   用于“获得金币”和“提高最大金币”同时存在的效果示例。

4. 本地项目代码与数据
   Assets/LearnHearthstone/Runtime
   Assets/LearnHearthstone/Resources/Data
   Assets/LearnHearthstone/Tests
   用于本文所有当前实现、覆盖数量、代码位置与测试结论。
