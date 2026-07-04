# 暗月奖品剩余问题与修改建议

更新日期：2026-07-03

## 结论

暗月奖品的共享发现链路已经完成，P0/P1/P2 奖品卡牌自身效果也已经补齐。当前剩余边界主要是图片来源和后续回归覆盖，而不是 Tickatus、饰品、时空酒馆或畸变的触发入口。

当前本地目录共有 33 张 `BGS_Treasures_*` 暗月奖品：

| 范围 | 数量 | 状态 |
| --- | ---: | --- |
| 已有共享目录/生成/发现链路 | 33 | 已完成 |
| 已实现可打出效果 | 33 | `Implemented` |
| 仍为代理效果 | 0 | `Proxy` |
| 有当前官方图片 | 2 | `BGS_Treasures_034`、`BGS_Treasures_016` |
| 已保存 IYingdi 历史图片 | 26 | 官方缺图的旧版奖品已补本地资源 |
| 使用统一 fallback 图片 | 5 | IYingdi/CDN 未提供的新奖品图片 |

当前不建议再新建暗月奖品系统。最小修改路线是继续扩展现有 `darkmoonPrizes.json`、`DarkmoonPrizeEngine` 和 `TavernSpellEngine`。

## 当前已完成

| 项目 | 状态 | 说明 |
| --- | --- | --- |
| 共享目录 | 已完成 | `DarkmoonPrizeCatalogLoader` 读取 `Resources/Data/darkmoonPrizes.json`。 |
| 共享生成 | 已完成 | `DarkmoonPrizeEngine.CreatePrizeCard(...)` 统一创建 `CardKind.Spell` 奖品卡。 |
| 共享发现 | 已完成 | `StartDarkmoonPrizeDiscover(...)` 从目录按等级抽 3 张。 |
| Tickatus | 已完成 | 每 4 回合发现当前等级奖品，奖品效果均走共享实现。 |
| Ticket Collector | 已完成 | 出售后发现下一等级奖品，奖品效果均走共享实现。 |
| Tickatus Sticker | 已完成 | 装备时发现 Tier 3 奖品，每 3 回合重复。Tier 3 奖品当前全部 `Implemented`。 |
| Timewarped Big Winner | 已完成 | 发现 Tier 3 暗月奖品并每 3 回合重复。 |
| 暗月奖品畸变 | 已完成 | Darkmoon Faire Prizes / Up-Prizing 走共享发现路径，奖品不再带 proxy 标记。 |

## 剩余问题

### 1. 24 张奖品 proxy 已清零

这些卡现在能被发现、能进入手牌、能被打出，并且都有 `TavernSpellEngine` / `MatchService` 可执行分支。下面保留原 P0/P1/P2 清单作为完成记录。

| 等级 | 卡牌 | 当前问题 | 修改建议 |
| --- | --- | --- | --- |
| 1 | `BGS_Treasures_004` Gacha Gift | Tier 1 随从发现未实现 | 复用现有 Tier Discover，候选限制为 Tier 1 当前池。 |
| 1 | `BGS_Treasures_007` Might of Stormwind | 全场按酒馆等级加攻击未实现 | 在 `TavernSpellEngine` 增加全友方随从 Attack buff 分支。 |
| 1 | `BGS_Treasures_013` The Good Stuff | 本局酒馆随从 +1/+1 未实现 | 复用现有“未来商店 buff”状态；刷新/补牌时应用。 |
| 1 | `BGS_Treasures_029` Rocking and Rolling | 每回合免费刷新未实现 | 增加持久计数器，回合开始增加免费刷新次数。 |
| 1 | `BGS_Treasures_033` New Recruit | 酒馆额外随从与 +2/+2 未实现 | 复用 Timewarped New Recruit 的商店大小和未来商店 buff 逻辑。 |
| 1 | `BGS_Treasures_040` Banana Bunch | 2 张香蕉未实现 | 复用现有 Tavern Dish Banana 生成逻辑。 |
| 1 | `BGS_Treasures_100` Unfurled Codex | 随机高费 Tavern spell 未实现 | 从 `SpellCatalog` 选 cost >= 2 的 Tavern spell 加手牌。 |
| 1 | `BGS_Treasures_110` Crystallization | 本局 Tavern spell 额外 +1/+1 未实现 | 复用现有 Tavern spell buff bonus 状态。 |
| 2 | `BGS_Treasures_006` Evolving Tavern | 酒馆升一级替换未实现 | 复用 Timewarped Evolving Tavern 的刷新到更高等级逻辑。 |
| 2 | `BGS_Treasures_009` Gruul Rules | 目标获得回合结束 +4/+4 未实现 | 给目标写入持久 enchantment/tag，回合结束统一触发。 |
| 2 | `BGS_Treasures_010` Time Thief | 上个对手战队发现未实现 | 复用 `OpponentHistoryState.LastPlayerWarband`。 |
| 2 | `BGS_Treasures_012` On the House | 当前酒馆等级随从发现未实现 | 复用当前 Tier Discover。 |
| 2 | `BGS_Treasures_014` The Unlimited Coin | 本回合 1 金币并回手未实现 | 加金币后记录 end-turn 回手状态，回合结束复制回手牌。 |
| 2 | `BGS_Treasures_018` Rat in a Cage | +2 Attack 后翻倍攻击未实现 | 复用目标型 buff，按当前 Attack 翻倍。 |
| 2 | `BGS_Treasures_026` The Bouncer | Taunt 后翻倍生命未实现 | 复用关键词添加和 Health 翻倍。 |
| 2 | `BGS_Treasures_030` Big Brann Play | 本回合战吼额外触发未实现 | 复用现有战吼重复/额外触发计数；限定本回合。 |
| 2 | `BGS_Treasures_101` Mageroyal Blossom | 当前等级 Tavern spell 发现未实现 | 复用 Tavern spell discover，候选限制为当前 Tavern Tier。 |
| 4 | `BGS_Treasures_016` Raise the Stakes | 友方随从金色并回手未实现 | 复用 `MakeGolden` 与 `Repeat Customer` 的回手路径。 |
| 4 | `BGS_Treasures_022` Friends and Family Discount | 本局酒馆随从费用变 2 未实现 | 在购买费用计算处加入持久开关。 |
| 4 | `BGS_Treasures_023` Open Bar | 免费刷新 5 次并每回合补 5 次未实现 | 复用免费刷新计数；回合开始补 5。 |
| 4 | `BGS_Treasures_025` Fresh Tab | 获得 12 金币未实现 | 直接复用 Gold 增加逻辑，注意不突破项目现有上限规则。 |
| 4 | `BGS_Treasures_028` Give a Dog a Bone | Divine Shield/Windfury/+15/+15 未实现 | 复用目标型 buff 和关键词添加。 |
| 4 | `BGS_Treasures_032` Big Winner! | 依次发现前 3 个等级奖品未实现 | 复用 Discover queue，按 Tier 1/2/3 顺序排队。 |
| 4 | `BGS_Treasures_106` Gorgeous Goblet | 填满随机 Tavern spell 未实现 | 复用 SpellCatalog 随机 Tavern spell 加手牌。 |

### 2. 当前官方数据边界不完整

本地目录使用旧 `BGS_Treasures_*` id，并通过 HearthstoneJSON 映射 dbf id。当前无需改这点，因为项目内部和历史暗月奖品都依赖这些 id。

但需要明确：

- 官方网页 API 当前只返回了 2 张旧奖品：`BGS_Treasures_034` Repeat Customer、`BGS_Treasures_016` Raise the Stakes。
- 其余 31 张未从当前官方 API 找到卡图；其中 26 张旧版奖品已补 IYingdi 本地图片。
- IYingdi 只能作为早期/首发版本参考，不应覆盖官方当前 API；本地映射保留 2 张官方图片优先。
- 仍缺图的新奖品是 `BGS_Treasures_100`、`BGS_Treasures_101`、`BGS_Treasures_104`、`BGS_Treasures_106`、`BGS_Treasures_110`。

修改建议：

1. 保留现有 `BGS_Treasures_*` 内部 id。
2. 每次补效果时同步记录 `dbfId`、`sourcePool`、官方 API 是否返回。
3. 如果后续拿到官方 Battle.net API 凭据，再重新校验当前奖品池；没有凭据前不要把本地 33 张声明为“官方当前完整池”。

### 3. 图片覆盖已提升到 28/33

`DarkmoonPrizeEngine.DefaultImagePath` 已经统一 fallback 到 `CardImages/DarkmoonPrizeFallback`，所以显示不会空白。

剩余问题是视觉准确性：

| 状态 | 数量 | 影响 |
| --- | ---: | --- |
| 官方当前图片 | 2 | 图像准确 |
| IYingdi 历史图片 | 26 | 可显示早期版本卡图，但不是当前官方 API 来源 |
| fallback 图片 | 5 | 新奖品仍显示同一张暗月奖品替代图 |

修改建议：

1. 官方当前图片优先；已有 `BGS_Treasures_034`、`BGS_Treasures_016` 不改为 IYingdi。
2. IYingdi 旧图只用于官方缺图的旧奖品，并保留 `imageUrl` 指向来源。
3. 对 5 张新奖品继续用空 `imagePath` 表示 fallback，等官方或可信图片源补齐。

### 4. Tier 3 虽标为 Implemented，但测试还不够细

Tier 3 的 8 张奖品都有 `TavernSpellEngine` 分支，当前可打出。但 focused 覆盖还不均匀。

已有直接测试：

- `Pocket Change`
- `Buy the Holy Light`
- `B.A.N.A.N.A.S.`
- `Reserve Prices`
- Big Winner / Tickatus / Tickatus Sticker 的发现链路

建议补直接测试：

| 卡牌 | 建议测试点 |
| --- | --- |
| `BGS_Treasures_011` Training Session | 打出后启动 Hero Power Discover，候选不含当前技能。 |
| `BGS_Treasures_020` Top Shelf | 打出后发现更高等级随从，最高到 Tier 7。 |
| `BGS_Treasures_034` Repeat Customer | 非金友方随从回手并获得 +6/+6。 |
| `BGS_Treasures_037` All That Glitters | 酒馆中随机随从变金色。 |
| `BGS_Treasures_039` Mindflayer Goggles | 把酒馆牌加入手牌，然后刷新酒馆。 |

## 推荐修改顺序

### P0：先补低风险、无新状态或少状态的效果

优先实现这些，因为都能复用现有 helper，收益大、风险小：

1. `Fresh Tab`
2. `Banana Bunch`
3. `Gacha Gift`
4. `On the House`
5. `Mageroyal Blossom`
6. `Unfurled Codex`
7. `Might of Stormwind`
8. `Rat in a Cage`
9. `The Bouncer`
10. `Give a Dog a Bone`

每张卡的最小完成标准：

1. `TavernSpellEngine` 增加明确 card id 分支。
2. `darkmoonPrizes.json` 从 `Proxy` 改为 `Implemented`。
3. 加 1 个 focused EditMode 测试。

### P1：补已有类似机制的持久效果

这些需要本局状态，但项目里已有近似路径：

1. `The Good Stuff`
2. `Rocking and Rolling`
3. `New Recruit`
4. `Crystallization`
5. `Evolving Tavern`
6. `Time Thief`
7. `Raise the Stakes`
8. `Gorgeous Goblet`

建议优先复用 Timewarped Tavern 已有逻辑，不要再加一套 Darkmoon-only 状态。

### P2：补需要触发顺序或跨系统约束的效果

这些最容易和战吼、购买费用、回合结束、发现队列互相影响：

1. `Gruul Rules`
2. `The Unlimited Coin`
3. `Big Brann Play`
4. `Friends and Family Discount`
5. `Open Bar`
6. `Big Winner!`

建议先写测试锁定触发顺序，再实现。特别是 `Big Winner!` 应该直接复用 Discover queue，不要写新的多段发现系统。

## 不建议做的事

- 不要为 Tickatus、饰品、畸变、时空酒馆分别维护暗月奖品池。
- 不要在 UI 层硬编码某个英雄或饰品的奖品列表。
- 不要为了图片新增复杂下载或缓存系统；找到可信图片时补资源文件即可。
- Tickatus 已可标成 `Implemented`，因为 1/2/4 级奖品效果已经补齐。

## 验证建议

每完成一批后运行：

1. Unity compile：`Tools/check-unity-compile.ps1`
2. 暗月奖品 focused tests：新增或复用 `DarkmoonPrize...` 测试列表
3. 消费端回归：`DarkmoonPrizeConsumersTests.xml` 覆盖的测试集
4. 注册表回归：`HeroEffectImplementationRegistryTests`
5. `git diff --check`

全部 33 张已完成；Tickatus 已从 `FrameworkFirst` 升为 `Implemented`。
