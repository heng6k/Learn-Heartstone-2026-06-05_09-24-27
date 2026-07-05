# Hero Power and Buddy Effects Implementation Order

## 目的

本文档把 `HeroPowerBuddyEffectsImplementationPlan.md` 中的推荐顺序落实到具体英雄和对应宝宝。后续实现时按本文档从上到下推进：先补可见性和测试基础，再继续低风险酒馆/经济效果，最后进入需要新框架的战斗、发现、选择、奖励和暂缓系统。

## 排序原则

- 先实现已经有运行时钩子的效果：买牌、卖牌、刷新、升级酒馆、回合开始/结束、使用英雄技能。
- 同一英雄和对应宝宝作为一个实现单元，避免英雄效果可用但宝宝效果长期缺失。
- 每个阶段都必须配套 EditMode 测试，至少覆盖英雄基础效果、宝宝在场效果、宝宝不在场时不触发。
- 复杂系统先补上下文或状态框架，再实现具体英雄，避免在单个英雄里硬编码临时逻辑。
- 不支持或暂缓的效果必须显式登记，不能静默 no-op。

## 状态标记

| 状态 | 含义 |
| --- | --- |
| Done | 已实现并已有针对性测试或测试用例已添加 |
| Next | 下一批建议实现 |
| Planned | 已排序，等待对应阶段 |
| Framework First | 先补公共框架，再实现英雄/宝宝 |
| Deferred | 暂缓，依赖更大的系统 |

## Phase 0: 基础可见性和验证

| 顺序 | 项目 | 状态 | 后续动作 |
| --- | --- | --- | --- |
| 0.1 | Hero/buddy implementation status registry | Next | 增加一个按 cardId 登记的状态表或调试输出，区分 Implemented、Unsupported、Deferred |
| 0.2 | Unsupported no-op visibility | Next | 所有暂未实现的英雄技能/宝宝效果应可在日志、调试 UI 或测试审计中看到 |
| 0.3 | Unity EditMode targeted test run | Next | 等 Unity 项目锁释放后运行 `HeroPowerBuddyEffectTests` |
| 0.4 | Existing plan cross-link | Next | 从 `HeroPowerBuddyEffectsImplementationPlan.md` 链接到本文档 |

## Phase 1: 已完成基线

这些组合已经作为第一批或经济扩展批实现。后续只需要回归测试和补状态登记。

| 顺序 | 英雄 | 宝宝 | 状态 | 已覆盖重点 |
| --- | --- | --- | --- | --- |
| 1.1 | Patchwerk | Weebomination | Done | 初始血量、回合结束左侧随从生命值增强 |
| 1.2 | Forest Lord Cenarius | Malorne | Done | 主动技能增加最大铸币、宝宝按花费累计成长 |
| 1.3 | Nozdormu | Chromie | Done | 每回合免费刷新、宝宝增强酒馆随从 |
| 1.4 | Kael'thas Sunstrider | Crimson Hand Centurion | Done | 第三个购买随从给 Tavern Coin、宝宝获得触发随从属性 |
| 1.5 | Exarch Othaar | The Celestial Archive | Done | 回合开始法术折扣、宝宝复制 0 费酒馆法术 |
| 1.6 | Tae'thelan Bloodwatcher | Reliquary Attendant | Done | 每第四个酒馆法术变 0 费、宝宝每回合复制首个施放法术 |
| 1.7 | Varden Dawngrasp | Varden's Aquarrior | Done | 刷新复制最高等级酒馆随从并冻结、宝宝增强复制体 |
| 1.8 | Millhouse Manastorm | Magnus Manastorm | Done | 随从/刷新/升级费用调整、宝宝每回合前两次刷新免费 |
| 1.9 | Trade Prince Gallywix | Bilgewater Mogul | Done | 卖随从储存下回合铸币、宝宝回合结束提升最大铸币 |

## Phase 2: 下一批低风险酒馆/经济组合

这一批优先做，因为主要依赖已有 `UpgradeTavern`、`CardBought`、`ShopRefreshed`、`TurnEnded` 等钩子。

| 顺序 | 英雄 | 宝宝 | 状态 | 主要钩子 | 验收测试 |
| --- | --- | --- | --- | --- | --- |
| 2.1 | Forest Warden Omu | Evergreen Botani | Done | UpgradeTavern, TurnEnded | 升级酒馆返还 2 铸币；宝宝在场时回合结束添加符合酒馆等级的随机随从 |
| 2.2 | Cap'n Hoggarr | Shining Sailor | Done | CardBought, ShopRefreshed | 购买 Pirate 后获得 1 铸币；宝宝在场刷新时额外注入 Pirate |
| 2.3 | Ysera | Valithria Dreamwalker | Done | ShopRefreshed, CardPlayed/shop enter | 刷新保证酒馆出现 Dragon；宝宝根据场上 Dragon 数量获得 +1/+1 |

## Phase 3: 购买/刷新/出售计数和酒馆状态组合

这一批仍在酒馆阶段内，但需要更仔细的每回合计数、临时光环或按种族替换。

| 顺序 | 英雄 | 宝宝 | 状态 | 主要钩子 | 实现注意 |
| --- | --- | --- | --- | --- | --- |
| 3.1 | Enhance-o Mechano | Enhance-o Medico | Done | ShopRefreshed, CardBought | 刷新时给随机酒馆随从随机关键词；买到带额外关键词的随从后宝宝成长 |
| 3.2 | Kurtrus Ashfallen | Living Nightmare | Done | CardBought, TurnStarted/TurnEnded | 每回合买 3 个随从后给一个普通复制；宝宝让本回合买牌后的酒馆随从获得 +2/+2 |
| 3.3 | Fungalmancer Flurgl | Sparkfin Soothsayer | Done | MinionSold, CardPlayed | 卖 5 个随从给随机 Murloc；宝宝战吼把酒馆随从变成同等级 Murloc |
| 3.4 | Overlord Saurfang | Dranosh Saurfang | Done | CardBought, Shop aura | 买 4 个随从后改善酒馆增益；宝宝获得被购买随从的一半属性 |
| 3.5 | Edwin VanCleef | SI:7 Scout | Done | HeroPowerUsed, CardBought | 主动技能按购买次数成长；宝宝在买牌后获得 +2/+2 |

## Phase 4: 主动、目标选择、每回合限制组合

这一批大多可复用 `UseHeroPower`，但需要更完整的目标校验、冷却、一次性或每回合状态。

| 顺序 | 英雄 | 宝宝 | 状态 | 依赖 |
| --- | --- | --- | --- | --- |
| 4.1 | Skycap'n Kragg | Sharkbait | Done | 主动技能可用回合与一次性奖励状态 |
| 4.2 | George the Fallen | Karl the Lost | Done | 目标随从选择和 Divine Shield 赋予 |
| 4.3 | Farseer Nobundo | None | Done | 主动技能阶段性升级或选项状态 |
| 4.4 | Doctor Holli'dae | The Nine Frogs | Done | Tavern spell reward and repeat/copy behavior |
| 4.5 | Death Speaker Blackthorn | Death's Head Sage | Done | Blood Gem 发放和宝宝增强 |
| 4.6 | Lich Baz'hial | Unearthed Underling | Done | 血量支付、金币/铸币奖励、宝宝成长 |
| 4.7 | Rakanishu | Lantern Tender | Done | 按酒馆等级目标增益 |
| 4.8 | Reno Jackson | Sr. Tomb Diver | Done | 目标随从金色化、一次性使用状态 |
| 4.9 | Patches the Pirate | Tuskarr Raider | Done | Pirate 获取、费用递减或计数 |
| 4.10 | King Mukla | Crazy Monkey | Done | Banana 发放、宝宝额外香蕉 |
| 4.11 | C'Thun | Tentacle of C'Thun | Done | 回合结束/主动分配增益 |
| 4.12 | Captain Eudora | Dagwik Stickytoe | Done | 挖掘计数和奖励 |
| 4.13 | Elise Starseeker | Jr. Navigator | Done | 招募图/发现奖励 |
| 4.14 | Millificent Manastorm | Elementium Squirrel Bomb | Done | Mech 增益和召唤/亡语相关状态 |
| 4.15 | The Lich King | Arfus | Done | Temporary Reborn target and Arfus Attack follow-up implemented. |
| 4.16 | Shudderwock | Muckslinger | Framework First | Muckslinger reward implemented; Snicker-snack records a visible Battlecry replay proxy until Battlecry replay is public. |
| 4.17 | Jandice Barov | Jandice's Apprentice | Done | Friendly/Tavern minion swap and repeat-play board buff implemented. |
| 4.18 | Mutanus the Devourer | Nightmare Ectoplasm | Done | Devour sell/stat spit and Nightmare extra target implemented. |
| 4.19 | Xyrella | Baby Elekk | Done | Tavern minion 2/2 hand pickup and Baby Elekk scaling buff implemented. |
| 4.20 | Pyramad | Titanic Guardian | Done | Random Tavern steal, Health doubling, and hero-effect Health gain sync implemented. |
| 4.21 | Vol'jin | Master Gadrin | Framework First | One-target plus random partner Spirit Swap proxy implemented; true two-target command and start-of-combat buddy hook still needed. |
| 4.22 | Inge, the Iron Hymn | Solemn Serenader | Done | Alternating Attack/Health hero power and Serenader enhancement implemented. |
| 4.23 | Malygos | Nexus Lord | Done | Twice-per-turn replacement and Nexus one-tier-higher replacement implemented. |
| 4.24 | Maiev Shadowsong | Shadow Warden | Done | Existing hand lock counters reused for Imprison; Shadow Warden Golden next target implemented. |
| 4.25 | Zephrys, the Great | Phyresz | Done | Three Wishes third-copy lookup and Phyresz singleton plain-copy Discover implemented through sell proxy. |
| 4.26 | Captain Hooktusk | Raging Contender | Done | Remove target, lower-tier Discover, and Tier-based Gold gain implemented. |
| 4.27 | Rock Master Voone | Akali, Rock Rhino | Done | Three-turn and two-turn left-most hand copy counters implemented. |
| 4.28 | Zerek, Master Cloner | Mini-Zerek | Done | Once-per-game exact board copy and Mini-Zerek Tavern copy proxy implemented. |
| 4.29 | Heistbaron Togwaggle | Waxadred, the Drippy | Done | Tavern steal and discount implemented; Waxadred now reads last-opponent warband history before falling back to current opponent board. |
| 4.30 | Chenvaala | Snow Elemental | Done | Third Elemental upgrade discount and extra Frozen Elemental refresh injection implemented. |
| 4.31 | The Curator | Mishmash | Done | Match-start Amalgam and Mishmash stat sync implemented. |
| 4.32 | Dancin' Deryl | Asher the Haberdasher | Done | Hat gain/pass behavior and Asher extra hats implemented through sell events. |
| 4.33 | Ragnaros the Firelord | Lucifron | Done | Buy-16 Sulfuras unlock and Lucifron repeated end-turn buff implemented. |
| 4.34 | Time Twister Chromie | None | Done | Tavern refresh converts offered slots into Tavern spells. |
| 4.35 | Sindragosa | Thawed Champion | Framework First | Minions cost 2, end-turn freeze, and Golden frozen-minion proxy implemented; exact smaller shop and per-card Frozen state still need shop-slot freezing support. |

## Phase 5: 战斗开始、召唤、击杀追踪组合

这一阶段必须先补战斗上下文。不要直接在单个英雄里绕过 `CombatEngine`。

| 顺序 | 英雄 | 宝宝 | 状态 | 先决条件 |
| --- | --- | --- | --- | --- |
| 5.0 | Combat hero effect context | Framework First | Implemented | 已增加英雄战斗开始上下文；攻击、击杀、通用死亡、通用召唤仍需 CombatEngine 内部事件 |
| 5.1 | Al'Akir | Spirit of Air | Framework First | 战斗开始关键词已实现；宝宝亡语暂用售出代理 |
| 5.2 | Y'Shaarj | Baby Y'Shaarj | Implemented | 战斗开始同等级召唤、入手复制、宝宝同等级召唤 +1/+1 已覆盖英雄效果召唤和 CombatEngine 内部召唤 |
| 5.3 | Deathwing | Sinestra | Implemented | 双方战斗副本 +2 攻击、友方永久 +2 攻击和 Sinestra +1 生命已实现；对手永久属性写回按单人训练器边界删除 |
| 5.4 | Illidan Stormrage | Eclipsion Illidari | Framework First | 边位 +2/+1、正常战斗开始前立即攻击、宝宝一次攻击免疫已实现；通用友方攻击计数与更完整英雄触发排序仍待后续框架 |
| 5.5 | Queen Wagtoggle | Elder Taggawag | Implemented | 多种族战斗开始增益和 Elder 四种族收益已实现 |
| 5.6 | N'Zoth | Baby N'Zoth | Framework First | 开局鱼和 Baby N'Zoth 战吼已实现；鱼收集亡语待亡语转移支持 |
| 5.7 | Vanndar Stormpike | Stormpike Lieutenant | Implemented | 7 回合后最高生命复制和宝宝右侧 +10 生命已实现 |
| 5.8 | Drek'Thar | Frostwolf Lieutenant | Implemented | 7 回合后最高攻击复制和宝宝左侧 +10 攻击已实现 |
| 5.9 | Tavish Stormpike | Crabby | Implemented | Deadeye 目标记录、战斗开始伤害/移除、有空位直接发射/结算和 Crabby 普通复制已实现；UI polish 后续跟进 |
| 5.10 | Tamsin Roame | Monstrosity | Implemented | Fragrant Phylactery 战斗开始亡语挂载、属性广播和 Monstrosity 友方死亡成长已实现 |
| 5.11 | Teron Gorefiend | Shadowy Construct | Framework First | 目标标记、战斗开始摧毁/召回代理、Shadowy Construct 收益已实现；死亡时序待战斗死亡钩子 |
| 5.12 | Arch-Villain Rafaam | Loyal Henchman | Framework First | 待击杀归属与第一/第二击杀复制奖励 |
| 5.13 | Rokara | Icesnarl the Mighty | Framework First | 待击杀归属事件和永久属性写回 |
| 5.14 | Sylvanas Windrunner | Nathanos Blightcaller | Framework First | Nathanos 战吼已实现；最近战斗死亡快照已有，英雄技能仍待接入死亡历史 Discover |
| 5.15 | Sneed | Piloted Whirl-O-Tron | Framework First | 开局 Shredder 已实现；手牌召唤亡语和亡语复制待支持 |
| 5.16 | The Jailer | Mawsworn Soulkeeper | Framework First | Runic Empowerment 已实现；宝宝亡语暂用售出代理 |
| 5.17 | Greybough | Wandering Treant | Framework First | 英雄效果和 CombatEngine 内部战斗召唤会获得 +1/+2 Taunt；Wandering Treant 的 Taunt 被攻击触发待攻击钩子 |
| 5.18 | Onyxia | Many Whelps | Implemented | 英雄级 Avenge(4)、Whelp 召唤、立即攻击队列和 Many Whelps 成长已实现 |
| 5.19 | Ini Stormcoil | Sub Scrubber | Framework First | Sub Scrubber Mech 打出成长已实现；MechGyver 待战斗死亡计数奖励 |
| 5.20 | Ozumat | Tamuzo | Framework First | 触手召唤、售出/战斗死亡成长和 Tamuzo 对英雄效果及 CombatEngine 内部战斗召唤翻倍已实现 |
| 5.21 | Aranna Starseeker | Sklibb, Demon Hunter | Framework First | Sklibb 刷新额外高等级随从已实现；Aranna 友方攻击解锁待攻击计数 |
| 5.22 | Lord Jaraxxus | Kil'rek | Framework First | Kil'rek 亡语售出代理已实现；Bloodfury 待战斗伤害累计和传送门奖励 |
| 5.23 | Bru'kan | Spirit Raptor | Implemented | 四元素选择、战斗开始元素调用和 Spirit Raptor 记忆/亡语重放已实现 |

## Phase 6: 发现、选择、奖励框架组合

这一阶段必须先扩展 `DiscoverState` 或等价选择状态，保证 UI/命令层能表达候选项、选择、延迟奖励和一次性选项。

| 顺序 | 英雄 | 宝宝 | 状态 | 先决条件 |
| --- | --- | --- | --- | --- |
| 6.0 | Discover/choice/reward framework | Framework First | Implemented | Added DiscoverChosen dispatch, target-backed Discover replacement, minion/hero-power/buddy Discover helpers |
| 6.1 | Silas Darkmoon | Burth | Implemented | Darkmoon Ticket 计数和发现奖励；Burth 发现后强化并成长 |
| 6.2 | Cookie the Cook | Sous Chef | Implemented | 投喂酒馆/友方随从，记录种族，三次后按种族发现；Sous Chef 每回合额外一次 |
| 6.3 | Galakrond | Galakrond's Apostle | Implemented | 目标酒馆随从后发现更高等级替换；Apostle 战吼升级酒馆随从 |
| 6.4 | E.T.C., Band Manager | Talent Scout | Implemented | Tier 2 后发现真实 Buddy；Talent Scout 战吼使 Buddy 金色 |
| 6.5 | Sir Finley Mrrgglton | Maxwell, Mighty Steed | Implemented | 开局发现英雄技能；Maxwell 出售获得当前英雄技能对应 Buddy |
| 6.6 | Murloc Holmes | Watfin | Implemented | 单人对手快照竞猜、正确奖励 Tavern Coin；Watfin 正确后获得普通复制 |
| 6.7 | Thorim, Stormlord | Veranus | Implemented | 开局合法 Tier 7 发现、60 金币延迟发放；Veranus 回合结束变形左侧随从，最高 Tier 7 |
| 6.8 | Snake Eyes | Box Cars | Implemented | 掷骰、冷却和回合开始酒馆法术发现 |
| 6.9 | Galewing | Flight Trainer | Implemented | 已接入飞行路径选择、延迟完成、不连续重复选择、官方三航线奖励和 Flight Trainer 双触发 |
| 6.10 | Cariel Roame | Captain Fairmount | Implemented | Conviction 主动随机 buff、战斗后改进选择和 Captain Fairmount 回合结束随机改进 |
| 6.11 | Infinite Toki | Clockwork Assistant | Implemented | Temporal Tavern 在可替换普通随从槽最右侧放入两个当前剩余池的高一等级普通随从，冻结牌按类型补齐；动态注入当前池的 Timewarped/Oathstone 随从进入通用随从候选；Clockwork Assistant 战吼从当前剩余池和 BuddyPool 发现高一等级随从，受当前 6/7 本上限截断 |
| 6.12 | Mr. Bigglesworth | Lil' K.T. | Framework First | 已淘汰玩家战队快照发现和 Lil' K.T. 单人最低血量代理已实现；真实大厅淘汰/血量排序待补 |
| 6.13 | Ambassador Faelin | Submersible Chef | Implemented | 首回合限制、开局 6/4/2 发现并按等级延迟获得；Submersible Chef 战吼给 1/3/5 级随从 |
| 6.14 | Guff Runetotem | Baby Kodo | Implemented | 购买卡牌累计酒馆等级，满 20 点给三连奖励；Baby Kodo 战吼刷新酒馆并尽量包含每个可用等级的随从 |
| 6.15 | The Rat King | Rat King buddy | Implemented | 回合开始种族轮换、当前类型发现；Pigeon Lord 在酒馆缺当前类型时给免费刷新 |
| 6.16 | Alexstrasza | Vaelastrasz | Implemented | 升到 4 级酒馆时触发 Dragon 发现；Vaelastrasz Rally 获得随机龙 |
| 6.17 | Sire Denathrius | Shady Aristocrat | Implemented | 复用 Quest/Reward 系统：开局二选一任务，Shady Aristocrat 出售发现 Bonus Quest，完成后获得 8 金币 Coin Pouch |
| 6.18 | Tickatus | Ticket Collector | Implemented | 每 4 回合暗月奖品发现和 Ticket Collector 出售发现下一等级奖品已接入共享 Darkmoon Prize 目录；33 张本地暗月奖品效果均已实现 |
| 6.19 | Master Nguyen | Lei Flamepaw | Implemented | 回合开始二选一临时英雄技能；选择后本回合替换并在回合结束恢复；Lei Flamepaw 按所选临时英雄技能映射 Buddy |
| 6.20 | Scabbs Cutterbutter | Warden Thelwater | Implemented | 按已接受的单人模拟对手代理完成：下个对手战队普通复制发现和下个对手 Buddy |
| 6.21 | A. F. Kay | Snack Vendor | Implemented | 前两回合限制主要操作，第 3 回合发放 3/4 级发现；Snack Vendor 回合结束转移属性 |
| 6.22 | Loh, the Living Legend | Stoneshell Guardian | Implemented | 本体友方攻击计数和 Triple Reward 已接；Stoneshell Guardian 每回合第一个 Triple Reward 改为从金色随从发现 |
| 6.23 | Dinotamer Brann | Brann's Epic Egg | Implemented | 本体 4 个战吼随从购买给 Brann Bronzebeard 已接；Brann's Epic Egg 亡语召唤并获得当前酒馆等级上限内的随机 Battlecry minion，金色翻倍 |
| 6.24 | Yogg-Saron, Hope's End | Acolyte of Yogg-Saron | Implemented | Puzzle Box 从第 3 回合起自动施放合法随机 Tavern spell；Acolyte 使用项目认可的五格 Wheel 表 |
| 6.25 | Queen Azshara | Imperial Defender | Implemented | 30 攻开启 Naga Conquest；Imperial Defender 每回合一次复制对另一个友方随从施放的 Spellcraft |
| 6.26 | Lady Vashj | Coilfang Elite | Implemented | 每回合获得不高于当前酒馆等级的临时 Spellcraft；Coilfang Elite 复制酒馆 Spellcraft 随从法术 |
| 6.27 | Lord Barov | Barov's Apprentice | Implemented | 战斗胜负下注选择、正确后 Tavern Coin、Coin 触发 Gold 奖励 |

## Phase 7: 明确暂缓系统

这些组合不建议在当前酒馆/基础战斗框架内硬做。先保留为 Deferred，并在状态登记中说明缺失系统。

| 英雄 | 宝宝 | 状态 | 暂缓原因 |
| --- | --- | --- | --- |
| Marin the Manager | Fantastic Bellhop | FrameworkFirst | Fantastic Bellhop 回合结束给 helpful card；Trinket 系统仍暂缓 |
| Buttons | Zippers | FrameworkFirst | Zippers 用 Tavern death proxy 给 helpful card；Trinket 系统仍暂缓 |
| Mister Clocksworth | None | Implemented | 两张合金和 Tavern Coin 奖励替换已接入 Triple 规则 |
| Morchie | None | Implemented | Turn 5 打开 Minor Timewarped Tavern |
| Murozond, Unbounded | None | Implemented | Turn 8 打开 Major Timewarped Tavern；previous-warband 属于 Timewarp 牌/数据底座 |
| Genn, Worgen King | None | Implemented | Cosmic Duality 下禁用；非 Cosmic Duality Turn 4 发现两个英雄技能替换 |
| The Great Akazamzarak | Street Magician | FrameworkFirst | Street Magician 生成 Better Secret proxy；Secret 系统仍暂缓 |
| Professor Putricide | Festergut | Implemented | Build-An-Undead 两次三选一组件 Discover 已接；身材/关键词/效果叠加，第二段过滤重复关键词；Festergut 复用同一 Putricide's Creation factory。 |
| Jim Raynor | Tychus | Implemented | 开局战巡、刷新/宝宝官方升级池、升级施放和核心战斗钩子已完成 |
| Artanis | Probius | Implemented | Warp Gate 二选一、买 14 张牌奖励、Protoss 核心钩子和 Probius 磁力变金已完成 |
| Kerrigan, Queen of Blades | Broken Horn | Implemented | 幼虫、解锁/费用递减、Zerg morph、Broken Horn 真实 6/6 非变异 Zerg 已完成 |
| Tess Greymane | Hunter of Old | FrameworkFirst | Tess 刷新上次对手战队普通复制，Hunter of Old 回合开始获取上次对手 Buddy；真实多对手排程仍暂缓 |
| Duos-only hero powers/buddies | Various | Deferred | 当前项目范围是单人 Battlegrounds Tavern |

## 每批实现的完成标准

- 英雄技能和对应宝宝效果都使用 cardId 作为逻辑键。
- 宝宝效果只在对应宝宝位于玩家战队时触发。
- 所有永久计数写入 `TavernState.HeroEffectCounters` 或更明确的状态结构。
- 回合内临时效果必须在 `TurnStarted` 或 `TurnEnded` 清理。
- 每个组合至少有一个正向测试和一个宝宝不在场的负向测试；涉及费用时要测试铸币不足。
- 新增暂缓项必须同步到状态登记或文档表格。

## 建议下一步

1. 先做 Phase 0.1 和 0.2，补实现状态登记，避免后续新增英雄时遗漏可见性。
2. 等 Unity 锁释放后运行现有 `HeroPowerBuddyEffectTests`，确认 Phase 1 没有回归。
3. 实现 Phase 2.1 `Forest Warden Omu / Evergreen Botani`，这是下一步风险最低且能验证升级酒馆和回合结束奖励的组合。
4. 接着完成 Phase 2.2 和 2.3，再进入 Phase 3 的计数类效果。
