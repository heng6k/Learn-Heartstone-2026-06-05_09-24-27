# 当前不可替换英雄技能清单

来源：`Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json`

应用记录：带 `（***）` 标记的条目已回写为 `DiscoverableAfterStart`，并已从数据 `tags` 中移除 `initialonly`。下方表格保留复核记录，方便继续人工核对。

当前判定口径：只有 `replacementEligibility = DiscoverableAfterStart` 的英雄技能会进入替换/发现池。下面列出当前不是 `DiscoverableAfterStart` 的英雄技能，供人工复核。

统计：

- 可替换：64
- 开局限定 `InitialOnly`：41
- 未启用 `Disabled`：9
- 不可选择 `NonSelectable`：0
- 当前不可替换合计：50

## 未启用 Disabled

| # | 英雄 | 技能 | CardId | 中文描述 |
|---:|---|---|---|---|
| 1 | Artanis | Warp Gate | BG31_HERO_802p | 对局开始时，从 2 个星灵随从中选择 1 个；在你购买 14 张牌后获得它。（还剩 14 张） |
| 2 | Buttons | Growing Collection | BG32_HERO_002p | 第 8 回合，选择一个大型饰品购买。（还剩 7 回合） |
| 3 | Jim Raynor | Lift Off | BG31_HERO_801p | 开局拥有一艘 2/2 的战列巡航舰。每当酒馆刷新时，向酒馆中加入一个战列巡航舰升级。 |
| 4 | Kerrigan, Queen of Blades | Spawning Pool | BG31_HERO_811p | 解锁 2 阶异虫。每回合法力值消耗减少 1 点。被动：开局拥有一个 2/2 的幼虫。 |
| 5 | Marin the Manager | Fantastic Treasure | BG30_HERO_304p | 第 5 回合，选择一个小型饰品购买。（还剩 4 回合） |
| 6 | Morchie | Warped Conflux | BG34_HERO_004p | 第 5 回合，造访小型时间扭曲。（还剩 4 回合） |
| 7 | Murozond, Unbounded | Alternate Timeline | BG34_HERO_000p | 第 8 回合，造访大型时间扭曲。（还剩 7 回合） |
| 8 | Professor Putricide | Build-An-Undead | BG25_HERO_100p | 制造一个自定义亡灵。（还剩 3 次创造） |
| 9 | Sire Denathrius | Whodunit? | BG24_HERO_100p | 对局开始时，选择两个任务之一。 |

## 开局限定 InitialOnly

| # | 英雄 | 技能 | CardId | 中文描述 |
|---:|---|---|---|---|
| 1 | A. F. Kay | Procrastinate | TB_BaconShop_HP_044 | 跳过你的前两个回合，然后发现一个 3 阶随从和一个 4 阶随从。 |
| 2 | Ambassador Faelin | Expedition Plans | BG22_HERO_201p | 跳过你的第一个回合。发现 6 阶、4 阶和 2 阶随从，并在达到对应酒馆等级时获得它们。 |
| 3 | Aranna Starseeker | Demon Hunter Training | TB_BaconShop_HP_065 | 在 14 个友方随从攻击后，你每回合购买的第一个随从免费。（还剩 14 次） |
| 4 | Bru'kan | Embrace the Elements | BG22_HERO_001p | 选择一个元素。战斗开始时：呼唤该元素。 |   （***）
| 5 | Cariel Roame | Conviction | BG21_HERO_000p | 使 {2} 个随机友方随从获得 +1/+1。被动：每场战斗后，选择一项强化。 |
| 6 | Cookie the Cook | Stir the Pot | BG21_HERO_020p | 将一个随从丢进你的锅里。收集 3 个后，从它们的类型中发现一个随从。（还剩 3 个） |   （***）
| 7 | Death Speaker Blackthorn | Bloodbound | BG20_HERO_103p | 获取 2 枚鲜血宝石。（每回合两次） |
| 8 | Dinotamer Brann | Battle Brand | TB_BaconShop_HP_048 | 在你购买 4 个战吼随从后，获得布莱恩·铜须。（每局一次） |
| 9 | Drek'Thar | Frostwolf Fervor | BG22_HERO_002p | 战斗中如果你有空位，召唤你攻击力最高的随从的一个复制。（第 7 回合解锁） |    （***）
| 10 | Enhance-o Mechano | Enhancification | BG24_HERO_204p | 酒馆刷新后，使其中一个随机随从获得一个随机额外关键词。 |   （***）
| 11 | Exarch Othaar | Arcane Knowledge | BG31_HERO_006p | 你购买的下一个酒馆法术消耗减少 1 点。（第 3 回合解锁） |   （***）
| 12 | Farseer Nobundo | The Galaxy's Lens | BG31_HERO_003p | 获取你上一个施放的酒馆法术的一张复制。每回合，你的下一个英雄技能消耗减少 1 点。 |   （***）
| 13 | Forest Lord Cenarius | Wisdom of Ancients | BG32_HERO_001p | 使你的铸币上限提高 1 点。 |   （***）
| 14 | Fungalmancer Flurgl | Gone Fishing | TB_BaconShop_HP_056 | 在你出售 5 个随从后，获得一个随机鱼人。（还剩 5 个） |   （***）
| 15 | Genn, Worgen King | King of Duality | BG35_HERO_001p | 第 4 回合，发现两个英雄技能来替换此技能。（还剩 3 回合） |
| 16 | Greybough | Sprout It Out! | TB_BaconShop_HP_107 | 使你在战斗中召唤的随从获得 +1/+2 和嘲讽。 |   （***）
| 17 | Guff Runetotem | Natural Balance | BG20_HERO_242p | 在你购买总计 20 个酒馆等级的牌后，获得一个三连奖励。（还剩 20 点） |   （***）
| 18 | Ini Stormcoil | MechGyver | BG22_HERO_200p | 在 9 个友方随从死亡后，获得一个随机机械。 |   （***）
| 19 | Kael'thas Sunstrider | Verdant Spheres | TB_BaconShop_HP_066 | 在你购买 3 个随从后，获得一枚酒馆铸币。 |   （***）
| 20 | Kurtrus Ashfallen | Glaive Ricochet | BG20_HERO_280p5 | 每回合一次，在你购买 3 个随从后，获得其中一个随从的普通复制。（还剩 3 个） |   （***）
| 21 | Loh, the Living Legend | Heroic Inspiration | BG33_HERO_001p_ALT | 在 15 个友方随从攻击后，获得一个三连奖励。（还剩 15 次） |
| 22 | Murloc Holmes | Detective for Hire | BG23_HERO_303p2 | 查看 2 个随从。猜测你的下一个对手上一场战斗拥有哪一个，猜中则获得一枚酒馆铸币。 |   （***）
| 23 | N'Zoth | Avatar of N'Zoth | TB_BaconShop_HP_105 | 开局拥有一条 2/2 的鱼；它会在战斗中获得你所有的亡语。 |
| 24 | Overlord Saurfang | For the Horde! | BG20_HERO_102p | 酒馆中的随从拥有 +1/+{1}。在你购买 4 个随从后提升。（还剩 4 个） |   （***）
| 25 | Ozumat | Tentacular | BG23_HERO_201p | 战斗中如果你有空位，召唤一个 2/2 并具有嘲讽的触手。（在你出售一个随从后获得 +1/+1） |
| 26 | Patchwerk | All Patched Up | TB_BaconShop_HP_035 | 开局额外拥有 30 点生命值。 |
| 27 | Queen Azshara | Azshara's Ambition | BG22_HERO_007p | 当你的战队总攻击力达到 30 时，开始你的纳迦征服。 |   （***）
| 28 | Silas Darkmoon | Come One, Come All! | TB_BaconShop_HP_101 | 暗月奖券会出现在酒馆中！收集 3 张后，发现一个你酒馆等级的随从。 |   （***）
| 29 | Sindragosa | Stay Frosty | TB_BaconShop_HP_014 | 随从消耗为 2。酒馆提供的随从少一个，并会在每回合结束时冻结。 |   （***）
| 30 | Sir Finley Mrrgglton | Adventure! | TB_BaconShop_HP_057 | 对局开始时，发现一个英雄技能。 |
| 31 | Sneed | Pilot the Shredder | BG21_HERO_030p | 开局拥有一个 2/1 的伐木机；它会从你的手牌中召唤一个随从并使其获得圣盾。 |
| 32 | Tae'thelan Bloodwatcher | Reliquary Research | BG28_HERO_800p | 你每购买的第四个酒馆法术消耗为 0。 |   （***）
| 33 | Tamsin Roame | Fragrant Phylactery | BG20_HERO_282p | 战斗开始时：使你攻击力最低的随从获得“亡语：使你的其他随从获得本随从的属性值。” |   （***）
| 34 | Tavish Stormpike | Deadeye | BG22_HERO_000p | 瞄准！战斗开始时：对你的目标造成 {1} 点伤害。（在有空位时立刻发射这个随从） |   （***）
| 35 | The Curator | Menagerist | TB_BaconShop_HP_033 | 开局拥有一个 2/2 的融合怪，具有剧毒和所有随从类型。 |
| 36 | Thorim, Stormlord | Choose Your Champion | BG27_HERO_801p2 | 被动。对局开始时，发现一个 7 阶随从；在你花费 60 枚铸币后获得它。（还剩 60 枚） |
| 37 | Tickatus | Prize Wall | TB_BaconShop_HP_106 | 每 4 回合，发现一个暗月奖品。（还剩 3 回合） |   （***）
| 38 | Time Twister Chromie | Mana Per Minute | BG34_HERO_001p | 使用酒馆法术刷新酒馆。 |   （***）
| 39 | Vanndar Stormpike | Stormpike Strength | BG22_HERO_003p | 战斗中如果你有空位，召唤你生命值最高的随从的一个复制。（第 7 回合解锁） |   （***）
| 40 | Varden Dawngrasp | Twice as Nice | BG22_HERO_004p | 酒馆刷新后，复制其中酒馆等级最高的随从，并将它们都冻结。 |   （***）
| 41 | Zerek, Master Cloner | Cloning Gallery | BG31_HERO_005p | 每局一次，召唤一个友方随从的完整复制。 |   （***）
