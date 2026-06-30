# Timewarped Minion Mechanisms

## Timewarped Acolyte (BG34_Giant_591)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 4/6
- 种族: MURLOC
- 触发时机: start_of_turn
- 效果类别: special
- 机制文本: At the start of your turn, spin the Wheel of Yogg-Saron.
- 中文文本: 在你的回合开始时，转动尤格-萨隆的命运之轮。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Alleycat (BG34_Giant_009)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 7/7
- 种族: BEAST
- 触发时机: end_of_turn
- 效果类别: stats, summon
- 机制文本: At the end of your turn, summon a Tabbycat with this minion's stats.
- 中文文本: 在你的回合结束时，召唤一只具有本随从属性值的雌斑虎。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Annoy-o-Tron (BG34_Giant_007)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/6
- 种族: MECH
- 触发时机: static_or_aura
- 效果类别: keyword_grant_or_keyword_body
- 机制文本: Taunt Divine Shield Reborn
- 中文文本: 嘲讽。圣盾。复生
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Archer (BG34_Giant_212)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 4/9
- 种族: NAGA
- 触发时机: spellcraft
- 效果类别: stats, card_generation, spellcraft
- 机制文本: Spellcraft: Give a minion +12 Attack.
- 中文文本: 塑造法术：使一个随从获得+12攻击力。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Bassgill (BG34_Giant_071)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 7/4
- 种族: MURLOC
- 触发时机: deathrattle
- 效果类别: stats, summon, combat_only
- 机制文本: Deathrattle: Summon the highest-Health minion from your hand and give it Divine Shield for this combat only.
- 中文文本: 亡语：召唤你手牌中生命值最高的随从并使其获得圣盾，其登场仅限本场战斗。
- 实现备注: 需要战斗临时召唤/临时增益清理。；可接入现有关键字触发分发。

## Timewarped Boar (BG34_Giant_201)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 1/1
- 种族: BEAST
- 触发时机: static_or_aura
- 效果类别: economy, tribe_synergy
- 机制文本: Whenever every third friendly Timewarped Boar dies, get a random Golden Beast. (0 left!)
- 中文文本: 每有三只友方时空扭曲野猪死亡，随机获取一张金色野兽牌。（还剩0只！）
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Botani (BG34_Giant_594)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 8/6
- 种族: NONE
- 触发时机: end_of_turn
- 效果类别: card_generation
- 机制文本: At the end of your turn, get a random minion of your Tier.
- 中文文本: 在你的回合结束时，随机获取一张你当前等级的随从牌。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Busker (BG34_Giant_001)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 4/2
- 种族: PIRATE
- 触发时机: battlecry, deathrattle
- 效果类别: economy
- 机制文本: Battlecry and Deathrattle: Gain 1 Gold next turn.
- 中文文本: 战吼，亡语：下回合获得1枚铸币。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Chimera (BG34_Giant_679)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 1/8
- 种族: ALL
- 触发时机: damage_reactive
- 效果类别: stats, damage
- 机制文本: Whenever this takes damage, give a friendly minion of each type +2/+1 permanently.
- 中文文本: 每当本随从受到伤害，使每个类型的各一个友方随从永久获得+2/+1。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Commander (BG34_Giant_210)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 5/5
- 种族: NAGA
- 触发时机: spellcraft
- 效果类别: stats, card_generation, spellcraft, tribe_synergy
- 机制文本: Spellcraft: Give a minion +2/+2 for each friendly Naga.
- 中文文本: 塑造法术：每有一个友方纳迦，使一个随从获得+2/+2。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Copter (BG34_Giant_302)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 4/6
- 种族: MECH
- 触发时机: avenge
- 效果类别: keyword_grant_or_keyword_body, card_generation, tribe_synergy
- 机制文本: Divine Shield Avenge (3): Get a random Mech.
- 中文文本: 圣盾。复仇（3）：随机获取一张机械牌。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Cyclone (BG34_Giant_012)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/1
- 种族: ELEMENTAL
- 触发时机: static_or_aura
- 效果类别: keyword_grant_or_keyword_body
- 机制文本: Divine Shield Windfury Reborn
- 中文文本: 圣盾。风怒。复生
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Deathswarmer (BG34_Giant_081)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 1/9
- 种族: UNDEAD
- 触发时机: damage_reactive
- 效果类别: stats, damage, tribe_synergy
- 机制文本: Whenever this takes damage, your Undead have +1 Attack this game (wherever they are).
- 中文文本: 每当本随从受到伤害时，在本局对战中，你的亡灵拥有+1攻击力（无论它们在哪）。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Devourer (BG34_Giant_583)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/5
- 种族: DEMON
- 触发时机: start_of_turn
- 效果类别: economy, stats, tribe_synergy
- 机制文本: At the start of your turn, consume the Demon to the right to gain its stats and 3 Gold.
- 中文文本: 在你的回合开始时，吞食本随从右边的恶魔以获得其属性值以及3枚铸币。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Dragonling (BG34_Giant_029)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 3/3
- 种族: DRAGON
- 触发时机: start_of_combat
- 效果类别: stats
- 机制文本: Start of Combat: Give this minion and its neighbors stats equal to your Tier.
- 中文文本: 战斗开始时：使本随从和相邻的随从获得等同于你当前等级的属性值。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Elise (BG34_Giant_038)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 6/7
- 种族: NONE
- 触发时机: recruit_phase_reactive
- 效果类别: economy, shop_or_refresh
- 机制文本: After you Refresh 5 times, make the highest-Tier minion in the Tavern Golden. (5 left!)
- 中文文本: 在你刷新5次后， 使酒馆中等级最高的随从变为金色。（还剩5次！）
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Embalmer (BG34_Giant_332)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 10/10
- 种族: UNDEAD
- 触发时机: static_or_aura
- 效果类别: keyword_grant_or_keyword_body, summon
- 机制文本: One minion you summon each turn gains Reborn. (1 left!)
- 中文文本: 每回合中，你召唤的一个随从获得复生。（还剩1个！）
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Festergut (BG34_Giant_590)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 7/3
- 种族: UNDEAD
- 触发时机: deathrattle
- 效果类别: summon, card_generation, tribe_synergy
- 机制文本: Deathrattle: Summon and get a random Undead Creation.
- 中文文本: 亡语：召唤并获取一个随机亡灵造物。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Geomancer (BG34_Giant_305)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 2/9
- 种族: QUILBOAR
- 触发时机: avenge
- 效果类别: stats, card_generation, blood_gem
- 机制文本: Avenge (5): Get a Blood Gem. Your Blood Gems give an extra +1/+1 this game.
- 中文文本: 复仇（5）：获取一张鲜血宝石。在本局对战中，你的鲜血宝石使随从额外获得+1/+1。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Greenskeeper (BG34_Giant_041)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/9
- 种族: DRAGON
- 触发时机: battlecry, deathrattle, rally
- 效果类别: special
- 机制文本: Rally: Trigger your right-most Battlecry and Deathrattle.
- 中文文本: 进击：触发你最右边的战吼和亡语。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Henchman (BG34_Giant_593)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/7
- 种族: NONE
- 触发时机: combat_kill
- 效果类别: card_generation, copy
- 机制文本: After you kill a second minion each combat, get a plain copy of it.
- 中文文本: 每场战斗中，在你消灭第二个随从后，获取一张它的原始版 复制。
- 实现备注: 需要生成新 InstanceId，避免复用源实例。

## Timewarped Hyena (BG34_Giant_581)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 3/5
- 种族: BEAST
- 触发时机: static_or_aura
- 效果类别: stats, tribe_synergy
- 机制文本: Whenever a friendly Beast dies, gain +2/+2 permanently.
- 中文文本: 每当一只友方野兽死亡，永久获得+2/+2。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Jazzer (BG34_Giant_306)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 5/3
- 种族: QUILBOAR
- 触发时机: deathrattle
- 效果类别: stats, blood_gem
- 机制文本: Deathrattle: Your Blood Gems give an extra +{1} Health this game.
- 中文文本: 亡语：在本局对战中，你的鲜血宝石会额外获得+{1}生命值。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Kil'rek (BG34_Giant_584)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 4/7
- 种族: DEMON
- 触发时机: deathrattle
- 效果类别: keyword_grant_or_keyword_body, card_generation, tribe_synergy
- 机制文本: Taunt Deathrattle: Get a random Demon.
- 中文文本: 嘲讽。亡语：随机获取一张恶魔牌。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Leapfrogger (BG34_Giant_031)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 3/3
- 种族: BEAST
- 触发时机: deathrattle
- 效果类别: stats, keyword_grant_or_keyword_body, tribe_synergy
- 机制文本: Taunt, Reborn Deathrattle: Give a friendly Beast +1/+1 and this Deathrattle.
- 中文文本: 嘲讽。复生。亡语：使一只友方野兽获得+1/+1以及此亡语。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Lei (BG34_Giant_602)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 3/5
- 种族: NONE
- 触发时机: start_of_turn
- 效果类别: card_generation, hero_or_buddy
- 机制文本: At the start of your turn, get the Buddy of your Hero Power.
- 中文文本: 在你的回合开始时，获取你的英雄技能对应的伙伴。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Lubber (BG34_Giant_066)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/7
- 种族: ELEMENTAL, PIRATE
- 触发时机: static_or_aura
- 效果类别: shop_or_refresh, stats, card_generation, tavern_spell_synergy
- 机制文本: The Tavern always offers 1 extra Tavern spells. Your Tavern spells give an extra +1/+1.
- 中文文本: 酒馆总会额外提供1张酒馆法术牌。你的酒馆法术使随从额外获得+1/+1。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Mothership (BG34_Giant_598)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/7
- 种族: MECH
- 触发时机: avenge
- 效果类别: card_generation
- 机制文本: Avenge (4): Get a random Protoss minion.
- 中文文本: 复仇（4）：随机获取一张星灵随从牌。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Murcules (BG34_Giant_207)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 10/5
- 种族: MURLOC
- 触发时机: hand_state, combat_kill
- 效果类别: stats, keyword_grant_or_keyword_body
- 机制文本: Divine Shield Whenever this kills a minion, give the left-most minion in your hand +4/+4.
- 中文文本: 圣盾。每当本随从消灭一个随从时，使你手牌中最左边的随从牌获得+4/+4。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Nellie's Ship (BG34_Giant_074t)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 2/6
- 种族: BEAST, PIRATE
- 触发时机: deathrattle, start_of_turn
- 效果类别: summon, card_generation, tribe_synergy
- 机制文本: At the start of each turn, Discover a Pirate to crew the ship. Deathrattle: Summon and get that Pirate.
- 中文文本: 在每个回合开始时，为这艘船发现一位海盗船员。亡语：召唤并获取船上的海盗。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Pagle (BG34_Giant_208)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 8/6
- 种族: PIRATE
- 触发时机: combat_kill
- 效果类别: stats, card_generation
- 机制文本: Once per combat, when this attacks and kills a minion, get a Triple Reward.
- 中文文本: 每场战斗一次：当本随从攻击并消灭一个随从时，获取一份三连奖励。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Pashmar (BG34_Giant_211)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 9/10
- 种族: NAGA
- 触发时机: avenge, spellcraft
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy, spellcraft
- 机制文本: Avenge (3): Get a random Spellcraft spell and Tavern spell.
- 中文文本: 复仇（3）：随机获取塑造法术的法术牌和酒馆法术牌各一张。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Pillager (BG34_Giant_204)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 4/3
- 种族: UNDEAD
- 触发时机: deathrattle
- 效果类别: economy, shop_or_refresh, keyword_grant_or_keyword_body, card_generation
- 机制文本: Taunt, Reborn Deathrattle: Get a Tavern Coin.
- 中文文本: 嘲讽。复生。亡语：获取一张酒馆币。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；可接入现有关键字触发分发。

## Timewarped Piper (BG34_Giant_069)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 2/8
- 种族: QUILBOAR
- 触发时机: damage_reactive
- 效果类别: stats, blood_gem, damage
- 机制文本: Whenever this takes damage, your Blood Gems give an extra +1 Attack this game. ({2} times per combat.)
- 中文文本: 每当本随从受到伤害，在本局对战中，你的鲜血宝石使随从额外获得+1攻击力。（每场战斗限{2}次。）
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Ragnaros (BG34_Giant_580)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 8/8
- 种族: ELEMENTAL
- 触发时机: start_of_combat
- 效果类别: stats, damage
- 机制文本: Start of Combat: Deal this minion's Attack to the highest-Health enemy minion.
- 中文文本: 战斗开始时：对生命值最高的敌方随从造成等同于本随从攻击力的伤害。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Recycler (BG34_Giant_082)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 2/7
- 种族: UNDEAD
- 触发时机: avenge
- 效果类别: economy
- 机制文本: Avenge (4): Increase your maximum Gold by (1).
- 中文文本: 复仇（4）：你的铸币上限提高（1）枚。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Red Whelp (BG34_Giant_091)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 4/6
- 种族: DRAGON
- 触发时机: start_of_combat, recruit_phase_reactive
- 效果类别: damage, tribe_synergy
- 机制文本: Start of Combat: Deal 3 damage to two random enemy minions. (Improves after you play a Dragon!)
- 中文文本: 战斗开始时：随机对两个敌方随从造成3点伤害。（在你使用一张龙牌后提升！）
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Rewinder (BG34_Giant_300)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/4
- 种族: DEMON
- 触发时机: damage_reactive
- 效果类别: stats, damage, tribe_synergy
- 机制文本: After your hero takes damage, rewind it and give your Demons +{2} Health.
- 中文文本: 在你的英雄受到伤害后，回溯该伤害并使你的恶魔获得+{2}生命值。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Sailor (BG34_Giant_589)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/4
- 种族: PIRATE
- 触发时机: static_or_aura
- 效果类别: shop_or_refresh, keyword_grant_or_keyword_body, tribe_synergy
- 机制文本: Divine Shield The Tavern offers an extra Pirate whenever it is Refreshed.
- 中文文本: 圣盾。每当酒馆刷新时，总会额外提供一个海盗。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Sapper (BG34_Giant_304)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 10/6
- 种族: NAGA
- 触发时机: deathrattle
- 效果类别: keyword_grant_or_keyword_body, card_generation
- 机制文本: Taunt Deathrattle: Get a Spitescale Special.
- 中文文本: 嘲讽。亡语：获取一张恶鳞套餐。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Saurolisk (BG34_Giant_202)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 4/4
- 种族: BEAST
- 触发时机: deathrattle
- 效果类别: stats
- 机制文本: After you trigger a Deathrattle, gain +3/+2 permanently.
- 中文文本: 在你触发一个亡语后，永久获得+3/+2。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Scourfin (BG34_Giant_017)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 7/7
- 种族: MURLOC
- 触发时机: deathrattle, hand_state
- 效果类别: stats, keyword_grant_or_keyword_body, summon, combat_only
- 机制文本: Taunt. Deathrattle: Give a random minion in your hand +7/+7 and summon it for this combat only.
- 中文文本: 嘲讽。亡语：随机使你手牌中的一张随从牌获得+7/+7并召唤它，其登场仅限本场 战斗。
- 实现备注: 需要战斗临时召唤/临时增益清理。；可接入现有关键字触发分发。

## Timewarped Sellemental (BG34_Giant_067)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 8/8
- 种族: ELEMENTAL
- 触发时机: end_of_turn
- 效果类别: card_generation
- 机制文本: At the end of your turn, get a Sellemental.
- 中文文本: 在你的回合结束时，获取一张商贩元素。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Sensei (BG34_Giant_209)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/6
- 种族: MECH
- 触发时机: end_of_turn
- 效果类别: stats, tribe_synergy
- 机制文本: At the end of your turn, give adjacent Mechs +3/+3.
- 中文文本: 在你的回合结束时，使相邻的机械获得+3/+3。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Shadowdancer (BG34_Giant_360)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/5
- 种族: DEMON
- 触发时机: end_of_turn
- 效果类别: keyword_grant_or_keyword_body
- 机制文本: Taunt At the end of your turn, cast Staff of Enrichment.
- 中文文本: 嘲讽。在你的回合结束时，施放富足之杖。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Skipper (BG34_Giant_072)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 5/6
- 种族: MURLOC
- 触发时机: recruit_phase_reactive
- 效果类别: card_generation
- 机制文本: After you sell a Tier 2 minion, get a random Tier 1 minion.
- 中文文本: 在你出售一个等级2的随从后，随机获取一张等级1的随从牌。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Snow Elemental (BG34_Giant_586)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/5
- 种族: ELEMENTAL
- 触发时机: static_or_aura
- 效果类别: shop_or_refresh, tribe_synergy
- 机制文本: The Tavern offers an extra Frozen Elemental whenever it is Refreshed.
- 中文文本: 每当酒馆刷新时，总会额外提供一个冻结的元素。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Sporebat (BG34_Giant_582)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 9/2
- 种族: BEAST
- 触发时机: deathrattle
- 效果类别: shop_or_refresh, keyword_grant_or_keyword_body, card_generation, tavern_spell_synergy
- 机制文本: Taunt Deathrattle: Get a random Tavern spell that costs (2) or more.
- 中文文本: 嘲讽。亡语：随机获取一张消耗为（2）或以上的酒馆法术牌。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Thorncaller (BG34_Giant_078)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 5/3
- 种族: QUILBOAR
- 触发时机: battlecry, deathrattle
- 效果类别: card_generation, blood_gem
- 机制文本: Battlecry and Deathrattle: Get a Blood Gem Barrage.
- 中文文本: 战吼，亡语：获取一张鲜血宝石弹幕。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Tipper (BG34_Giant_604)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/8
- 种族: NONE
- 触发时机: end_of_turn
- 效果类别: economy
- 机制文本: If you have any unspent Gold at the end of your turn, increase your maximum Gold by 1.
- 中文文本: 在你的回合结束时，如果你有未花费的铸币，你的铸币上限提高1枚。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Traveler (BG34_Giant_605)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 4/8
- 种族: NONE
- 触发时机: avenge
- 效果类别: card_generation
- 机制文本: Avenge (4): Get a random 1-Cost card from the Minor Timewarp.
- 中文文本: 复仇（4）：随机获取一张来自小型时空扭曲的消耗为1的牌。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Vaelastrasz (BG34_Giant_585)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/6
- 种族: DRAGON
- 触发时机: rally
- 效果类别: card_generation, tribe_synergy
- 机制文本: Rally: Get a random Dragon.
- 中文文本: 进击：随机获取一张龙牌。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Whelp Smuggler (BG34_Giant_064)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 2
- 身材: 3/8
- 种族: NONE
- 触发时机: static_or_aura
- 效果类别: stats
- 机制文本: Whenever a friendly minion gains Attack, give it +{1} Health.
- 中文文本: 每当一个友方随从获得攻击力，使其获得+{1}生命值。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Winner (BG34_Giant_039)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/6
- 种族: NONE
- 触发时机: start_of_turn
- 效果类别: keyword_grant_or_keyword_body, card_generation
- 机制文本: Stealth At the start of your turn, if this minion survived last combat, get a Triple Reward.
- 中文文本: 潜行。在你的回合开始时，如果本随从在上一场战斗中存活下来，获取一份 三连奖励。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Zerus (BG34_Giant_671)

- 状态: 当前 Firestone 池
- 分档: minor / techLevel 3
- 成本: 1
- 身材: 6/6
- 种族: NONE
- 触发时机: static_or_aura
- 效果类别: stats, transform
- 机制文本: Once per turn, choose from 2 Minor Timewarped minions to transform into. Keep this minion's stats.
- 中文文本: 每回合一次：从2个来自小型时空扭曲的随从中选择一个，变形成为该随从，并保留本随从的属性值。
- 实现备注: 需要生成新 InstanceId，避免复用源实例。

## Timewarped Anub'arak (BG34_PreMadeChamp_083)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 12/8
- 种族: UNDEAD
- 触发时机: recruit_phase_reactive
- 效果类别: stats, tribe_synergy
- 机制文本: After you play an Undead, your Undead have an extra +3 Attack this game.
- 中文文本: 在你使用一张亡灵牌后，在本局对战中，你的亡灵额外拥有+3攻击力。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Archimonde (BG34_Giant_596)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 5/5
- 种族: DEMON
- 触发时机: damage_reactive
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy, damage
- 机制文本: After your hero takes damage, rewind it and reduce the Cost of your next Tavern spell by (1).
- 中文文本: 在你的英雄受到伤害后，回溯该伤害并使你下一个酒馆法术消耗的铸币减少（1）枚。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Astrogill (BG34_Giant_801)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 5/5
- 种族: MURLOC
- 触发时机: hand_state
- 效果类别: stats, tribe_synergy
- 机制文本: While this is in your hand, after a different friendly Murloc gains stats, gain +3/+2.
- 中文文本: 当本牌在你手牌中时，在一个不同的友方鱼人获得属性值后，获得+3/+2。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Bandit (BG34_PreMadeChamp_078)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 7/13
- 种族: QUILBOAR
- 触发时机: start_of_turn
- 效果类别: card_generation, blood_gem
- 机制文本: At the start of your turn, discard a spell for this to play 4 Blood Gems on all your minions.
- 中文文本: 在你的回合开始时，弃掉一张法术牌，以使本随从对你的所有随从各使用4张鲜血宝石。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Behemoth (BG34_Giant_777)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 9/11
- 种族: ELEMENTAL
- 触发时机: recruit_phase_reactive
- 效果类别: stats, keyword_grant_or_keyword_body, tribe_synergy
- 机制文本: Taunt After you buy an Elemental, gain its stats.
- 中文文本: 嘲讽。在你购买一个元素后，获得其 属性值。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Bloodbinder (BG34_PreMadeChamp_076)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 12/8
- 种族: QUILBOAR
- 触发时机: start_of_turn
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy, blood_gem
- 机制文本: At the start of your turn, get 5 Blood Gems. They also count as Tavern spells.
- 中文文本: 在你的回合开始时，获取5张视为酒馆法术的鲜血宝石。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Bonker (BG34_Giant_102)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 7/14
- 种族: QUILBOAR
- 触发时机: rally
- 效果类别: keyword_grant_or_keyword_body, blood_gem
- 机制文本: Windfury Rally: This plays 2 permanent Blood Gems on all your other minions.
- 中文文本: 风怒。进击：本随从对你的所有其他随从各使用2张永久的鲜血宝石。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Calligrapher (BG34_PreMadeChamp_091)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 12/14
- 种族: DEMON
- 触发时机: battlecry, deathrattle, rally
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy
- 机制文本: Battlecry, Deathrattle, and Rally: Get a random Tavern spell.
- 中文文本: 战吼，亡语，进击：随机获取一张酒馆法术牌。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Caretaker (BG34_Giant_618)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 5/5
- 种族: UNDEAD
- 触发时机: deathrattle
- 效果类别: stats, summon, tribe_synergy
- 机制文本: Deathrattle: Summon five 1/1 Skeletons. Any that don't fit give your Undead +1 Attack this game (wherever they are).
- 中文文本: 亡语：召唤五个1/1的骷髅。每有一个放不下的骷髅，使你的亡灵在本局对战中 获得+1攻击力（无论它们在哪）。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Centurion (BG34_PreMadeChamp_200)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 8/8
- 种族: DRAGON
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy, copy
- 机制文本: After you cast a Tavern spell, get an extra copy of it. (3 times per turn.)
- 中文文本: 在你施放一个酒馆法术后，额外获取它的一张复制。（每回合限3次。）
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要生成新 InstanceId，避免复用源实例。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Chameleon (BG34_Giant_042)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 6/15
- 种族: BEAST
- 触发时机: start_of_combat
- 效果类别: card_generation, transform, copy
- 机制文本: Start of Combat: Transform into a copy of the minion to the left of this.
- 中文文本: 战斗开始时：变形成为本随从左边的随从的复制。
- 实现备注: 需要生成新 InstanceId，避免复用源实例。；可接入现有关键字触发分发。

## Timewarped Clefthoof (BG34_PreMadeChamp_090)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 1/10
- 种族: BEAST
- 触发时机: end_of_turn
- 效果类别: stats, damage, tribe_synergy
- 机制文本: At the end of your turn, give your Beasts +2/+2 and deal 1 damage to them, three times.
- 中文文本: 在你的回合结束时，使你的野兽获得+2/+2并对它们造成1点伤害，触发三次。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Collector (BG34_Giant_680)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 12/12
- 种族: PIRATE
- 触发时机: rally
- 效果类别: economy, keyword_grant_or_keyword_body, damage
- 机制文本: Also damages adjacent minions. Rally: If you control 4 Golden minions, gain Divine Shield.
- 中文文本: 同时对其攻击目标相邻的随从造成伤害。进击：如果你控制着4个金色随从，获得圣盾。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Deadstomper (BG34_Giant_654)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/12
- 种族: UNDEAD, BEAST
- 触发时机: recruit_phase_reactive
- 效果类别: stats, summon
- 机制文本: After you summon a minion, give your minions +4 Attack permanently.
- 中文文本: 在你召唤一个随从后，使你的随从永久获得+4攻击力。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Duskmaw (BG34_PreMadeChamp_020)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 6/14
- 种族: DRAGON
- 触发时机: avenge
- 效果类别: stats, tribe_synergy
- 机制文本: Avenge (1): Give your Dragons +6/+{2}.
- 中文文本: 复仇（1）：使你的龙获得+6/+{2}。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Geist (BG34_Giant_034)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 10/6
- 种族: UNDEAD
- 触发时机: deathrattle
- 效果类别: shop_or_refresh, stats, card_generation, tavern_spell_synergy
- 机制文本: Deathrattle: Your Tavern spells give an extra +2/+2 this game.
- 中文文本: 亡语：在本局对战中，你的酒馆法术使随从额外获得+2/+2。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Gemsplitter (BG34_Giant_644)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 5/10
- 种族: QUILBOAR
- 触发时机: static_or_aura
- 效果类别: stats, keyword_grant_or_keyword_body, blood_gem
- 机制文本: Divine Shield. After a friendly minion loses Divine Shield, your Blood Gems give an extra +1 Attack this game.
- 中文文本: 圣盾。在一个友方随从失去圣盾后，你的鲜血宝石会在本局对战中使随从额外获得+1攻击力。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Ghoul-acabra (BG34_Giant_609)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 6/15
- 种族: UNDEAD, BEAST
- 触发时机: deathrattle
- 效果类别: stats
- 机制文本: After you trigger a Deathrattle, give your minions +3/+2 permanently.
- 中文文本: 在你触发一个亡语后，使你的随从永久获得+3/+2。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Glowscale (BG34_Giant_035)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/12
- 种族: NAGA
- 触发时机: spellcraft
- 效果类别: keyword_grant_or_keyword_body, card_generation, spellcraft
- 机制文本: Taunt Spellcraft: Give a minion Divine Shield.
- 中文文本: 嘲讽。塑造法术：使一个随从获得圣盾。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Hag (BG34_Giant_342)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/8
- 种族: UNDEAD
- 触发时机: start_of_combat
- 效果类别: stats, keyword_grant_or_keyword_body, tribe_synergy
- 机制文本: Start of Combat: Give the Undead to the right Reborn and "This is Reborn with full Health and enchantments".
- 中文文本: 战斗开始时：使本随从右边的亡灵获得复生和“本随从复生时会具有所有生命值和附加效果”。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Hawkstrider (BG34_Giant_370)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 8/4
- 种族: BEAST
- 触发时机: start_of_combat
- 效果类别: special
- 机制文本: Start of Combat: Trigger all friendly Deathrattles.
- 中文文本: 战斗开始时：触发所有友方亡语。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Hooktail (BG34_Giant_015)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 6/12
- 种族: DRAGON, PIRATE
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, stats, card_generation, tavern_spell_synergy
- 机制文本: Whenever you cast a Tavern spell, give your minions +2/+2.
- 中文文本: 每当你施放一个酒馆法术，使你的随从获得+2/+2。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Ichoron (BG34_Giant_040)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 7/4
- 种族: ELEMENTAL
- 触发时机: recruit_phase_reactive
- 效果类别: keyword_grant_or_keyword_body
- 机制文本: Divine Shield Whenever you play a minion, give it Divine Shield.
- 中文文本: 圣盾。每当你使用一张随从牌，使其获得圣盾。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Icky Imp (BG34_Giant_674)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 12/12
- 种族: DEMON
- 触发时机: deathrattle
- 效果类别: stats, summon
- 机制文本: Deathrattle: Summon 2 Imps with this minion's maximum stats.
- 中文文本: 亡语：召唤2个具有本随从最大属性值的 小鬼。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Immortal (BG34_Giant_597)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 8/8
- 种族: MECH
- 触发时机: start_of_combat
- 效果类别: stats
- 机制文本: Start of Combat: Gain the stats of adjacent minions.
- 中文文本: 战斗开始时：获得相邻随从的属性值。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Imp-filtrator (BG34_PreMadeChamp_013)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 13/7
- 种族: DEMON
- 触发时机: static_or_aura
- 效果类别: economy, shop_or_refresh, stats
- 机制文本: After you spend {4} Gold, give minions in the Tavern +{2}/+{3} this game. (8 Gold left!)
- 中文文本: 在你花掉{4}枚铸币后，使酒馆中的随从在本局对战中获得+{2}/+{3}。（还剩8枚！）
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Interpreter (BG34_Giant_120)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 6/8
- 种族: MECH
- 触发时机: recruit_phase_reactive
- 效果类别: stats, tribe_synergy
- 机制文本: Whenever you play or Magnetize a Mech, give your Mechs +3/+3.
- 中文文本: 每当你使用或磁力吸附一个机械时，使你的机械获得+3/+3。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Jungle King (BG34_PreMadeChamp_004)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 8/12
- 种族: BEAST
- 触发时机: recruit_phase_reactive
- 效果类别: stats, keyword_grant_or_keyword_body, summon, card_generation, tribe_synergy
- 机制文本: Stealth After you summon a Beast, give it +4/+3. Improves after you cast a spell.
- 中文文本: 潜行。在你召唤一只野兽后，使其获得+4/+3。在你施放一个法术后提升。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Kil'jaeden (BG34_Giant_313)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 7/7
- 种族: DEMON
- 触发时机: static_or_aura
- 效果类别: shop_or_refresh, stats, tribe_synergy
- 机制文本: The Tavern offers two extra Demons with +7/+{0} whenever it is Refreshed. (Upgrades each turn!)
- 中文文本: 每当酒馆刷新，酒馆额外提供两个具有+7/+{0}的恶魔。（每回合都会升级！）
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Lava Lurker (BG34_Giant_678)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 8/9
- 种族: NAGA
- 触发时机: recruit_phase_reactive, spellcraft
- 效果类别: card_generation, spellcraft
- 机制文本: After you cast a Spellcraft spell from hand on a minion, also cast a permanent copy on this. (Twice per turn.)
- 中文文本: 在你从手牌中对一个随从施放塑造法术的法术后，还会对本随从施放一张永久的复制。（每回合两次。）
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Lil' Quilboar (BG34_Giant_608)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 5/5
- 种族: QUILBOAR
- 触发时机: deathrattle
- 效果类别: keyword_grant_or_keyword_body, blood_gem, tribe_synergy
- 机制文本: Reborn Deathrattle: This plays 3 Blood Gems on all your Quilboar.
- 中文文本: 复生。亡语：本随从对你的所有野猪人各使用3张鲜血宝石。
- 实现备注: 需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Lucky Egg (BG34_Giant_683)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 2/2
- 种族: NONE
- 触发时机: static_or_aura
- 效果类别: economy, transform
- 机制文本: In two turns, choose from three Golden Tier 7 minions to transform into. (2 turns left!)
- 中文文本: 两回合后，从三个等级7的金色随从中选择一个并变形成为该随从。（还剩2回合！）
- 实现备注: 需要生成新 InstanceId，避免复用源实例。

## Timewarped Molten Rock (BG34_Giant_006)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 7/7
- 种族: ELEMENTAL
- 触发时机: recruit_phase_reactive
- 效果类别: stats, tribe_synergy
- 机制文本: After you play an Elemental, gain +1/+1 and improve this.
- 中文文本: 在你使用一张元素牌后，获得+1/+1并提升此效果。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Mrrrglr (BG34_Giant_321)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 8/8
- 种族: MURLOC
- 触发时机: start_of_combat, hand_state
- 效果类别: stats, tribe_synergy
- 机制文本: Start of Combat: Give adjacent Murlocs the stats of all the minions in your hand.
- 中文文本: 战斗开始时：使相邻的鱼人获得你手牌中所有随从牌的属性值。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Murk-Eye (BG34_Giant_318)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 12/5
- 种族: MURLOC
- 触发时机: end_of_turn
- 效果类别: special
- 机制文本: At the end of your turn, trigger all friendly Battlecries.
- 中文文本: 在你的回合结束时，触发所有友方战吼。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Murky (BG34_Giant_206)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 5/5
- 种族: MURLOC
- 触发时机: battlecry, end_of_turn
- 效果类别: stats
- 机制文本: At the end of your turn, gain +2/+2. (Improved by each Battlecry you've triggered this game!)
- 中文文本: 在你的回合结束时，获得+2/+2。（在本局对战中你每触发一个战吼都会提升！）
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Mystic (BG34_Giant_320)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/6
- 种族: MURLOC
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, stats, card_generation, tavern_spell_synergy, tribe_synergy
- 机制文本: After you sell 3 Murlocs, your Tavern spells give an extra +{2}/+{3} this game. (3 left!)
- 中文文本: 在你出售3个鱼人后，在本局对战中，你的酒馆法术使随从额外获得+{2}/+{3}。（还剩3个！）
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Mythrax (BG34_Giant_684)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 8/9
- 种族: ALL
- 触发时机: start_of_combat
- 效果类别: stats
- 机制文本: Start of Combat: Gain the stats of 3 friendly minions of different types (except Timewarped Mythrax).
- 中文文本: 战斗开始时：获得不同类型的3个友方随从的属性值（时空扭曲米斯拉克斯除外）。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Nalaa (BG34_Giant_205)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 12/12
- 种族: NONE
- 触发时机: recruit_phase_reactive
- 效果类别: stats, card_generation
- 机制文本: Whenever you cast a spell, give a friendly minion of each type +4/+3.
- 中文文本: 每当你施放一个法术，使每个类型的各一个友方随从获得+4/+3。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Nest Swarmer (BG34_Giant_687)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 7/7
- 种族: BEAST
- 触发时机: battlecry, deathrattle, rally
- 效果类别: stats, summon
- 机制文本: Battlecry, Deathrattle, and Rally: Your Beetles have +{2}/+{3} this game. Summon a 2/2 Beetle.
- 中文文本: 战吼，亡语，进击：在本局对战中，你的甲虫拥有+{2}/+{3}。召唤一只2/2的甲虫。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Nine Frogs (BG34_Giant_309)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 12/12
- 种族: BEAST
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy
- 机制文本: After you buy a minion, get a random Tavern spell from the same Tier. (9 left!)
- 中文文本: 在你购买随从牌后，随机获取一张相同等级的酒馆法术牌。（还剩9张！）
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Nomi (BG34_Giant_032)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 10/10
- 种族: NONE
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, stats, tribe_synergy
- 机制文本: After you play an Elemental, give minions in the Tavern +4/+3 this game.
- 中文文本: 在你使用一张元素牌后，使酒馆中的随从在本局对战中获得+4/+3。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Overfiend (BG34_PreMadeChamp_011)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 7/13
- 种族: DEMON
- 触发时机: recruit_phase_reactive
- 效果类别: stats, tribe_synergy
- 机制文本: After you buy a card, give your Demons +4/+4.
- 中文文本: 在你购买一张牌后，使你的恶魔获得+4/+4。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Painter (BG34_Giant_319)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 4/9
- 种族: MURLOC
- 触发时机: end_of_turn, recruit_phase_reactive
- 效果类别: stats, card_generation
- 机制文本: At the end of your turn, give adjacent minions +3/+2. After you play a card from Tier 3 or below, improve this.
- 中文文本: 在你的回合结束时，使相邻的随从获得+3/+2。在你使用一张等级3或以下的牌后提升此效果。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Peggy (BG34_Giant_327)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 9/5
- 种族: PIRATE
- 触发时机: static_or_aura
- 效果类别: stats, tribe_synergy
- 机制文本: Whenever a card is added to your hand, give your Pirates +1/+1.
- 中文文本: 每当一张卡牌被置入你的手牌，使你的海盗获得+1/+1。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Pioneer (BG34_Giant_322)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 4/13
- 种族: NAGA
- 触发时机: recruit_phase_reactive, spellcraft
- 效果类别: shop_or_refresh, card_generation, spellcraft
- 机制文本: After you Refresh 3 times, get a random Spellcraft spell. (3 left!)
- 中文文本: 在你刷新3次后，随机获取一张塑造法术的法术牌。(还剩3次！)
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Plunderer (BG34_PreMadeChamp_067)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 15/5
- 种族: PIRATE
- 触发时机: deathrattle
- 效果类别: economy
- 机制文本: Deathrattle: Increase your maximum Gold by 2.
- 中文文本: 亡语：你的铸币上限提高2枚。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Poet (BG34_Giant_314)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/7
- 种族: DRAGON
- 触发时机: static_or_aura
- 效果类别: stats, keyword_grant_or_keyword_body, tribe_synergy
- 机制文本: Divine Shield All your Dragons keep Bonus Keywords and stats gained in combat.
- 中文文本: 圣盾。你的所有龙均可永久保留战斗阶段获得的额外关键词和属性值。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Prismscale (BG34_PreMadeChamp_022)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 8/12
- 种族: DRAGON
- 触发时机: avenge
- 效果类别: card_generation
- 机制文本: Avenge (2): Get an Azerite Empowerment.
- 中文文本: 复仇（2）：获取一张艾泽里特强化。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Promo-Drake (BG34_Giant_088)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 6/6
- 种族: DRAGON
- 触发时机: start_of_combat, end_of_turn
- 效果类别: stats
- 机制文本: Start of Combat: Give your minions +{3}/+{3}. At the end of your turn, improve this.
- 中文文本: 战斗开始时：使你的随从获得+{3}/+{3}。在你的回合结束时，提升此效果。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Radio Star (BG34_Giant_330)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 1/1
- 种族: UNDEAD
- 触发时机: deathrattle
- 效果类别: stats, card_generation, copy
- 机制文本: Deathrattle: Get a copy of the enemy minion that killed this with full Health and enchantments.
- 中文文本: 亡语：获取击杀本随从的敌方随从的一张复制。复制具有所有生命值和附加效果。
- 实现备注: 需要生成新 InstanceId，避免复用源实例。；可接入现有关键字触发分发。

## Timewarped Raider (BG34_PreMadeChamp_065)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 14/6
- 种族: PIRATE
- 触发时机: recruit_phase_reactive
- 效果类别: stats, card_generation, tribe_synergy
- 机制文本: After you play a card from Tier 4 or above, give your Pirates +3/+2.
- 中文文本: 在你使用一张等级4或以上的牌后，使你的海盗获得+3/+2。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Riplash (BG34_Giant_325)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 13/5
- 种族: NAGA
- 触发时机: deathrattle
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy, copy
- 机制文本: Deathrattle: Get a copy of the last Tavern spell you cast.
- 中文文本: 亡语：获取你施放的上一个酒馆法术的一张复制。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要生成新 InstanceId，避免复用源实例。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Scout (BG34_Giant_333)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 7/7
- 种族: NONE
- 触发时机: end_of_turn, recruit_phase_reactive
- 效果类别: card_generation
- 机制文本: When you sell this, get 1 random minions from Tier 7. (Improves each turn!)
- 中文文本: 当你出售本随从时，随机获取1张等级7的随从牌。（每回合都会提升！）
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Sea Glass (BG34_Giant_110)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 10/8
- 种族: ELEMENTAL
- 触发时机: rally
- 效果类别: stats, keyword_grant_or_keyword_body
- 机制文本: Divine Shield Rally: Double this minion's stats. (2 times per combat.)
- 中文文本: 圣盾。进击：本随从的属性值翻倍。（每场战斗限2次。）
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Secretary (BG34_Giant_323)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 5/11
- 种族: NAGA
- 触发时机: recruit_phase_reactive, spellcraft
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy, spellcraft
- 机制文本: After you cast 2 Spellcraft spells, get a random Tavern spell. (2 left!)
- 中文文本: 在你施放2个塑造法术的法术后，随机获取一张酒馆法术牌。（还剩2个！）
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Shivarra (BG34_Giant_311)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 4/2
- 种族: DEMON
- 触发时机: static_or_aura
- 效果类别: stats
- 机制文本: Whenever a minion is consumed, this gains its stats.
- 中文文本: 每当一个随从被吞食，本随从获得其属性值。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Siren (BG34_PreMadeChamp_058)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/14
- 种族: NAGA
- 触发时机: recruit_phase_reactive
- 效果类别: stats, tribe_synergy
- 机制文本: After you play a Naga, give all your Naga +6/+10.
- 中文文本: 在你使用一张纳迦牌后，使你的所有纳迦获得+6/+10。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Squallfin (BG34_PreMadeChamp_049)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 6/14
- 种族: MURLOC
- 触发时机: recruit_phase_reactive, hand_state
- 效果类别: stats, tribe_synergy
- 机制文本: After you play a Murloc, give minions in your hand and board +2/+2.
- 中文文本: 在你使用一张鱼人牌后，使你手牌中和场上的随从获得+2/+2。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Stone Drake (BG34_Giant_675)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/6
- 种族: DRAGON, ELEMENTAL
- 触发时机: start_of_combat
- 效果类别: stats
- 机制文本: Start of Combat: Gain the stats of all the minions you sold this turn. (0/0)
- 中文文本: 战斗开始时：获得你在本回合中出售的所有随从的属性值。（0/0）
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Stormcloud (BG34_PreMadeChamp_031)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 11/9
- 种族: ELEMENTAL
- 触发时机: deathrattle, avenge
- 效果类别: shop_or_refresh, card_generation
- 机制文本: Deathrattle and Avenge (3): Get a Tavern Tempest.
- 中文文本: 亡语，复仇（3）：获取一张酒馆旋风。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；可接入现有关键字触发分发。

## Timewarped Substrate (BG34_PreMadeChamp_032)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 8/8
- 种族: ELEMENTAL
- 触发时机: end_of_turn
- 效果类别: keyword_grant_or_keyword_body, card_generation
- 机制文本: Divine Shield At the end of your turn, get a Temperature Shift.
- 中文文本: 圣盾。在你的回合结束时，获取一张寒热骤变。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Summoner (BG34_Giant_324)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 6/9
- 种族: NAGA, ELEMENTAL
- 触发时机: spellcraft
- 效果类别: shop_or_refresh, card_generation, spellcraft, transform
- 机制文本: Spellcraft: Choose a minion. Transform all minions in the Tavern into ones of its type, keeping Tiers.
- 中文文本: 塑造法术：选择一个随从，将酒馆中的所有随从变形成为原有等级的所选类型的随从。
- 实现备注: 需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要生成新 InstanceId，避免复用源实例。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Swirler (BG34_Giant_686)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 9/9
- 种族: ELEMENTAL
- 触发时机: static_or_aura
- 效果类别: stats, tribe_synergy
- 机制文本: Your Elementals give an extra +3/+3 this game.
- 中文文本: 在本局对战中，你的元素使随从额外获得+3/+3。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Tamuzo (BG34_Giant_595)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 5/5
- 种族: BEAST
- 触发时机: recruit_phase_reactive
- 效果类别: stats, summon
- 机制文本: After you summon a minion in combat, double its stats.
- 中文文本: 在战斗中，在你召唤一个随从后，使其属性值翻倍。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Tide Razor (BG34_Giant_328)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 12/8
- 种族: NONE
- 触发时机: deathrattle
- 效果类别: summon, card_generation, tribe_synergy
- 机制文本: Deathrattle: Summon and get 4 random Pirates.
- 中文文本: 亡语：召唤并获取4个随机海盗。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Trumpeter (BG34_Giant_676)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 7/8
- 种族: ELEMENTAL
- 触发时机: recruit_phase_reactive
- 效果类别: card_generation, tribe_synergy
- 机制文本: After you sell 5 Elementals, get a random Elemental. (5 left!)
- 中文文本: 在你出售5个元素后，随机获取一张元素牌。（还剩5个！）
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Wargear (BG34_Giant_677)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 5/5
- 种族: MECH
- 触发时机: static_or_aura
- 效果类别: stats, keyword_grant_or_keyword_body
- 机制文本: Magnetic After you Magnetize this, double the target's stats.
- 中文文本: 磁力。在你磁力吸附本随从后，目标的属性值翻倍。
- 实现备注: 按牌面文本实现，无额外跨系统依赖。

## Timewarped Warghoul (BG34_Giant_331)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 1
- 身材: 9/3
- 种族: UNDEAD
- 触发时机: deathrattle
- 效果类别: keyword_grant_or_keyword_body
- 机制文本: Taunt. Deathrattle: Trigger an adjacent minion's Deathrattle (except Timewarped Warghoul).
- 中文文本: 嘲讽。亡语：触发一个相邻随从的亡语（时空扭曲战争食尸鬼除外）。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Whirl-O-Tron (BG34_Giant_599)

- 状态: 当前 Firestone 池
- 分档: major / techLevel 5
- 成本: 2
- 身材: 7/5
- 种族: MECH
- 触发时机: start_of_combat
- 效果类别: card_generation
- 机制文本: Start of Combat: Copy your two left-most Deathrattles (except other Whirl-O-Trons).
- 中文文本: 战斗开始时：复制你最左边的两个亡语（其他飓风机甲的除外）。
- 实现备注: 可接入现有关键字触发分发。

## Timewarped Amalgam (BG34_Giant_336)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 7/9
- 种族: ALL
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, stats
- 机制文本: After you play a minion, give minions of its type in the Tavern +4/+4 this game.
- 中文文本: 在你使用一张随从牌后，在本局对战中使酒馆中该类型的随从获得+4/+4。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Arm (BG34_Giant_027)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 8/8
- 种族: NONE
- 触发时机: damage_reactive
- 效果类别: stats
- 机制文本: Whenever a friendly minion is attacked, give it +8 Attack permanently.
- 中文文本: 每当一个友方随从受到攻击时，使其永久获得+8攻击力。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Bristler (BG34_Giant_104)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 6/6
- 种族: QUILBOAR
- 触发时机: deathrattle
- 效果类别: blood_gem, tribe_synergy
- 机制文本: Deathrattle: Give this minion's Blood Gems to 2 different friendly Quilboar.
- 中文文本: 亡语：使2个不同的友方野猪人获得本随从的鲜血宝石。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。；可接入现有关键字触发分发。

## Timewarped Deios (BG34_Giant_376)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 6/10
- 种族: NONE
- 触发时机: static_or_aura
- 效果类别: special
- 机制文本: Your Battlecries, Deathrattles, and Rallies trigger twice.
- 中文文本: 你的战吼，亡语和进击会触发两次。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Electron (BG34_Giant_610)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 9/9
- 种族: MECH
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, card_generation, tribe_synergy
- 机制文本: After you cast 2 Tavern spells, Magnetize a {2}/{3} Satellite to all your Mechs. (2 left!)
- 中文文本: 在你施放2个酒馆法术后，为你的所有机械磁力吸附一个{2}/{3}的卫星。（还剩2个！）
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Elegist (BG34_Giant_310)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 3/5
- 种族: MURLOC
- 触发时机: end_of_turn, hand_state
- 效果类别: stats
- 机制文本: At the end of your turn, give minions in your hand and board +2/+1.
- 中文文本: 在你的回合结束时，使你手牌中和场上的随从获得+2/+1。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Expeditioner (BG34_Giant_317)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 6/12
- 种族: MURLOC
- 触发时机: hand_state
- 效果类别: stats, keyword_grant_or_keyword_body
- 机制文本: Taunt, Divine Shield. After this gains stats, also give the stats to the two left-most minions in your hand.
- 中文文本: 嘲讽。圣盾。在本随从获得属性值后，还会使你手牌中最左边的两张随从牌获得属性值。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Goldrinn (BG34_Giant_362)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 6/6
- 种族: BEAST
- 触发时机: deathrattle
- 效果类别: stats, tribe_synergy
- 机制文本: Deathrattle: Your Beasts have +4/+4 this game (wherever they are).
- 中文文本: 亡语：在本局对战中，你的野兽拥有+4/+4（无论它们在哪）。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Grease Bot (BG34_Giant_656)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 6/12
- 种族: MECH
- 触发时机: static_or_aura
- 效果类别: stats, keyword_grant_or_keyword_body
- 机制文本: Divine Shield. After a friendly minion loses Divine Shield, give your minions +3/+3 permanently.
- 中文文本: 圣盾。在一个友方随从失去圣盾后，使你的随从永久获得+3/+3。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Guard (BG34_Giant_068)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 5/10
- 种族: MECH
- 触发时机: rally
- 效果类别: keyword_grant_or_keyword_body
- 机制文本: Divine Shield Rally: Give a different friendly minion Divine Shield permanently.
- 中文文本: 圣盾。进击：使一个不同的友方随从永久获得圣盾。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Hunter (BG34_Giant_588)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 8/5
- 种族: MECH
- 触发时机: battlecry, deathrattle
- 效果类别: card_generation
- 机制文本: Battlecry and Deathrattle: Get a Pointy Arrow.
- 中文文本: 战吼，亡语：获取一张尖利箭矢。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Jelly Belly (BG34_Giant_024)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 5/6
- 种族: UNDEAD
- 触发时机: damage_reactive
- 效果类别: stats, keyword_grant_or_keyword_body
- 机制文本: After a friendly minion is Reborn, give your minions +2/+2 permanently.
- 中文文本: 在一个友方随从复生后，使你的随从永久获得+2/+2。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Karathress (BG34_PreMadeChamp_056)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 14/6
- 种族: NAGA
- 触发时机: recruit_phase_reactive
- 效果类别: summon, card_generation, copy
- 机制文本: After you summon a minion in combat, get a copy of Deep Blues.
- 中文文本: 在战斗中，在你召唤一个随从后，获取深沉蓝调的一张复制。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要生成新 InstanceId，避免复用源实例。

## Timewarped Lab Rat (BG34_PreMadeChamp_002)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 12/8
- 种族: BEAST
- 触发时机: recruit_phase_reactive
- 效果类别: stats, card_generation, tribe_synergy
- 机制文本: After you cast a spell, give your Beasts +2/+2.
- 中文文本: 在你施放一个法术后，使你的野兽获得+2/+2。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Low-Flier (BG34_Giant_065)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 10/10
- 种族: DRAGON
- 触发时机: end_of_turn
- 效果类别: stats
- 机制文本: At the end of your turn, give +2 Attack to your minions with less Attack than this. Repeat with Health.
- 中文文本: 在你的回合结束时，使你攻击力低于本随从的随从获得+2攻击力。然后依此法检定并获得生命值。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Magnanimoose (BG34_Giant_619)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 8/2
- 种族: BEAST
- 触发时机: deathrattle
- 效果类别: summon, card_generation
- 机制文本: Deathrattle: Summon and get a minion from a random opponent's warband.
- 中文文本: 亡语：召唤并获取来自一个随机对手的战队的一个随从。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Paleofin (BG34_PreMadeChamp_047)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 2/18
- 种族: MURLOC
- 触发时机: end_of_turn
- 效果类别: card_generation
- 机制文本: At the end of your turn, get a Cloning Conch.
- 中文文本: 在你的回合结束时，获取一张克隆螺号。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Probius (BG34_Giant_121)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 12/7
- 种族: MECH
- 触发时机: static_or_aura
- 效果类别: economy, keyword_grant_or_keyword_body, tribe_synergy
- 机制文本: Magnetic After you Magnetize this to a Mech, make it Golden.
- 中文文本: 磁力。在你将本随从磁力吸附到机械上后，将目标机械变为 金色。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Relaxer (BG34_Giant_002)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 3/4
- 种族: QUILBOAR
- 触发时机: recruit_phase_reactive
- 效果类别: blood_gem, tribe_synergy
- 机制文本: After you sell a Quilboar, this plays 4 Blood Gems on a random friendly minion.
- 中文文本: 在你出售一个野猪人后，本随从随机对一个友方随从使用4张鲜血宝石。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Seer (BG34_Giant_008)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 8/8
- 种族: DEMON, NAGA
- 触发时机: static_or_aura
- 效果类别: shop_or_refresh, card_generation, tavern_spell_synergy
- 机制文本: Two Tavern spells each turn cost (2) less. (2 left!)
- 中文文本: 每回合中，有两张酒馆法术的消耗减少（2）。（还剩2张！）
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Shadequill (BG34_Giant_681)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 7/11
- 种族: QUILBOAR
- 触发时机: end_of_turn
- 效果类别: shop_or_refresh, stats
- 机制文本: At the end of your turn, gain the stats of the 3 highest-Health minions in the Tavern.
- 中文文本: 在你的回合结束时，获取酒馆中生命值最高的3个随从的属性值。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Spirit of Air (BG34_Giant_592)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 5/3
- 种族: ELEMENTAL
- 触发时机: deathrattle
- 效果类别: keyword_grant_or_keyword_body
- 机制文本: Deathrattle: Give a random friendly minion Windfury, Divine Shield, and Taunt.
- 中文文本: 亡语：随机使一个友方随从获得风怒，圣盾和嘲讽。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Steamer (BG34_PreMadeChamp_038)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 13/7
- 种族: MECH
- 触发时机: end_of_turn
- 效果类别: keyword_grant_or_keyword_body, card_generation
- 机制文本: At the end of your turn, get one of each Magnetic Volumizer.
- 中文文本: 在你的回合结束时，获取每种磁力扩音机各一张。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Stoneshell (BG34_Giant_601)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 4/8
- 种族: NONE
- 触发时机: start_of_combat
- 效果类别: card_generation
- 机制文本: Start of Combat: Copy all friendly Rallies (except other Stoneshells).
- 中文文本: 战斗开始时：复制所有友方进击效果（其他石壳守卫的除外）。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Sylvar (BG34_Giant_021)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 7/10
- 种族: PIRATE
- 触发时机: end_of_turn
- 效果类别: economy, stats
- 机制文本: At the end of your turn, give adjacent minions +8/+8. Repeat for each friendly Golden minion.
- 中文文本: 在你的回合结束时，使相邻的随从获得+8/+8。每有一个友方金色随从，重复一次。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Tender (BG34_Giant_603)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 7/5
- 种族: NONE
- 触发时机: end_of_turn
- 效果类别: shop_or_refresh, stats, card_generation
- 机制文本: At the end of your turn, get 2 random Tavern spells that give stats.
- 中文文本: 在你的回合结束时， 随机获取2张能使随从获得属性值的酒馆法术牌。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Theotar (BG34_Giant_335)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 8/8
- 种族: ALL
- 触发时机: recruit_phase_reactive
- 效果类别: stats
- 机制文本: After you play a minion with no type, give a friendly minion of each type +6/+6.
- 中文文本: 在你使用没有类型的随从牌后，使每个类型的各一个友方随从获得+6/+6。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。

## Timewarped Tony (BG34_Giant_326)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 12/6
- 种族: PIRATE
- 触发时机: deathrattle
- 效果类别: card_generation, copy
- 机制文本: Deathrattle: Get a copy of Eyes of the Earth Mother.
- 中文文本: 亡语：获取大地母亲之眼的一张复制。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要生成新 InstanceId，避免复用源实例。；可接入现有关键字触发分发。

## Timewarped Trickster (BG34_Giant_010)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 8/8
- 种族: DEMON
- 触发时机: deathrattle
- 效果类别: stats
- 机制文本: Deathrattle: Give this minion's maximum stats to another friendly minion.
- 中文文本: 亡语：使另一个友方随从获得本随从的最大属性值。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Twirler (BG34_Giant_105)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 2
- 身材: 7/5
- 种族: QUILBOAR
- 触发时机: recruit_phase_reactive
- 效果类别: blood_gem
- 机制文本: After you play a Blood Gem on this, cast Blood Gem Barrage.
- 中文文本: 在你对本随从使用一张鲜血宝石后，施放鲜血宝石弹幕。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要复用现有鲜血宝石、酒馆法术或塑造法术管线。

## Timewarped Ultralisk (BG34_Treasure_994)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 8/8
- 种族: NONE
- 触发时机: start_of_combat
- 效果类别: stats, damage
- 机制文本: Also damages adjacent minions. Start of Combat: Double this minion's stats.
- 中文文本: 同时对其攻击目标相邻的随从造成伤害。战斗开始时：本随从的属性值翻倍。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；可接入现有关键字触发分发。

## Timewarped Upstart (BG34_Giant_361)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 4/7
- 种族: ELEMENTAL
- 触发时机: recruit_phase_reactive
- 效果类别: shop_or_refresh, stats
- 机制文本: After the Tavern is Refreshed, double the Health of its right-most minion.
- 中文文本: 在酒馆刷新后，使酒馆中最右边的随从生命值翻倍。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。；需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。

## Timewarped Viper (BG34_Treasure_990)

- 状态: 历史/上线版本额外池
- 分档: unknown / techLevel 0
- 成本: 1
- 身材: 8/8
- 种族: NONE
- 触发时机: static_or_aura
- 效果类别: stats, keyword_grant_or_keyword_body
- 机制文本: Venomous Immune while attacking.
- 中文文本: 烈毒。攻击时免疫。
- 实现备注: 非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。
