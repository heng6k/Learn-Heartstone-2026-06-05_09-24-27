# Image Asset Audit

Audit date: 2026-06-18

## Summary

This project already has a solid image base:

- `Assets/LearnHearthstone/Resources/CardImages`: 253 images
- `Assets/LearnHearthstone/Resources/CardImages/TavernSpells`: 57 images
- `Assets/LearnHearthstone/Resources/HeroBuddyImages/heroes`: 114 images
- `Assets/LearnHearthstone/Resources/HeroBuddyImages/heroPowers`: 114 images
- `Assets/LearnHearthstone/Resources/HeroBuddyImages/buddies`: 108 images

Confirmed coverage:

- Hero portrait images: no missing resources found.
- Hero power images: no missing resources found.
- Existing mapped buddy images: no missing resources found.
- Card library tier icons 1-7: present.

Main gaps:

- 16 Tavern spell images are missing and will fall through to fallback card visuals.
- 36 minion base images are missing. Of these, 29 are Duos cards and should likely stay out of scope rather than be filled.
- Several hard-coded/generated tokens and proxy cards have no `ImagePath` and no matching resource.
- 6 heroes have no buddy mapping.
- 280 golden minion IDs exist in data, but current runtime uses the base minion `ImagePath` for golden instances. This is not a hard runtime miss today, but it is a visual polish opportunity.

## Runtime Fallback Behavior

Image loading is centralized in:

- `Assets/LearnHearthstone/Runtime/Adapters/Images/CardImageProvider.cs`

If a card image does not load, the UI falls back to generated visuals:

- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernCardComponent.cs`: shows a colored block plus fallback text.
- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/Realistic/TavernCardView.cs`: shows a brown placeholder portrait.

So every missing item below is a place where the player can see placeholder art.

## P0: Missing Tavern Spell Images

Add these files under `Assets/LearnHearthstone/Resources/CardImages/TavernSpells/`.

| CardNumber | Name | Tier | Status | Expected file |
| - | - | - | - | - |
| 131152 | Might of Stormwind | 2 | implemented | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/131152.png |
| 122182 | Healthy Bounty | 3 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/122182.png |
| 122183 | Hostile Bounty | 3 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/122183.png |
| 122184 | Selfish Bounty | 3 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/122184.png |
| 122185 | Friendly Bounty | 3 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/122185.png |
| 122186 | Wealthy Bounty | 3 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/122186.png |
| 110401 | Boon of Beetles | 4 | implemented | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/110401.png |
| 130310 | Conflagration | 4 | implemented | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/130310.png |
| 130311 | Arcane Absorption | 4 | implemented | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/130311.png |
| 130312 | Eonar's Favor | 4 | implemented | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/130312.png |
| 131153 | Back to Back | 4 | implemented | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/131153.png |
| 131218 | Deepwater Clan | 4 | implemented | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/131218.png |
| 110412 | Butchering | 5 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/110412.png |
| 130713 | Queen's Command | 5 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/130713.png |
| 113902 | Knockoff Wisdomball | 6 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/113902.png |
| 130527 | Menagerie Tableware | 7 | official-data | Assets/LearnHearthstone/Resources/CardImages/TavernSpells/130527.png |

## P0: Missing Single-Player/Review Minion Images

These are not `BGDUO` IDs. They are most worth replacing first if they appear in current single-player Tavern flow, generated combat, or discovery pools.

| CardId | Name | Tier | InPool | Expected file |
| - | - | - | - | - |
| BG25_013 | 腐皮豺狼人 | 1 | 1 | Assets/LearnHearthstone/Resources/CardImages/BG25_013.png |
| BG26_800 | 魔刃豹 | 1 | 0 | Assets/LearnHearthstone/Resources/CardImages/BG26_800.png |
| BG34_638t | 红色多彩幼龙 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BG34_638t.png |
| BG34_636t | 绿色多彩幼龙 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BG34_636t.png |
| BG34_634t | 蓝色多彩幼龙 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BG34_634t.png |
| BG34_637t | 青铜多彩幼龙 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BG34_637t.png |
| BG34_635t | 黑色多彩幼龙 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BG34_635t.png |

## P1: Missing Duos Minion Images

Project scope says Duos systems are intentionally out of scope. These should probably be removed from active pools or marked out of scope rather than filled, unless you decide to show Duos cards anyway.

| CardId | Name | Tier | InPool | Expected file |
| - | - | - | - | - |
| BGDUO_114 | 过路旅客 | 1 | 0 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_114.png |
| BGDUO_104 | 热心的沙龙酒保 | 2 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_104.png |
| BGDUO_100 | 远行者周卓 | 2 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_100.png |
| BGDUO31_201 | 聚积风暴 | 2 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_201.png |
| BGDUO_111 | 大方的地卜师 | 2 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_111.png |
| BGDUO_119 | 兽人指挥 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_119.png |
| BGDUO_115 | 跳跳娃娃 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_115.png |
| BGDUO_118 | 打劫共犯 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_118.png |
| BGDUO31_207 | 摩托水手 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_207.png |
| BGDUO33_140 | 底栖饲育者 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO33_140.png |
| BGDUO_117 | 滩涂跳跳鱼 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_117.png |
| BGDUO_107 | 护雏的龙希尔 | 3 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_107.png |
| BGDUO31_212 | 螳螂妖国王 | 4 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_212.png |
| BGDUO_112 | 墓后解说员 | 4 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_112.png |
| BGDUO31_208 | 萨莱因铭文师 | 4 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_208.png |
| BGDUO_110 | 活泼的淡水元素 | 4 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_110.png |
| BGDUO_108 | 镜中鬼怪 | 4 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_108.png |
| BGDUO31_209 | 私房主厨 | 4 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_209.png |
| BGDUO31_203 | 狡捷灵蛇 | 4 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_203.png |
| BGDUO_120 | 井边许愿者 | 5 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_120.png |
| BGDUO_121 | 堕落者信使 | 5 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_121.png |
| BGDUO_109 | 增援系统 | 5 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_109.png |
| BGDUO_122 | 风暴分流者 | 5 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_122.png |
| BGDUO_105 | 宽厚的驼鹿 | 5 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_105.png |
| BGDUO31_205 | 无私的观光客 | 5 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_205.png |
| BGDUO33_150 | 黑暗炫魔 | 6 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO33_150.png |
| BGDUO31_211 | 转运反应堆 | 6 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_211.png |
| BGDUO31_202 | 忠实的帮凶 | 6 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO31_202.png |
| BGDUO_125 | 流沙幻象 | 7 | 1 | Assets/LearnHearthstone/Resources/CardImages/BGDUO_125.png |

## P1: Generated Cards With No ImagePath

These are created directly in code instead of loaded from the JSON catalogs. Most do not set `ImagePath`, so `CardImageProvider` tries a default `CardImages/{CardId}` or `CardImages/TavernSpells/{CardId}` path and then shows fallback art.

For these, either add matching images at the expected path or set a deliberate `ImagePath` in code.

| CardId | Display name | Kind | Source | Suggested resource |
| - | - | - | - | - |
| MOONSTEEL_SATELLITE | Moonsteel Satellite | Minion | `MatchService.cs` | `Assets/LearnHearthstone/Resources/CardImages/MOONSTEEL_SATELLITE.png` |
| TAUGHT_MURLOC | Taught Murloc | Minion | `MatchService.cs` | `Assets/LearnHearthstone/Resources/CardImages/TAUGHT_MURLOC.png` |
| GENERATED_ELEMENTAL | 商贩元素 | Minion | `MatchService.cs` | `Assets/LearnHearthstone/Resources/CardImages/GENERATED_ELEMENTAL.png` |
| DEMON_FODDER | 恶魔饲料 | Minion | `MatchService.cs` | `Assets/LearnHearthstone/Resources/CardImages/DEMON_FODDER.png` |
| NZOTH_FISH | Fish of N'Zoth | Minion | `HeroEffectEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/NZOTH_FISH.png` |
| SNEED_SHREDDER | Sneed's Shredder | Minion | `HeroEffectEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/SNEED_SHREDDER.png` |
| CURATOR_AMALGAM | Amalgam | Minion | `HeroEffectEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/CURATOR_AMALGAM.png` |
| UNDEAD_CREATION_PROXY | Undead Creation | Minion | `HeroEffectEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/UNDEAD_CREATION_PROXY.png` |
| ZERG_MINION_PROXY | Zerg minion proxy | Minion | `HeroEffectEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/ZERG_MINION_PROXY.png` |
| OZUMAT_TENTACLE | Tentacle | Minion | `HeroEffectEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/OZUMAT_TENTACLE.png` |
| BLOOD_GEM | Blood Gem / 鲜血宝石 | Spell | `MatchService.cs`, `HeroEffectEngine.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/BLOOD_GEM.png` |
| BRISTLEBACK_BLOOD_GEM | Bristleback Blood Gem | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/BRISTLEBACK_BLOOD_GEM.png` |
| REBORN_BLOOD_GEM | Reborn Blood Gem | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/REBORN_BLOOD_GEM.png` |
| SLIMY_SHIELD | 黏黏盾 | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/SLIMY_SHIELD.png` |
| 100596 | Pointy Arrow | Spell | `MatchService.cs` | `Assets/LearnHearthstone/Resources/CardImages/100596.png` |
| REEF_RIFFER_SPELL | Reef Riff | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/REEF_RIFFER_SPELL.png` |
| SURF_N_SURF_SPELL | Surf n' Surf | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/SURF_N_SURF_SPELL.png` |
| DEEP_SEA_ANGLER_SPELL | Deep Sea Angling | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/DEEP_SEA_ANGLER_SPELL.png` |
| DEEP_BLUE_SPELL | Deep Blue | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/DEEP_BLUE_SPELL.png` |
| VOLCANIC_VISITOR_ATTACK_SPELL | Volcanic Visitor attack spell | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/VOLCANIC_VISITOR_ATTACK_SPELL.png` |
| VOLCANIC_VISITOR_HEALTH_SPELL | Volcanic Visitor health spell | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/VOLCANIC_VISITOR_HEALTH_SPELL.png` |
| FROSTLING_PRIESTESS_SPELL | Frostling Priestess | Spell | `MatchService.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/FROSTLING_PRIESTESS_SPELL.png` |
| RAKANISHU_LANTERN_LIGHT | Lantern Light | Spell | `HeroEffectEngine.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/RAKANISHU_LANTERN_LIGHT.png` |
| MUKLA_BANANA | Banana | TavernSpell | `HeroEffectEngine.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/TavernSpells/MUKLA_BANANA.png` |
| BATTLECRUISER_UPGRADE | Battlecruiser Upgrade | TavernSpell | `HeroEffectEngine.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/TavernSpells/BATTLECRUISER_UPGRADE.png` |
| BETTER_SECRET_PROXY | Better Secret | TavernSpell | `HeroEffectEngine.cs`, `TavernSpellEngine.cs` | `Assets/LearnHearthstone/Resources/CardImages/TavernSpells/BETTER_SECRET_PROXY.png` |
| TRIPLE_REWARD | Triple Reward | TavernSpell | `MatchService.cs` | `Assets/LearnHearthstone/Resources/CardImages/TavernSpells/TRIPLE_REWARD.png` |

## P1: Hero Buddy Mappings Missing

These heroes have images and hero power images, but their buddy mapping is missing. The result is not a missing image file; it is a missing buddy entry, which means buddy-focused UI/effects cannot show a correct buddy card.

| HeroCardId | Hero | Missing buddy | Missing hero power | HeroPower | Buddy |
| - | - | - | - | - | - |
| BG34_HERO_004 | Morchie | true | false | BG34_HERO_004p |  |
| BG34_HERO_002 | Mister Clocksworth | true | false | BG34_HERO_002p |  |
| BG34_HERO_000 | Murozond, Unbounded | true | false | BG34_HERO_000p |  |
| BG31_HERO_003 | Farseer Nobundo | true | false | BG31_HERO_003p |  |
| BG35_HERO_001 | Genn, Worgen King | true | false | BG35_HERO_001p |  |
| BG34_HERO_001 | Time Twister Chromie | true | false | BG34_HERO_001p |  |

## P2: Golden Minion Art

The JSON has golden card IDs for all 280 minions, and none of those golden image files are present. Current runtime does not directly use the golden `cardId` for image loading; `MinionFactory.Create(... golden: true)` keeps the base `ImagePath`, and `TripleEngine.ResolveTriple` clones the base instance. So these are not current placeholder triggers.

If you want real golden visuals later, either:

- Add golden images and update runtime image selection for `MinionInstance.Golden`.
- Or intentionally keep base art and add a visual gold frame/effect instead.

Missing golden image count by tier:

| Tier | Missing golden images |
| - | - |
| 1 | 24 |
| 2 | 36 |
| 3 | 48 |
| 4 | 60 |
| 5 | 61 |
| 6 | 38 |
| 7 | 13 |

## Suggested Fix Order

1. Add the 16 Tavern spell images.
2. Add the 7 non-Duos minion/token images.
3. Add images or explicit `ImagePath` values for generated cards that are visible in normal play.
4. Decide whether Duos cards should be excluded from active data instead of filled.
5. Add buddy mappings for the 6 heroes.
6. Decide whether golden cards need separate art or only a gold visual treatment.
