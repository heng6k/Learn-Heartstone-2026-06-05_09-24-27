# 英雄、宝宝、英雄技能接入开发文档

> 目标：把 Firestone 页面和官方 Hearthstone API 能提供的信息整理为本地可运行数据，并在现有 Unity 酒馆训练器中接入英雄、英雄技能、英雄宝宝，以及 5 本酒馆法术 `Unmasked Identity` 的三选一换技能流程。

## 1. 背景和结论

当前项目已经有随从和酒馆法术两套本地数据：

- `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json`
- `Assets/LearnHearthstone/Resources/Data/battlegroundsSpells.json`
- `MinionCatalogLoader` 和 `SpellCatalogLoader` 通过 `Resources.Load<TextAsset>` 加载 JSON。
- `MinionCatalog`、`SpellCatalog`、`MinionFactory` 已经支持把随从和酒馆法术转成卡牌实例。
- `UnityTavernTrainerController` 的卡牌库目前只有两个模式：随从、酒馆法术。
- `LocalPlayerState` 已经有 `HeroId`、`Health`、`Armor`，但 `MatchService.CreateMatch` 当前固定初始化为 `Health = 30`、`Armor = 0`。

已经完成的资源抓取：

- 本地目录：`Assets/LearnHearthstone/Resources/HeroBuddyImages`
- 英雄图片：`heroes/*.jpg`，共 114 张。
- 宝宝图片：`buddies/*.jpg`，共 108 张。
- 清单文件：`Assets/LearnHearthstone/Resources/HeroBuddyImages/manifest.json`
- manifest 当前包含英雄、宝宝、图片路径、Firestone 统计字段。
- 当前 Firestone 映射中有 6 个英雄没有宝宝：`Morchie`、`Mister Clocksworth`、`Murozond, Unbounded`、`Farseer Nobundo`、`Genn, Worgen King`、`Time Twister Chromie`。

核心设计结论：

1. 运行时不依赖外网。所有英雄、宝宝、英雄技能和图片都落本地 JSON 和 `Resources` 图片目录。
2. Blizzard 官方 API 用于校验官方字段、文本、图片、英雄血量和护甲。Firestone/ZeroToHeroes 用于补足英雄统计、英雄到宝宝映射，以及页面当前实际使用的数据。
3. 英雄技能应该作为新的卡牌类型接入卡牌库，不要伪装成酒馆法术。建议新增 `CardKind.HeroPower` 和 `CardKind.HeroBuddy`。
4. 英雄宝宝是随从牌，但不应进入普通酒馆随从池。建议以独立 `HeroBuddyDefinition` 管理，或者转成 `MinionDefinition` 时强制 `InPool = false`、`Tags` 包含 `hero_buddy`。
5. `Unmasked Identity` 已经在当前 `battlegroundsSpells.json` 中存在，数据为 5 本、3 费、英文名 `Unmasked Identity`、cardNumber `100910`、Firestone/官方 cardId `EBG_Spell_037`，但还没有实现施放逻辑。

## 2. 数据来源

| 用途 | 来源 | 当前用途 |
| --- | --- | --- |
| 页面入口 | `https://www.firestoneapp.com/battlegrounds/heroes?time=all-time` | 观察 Firestone 当前英雄榜页面真实请求 |
| 英雄统计 | `https://static.zerotoheroes.com/api/bgs/hero-stats/mmr-100/all-time/overview-from-hourly.gz.json` | 提供 heroCardId、场次、平均排名、选择率等 |
| 卡牌库 | `https://static.zerotoheroes.com/data/cards/cards_enUS.gz.json` | 提供英雄、宝宝、英雄技能、酒馆法术的英文名、文本、dbfId、血量、护甲、攻击、生命等 |
| 图片 | `https://static.zerotoheroes.com/hearthstone/cardart/256x/{cardId}.jpg` | 已用于下载英雄和宝宝图片 |
| Firestone 前端 bundle | `https://www.firestoneapp.com/main.*.js` | 解析 `getBuddy(heroCardId)` 和后续可解析 `getHeroPower(heroCardId)` |
| 官方 API 文档 | `https://develop.battle.net/documentation/hearthstone/game-data-apis` | 官方 Hearthstone Game Data API 入口 |
| 官方 OAuth 文档 | `https://develop.battle.net/documentation/battle-net/oauth-apis` | 获取 Battle.net OAuth token，用于官方 API 离线导入 |

官方 API 注意事项：

- Battle.net 官方 API 需要 OAuth token，不适合运行时直接调用。
- 建议做成编辑器工具或离线导入脚本，生成本地 JSON 后进入 Unity 测试。
- 官方 API 负责校验真实文本、类型、关键字、英雄血量、护甲、图片来源。
- Firestone 数据负责英雄榜和宝宝关系，因为官方 API 不一定直接给出完整的英雄到宝宝映射。

## 3. 本地数据目标

新增或生成以下本地数据：

```text
Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json
Assets/LearnHearthstone/Resources/Data/battlegroundsHeroPowers.json
Assets/LearnHearthstone/Resources/Data/battlegroundsHeroBuddies.json
```

也可以先使用一个合并文件：

```text
Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json
```

推荐首版使用合并文件，结构如下：

```json
{
  "sourcePage": "https://www.firestoneapp.com/battlegrounds/heroes?time=all-time",
  "cardsSource": "https://static.zerotoheroes.com/data/cards/cards_enUS.gz.json",
  "heroStatsSource": "https://static.zerotoheroes.com/api/bgs/hero-stats/mmr-100/all-time/overview-from-hourly.gz.json",
  "generatedAt": "2026-06-15T00:00:00Z",
  "heroes": [
    {
      "heroCardId": "TB_BaconShop_HERO_34",
      "heroDbfId": 59397,
      "name": "Patchwerk",
      "health": 60,
      "armor": 0,
      "imagePath": "HeroBuddyImages/heroes/TB_BaconShop_HERO_34",
      "heroPower": {
        "cardId": "TODO_RESOLVE_FROM_IMPORTER",
        "dbfId": 0,
        "name": "TODO",
        "cost": 0,
        "text": "",
        "imagePath": "HeroBuddyImages/heroPowers/TB_BaconShop_HP_034",
        "primaryCategory": "Health",
        "tags": ["health", "passive"],
        "replacementEligibility": "InitialOnly"
      },
      "buddy": {
        "cardId": "TB_BaconShop_HERO_34_Buddy",
        "dbfId": 0,
        "name": "",
        "tavernTier": 0,
        "attack": 0,
        "health": 0,
        "text": "",
        "imagePath": "HeroBuddyImages/buddies/TB_BaconShop_HERO_34_Buddy"
      },
      "stats": {
        "dataPoints": 0,
        "totalPicked": 0,
        "totalOffered": 0,
        "averagePosition": 0
      }
    }
  ]
}
```

字段要求：

- `heroCardId` 和 `heroPower.cardId` 是逻辑主键，不使用显示名做逻辑判断。
- `health` 是英雄基础血量。Patchwerk 必须是 60。
- `armor` 是当前版本护甲。无数据时使用 0，但导入器必须保留字段。
- `heroPower.replacementEligibility` 用于控制能不能被 `Unmasked Identity` 发现。
- `heroPower.primaryCategory` 用于 UI 主分类。
- `heroPower.tags` 用于二级筛选和后续规则引擎。
- `buddy` 可以为空，但字段必须存在，方便 UI 显示“暂无宝宝映射”。

## 4. 领域模型设计

新增文件建议：

```text
Assets/LearnHearthstone/Runtime/Domain/Models/HeroModels.cs
Assets/LearnHearthstone/Runtime/Domain/Data/HeroCatalog.cs
Assets/LearnHearthstone/Runtime/Adapters/Data/HeroCatalogLoader.cs
```

建议模型：

```csharp
public enum HeroPowerCategory
{
    Economy,
    Buff,
    Combat,
    Minion,
    Discover,
    Health,
    Passive,
    HeroSwap,
    Other
}

public enum HeroPowerReplacementEligibility
{
    DiscoverableAfterStart,
    InitialOnly,
    NonSelectable,
    Disabled
}

public sealed class HeroDefinition
{
    public string HeroCardId;
    public int HeroDbfId;
    public string Name;
    public int Health;
    public int Armor;
    public string ImagePath;
    public HeroPowerDefinition HeroPower;
    public HeroBuddyDefinition Buddy;
}

public sealed class HeroPowerDefinition
{
    public string CardId;
    public int DbfId;
    public string Name;
    public int Cost;
    public string Text;
    public string ImagePath;
    public HeroPowerCategory PrimaryCategory;
    public List<string> Tags = new List<string>();
    public HeroPowerReplacementEligibility ReplacementEligibility;
}

public sealed class HeroBuddyDefinition
{
    public string CardId;
    public int DbfId;
    public string Name;
    public int TavernTier;
    public int Attack;
    public int Health;
    public string Text;
    public string ImagePath;
    public List<Tribe> Tribes = new List<Tribe>();
    public List<Keyword> Keywords = new List<Keyword>();
}
```

`HeroCatalog` 需要支持：

- `AllHeroes`
- `AllHeroPowers`
- `AllBuddies`
- `GetHeroByCardId(string heroCardId)`
- `GetHeroPowerByCardId(string heroPowerCardId)`
- `GetBuddyByCardId(string buddyCardId)`
- `GetDiscoverableHeroPowers(string currentHeroPowerCardId)`
- `GetInitialSelectableHeroes()`

`CardKind` 需要新增：

```csharp
public enum CardKind
{
    Minion,
    TavernSpell,
    Spell,
    Hero,
    HeroPower,
    HeroBuddy
}
```

首版建议把英雄本体继续留在 `HeroCatalog`，不作为可购买或可加入手牌的卡牌。英雄宝宝需要能成为手牌卡或场上卡，因此应保留随从属性：攻击、生命、酒馆等级、随从类型、关键词和文本都要进入 `HeroBuddyDefinition`，在需要加入手牌或战场时再转换成 `MinionInstance`。英雄技能是“当前技能槽”的替换对象，不作为普通手牌卡买卖，也不进入酒馆法术池。

## 5. 图片加载策略

当前 `CardImageProvider` 支持从 `ImagePath` 直接 `Resources.Load`，所以首版不需要移动图片。

推荐保留当前目录：

```text
HeroBuddyImages/heroes/{heroCardId}
HeroBuddyImages/buddies/{buddyCardId}
HeroBuddyImages/heroPowers/{heroPowerCardId}
```

需要补做：

1. 下载英雄技能图片，保存到 `HeroBuddyImages/heroPowers`。
2. `CardImageProvider.CandidatePaths` 增加 `HeroPower` 和 `HeroBuddy` 的默认候选路径。
3. 保持 JSON 中 `imagePath` 无扩展名，符合 Unity `Resources.Load` 约定。

## 6. 英雄技能分类

UI 主分类建议如下。一个技能可以有多个 `tags`，但只能有一个 `primaryCategory`，用于列表主筛选。

| 分类 | 用途 | 判定规则 |
| --- | --- | --- |
| `Economy` | 金币、刷新、升本费用、买卖收益、资源生成 | 文本或规则涉及 gold、refresh、cost、tavern tier、coin |
| `Buff` | 酒馆阶段直接加身材或关键词 | 涉及 give +attack/+health、keyword grant |
| `Combat` | 战斗开始、战斗中召唤、伤害、护盾等 | 涉及 start of combat、during combat、attack trigger |
| `Minion` | 发现、生成、复制、强化随从或特定种族 | 涉及 minion、type、tribe、discover minion |
| `Discover` | 发现牌、选择奖励、抽取资源，但不纯经济 | 涉及 discover、choose one、reward |
| `Health` | 英雄血量、护甲、免伤 | 涉及 health、armor、damage prevention |
| `Passive` | 持续规则改变或被动效果 | 被动技能，且不适合归入以上分类 |
| `HeroSwap` | 换英雄或换技能 | `Unmasked Identity` 相关，或英雄技能替换 |
| `Other` | 未分类或需要人工确认 | 默认兜底 |

替换资格：

| 值 | 含义 |
| --- | --- |
| `DiscoverableAfterStart` | 可以被 `Unmasked Identity` 三选一发现 |
| `InitialOnly` | 只能开局选，不能后续替换 |
| `NonSelectable` | 系统、占位、皮肤、特殊 token，不能被玩家选 |
| `Disabled` | 数据存在，但项目暂不支持逻辑 |

首版规则建议：

- 默认英雄技能先标记 `DiscoverableAfterStart`。
- 明显绑定英雄实体、开局状态或特殊系统的技能标记 `InitialOnly`。
- 临时 token、皮肤技能、伙伴衍生技能标记 `NonSelectable`。
- 项目尚不能模拟的复杂技能标记 `Disabled`，可以在卡牌库展示，但不能加入三选一池。

## 7. `Unmasked Identity` 实现方案

现有数据：

- 英文名：`Unmasked Identity`
- cardId：`EBG_Spell_037`
- dbfId/cardNumber：`100910`
- 费用：3
- 酒馆等级：5
- 文本：`Discover a new Hero Power.`
- 当前状态：数据已在 `battlegroundsSpells.json` 中，但没有 engine 逻辑。

目标行为：

1. 玩家购买并使用 `Unmasked Identity`。
2. 系统从 `HeroCatalog.GetDiscoverableHeroPowers(currentHeroPowerCardId)` 中抽取 3 个候选。
3. 打开三选一发现弹窗。
4. 玩家选择后，将 `State.Player.HeroPowerCardId` 更新为新技能。
5. 新技能立即进入后续回合逻辑。

需要改造的状态：

```csharp
public sealed class LocalPlayerState
{
    public string HeroId;
    public string HeroPowerCardId;
    public int Health;
    public int MaxHealth;
    public int Armor;
}
```

`DiscoverState` 当前 `Options` 是 `List<MinionInstance>`，只能表达随从或法术。需要泛化为卡牌选择：

```csharp
public sealed class CardChoice
{
    public CardKind CardKind;
    public string CardId;
    public string Name;
    public string Text;
    public string ImagePath;
    public List<string> Tags;
}

public sealed class DiscoverState
{
    public string Source;
    public int RewardTier;
    public string TargetInstanceId;
    public int RemainingPicks;
    public List<CardChoice> Options;
}
```

兼容方案：

- 如果改 `DiscoverState` 风险太高，短期可以新增 `HeroPowerDiscoverState`，但长期会造成两个发现弹窗和两套选择逻辑。
- 推荐直接泛化为 `CardChoice`，再让三连发现、甲虫选择、英雄技能三选一都走同一个 UI。

`TavernSpellEngine` 需要新增分支：

```text
if spell.CardNumber == "100910" or spell.CardId == "EBG_Spell_037":
    start hero power discover
    return "Discover a new Hero Power."
```

选择解析：

```text
if discover.Source == "hero-power:unmasked-identity":
    State.Player.HeroPowerCardId = picked.CardId
    AddRecruitLog(Discover, "已更换英雄技能：" + picked.Name)
```

验收标准：

- `Unmasked Identity` 在 5 本酒馆法术池可出现。
- 使用后出现 3 个英雄技能选项。
- 不出现当前英雄技能。
- 不出现 `InitialOnly`、`NonSelectable`、`Disabled` 技能。
- 选择后玩家英雄技能立即改变。
- 发现弹窗可以展示技能图、名称、费用、文本、分类。

## 8. 英雄和宝宝接入卡牌库

当前 UI 入口：

- `UnityTavernTrainerController.BuildCardLibraryHeader`
- `UnityTavernTrainerController.BuildToolsAcquisitionModeRow`
- 目前只显示“随从”和“酒馆法术”。

推荐 UI：

```text
随从 | 酒馆法术 | 英雄技能 | 英雄宝宝
```

英雄技能 tab：

- 数据源：`HeroCatalog.AllHeroPowers`
- 筛选：分类、是否可替换、是否已实现、搜索文本。
- 展示：图片、名称、费用、文本、分类 badge、替换资格 badge。
- 操作：只浏览，不默认加入手牌。调试模式可以加“设为当前技能”按钮。

英雄宝宝 tab：

- 数据源：`HeroCatalog.AllBuddies`
- 筛选：宝宝酒馆等级、种族、对应英雄、是否有映射。
- 展示：图片、名称、攻击、生命、等级、种族、文本、对应英雄。
- 操作：默认只浏览。调试模式可以“加入手牌”或“加入战场”，但必须标记 `PoolSource.Debug`，不回到普通随从池。

不要做的事：

- 不要把宝宝直接加入 `MinionCatalog.GetMinionsForTier` 的普通池。
- 不要让宝宝参与普通刷新。
- 不要让英雄技能作为酒馆法术被买卖。

宝宝发现池与无宝宝英雄说明：

- 当前 Firestone 映射中没有宝宝的英雄必须在数据导入报告和 UI 中显式标记为 `MissingBuddyMapping`，不要静默留空。当前已知缺失：`Morchie`、`Mister Clocksworth`、`Murozond, Unbounded`、`Farseer Nobundo`、`Genn, Worgen King`、`Time Twister Chromie`。
- 对于“出售本随从，发现一个宝宝”的宝宝类效果，普通版本发现 1 个宝宝，金色版本发现 2 个宝宝。
- 宝宝发现池只包含酒馆等级不高于当前玩家酒馆等级的宝宝。
- 特定不可发现宝宝必须从发现池排除。导入器应从 Firestone 的 `NON_DISCOVERABLE_BUDDIES` 类规则或本地手工黑名单生成 `excludedFromBuddyDiscover = true`。
- E.T.C. / 牛头人的发现宝宝逻辑也走同一套宝宝发现池：先按当前酒馆等级过滤，再排除不可发现宝宝，再排除当前规则不允许出现的特殊宝宝。
- 如果当前等级和排除规则导致没有可发现宝宝，逻辑必须给出清晰结果：不打开空发现弹窗，招募日志记录“没有可发现的宝宝”，UI 提示该效果本次没有可用目标。

## 9. 开局英雄选择、血量、护甲

当前问题：

- `MatchService.CreateMatch` 固定玩家 `Health = 30`、`Armor = 0`。
- `HeroId` 没有从选择结果初始化。
- Patchwerk 的官方/Firestone 卡牌数据中 `health = 60`，必须覆盖默认 30。
- 其他英雄需要从本地英雄数据读取 `health` 和 `armor`。

建议改造：

```csharp
public sealed class MatchSetupOptions
{
    public List<Tribe> ActiveTribes = new List<Tribe>();
    public string SelectedHeroCardId;
}
```

`MatchService.CreateWithDefaultCatalog` 改为加载 `HeroCatalog`：

```csharp
return new MatchService(
    MinionCatalogLoader.LoadFromResources(),
    SpellCatalogLoader.LoadFromResources(),
    HeroCatalogLoader.LoadFromResources(),
    seed,
    scenarios,
    setup);
```

初始化规则：

1. 如果 `setup.SelectedHeroCardId` 为空，使用默认英雄，例如 Patchwerk 或 A. F. Kay。
2. 从 `HeroCatalog` 查英雄。
3. `Player.HeroId = hero.HeroCardId`
4. `Player.HeroPowerCardId = hero.HeroPower.CardId`
5. `Player.MaxHealth = hero.Health`
6. `Player.Health = hero.Health`
7. `Player.Armor = hero.Armor`
8. 对手也保留 `HeroId/Health/Armor`，首版可继续默认 30/0。

验收标准：

- 选择 Patchwerk 开局为 60 血。
- 选择 A. F. Kay 等英雄时血量和护甲来自本地数据。
- 没有英雄数据时有明确 fallback，并写 warning log。
- UI 显示当前英雄、技能、血量、护甲。

## 10. 延期范围

以下内容本阶段只写计划，不实现：

| 主题 | 暂不做原因 | 后续准备 |
| --- | --- | --- |
| 时空扭曲 | 需要跨回合状态回滚和历史快照，风险高 | 后续设计 `TimelineState` 和可回放事件日志 |
| 任务 | 涉及任务条件、奖励、进度追踪 | 后续新增 `QuestDefinition`、`QuestProgressState` |
| 小饰品/大饰品 | 需要饰品槽位、发现流程、持续效果 | 后续新增 `TrinketCatalog`、`TrinketState` |
| 暗月宝藏 | 奖励池和发现逻辑复杂 | 后续复用 `CardChoice` 泛化发现系统 |
| 异虫 | 需要种族专属 token 和成长规则 | 后续作为种族扩展包接入 |
| 战舰 | 需要专属资源/召唤或战斗规则 | 后续独立 RFC |
| 阿塔尼斯的 Protoss | 英雄专属体系，不应混入基础英雄技能接入 | 后续以英雄专题实现 |
| 普崔塞德的亡灵 | 英雄专属亡灵构筑或创造逻辑复杂 | 后续以英雄专题实现 |

本阶段只为这些系统预留能力：

- `CardKind` 可扩展。
- `CardChoice` 发现系统可复用。
- `HeroPowerDefinition.Tags` 支持复杂规则标签。
- `MatchState` 保持可加新状态对象。

## 11. 实施里程碑

### 阶段 1：数据固化

目标：

- 生成 `battlegroundsHeroes.json`。
- 补齐英雄技能数据和图片。
- 保留当前 `HeroBuddyImages/manifest.json` 作为导入原始快照。

任务：

1. 新增 `Tools/ImportHeroBuddyData` 脚本。
2. 从 Firestone 页面自动找到当前 `main.*.js`。
3. 解析 `CardIds`、`getBuddy`、`getHeroPower`。
4. 合并 `cards_enUS.gz.json`、英雄统计 JSON、官方 API 校验结果。
5. 输出本地 JSON。
6. 下载缺失英雄技能图片。
7. 生成导入报告：总英雄数、宝宝映射数、技能映射数、缺失项。

验收：

- 本地 JSON 能被 Unity `JsonUtility` 正常解析。
- 图片路径均能 `Resources.Load`。
- 缺失宝宝和缺失技能被显式列出。

### 阶段 2：领域层接入

任务：

1. 新增 `HeroModels.cs`。
2. 新增 `HeroCatalog.cs`。
3. 新增 `HeroCatalogLoader.cs`。
4. `CardKind` 新增 `HeroPower`、`HeroBuddy`。
5. `CardImageProvider` 支持新类型候选路径。
6. `MatchService` 加载 `HeroCatalog`。

验收：

- `HeroCatalogTests` 覆盖读取、按 cardId 查询、宝宝查询、技能查询。
- `HeroCatalog` 至少能加载 114 个英雄、108 个宝宝。
- Patchwerk 的 `Health` 为 60。

### 阶段 3：UI 卡牌库接入

任务：

1. `UnityTavernTrainerController` 添加“英雄技能”和“英雄宝宝”标签。
2. 卡牌库列表支持 `HeroPowerDefinition` 和 `HeroBuddyDefinition`。
3. 英雄技能分类筛选。
4. 宝宝按英雄、等级、种族筛选。
5. 详情面板展示 hero/buddy/power 的来源和标签。

验收：

- 卡牌库四个标签均可切换。
- 英雄技能能按分类筛选。
- 英雄宝宝能展示对应英雄。
- 普通酒馆刷新不出现宝宝。

### 阶段 4：开局英雄选择和初始生命护甲

任务：

1. `MatchSetupOptions` 新增 `SelectedHeroCardId`。
2. 新增英雄选择 UI 或先做调试下拉框。
3. `CreateMatch` 使用英雄数据初始化血量、护甲、技能。
4. UI 展示当前英雄头像、英雄技能、血量、护甲。

验收：

- Patchwerk 开局 60 血。
- 其他英雄按数据设置血量和护甲。
- 切换英雄后新对局状态正确重建。

### 阶段 5：`Unmasked Identity`

任务：

1. 泛化 `DiscoverState` 为 `CardChoice`。
2. `TavernSpellEngine` 实现 `EBG_Spell_037`。
3. 英雄技能三选一弹窗复用发现 UI。
4. 选择后更新 `State.Player.HeroPowerCardId`。
5. 过滤不可替换技能。

验收：

- 5 本酒馆法术 `Unmasked Identity` 能正常购买和使用。
- 三选一只出现可替换技能。
- 选择后技能更新。
- 招募日志记录换技能事件。

## 12. 测试计划

新增测试文件建议：

```text
Assets/LearnHearthstone/Tests/EditMode/HeroCatalogTests.cs
Assets/LearnHearthstone/Tests/EditMode/HeroBuddyCatalogTests.cs
Assets/LearnHearthstone/Tests/EditMode/HeroPowerCatalogTests.cs
Assets/LearnHearthstone/Tests/EditMode/HeroSetupTests.cs
Assets/LearnHearthstone/Tests/EditMode/UnmaskedIdentityTests.cs
Assets/LearnHearthstone/Tests/EditMode/UnityHeroLibraryTests.cs
```

关键测试：

- JSON 可加载。
- 所有 `heroCardId` 唯一。
- 所有 `heroPower.cardId` 唯一或被显式允许重复。
- 所有 `buddy.cardId` 唯一。
- 图片路径不带扩展名，且资源存在。
- Patchwerk 初始血量 60。
- `Unmasked Identity` 不发现当前技能。
- `Unmasked Identity` 不发现 `InitialOnly`、`NonSelectable`、`Disabled`。
- 英雄宝宝不进入普通随从池。
- 英雄技能不进入酒馆法术池。

回归测试：

- `MinionCatalogTests`
- `SpellCatalogTests`
- `TavernSpellEngineTests`
- `MatchServiceSpellTests`
- `UnityTavernTrainerViewTests`
- `OfficialSoloTavernSpellCoverageTests`

## 13. 风险和处理

| 风险 | 影响 | 处理 |
| --- | --- | --- |
| Firestone bundle 是前端实现，不是稳定 API | 映射解析可能随版本改变 | 导入脚本每次输出解析报告，失败时保留旧 JSON |
| 官方 API 需要 token | 自动导入需要配置凭据 | 只在离线工具中使用，运行时不依赖 |
| 英雄技能逻辑复杂 | 数据能展示但效果未实现 | 数据层先完整接入，效果按标签逐步实现 |
| 宝宝不属于普通池 | 错放进刷新池会破坏玩法 | 强制 `InPool = false`，单独 `HeroBuddy` tab |
| `DiscoverState` 泛化影响现有发现 | 可能破坏三连和特殊选择 | 先加测试覆盖三连发现、甲虫选择、英雄技能选择 |
| 当前 spell JSON 存在历史来源和文本编码问题 | PowerShell 严格 JSON 解析可能失败 | 新导入器输出干净 UTF-8 JSON，旧数据逐步替换 |

## 14. 推荐开发顺序

最稳的顺序：

1. 先做数据导入和 `HeroCatalog`。
2. 再做 UI 浏览：英雄技能、英雄宝宝只展示。
3. 再做开局英雄选择和血量护甲。
4. 最后做 `Unmasked Identity` 的三选一换技能。

这样能保证每一步都可单独验收，且不会一开始就碰复杂的技能效果系统。

## 15. Sources

- Firestone heroes page: https://www.firestoneapp.com/battlegrounds/heroes?time=all-time
- Firestone hero stats JSON: https://static.zerotoheroes.com/api/bgs/hero-stats/mmr-100/all-time/overview-from-hourly.gz.json
- ZeroToHeroes card data: https://static.zerotoheroes.com/data/cards/cards_enUS.gz.json
- ZeroToHeroes image pattern: https://static.zerotoheroes.com/hearthstone/cardart/256x/{cardId}.jpg
- Battle.net Hearthstone Game Data APIs: https://develop.battle.net/documentation/hearthstone/game-data-apis
- Battle.net OAuth APIs: https://develop.battle.net/documentation/battle-net/oauth-apis
