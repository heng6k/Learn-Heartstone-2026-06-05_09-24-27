# 酒馆战棋畸变机制调研与实现计划

日期：2026-06-28

## 目标

为项目实现酒馆战棋畸变机制建立一份可执行计划。计划覆盖：

- 当前版本 HSReplay 可见畸变池。
- 27.2 赛季 5 初始/历史畸变池的维护方式。
- 官方 Hearthstone Game Data API 能提供的数据边界。
- 伙伴、暗月奖品、第二英雄技能、时空扭曲、Tier 7 等强依赖的门控策略。
- 分批实现顺序、数据模型、运行时挂点、UI/测试要求。

本计划不直接实现畸变代码；它是后续实现和验收的入口文档。

## 调研结论

### 机制事实

官方 27.2 补丁说明把畸变定义为：每局开始随机设置的全局规则，所有玩家在同一局中获得同一个畸变，并且在英雄选择阶段可见。官方还说明畸变出现率不同，部分畸变会导致某些随从、随从类型或英雄不被提供。

因此项目内必须把畸变建模为“对局级规则”，而不是玩家个人奖励。当前单人训练器可以先把状态存到玩家 `TavernState` 下，但数据结构和命名要保留提升到共享 `MatchAnomalyState` 的空间。

### 官方 API 边界

Battle.net 官方 Hearthstone Game Data API 提供：

- `/hearthstone/cards`
- `/hearthstone/cards/:idorslug`
- `/hearthstone/metadata`
- `/hearthstone/metadata/:type`
- `gameMode=battlegrounds`
- Battlegrounds 专用 `tier` 搜索参数

未在官方文档中找到“当前畸变轮换池”或“畸变出现率”接口。因此：

- 官方 API 用于卡牌/元数据校验。
- 当前池、初始池、历史池、出现开关、依赖门控都必须维护为本地快照数据。
- 本地快照要记录来源、抓取日期、池版本和人工确认状态。

### 外部资料定位

| 来源 | 用途 | 可信边界 |
| --- | --- | --- |
| Blizzard 官方 27.2 补丁说明 | 机制定义、开局规则、所有玩家相同、英雄选择可见、部分禁用、Tier 7、Oops All 规则 | 机制权威来源 |
| Battle.net Hearthstone Game Data API 文档 | 官方卡牌/元数据 API 边界 | 不提供当前畸变轮换池 |
| HSReplay 当前畸变页面 | 当前版本可见畸变池快照 | 直连会被 Cloudflare 拦截，本次通过文本代理提取页面图片 ID；需要快照日期 |
| IYingdi 酒馆战棋工具 | 中文分类和旧版本人工核对入口 | 页面可访问，能确认畸变、暗月奖品、伙伴、凯瑞甘、吉姆雷诺、阿塔尼斯、时空扭曲等分类；未确认稳定公开 API |
| HearthstoneJSON latest cards | 机器化卡牌 ID/name/text/dbfId 辅助源 | 非官方 API，用于构建本地数据初稿和 diff，不作为轮换池权威 |

## 当前 HSReplay 畸变池快照

来源：`https://hsreplay.net/battlegrounds/anomalies/`，2026-06-28 通过文本代理提取卡牌图片 ID，再用 HearthstoneJSON latest/enUS 解析名称和文本。

| Card ID | 名称 | 文本摘要 | 初始实现分类 |
| --- | --- | --- | --- |
| `BG31_Anomaly_123` | Cosmic Duality | 开局发现第二英雄技能 | 第二英雄技能依赖 |
| `BG27_Anomaly_504` | Secrets of Norgannon | Tier 7 存在，开局 +10 护甲 | Tier 7/护甲 |（其余情况只能到6本注意区分）
| `BG35_Anomaly_005` | Anomalous Timeline | 以 Alternate Timeline 作为第二英雄技能 | 第二英雄技能依赖 |
| `BG32_Anomaly_001` | Greater Pouches | 以 Growing Collection 作为第二英雄技能 | 第二英雄技能依赖 |
| `BG35_Anomaly_007` | Lesser Fortune | 以 Lesser Crystal Ball 作为第二英雄技能 | 第二英雄技能依赖 |
| `BG34_Anomaly_805` | Oathstone's Summoning | 第 7 回合加入 Minor Timewarped，第 10 回合加入 Major Timewarped | 时空扭曲依赖 |
| `BG32_Anomaly_002` | Lesser Pouches | 以 Fantastic Treasure 作为第二英雄技能 | 第二英雄技能依赖 |
| `BG35_Anomaly_004` | Anomalous Conflux | 以 Warped Conflux 作为第二英雄技能 | 第二英雄技能依赖 |
| `BG31_Anomaly_106` | Marin's Treasure Box | 所有英雄是 Marin，并获得 Growing Collection 第二英雄技能 | 英雄替换 + 第二英雄技能 |
| `BG35_Anomaly_002` | Anomalous Cube | 以 Mystery Cube 作为第二英雄技能，第 5 回合解锁 | 第二英雄技能依赖 |
| `BG27_Anomaly_711` | Double Header | 每回合第一次购买牌时额外获得一张复制 | 购买触发 |
| `BG35_Anomaly_001` | Fly the Flag | 每 3 回合获得把普通复制加入自己随从池的法术 | 生成法术 + 个人池修改 |
| `BG35_Anomaly_008` | Greater Fortune | 以 Greater Crystal Ball 作为第二英雄技能 | 第二英雄技能依赖 |
| `BG27_Anomaly_Prizes2` | Darkmoon Faire Prizes | 每 4 回合发现暗月奖品 | 暗月奖品依赖 |
| `BG27_Anomaly_303` | Grapnel of the Titans | 每回合第一个购买的随从免费 | 经济/购买费用 |
| `BG27_Anomaly_580` | Audience's Choice | 每回合一名玩家为所有玩家选择回合末获得的牌 | 多玩家共享选择，后置 |
| `BG27_Anomaly_751` | Perfected Alchemy | 开局获得 Goldenizer | 生成法术 |
| `BG35_Anomaly_006` | Anomalous Expedition | 开局发现 Tier 6/4/2 随从，在对应等级获得 | 延迟奖励 |
| `BG31_Anomaly_124` | Golden Arrow | 每 3 回合获得 Golden Arrow | 生成法术 |
| `BG27_Anomaly_301` | False Idols | 两张即可三连，三连给铸币而不是奖励 | 三连规则改写 |
| `BG27_Anomaly_716` | Up-Prizing | 升级酒馆后发现暗月奖品，奖品随时间提升 | 暗月奖品依赖 |
| `BG27_Anomaly_810` | Bring in the Buddies | 伙伴在酒馆中出现 | 伙伴依赖 |
| `BG27_Anomaly_900` | Golganneth's Tempest | 随从 2 金，不能手动刷新，买牌后自动刷新 | 经济/刷新规则 |
| `BG31_Anomaly_120` | Scout's Honor | 开局场上有金色 Patient Scout | 生成随从 |
| `BG27_Anomaly_503` | The Yogg-iseum | 每回合所有玩家转同一个尤格萨隆轮盘 | 尤格萨隆轮盘 + 共享结果 |
| `BG27_Anomaly_572` | Treasure Hoard | 第 5 回合发现金色 Tier 3 随从 | 延迟发现 |
| `BG27_Anomaly_570` | Treasure Hoard | 第 7 回合发现金色 Tier 5 随从 | 延迟发现 |
| `BG27_Anomaly_571` | Treasure Hoard | 第 8 回合发现金色 Tier 6 随从 | 延迟发现 |

当前池实现策略：先全部进入本地 catalog，但默认随机池只开放 `Implemented` 或 `OfferableWithExactProxy` 项。第二英雄技能、暗月奖品、伙伴、多玩家共享选择等未满足依赖时必须显示为 `BlockedByDependency`，不能进入默认随机。

## 历史/初始池策略

HearthstoneJSON latest 本次统计到 `BATTLEGROUND_ANOMALY` 共 111 条，其中 `BG27_Anomaly*` 赛季 5 家族 67 条。官方 27.2 说明还明确：赛季 5 启动时有初始畸变池，之后每周继续加入。

项目内不要把“BG27 全部”误当成“开服第一天全部”。建议拆成四个池版本：

```csharp
public enum AnomalyPoolVersion
{
    CurrentHsReplay,
    Season5Launch,
    Season5AllBg27,
    AllKnown
}
```

推荐数据字段：

```json
{
  "cardId": "BG27_Anomaly_504",
  "name": "Secrets of Norgannon",
  "text": "Tavern Tier 7 exists. Start with 10 extra Armor.",
  "sourcePools": ["CurrentHsReplay", "Season5Launch", "Season5AllBg27", "AllKnown"],
  "sourceUrls": [
    "https://hsreplay.net/battlegrounds/anomalies/",
    "https://hearthstone.blizzard.com/en-us/news/23987537/27-2-patch-notes"
  ],
  "snapshotDate": "2026-06-28",
  "implementationStatus": "Planned",
  "availabilityStatus": "BlockedByDependency",
  "availabilityReasons": ["RequiresTier7Pool"]
}
```

`Season5Launch` 必须人工标注；`Season5AllBg27` 可以先由 ID 前缀和卡表生成；`CurrentHsReplay` 使用当前快照维护。

## 依赖门控规则

### 伙伴相关畸变

用户要求：伙伴模式相关的畸变要有伙伴才会出现。这个要作为 P0 规则。

当前识别出的伙伴相关 ID：

- `BG27_Anomaly_810` Bring in the Buddies
- `BG27_Anomaly_Buddies` Buddies
- `BG33_Anomaly_001` Summoning Pact
- `BG33_Anomaly_002` Spirit of Friendship
- `BG33_Anomaly_003` Third Nature
- `BG33_Anomaly_005` Colorful Camaraderie
- `BG33_Anomaly_008` Partner in Crime
- `BG33_Anomaly_009` Amicable Amendment

开放条件：

- `BuddiesEnabled == true`
- 当前英雄池有完整 buddy card id 映射
- Buddy Button/伙伴费用/发现伙伴/伙伴入池等对应 UI 和命令路径已实现
- 缺失英雄 buddy 时能从英雄候选池排除该英雄，或从畸变候选池排除该畸变

默认随机池规则：只要任一条件不满足，伙伴畸变为 `BlockedByBuddyMode`。

### 暗月奖品相关畸变

相关 ID：

- `BG27_Anomaly_Prizes2` Darkmoon Faire Prizes
- `BG27_Anomaly_716` Up-Prizing
- `BG27_Anomaly_755` A Faire Reward

当前项目已经为 Timewarped Big Winner 补了 Tier 3 Darkmoon Prize 基线，但畸变需要完整共享后端，而不是只为某张牌写分支。

先实现：

- `DarkmoonPrizeCatalog`
- `DarkmoonPrizeEngine`
- 按 Tier 1-4 或官方实际奖品等级划分的本地 JSON
- Discover 入口：按等级、按当前回合、按来源过滤
- 奖品结算入口：可从畸变、英雄、时空扭曲、后续旧机制复用

开放条件：

- 对应奖品 Tier 有完整定义。
- Discover UI 能显示奖品。
- 奖品作为手牌法术或即时奖励的结算路径明确。
- 至少当前池涉及的奖品畸变有 focused tests。

### 第二英雄技能相关畸变

当前池中大量畸变给第二英雄技能：

- `BG31_Anomaly_123`
- `BG35_Anomaly_005`
- `BG32_Anomaly_001`
- `BG35_Anomaly_007`
- `BG32_Anomaly_002`
- `BG35_Anomaly_004`
- `BG31_Anomaly_106`
- `BG35_Anomaly_002`
- `BG35_Anomaly_008`

项目内已有 `ExtraHeroPowerCardIds` 存储目标，但之前审计结论是：使用命令和 UI 仍主要走主英雄技能 `HeroPowerCardId`。因此这些畸变不能先进入默认随机池。

开放条件：

- UI 显示多个英雄技能按钮。
- `UseHeroPower` 命令能指定具体 hero power card id。
- 每个额外英雄技能有费用、锁定回合、使用次数、被动/主动差异处理。
- 额外英雄技能和主英雄技能的日志、冷却、禁用状态分离。

### 时空扭曲相关畸变

当前池 `BG34_Anomaly_805` Oathstone's Summoning 依赖 Timewarped Tavern/Timewarp 候选池：

- 第 7 回合把 Minor Timewarped 随从加入酒馆池。
- 第 10 回合把 Major Timewarped 随从加入酒馆池。

开放条件：

- Timewarped 当前池和历史池开关已清楚。
- 加入酒馆池与普通当前池、历史额外池、非随从 Timewarped 默认投放策略不冲突。
- 有回合门槛测试：6/7、9/10 前后池变化。

### Tier 7 相关畸变

`BG27_Anomaly_504` Secrets of Norgannon 是当前池关键畸变。官方说明：该畸变启用时 Tavern Tier 7 存在，Tier 7 可通过正常升级、三连奖励、Patient Scout 等方式获得；每个可用 Tier 7 随从在池中有 5 张。

开放条件：

- Tavern Tier cap 可提升到 7。
- Tier 7 随从池可被普通酒馆、三连、发现、随机生成复用。
- 当前可用种族过滤后，Tier 7 随从池不会生成禁用种族。
- 开局 +10 护甲走现有护甲路径。

### 多玩家共享畸变

包括：

- `BG27_Anomaly_580` Audience's Choice
- `BG27_Anomaly_503` The Yogg-iseum
- 旧池里类似 Match Fixing、No Place Like Holmes、Feline Fortune 等共享/跨玩家畸变

单人训练器可以做确定性代理，但默认随机池不能把代理当完整实现。建议状态：

- `DebugOnly`：允许手动指定测试。
- `BlockedByLobbyModel`：不进默认随机。
- 日志必须写明“single-player proxy”。

## 数据模型设计

新增或扩展：

- `Assets/LearnHearthstone/Runtime/Domain/Models/AnomalyModels.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Data/AnomalyCatalog.cs`
- `Assets/LearnHearthstone/Runtime/Adapters/Data/AnomalyCatalogLoader.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/AnomalyEngine.cs`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsAnomalies.json`

建议模型：

```csharp
public enum AnomalyImplementationStatus
{
    Implemented,
    OfferableWithExactProxy,
    FrameworkOnly,
    Planned,
    BlockedByDependency,
    DebugOnly,
    Unsupported
}

public enum AnomalyAvailabilityReason
{
    None,
    RequiresBuddyMode,
    RequiresDarkmoonPrizeBackend,
    RequiresSecondHeroPowerUi,
    RequiresTier7Pool,
    RequiresTimewarpPool,
    RequiresSharedLobbyChoice,
    RequiresYoggWheel,
    RequiresDuos,
    RequiresCombatRewrite,
    RequiresOfficialDataReview
}

public enum AnomalyEffectFamily
{
    Economy,
    ShopRefresh,
    ShopOffer,
    MinionPool,
    TavernTier,
    HeroPower,
    Buddy,
    DarkmoonPrize,
    Timewarp,
    GeneratedSpell,
    GeneratedMinion,
    TripleRule,
    CombatRule,
    SharedLobbyChoice
}

public sealed class AnomalyDefinition
{
    public string Id;
    public string CardId;
    public int DbfId;
    public string Name;
    public string Text;
    public List<AnomalyPoolVersion> SourcePools = new();
    public AnomalyEffectFamily EffectFamily;
    public AnomalyImplementationStatus ImplementationStatus;
    public List<AnomalyAvailabilityReason> AvailabilityReasons = new();
    public List<string> Tags = new();
    public List<string> SourceUrls = new();
    public string SnapshotDate;
    public string Notes;
}
```

运行时状态：

```csharp
public sealed class AnomalyState
{
    public string ActiveAnomalyId;
    public string ActiveCardId;
    public string ActiveName;
    public string ActiveText;
    public AnomalyPoolVersion PoolVersion;
    public Dictionary<string, int> Counters = new();
    public Dictionary<string, string> Flags = new();
    public List<string> AppliedPoolModifiers = new();
    public List<string> BlockedHeroIds = new();
    public List<string> BlockedTribes = new();
}
```

`AdvancedMechanicState` 目前已有 `Trinkets` 和 `Quests`，但没有 `Anomalies`。建议加：

```csharp
public AnomalyState Anomalies = new AnomalyState();
```

如果后续有真正多玩家 lobby 状态，再把 `ActiveAnomalyId` 提升到共享 `MatchState`。

## 选择和开局流程

畸变必须发生在英雄选择前。当前训练器没有完整多人英雄选择阶段时，建议分两层实现：

1. 对局设置阶段先选定畸变。
2. 英雄候选生成时读取畸变要求，过滤不合法英雄/种族/随从池。
3. 玩家进入训练器后，在顶部状态区显示畸变。

设置选项：

```csharp
public sealed class AnomalySetupOptions
{
    public bool EnableAnomalies;
    public bool RandomizeAnomaly;
    public string SelectedAnomalyCardId;
    public AnomalyPoolVersion PoolVersion;
    public bool IncludeDebugOnly;
}
```

随机选择规则：

1. 从目标 `PoolVersion` 取候选。
2. 排除 `Unsupported` 和 `DebugOnly`，除非显式允许。
3. 排除所有依赖未满足的 `BlockedByDependency`。
4. 应用当前设置：伙伴、暗月奖品、第二英雄技能、时空扭曲、Duos、种族可用性。
5. 用 match seed 做确定性随机。

## 运行时挂点

`AnomalyEngine` 要尽量薄，真正的通用能力复用已有 engine/helper：

- 经济：复用金币、最大金币、免费购买、购买费用入口。
- 酒馆刷新：复用 P2-B/P2-C 已整理的供给入口。
- 选择器：复用种族过滤、每类型、友方 Naga 数、Beast/Undead/Demon 计数、All 族归属处理。
- 衍生牌：复用 Tavern Coin、Blood Gem、Tavern Spell、Spellcraft、随机同等级随从、随机种族随从生成入口。
- 暗月奖品：走新的 `DarkmoonPrizeEngine`。
- 伙伴：走 `HeroCatalog`/`HeroEffectEngine`/buddy catalog。
- 第二英雄技能：走扩展后的 hero power UI/command。

建议事件：

```csharp
public enum AnomalyTrigger
{
    MatchSelected,
    BeforeHeroChoices,
    MatchStarted,
    TurnStarted,
    TurnEnded,
    BeforeShopRefresh,
    AfterShopRefresh,
    BeforeBuyCard,
    AfterBuyCard,
    BeforeTavernUpgrade,
    AfterTavernUpgrade,
    BeforeTripleReward,
    AfterTripleReward,
    BeforeCombat,
    AfterCombat
}
```

结算顺序建议：

- 基础规则先初始化。
- 畸变作为对局规则先于个人机制。
- 英雄/伙伴、任务、饰品、时空扭曲作为个人或局部机制后结算。
- 若当前只有单个 `PendingChoice`，畸变触发的选择不得覆盖已有选择，必须排队或阻塞主流程。

## 分批实现计划

### P0：数据、状态、门控

目标：让所有畸变先可见、可审计、不会错误进入默认随机池。

内容：

- 新增 `battlegroundsAnomalies.json`。
- 录入当前 HSReplay 28 个畸变。
- 录入 HearthstoneJSON 全量 111 个畸变的基础 card id/name/text/dbfId。
- 标注 `CurrentHsReplay`、`Season5Launch`、`Season5AllBg27`、`AllKnown`。
- 新增 `AnomalyCatalog`、loader、duplicate id 测试。
- 新增 availability 计算。
- 默认随机池先只允许 `Implemented`。
- 伙伴、暗月奖品、第二英雄技能、Duos、共享选择全部门控。

验收：

- catalog 能加载。
- 当前池数量为 28。
- 全量已知池数量为 111。
- 伙伴关闭时伙伴畸变不进入随机池。
- 暗月奖品后端未完整时奖品畸变不进入随机池。
- 第二英雄技能 UI 未完成时相关畸变不进入随机池。

### P1：框架挂点和 UI 可见性

目标：畸变可在对局设置、状态区、日志和存档中出现。

内容：

- 扩展 `AdvancedMechanicState` 增加 `AnomalyState`。
- 对局设置支持：关闭、指定、随机。
- `AnomalyEngine` 空分发。
- 接入开局、回合开始、回合结束、刷新前后、购买前后、升级前后、三连奖励、战斗前后。
- UI 显示当前畸变名称、文本、状态。
- 手动指定 `DebugOnly` 畸变时日志提示代理/未完整实现。

验收：

- 无畸变默认流程不变。
- 指定畸变可复现。
- 随机畸变按 seed 可复现。
- 畸变和任务/饰品/时空扭曲不会互相覆盖 pending choice。

### P2：当前池低风险畸变

优先做不依赖伙伴、暗月奖品、第二英雄技能、多玩家共享选择的当前池畸变：

- `BG27_Anomaly_303` Grapnel of the Titans：每回合第一个购买随从免费。
- `BG27_Anomaly_711` Double Header：每回合第一次购买牌获得额外复制。
- `BG27_Anomaly_751` Perfected Alchemy：开局给 Goldenizer。
- `BG31_Anomaly_120` Scout's Honor：开局召唤金色 Patient Scout。
- `BG27_Anomaly_572` Treasure Hoard：第 5 回合发现金色 Tier 3。
- `BG27_Anomaly_570` Treasure Hoard：第 7 回合发现金色 Tier 5。
- `BG27_Anomaly_571` Treasure Hoard：第 8 回合发现金色 Tier 6。
- `BG35_Anomaly_006` Anomalous Expedition：开局延迟记录 Tier 6/4/2 奖励。

可并行准备：

- `BG27_Anomaly_504` Secrets of Norgannon：如果 Tier 7 池和升级 cap 已可用，则放入本批；否则放 P3。

验收：

- 每张有 focused EditMode。
- 回合计数、免费购买、额外复制、延迟发现都有边界测试。
- 禁用种族不会被奖励发现绕过。

### P3：经济、刷新、池修改、Tier 7

目标：补齐会改写酒馆规则但不强依赖暗月/伙伴/第二英雄技能的畸变。

内容：

- `BG27_Anomaly_900` Golganneth's Tempest：随从 2 金，禁手动刷新，买牌后自动刷新。
- `BG27_Anomaly_301` False Idols：两张三连，奖励改 Tavern Coin。
- `BG35_Anomaly_001` Fly the Flag：生成加个人随从池复制的法术。
- `BG31_Anomaly_124` Golden Arrow：生成 Golden Arrow。
- `BG34_Anomaly_805` Oathstone's Summoning：第 7/10 回合加入 Timewarped 池。
- `BG27_Anomaly_504` Secrets of Norgannon：Tier 7 存在、护甲、池数量、升级 cap。

验收：

- 刷新保留/额外供给/指定槽替换与畸变修改不冲突。
- 三连规则修改不破坏普通三连、金色随从、奖励队列。
- Timewarped 加池不把历史额外池误加入默认当前池。
- Tier 7 只生成可用种族和可用池版本中的牌。

### P4：第二英雄技能畸变

前置：完成第二英雄技能 UI/命令。

内容：

- `BG31_Anomaly_123` Cosmic Duality。
- `BG35_Anomaly_005` Anomalous Timeline。
- `BG32_Anomaly_001` Greater Pouches。
- `BG35_Anomaly_007` Lesser Fortune。
- `BG32_Anomaly_002` Lesser Pouches。
- `BG35_Anomaly_004` Anomalous Conflux。
- `BG31_Anomaly_106` Marin's Treasure Box。
- `BG35_Anomaly_002` Anomalous Cube。
- `BG35_Anomaly_008` Greater Fortune。

验收：

- UI 能选择主/副英雄技能。
- 主英雄技能不被替换，除非畸变明确要求全英雄替换。
- 被动/主动/解锁回合/费用都能正确显示和使用。

### P5：暗月奖品畸变

前置：完成共享 `DarkmoonPrizeCatalog/Engine`。

内容：

- `BG27_Anomaly_Prizes2` Darkmoon Faire Prizes。
- `BG27_Anomaly_716` Up-Prizing。
- `BG27_Anomaly_755` A Faire Reward。
- 回填 Timewarped Big Winner 继续使用同一后端。
- 后续凯瑞甘、吉姆雷诺、阿塔尼斯、畸变中涉及暗月奖品的效果统一走此后端。

验收：

- 所有奖品 Tier 有数据和状态。
- 奖品 Discover、发牌、即时结算路径统一。
- Big Winner、Up-Prizing、Darkmoon Faire Prizes 不再各自维护奖品池。
- 缺失奖品效果必须在 registry 中可见，不能静默 fallback。

### P6：伙伴畸变

前置：伙伴模式、buddy 映射、Buddy Button、伙伴入池/发现/费用路径完成。

内容：

- `BG27_Anomaly_810` Bring in the Buddies。
- `BG27_Anomaly_Buddies` Buddies。
- `BG33_*` 伙伴按钮/伙伴发现/伙伴费用/伙伴类型相关畸变。

验收：

- 伙伴关闭时这些畸变不出现。
- 伙伴开启但英雄缺 buddy 映射时，能过滤英雄或过滤畸变。
- 伙伴在酒馆池、手牌、按钮奖励中的来源清晰。

### P7：复杂共享/战斗/旧池畸变

内容：

- `BG27_Anomaly_580` Audience's Choice。
- `BG27_Anomaly_503` The Yogg-iseum。
- Match Fixing、No Place Like Holmes、Feline Fortune 等共享/跨玩家畸变。
- Anomalous Twin、Blessed or Blighted、Echoes of Argus 等复杂战斗畸变。
- Duos-only 畸变。

策略：

- 先全部 `DebugOnly` 或 `Unsupported`。
- 单人代理必须写日志和文档。
- 真正进入默认随机池前，需要 lobby/shared choice/combat rewrite 支撑。

## 和现有系统的复用边界

| 领域 | 复用现有能力 | 新增能力 |
| --- | --- | --- |
| 高级机制状态 | `AdvancedMechanicState`、`MechanicChoiceRequest` | `AnomalyState`、pool version、availability reasons |
| 英雄/伙伴 | `HeroCatalog`、`HeroEffectEngine` | buddy-gated anomaly availability |
| 选择器/计数器 | 已整理的种族过滤、每类型、Naga 数、Beast/Undead/Demon 统计、All 族归属 | 畸变统一调用入口 |
| 酒馆供给 | P2-B/P2-C 刷新、额外槽、保留/替换逻辑 | anomaly shop modifier pipeline |
| 时空扭曲 | Timewarped pool/current/historical switch | 畸变按回合把 Timewarped 加入普通池 |
| 暗月奖品 | Big Winner Tier 3 基线 | 完整共享 Darkmoon Prize 后端 |
| 全量测试 | `Tools/run-editmode-bisect.ps1` | 畸变 catalog/availability/effect shard |

## 测试策略

新增测试建议：

- `AnomalyCatalogTests`
- `AnomalyAvailabilityTests`
- `AnomalySetupTests`
- `AnomalyEconomyTests`
- `AnomalyShopTests`
- `AnomalyTripleRuleTests`
- `AnomalyDarkmoonPrizeTests`
- `AnomalyBuddyGateTests`
- `AnomalySecondHeroPowerTests`
- `AnomalyTimewarpIntegrationTests`

必须覆盖：

- 当前池 28 条数量锁定。
- 全量已知池 111 条数量锁定。
- 同一 card id 不重复。
- 默认关闭畸变时旧流程不变。
- 指定畸变和随机畸变可复现。
- 伙伴关闭时伙伴畸变被过滤。
- 暗月奖品后端缺失时奖品畸变被过滤。
- 第二英雄技能 UI 未完成时相关畸变被过滤。
- 手动指定 blocked/debug 畸变时可见但不进入默认随机。
- 畸变选择不覆盖任务、饰品、时空扭曲 pending choice。
- full EditMode 不盲跑；需要全量时用 `Tools/run-editmode-bisect.ps1` 分片/二分定位。

## 推荐下一步

1. 做 P0：新增 `battlegroundsAnomalies.json`，先录入当前 HSReplay 28 条和全量 111 条基础索引。
2. 增加 `AnomalyCatalog`、availability 计算和测试，不实现具体效果。
3. 明确把伙伴、暗月奖品、第二英雄技能相关畸变先挡在默认随机池外。
4. P0 过后再做 P1 框架挂点和 UI 可见性。
5. 之后从 P2 的低风险当前池畸变开始逐张实现。

## Sources

1. HSReplay Battlegrounds Anomalies: https://hsreplay.net/battlegrounds/anomalies/
2. IYingdi Battlegrounds tool: https://www.iyingdi.com/tz/tool/general/battlegrounds
3. Blizzard Hearthstone 27.2 Patch Notes: https://hearthstone.blizzard.com/en-us/news/23987537/27-2-patch-notes
4. Battle.net Hearthstone Game Data APIs: https://develop.battle.net/documentation/hearthstone/game-data-apis
5. Battle.net Hearthstone Game Modes Guide: https://develop.battle.net/documentation/hearthstone/guides/game-modes
6. HearthstoneJSON latest enUS cards: https://api.hearthstonejson.com/v1/latest/enUS/cards.json
