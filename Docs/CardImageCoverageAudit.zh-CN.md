# 卡牌图片覆盖审计（2026-07-11）

## 审计方法

- 以 `CardImageProvider` 的显式路径、类型目录和 `CardImages/{CardId}` 回退顺序为准。
- 数据文件强制按 UTF-8 读取，并按 `CardKind + CardId` 去重。
- “确认缺图”表示当前 Resources 中没有任何可由运行时加载的正式图片。
- 代码动态生成项单独列为“复核候选”，因为其中也包含常量、选项和查找 ID。

## 总结

| 项目 | 数量 |
|---|---:|
| 唯一目录卡牌 | 1403 |
| 有正式图片 | 1255 |
| 确认缺图 | 148 |
| 代码生成复核候选 | 140 |

确认缺图分组：

- `HeroPower`：3 张
- `Minion`：29 张
- `Spell`：111 张
- `TavernSpell`：5 张

已确认基础随从、酒馆法术、英雄、伙伴、任务、任务奖励、饰品和扭曲时空目录没有新的正式图片缺口。当前缺口集中在全部 111 个畸变、29 个双打随从、5 个暗月奖品和 3 个畸变生成英雄技能。

## 确认缺图清单

| 类型 | CardId | 名称 | 来源 | 当前 imagePath |
|---|---|---|---|---|
| HeroPower | `BG35_Anomaly_002t` | Mystery Cube | 英雄技能 | `HeroBuddyImages/heroPowers/BG35_Anomaly_002t` |
| HeroPower | `BG35_Anomaly_007t` | Lesser Crystal Ball | 英雄技能 | `HeroBuddyImages/heroPowers/BG35_Anomaly_007t` |
| HeroPower | `BG35_Anomaly_008t` | Greater Crystal Ball | 英雄技能 | `HeroBuddyImages/heroPowers/BG35_Anomaly_008t` |
| Minion | `BGDUO_100` | 远行者周卓 | 基础随从 | `CardImages/BGDUO_100` |
| Minion | `BGDUO_104` | 热心的沙龙酒保 | 基础随从 | `CardImages/BGDUO_104` |
| Minion | `BGDUO_105` | 宽厚的驼鹿 | 基础随从 | `CardImages/BGDUO_105` |
| Minion | `BGDUO_107` | 护雏的龙希尔 | 基础随从 | `CardImages/BGDUO_107` |
| Minion | `BGDUO_108` | 镜中鬼怪 | 基础随从 | `CardImages/BGDUO_108` |
| Minion | `BGDUO_109` | 增援系统 | 基础随从 | `CardImages/BGDUO_109` |
| Minion | `BGDUO_110` | 活泼的淡水元素 | 基础随从 | `CardImages/BGDUO_110` |
| Minion | `BGDUO_111` | 大方的地卜师 | 基础随从 | `CardImages/BGDUO_111` |
| Minion | `BGDUO_112` | 墓后解说员 | 基础随从 | `CardImages/BGDUO_112` |
| Minion | `BGDUO_114` | 过路旅客 | 基础随从 | `CardImages/BGDUO_114` |
| Minion | `BGDUO_115` | 跳跳娃娃 | 基础随从 | `CardImages/BGDUO_115` |
| Minion | `BGDUO_117` | 滩涂跳跳鱼 | 基础随从 | `CardImages/BGDUO_117` |
| Minion | `BGDUO_118` | 打劫共犯 | 基础随从 | `CardImages/BGDUO_118` |
| Minion | `BGDUO_119` | 兽人指挥 | 基础随从 | `CardImages/BGDUO_119` |
| Minion | `BGDUO_120` | 井边许愿者 | 基础随从 | `CardImages/BGDUO_120` |
| Minion | `BGDUO_121` | 堕落者信使 | 基础随从 | `CardImages/BGDUO_121` |
| Minion | `BGDUO_122` | 风暴分流者 | 基础随从 | `CardImages/BGDUO_122` |
| Minion | `BGDUO_125` | 流沙幻象 | 基础随从 | `CardImages/BGDUO_125` |
| Minion | `BGDUO31_201` | 聚积风暴 | 基础随从 | `CardImages/BGDUO31_201` |
| Minion | `BGDUO31_202` | 忠实的帮凶 | 基础随从 | `CardImages/BGDUO31_202` |
| Minion | `BGDUO31_203` | 狡捷灵蛇 | 基础随从 | `CardImages/BGDUO31_203` |
| Minion | `BGDUO31_205` | 无私的观光客 | 基础随从 | `CardImages/BGDUO31_205` |
| Minion | `BGDUO31_207` | 摩托水手 | 基础随从 | `CardImages/BGDUO31_207` |
| Minion | `BGDUO31_208` | 萨莱因铭文师 | 基础随从 | `CardImages/BGDUO31_208` |
| Minion | `BGDUO31_209` | 私房主厨 | 基础随从 | `CardImages/BGDUO31_209` |
| Minion | `BGDUO31_211` | 转运反应堆 | 基础随从 | `CardImages/BGDUO31_211` |
| Minion | `BGDUO31_212` | 螳螂妖国王 | 基础随从 | `CardImages/BGDUO31_212` |
| Minion | `BGDUO33_140` | 底栖饲育者 | 基础随从 | `CardImages/BGDUO33_140` |
| Minion | `BGDUO33_150` | 黑暗炫魔 | 基础随从 | `CardImages/BGDUO33_150` |
| Spell | `BG27_Anomaly_000` | Money Match | 畸变 | `` |
| Spell | `BG27_Anomaly_001` | Finicky Hourglass | 畸变 | `` |
| Spell | `BG27_Anomaly_002` | Prudence of Amitus | 畸变 | `` |
| Spell | `BG27_Anomaly_005` | Mimiron's Clockwork Stadium | 畸变 | `` |
| Spell | `BG27_Anomaly_006` | Curse of Aggramar | 畸变 | `` |
| Spell | `BG27_Anomaly_100` | Big League | 畸变 | `` |
| Spell | `BG27_Anomaly_101` | What Are the Odds? | 畸变 | `` |
| Spell | `BG27_Anomaly_102` | How to Even?? | 畸变 | `` |
| Spell | `BG27_Anomaly_103` | Tavern Special | 畸变 | `` |
| Spell | `BG27_Anomaly_104t` | Oops, All Beasts! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t10` | Oops, All Pirates! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t2` | Oops, All Demons! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t3` | Oops, All Dragons! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t4` | Oops, All Elementals! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t5` | Oops, All Mechs! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t6` | Oops, All Murlocs! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t7` | Oops, All Naga! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t8` | Oops, All Quilboar! | 畸变 | `` |
| Spell | `BG27_Anomaly_104t9` | Oops, All Undead! | 畸变 | `` |
| Spell | `BG27_Anomaly_301` | False Idols | 畸变 | `` |
| Spell | `BG27_Anomaly_302` | Bring Home the Bacon | 畸变 | `` |
| Spell | `BG27_Anomaly_303` | Grapnel of the Titans | 畸变 | `` |
| Spell | `BG27_Anomaly_307` | Oops, All EVIL! | 畸变 | `` |
| Spell | `BG27_Anomaly_501` | Temperance of Aman'Thul | 畸变 | `` |
| Spell | `BG27_Anomaly_502` | Blood of Sargeras | 畸变 | `` |
| Spell | `BG27_Anomaly_503` | The Yogg-iseum | 畸变 | `` |
| Spell | `BG27_Anomaly_504` | Secrets of Norgannon | 畸变 | `` |
| Spell | `BG27_Anomaly_505` | Reckless Enhancement | 畸变 | `` |
| Spell | `BG27_Anomaly_555` | No Place Like Holmes | 畸变 | `` |
| Spell | `BG27_Anomaly_556` | Valuation Inflation | 畸变 | `` |
| Spell | `BG27_Anomaly_558` | Feline Fortune | 畸变 | `` |
| Spell | `BG27_Anomaly_559` | Treasure Hoard | 畸变 | `` |
| Spell | `BG27_Anomaly_560` | Anomalous Twin | 畸变 | `` |
| Spell | `BG27_Anomaly_561` | Anomalous Bribe | 畸变 | `` |
| Spell | `BG27_Anomaly_562` | Anomalous Wisdomball | 畸变 | `` |
| Spell | `BG27_Anomaly_570` | Treasure Hoard | 畸变 | `` |
| Spell | `BG27_Anomaly_571` | Treasure Hoard | 畸变 | `` |
| Spell | `BG27_Anomaly_572` | Treasure Hoard | 畸变 | `` |
| Spell | `BG27_Anomaly_573` | Treasure Hoard | 畸变 | `` |
| Spell | `BG27_Anomaly_575` | Eleventh Hour | 畸变 | `` |
| Spell | `BG27_Anomaly_577` | No Face, No Case | 畸变 | `` |
| Spell | `BG27_Anomaly_580` | Audience's Choice | 畸变 | `` |
| Spell | `BG27_Anomaly_711` | Double Header | 畸变 | `` |
| Spell | `BG27_Anomaly_714` | Everything's on Fire! | 畸变 | `` |
| Spell | `BG27_Anomaly_715` | Gladiator's Spoils | 畸变 | `` |
| Spell | `BG27_Anomaly_716` | Up-Prizing | 畸变 | `` |
| Spell | `BG27_Anomaly_718` | Overseer's Orb | 畸变 | `` |
| Spell | `BG27_Anomaly_720` | Nguyen's Shifting Disks | 畸变 | `` |
| Spell | `BG27_Anomaly_721` | Uncompensated Upset | 畸变 | `` |
| Spell | `BG27_Anomaly_723` | Summoning of Champions | 畸变 | `` |
| Spell | `BG27_Anomaly_726` | Blessed or Blighted | 畸变 | `` |
| Spell | `BG27_Anomaly_750` | Packed Stands | 畸变 | `` |
| Spell | `BG27_Anomaly_751` | Perfected Alchemy | 畸变 | `` |
| Spell | `BG27_Anomaly_754` | Path of the Treasure-Seeker | 畸变 | `` |
| Spell | `BG27_Anomaly_755` | A Faire Reward | 畸变 | `` |
| Spell | `BG27_Anomaly_800` | Little League | 畸变 | `` |
| Spell | `BG27_Anomaly_801` | The Golden Arena | 畸变 | `` |
| Spell | `BG27_Anomaly_802` | Echoes of Argus | 畸变 | `` |
| Spell | `BG27_Anomaly_803` | Anomalous Evidence | 畸变 | `` |
| Spell | `BG27_Anomaly_805` | Match Fixing | 畸变 | `` |
| Spell | `BG27_Anomaly_810` | Bring in the Buddies | 畸变 | `` |
| Spell | `BG27_Anomaly_820` | Deep Blue Sooner | 畸变 | `` |
| Spell | `BG27_Anomaly_822` | Denathrius' Anima Reserves | 畸变 | `` |
| Spell | `BG27_Anomaly_900` | Golganneth's Tempest | 畸变 | `` |
| Spell | `BG27_Anomaly_Buddies` | Buddies | 畸变 | `` |
| Spell | `BG27_Anomaly_Prizes2` | Darkmoon Faire Prizes | 畸变 | `` |
| Spell | `BG27_Anomaly_Quests` | Quests | 畸变 | `` |
| Spell | `BG31_Anomaly_101` | Lay of the Land | 畸变 | `` |
| Spell | `BG31_Anomaly_102` | Continuing Education | 畸变 | `` |
| Spell | `BG31_Anomaly_104` | Rising Current | 畸变 | `` |
| Spell | `BG31_Anomaly_105` | Sin'dorei Mirror | 畸变 | `` |
| Spell | `BG31_Anomaly_106` | Marin's Treasure Box | 畸变 | `` |
| Spell | `BG31_Anomaly_109` | Mystical Blossom | 畸变 | `` |
| Spell | `BG31_Anomaly_111` | Elven Elite | 畸变 | `` |
| Spell | `BG31_Anomaly_112` | Incubation Mutation | 畸变 | `` |
| Spell | `BG31_Anomaly_114` | Factory Line | 畸变 | `` |
| Spell | `BG31_Anomaly_115` | Magic Shop | 畸变 | `` |
| Spell | `BG31_Anomaly_116` | Light the Way | 畸变 | `` |
| Spell | `BG31_Anomaly_117` | Emergency Landing | 畸变 | `` |
| Spell | `BG31_Anomaly_120` | Scout's Honor | 畸变 | `` |
| Spell | `BG31_Anomaly_123` | Cosmic Duality | 畸变 | `` |
| Spell | `BG31_Anomaly_124` | Golden Arrow | 畸变 | `` |
| Spell | `BG31_Anomaly_126` | Planar Alignment | 畸变 | `` |
| Spell | `BG31_Anomaly_127` | Instant Warband | 畸变 | `` |
| Spell | `BG32_Anomaly_001` | Greater Pouches | 畸变 | `` |
| Spell | `BG32_Anomaly_002` | Lesser Pouches | 畸变 | `` |
| Spell | `BG32_Anomaly_003` | Impressive Foresight | 畸变 | `` |
| Spell | `BG33_Anomaly_001` | Summoning Pact | 畸变 | `` |
| Spell | `BG33_Anomaly_002` | Spirit of Friendship | 畸变 | `` |
| Spell | `BG33_Anomaly_003` | Third Nature | 畸变 | `` |
| Spell | `BG33_Anomaly_005` | Colorful Camaraderie | 畸变 | `` |
| Spell | `BG33_Anomaly_008` | Partner in Crime | 畸变 | `` |
| Spell | `BG33_Anomaly_009` | Amicable Amendment | 畸变 | `` |
| Spell | `BG34_Anomaly_800` | Major Goldthorn Potion | 畸变 | `` |
| Spell | `BG34_Anomaly_800t` | Minor Goldthorn Potion | 畸变 | `` |
| Spell | `BG34_Anomaly_801` | Boon of Chronum | 畸变 | `` |
| Spell | `BG34_Anomaly_802` | Major Waygate | 畸变 | `` |
| Spell | `BG34_Anomaly_804` | Twisting Hourglass | 畸变 | `` |
| Spell | `BG34_Anomaly_805` | Oathstone's Summoning | 畸变 | `` |
| Spell | `BG34_Anomaly_809` | Unforeseen Portal | 畸变 | `` |
| Spell | `BG35_Anomaly_001` | Fly the Flag | 畸变 | `` |
| Spell | `BG35_Anomaly_002` | Anomalous Cube | 畸变 | `` |
| Spell | `BG35_Anomaly_004` | Anomalous Conflux | 畸变 | `` |
| Spell | `BG35_Anomaly_005` | Anomalous Timeline | 畸变 | `` |
| Spell | `BG35_Anomaly_006` | Anomalous Expedition | 畸变 | `` |
| Spell | `BG35_Anomaly_007` | Lesser Fortune | 畸变 | `` |
| Spell | `BG35_Anomaly_008` | Greater Fortune | 畸变 | `` |
| Spell | `BGDUO_Anomaly_003` | Golden Friendship | 畸变 | `` |
| Spell | `BGDUO_Anomaly_005` | All Bottled Up | 畸变 | `` |
| Spell | `BGDUO_Anomaly_006` | Line in the Sand | 畸变 | `` |
| Spell | `BGDUO_Anomaly_007` | Pooled Resources | 畸变 | `` |
| TavernSpell | `BGS_Treasures_100` | Unfurled Codex | 暗月奖品 | `` |
| TavernSpell | `BGS_Treasures_101` | Mageroyal Blossom | 暗月奖品 | `` |
| TavernSpell | `BGS_Treasures_104` | Reserve Prices | 暗月奖品 | `` |
| TavernSpell | `BGS_Treasures_106` | Gorgeous Goblet | 暗月奖品 | `` |
| TavernSpell | `BGS_Treasures_110` | Crystallization | 暗月奖品 | `` |

## 代码生成复核候选

这些 ID 在 Runtime C# 中以 `CardId = "..."` 出现、不属于上述目录，并且没有按通用路径找到图片。它们不全部等于玩家可见卡牌，后续补正式图片时需结合生成入口逐项分类。基础占位视觉已经覆盖实际进入 `UnityTavernCardComponent` 的项目。

| CardId | 首次发现位置 |
|---|---|
| `ARCANE_CONSUMPTION` | `Assets\LearnHearthstone\Runtime\Domain\Engine\TavernSpellEngine.cs:103` |
| `BATTLECRUISER_UPGRADE` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:292` |
| `BETTER_SECRET_PROXY` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:4310` |
| `BG_BOT_606` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:972` |
| `BG_EX1_564` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:840` |
| `BG_OG_221` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:975` |
| `BG21_006` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:971` |
| `BG21_013` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:974` |
| `BG22_403` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:976` |
| `BG24_HERO_100_Buddy_G` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:111` |
| `BG24_Reward_362t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:227` |
| `BG24_Reward_715t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:219` |
| `BG24_Reward_715t2` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:220` |
| `BG24_Reward_715t3` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:221` |
| `BG24_Reward_715t4` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:222` |
| `BG24_Reward_718t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:225` |
| `BG24_Reward_719t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:226` |
| `BG25_HERO_100pt` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:283` |
| `BG26_350` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:232` |
| `BG27_Reward_504t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:224` |
| `BG28_585` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:268` |
| `BG28_707` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:801` |
| `BG29_140` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:970` |
| `BG29_801` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:969` |
| `BG29_873` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:967` |
| `BG30_119` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:793` |
| `BG31_148` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:531` |
| `BG31_360` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:839` |
| `BG31_822` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:530` |
| `BG31_830` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:802` |
| `BG31_924` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:838` |
| `BG31_HERO_801pt` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:311` |
| `BG31_HERO_802pt` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:317` |
| `BG31_HERO_802pt1` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:318` |
| `BG31_HERO_802pt4` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:319` |
| `BG31_HERO_802pt5` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:320` |
| `BG31_HERO_802pt7` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:781` |
| `BG31_HERO_811t` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:289` |
| `BG31_HERO_811t10` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:316` |
| `BG31_HERO_811t2` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:312` |
| `BG31_HERO_811t5` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:313` |
| `BG31_HERO_811t6` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:314` |
| `BG31_HERO_811t7` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:315` |
| `BG33_811` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:483` |
| `BG33_812` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:484` |
| `BG33_813` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:485` |
| `BG33_814` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:486` |
| `BG33_815` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:487` |
| `BG33_Reward_006t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:223` |
| `BG33_Reward_011t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:228` |
| `BG34_BlackMarket_Skip` | `Assets\LearnHearthstone\Runtime\Adapters\Data\TimewarpedTavernCatalogLoader.cs:177` |
| `BG34_Giant_210t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:889` |
| `BG34_Giant_212t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:887` |
| `BG34_Treasure_300` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:318` |
| `BG34_Treasure_301` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:330` |
| `BG34_Treasure_302` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:340` |
| `BG34_Treasure_606` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:332` |
| `BG34_Treasure_607` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:333` |
| `BG34_Treasure_608` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:335` |
| `BG34_Treasure_609` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:336` |
| `BG34_Treasure_620` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:337` |
| `BG34_Treasure_625` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:331` |
| `BG34_Treasure_900` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:334` |
| `BG34_Treasure_902` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:338` |
| `BG34_Treasure_903` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:319` |
| `BG34_Treasure_905` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:320` |
| `BG34_Treasure_912` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:321` |
| `BG34_Treasure_917` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:322` |
| `BG34_Treasure_919` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:326` |
| `BG34_Treasure_932` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:328` |
| `BG34_Treasure_933` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:327` |
| `BG34_Treasure_934` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:317` |
| `BG34_Treasure_937` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:323` |
| `BG34_Treasure_940` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:324` |
| `BG34_Treasure_950` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:341` |
| `BG34_Treasure_951` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:342` |
| `BG34_Treasure_953` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:325` |
| `BG34_Treasure_955` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:329` |
| `BG34_Treasure_966` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:339` |
| `BG35_MagicItem_812t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:782` |
| `BGS_009` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:565` |
| `BGS_066` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:968` |
| `BLOOD_GEM` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:229` |
| `BRISTLEBACK_BLOOD_GEM` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:266` |
| `CURATOR_AMALGAM` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:913` |
| `DEEP_BLUE_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:283` |
| `DEEP_SEA_ANGLER_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:282` |
| `DEEPWATER_SCHOOL` | `Assets\LearnHearthstone\Runtime\Domain\Engine\TavernSpellEngine.cs:102` |
| `DEMON_FODDER` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:271` |
| `FEARLESS_FOODIE_GEMS_OPTION` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:526` |
| `FEARLESS_FOODIE_GROWTH_OPTION` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:525` |
| `FISH_OF_NZOTH` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:272` |
| `FLY_THE_FLAG_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:438` |
| `FROSTLING_PRIESTESS_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:286` |
| `GENERATED_ELEMENTAL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:29944` |
| `MOONSTEEL_SATELLITE` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:27877` |
| `MUKLA_BANANA` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:344` |
| `NZOTH_FISH` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:273` |
| `OZUMAT_TENTACLE` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:47` |
| `RAKANISHU_LANTERN_LIGHT` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:139` |
| `REBORN_BLOOD_GEM` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:267` |
| `REEF_RIFFER_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:280` |
| `SKY_PIRATE` | `Assets\LearnHearthstone\Runtime\Domain\Engine\CombatEngine.cs:143` |
| `SLIMY_SHIELD` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:265` |
| `SNEED_SHREDDER` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:295` |
| `SPRIGHTLY_SCARAB_REBORN_OPTION` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:478` |
| `SPRIGHTLY_SCARAB_WINDFURY_OPTION` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:479` |
| `SURF_N_SURF_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:281` |
| `TAUGHT_MURLOC` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:27911` |
| `TB_BaconShop_HERO_35_Buddy_G` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:144` |
| `TB_BaconShop_HP_105t` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:955` |
| `THAUMATURGIST_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:295` |
| `TIMEWARPED_ELECTRON_SATELLITE` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:24680` |
| `TIMEWARPED_EVOLVING_TAVERN_SPELL` | `Assets\LearnHearthstone\Runtime\Domain\Engine\TavernSpellEngine.cs:35` |
| `TIMEWARPED_GLOWSCALE_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:293` |
| `TIMEWARPED_SUMMONER_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:296` |
| `TRINKET_BATTLECRUISER` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1052` |
| `TRINKET_BATTLECRUISER_UPGRADE` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1053` |
| `TRINKET_CHILLMERE_MOSAIC_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:292` |
| `TRINKET_COIN_POUCH_3` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1057` |
| `TRINKET_CURATOR_AMALGAM` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1062` |
| `TRINKET_DEMONBLOOD_GOURD_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1059` |
| `TRINKET_DOUBLE_STITCH_NEEDLE_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:290` |
| `TRINKET_DOUBLOON_GRIFTER` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1054` |
| `TRINKET_JAILER_STICKER_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1058` |
| `TRINKET_JEWELRY_BOX_DIVINE_SHIELD_GEM` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:500` |
| `TRINKET_JEWELRY_BOX_REBORN_GEM` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:501` |
| `TRINKET_JEWELRY_BOX_TAUNT_GEM` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:499` |
| `TRINKET_MAGICFIN_MURLOC` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1056` |
| `TRINKET_MAW_CASTER` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1055` |
| `TRINKET_OPHIDIAN_STAFF_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:288` |
| `TRINKET_PRECIOUS_PEARL_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:287` |
| `TRINKET_SHIFTING_TIDE_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:1060` |
| `TRINKET_TOKEN_OF_THE_OLD_GODS_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:291` |
| `TRINKET_VIBRANT_BUBBLE_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:289` |
| `TRIPLE_REWARD` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:51` |
| `VOLCANIC_VISITOR_ATTACK_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:284` |
| `VOLCANIC_VISITOR_HEALTH_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:285` |
| `WEARY_MAGE_SPELL` | `Assets\LearnHearthstone\Runtime\Application\Services\MatchService.cs:294` |
| `ZERG_MINION_PROXY` | `Assets\LearnHearthstone\Runtime\Domain\Engine\HeroEffectEngine.cs:293` |

## 本阶段处理

- 缺图卡使用稳定背景色和卡名简称。
- 中文/中日韩名称取前两个有效字符。
- 英文多词名称取前两个单词首字母；单词名称取前两个字符。
- 完整卡名和原有卡牌信息继续显示。
- 只有 `TavernSpell` 显示费用；普通或临时法术不显示 `0` 费。
- 不同卡牌类型的专属底图、图标、纹章和动画效果留待后续实现。
