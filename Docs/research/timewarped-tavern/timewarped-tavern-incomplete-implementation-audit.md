# Timewarped Tavern Incomplete Implementation Audit

日期：2026-06-28

## 审计范围

本次只做记录，不修改卡牌效果。审计目标是找出扭曲时空酒馆中仍可能缺实现、使用代理/近似实现、或缺少直接验证的牌，方便后续逐项修。

审计口径：

- 当前池 125 张随从和补充的 38 张非随从 Timewarped 牌。
- 历史/上线额外池 33 张只按开启历史池后的可用性审计；默认不进当前池不是问题。
- `BG34_BlackMarket_Skip` 是工具退出牌，不按未完成卡统计。

数据文件存在 mojibake/非标准 JSON 内容，PowerShell `ConvertFrom-Json` 不可靠。本次用行块切分和卡牌 ID 文本命中检查：

- 数据：`Assets/LearnHearthstone/Resources/Data/timewarpedTavernCards.json`
- 实现：`MatchService.cs`、`CombatEngine.cs`、`TavernSpellEngine.cs`
- 测试：`MatchServiceTests.cs`、`DomainEngineTests.cs`
- 机制文档：`Docs/research/timewarped-tavern/timewarped-minion-mechanisms.md`

## 总结

- 非随从安全阻塞桩已清空：`BlockedNonMinions.Count == 0`。
- 当前池发现 9 张疑似真实漏实现随从：这些牌有非静态触发效果，但卡牌 ID 没有运行时代码命中；其中 Winner 只有静态关键词测试。
- 12 张“第二英雄技能”非随从牌能写入 `ExtraHeroPowerCardIds`，但使用/显示入口仍不完整。
- Big Winner 已不再使用 Bounty proxy，但 Tier 3 Darkmoon Prize 只有部分奖品有 focused tests。
- 历史额外池大多仍是候选数据：33 张里只有 Deios、Upstart 有直接 targeted tests。
- 全量默认 EditMode 仍是操作性缺口，应继续用 `Tools/run-editmode-bisect.ps1` 定位，不建议盲跑旧全量方式。

## P0 当前池疑似漏实现

这些牌在当前默认候选池内；不是历史池问题。优先补实现和 focused EditMode。

| Card ID | 名称 | 问题 | 证据 | 建议修改入口 |
| --- | --- | --- | --- | --- |
| `BG34_Giant_201` | Timewarped Boar | 每第 3 个友方 Boar 死亡给随机 Golden Beast，未见运行时分支。 | 数据行 181；机制文档行 68；运行时/测试卡牌 ID 无命中。 | 在战斗死亡事件记录友方 Boar 死亡计数，第三次时生成随机 Golden Beast 到手牌或奖励区，并补第三次死亡测试。 |
| `BG34_Giant_039` | Timewarped Winner | 开始回合若上场战斗存活则给 Triple Reward；目前只测了 Stealth/Trigger 静态关键词。 | 数据行 1852；机制文档行 692；运行时卡牌 ID 无命中。 | 在战斗结束记录是否存活，下一回合开始发三连奖励；补死亡/存活两条测试。 |
| `BG34_Giant_598` | Timewarped Mothership | Avenge (4) 获取随机 Protoss 随从，未见实现。 | 数据行 942；机制文档行 354；运行时/测试卡牌 ID 无命中。 | 扩展 Avenge 分发，补 Protoss 随从选择器或本地候选 fallback，并测第 4 次死亡触发。 |
| `BG34_Giant_678` | Timewarped Lava Lurker | Spellcraft 从手牌施放到随从后，在本体永久复制，且每回合 2 次限制；未见实现。 | 数据行 2945；机制文档行 1095；运行时/测试卡牌 ID 无命中。 | 挂 Spellcraft cast-from-hand 事件，复制永久效果到本体，记录每回合次数。 |
| `BG34_Giant_309` | Timewarped Nine Frogs | 买随从后获取同 Tavern Tier 随机 Tavern Spell，9 次计数；未见实现。 | 数据行 3328；机制文档行 1238；运行时/测试卡牌 ID 无命中。 | 在买随从事件按被买随从 Tier 生成 Tavern Spell，递减/记录 9 次计数。 |
| `BG34_Giant_333` | Timewarped Scout | 出售时获取 Tier 7 随从，数量每回合提升；未见实现。 | 数据行 3786；机制文档行 1407；运行时/测试卡牌 ID 无命中。 | 在出售事件处理 Scout，回合开始或结束提升奖励数量，补售出测试。 |
| `BG34_Giant_323` | Timewarped Secretary | 每施放 2 个 Spellcraft 获取随机 Tavern Spell；未见实现。 | 数据行 3854；机制文档行 1433；运行时/测试卡牌 ID 无命中。 | 复用 Spellcraft 计数器，第二次触发生成 Tavern Spell 并重置。 |
| `BG34_Giant_676` | Timewarped Trumpeter | 出售 5 个 Elemental 后获取随机 Elemental；未见实现。 | 数据行 4240；机制文档行 1576；运行时/测试卡牌 ID 无命中。 | 在出售事件统计 Elemental，满 5 后生成 Elemental 并重置。 |
| `BG34_Giant_599` | Timewarped Whirl-O-Tron | 战斗开始复制最左两个 Deathrattle，排除其他 Whirl-O-Tron；未见实现。 | 数据行 4342；机制文档行 1615；运行时/测试卡牌 ID 无命中。 | 在 combat start 复制可触发 Deathrattle 的效果列表或标记，并补排除同名牌测试。 |

备注：`BG34_Giant_007` Timewarped Annoy-o-Tron 和 `BG34_Giant_012` Timewarped Cyclone 也没有运行时 ID 分支，但它们是静态关键词体且已有关键词测试，不列为缺实现。

## P1 可玩但边界未完整

| 范围 | 牌 | 当前状态 | 建议 |
| --- | --- | --- | --- |
| 第二英雄技能 | `BG34_HeroPowerSpell_003`, `_005`, `_006`, `_008`, `_009`, `_010`, `_012`, `_015`, `_016`, `_017`, `_018`, `_022` | 购买后写入 `ExtraHeroPowerCardIds`，测试只确认不会替换主英雄技能；`UseHeroPower` 仍通过 `State.Player.HeroPowerCardId` 分发，UI 搜索未发现第二英雄技能选择入口。 | 增加命令参数或独立命令来选择使用哪个英雄技能；UI 显示额外英雄技能按钮；补第二技能实际使用测试。 |
| 上一局战队快照 | `BG34_Treasure_902` Master Thief, `BG34_Treasure_966` Thief | 使用 `OpponentHistoryState.LastPlayerWarband`，来源是当前 match 上一次战斗开始时的玩家战队快照，不是真正“上一局游戏”。 | 若项目有跨局历史存档，改接真实 previous game warband；否则在 UI/日志/文档标记为本地快照近似。 |
| 本地生成 Evolving Tavern | `BG34_Treasure_900` Timewarped Evolving Tavern | 给的是本地生成 `TIMEWARPED_EVOLVING_TAVERN_SPELL`，因为本地 Tavern Spell catalog 没有官方条目。 | 后续补官方 Evolving Tavern spell 数据后替换生成牌；验证 cost/tier/art/文本与官方一致。 |
| Darkmoon Prize 覆盖 | `BG34_Treasure_606` Big Winner 派生的 `BGS_Treasures_*` | 8 张 Tier 3 奖品都有 playable 分支；focused tests 只覆盖 Big Winner Discover、Holy Light、B.A.N.A.N.A.S.、Reserve Prices。 | 给 `BGS_Treasures_011` Training Session、`020` Top Shelf、`034` Repeat Customer、`037` All That Glitters、`039` Mindflayer Goggles 补直接测试。 |

## P1 历史额外池

历史额外池默认不开是设计要求；问题是开启后多数牌仍缺逐卡实现证据。

| 状态 | 卡牌 |
| --- | --- |
| 有 targeted tests | `BG34_Giant_376` Timewarped Deios；`BG34_Giant_361` Timewarped Upstart |
| 有运行时代码命中但缺 focused tests | `BG34_Giant_310` Timewarped Elegist；`BG34_Giant_362` Timewarped Goldrinn；`BG34_Giant_588` Timewarped Hunter；`BG34_PreMadeChamp_047` Timewarped Paleofin |
| 无运行时代码/测试命中，按 data-only 候选处理 | `BG34_Giant_336` Amalgam；`BG34_Giant_027` Arm；`BG34_Giant_104` Bristler；`BG34_Giant_610` Electron；`BG34_Giant_317` Expeditioner；`BG34_Giant_656` Grease Bot；`BG34_Giant_068` Guard；`BG34_Giant_024` Jelly Belly；`BG34_PreMadeChamp_056` Karathress；`BG34_PreMadeChamp_002` Lab Rat；`BG34_Giant_065` Low-Flier；`BG34_Giant_619` Magnanimoose；`BG34_Giant_121` Probius；`BG34_Giant_002` Relaxer；`BG34_Giant_008` Seer；`BG34_Giant_681` Shadequill；`BG34_Giant_592` Spirit of Air；`BG34_PreMadeChamp_038` Steamer；`BG34_Giant_601` Stoneshell；`BG34_Giant_021` Sylvar；`BG34_Giant_603` Tender；`BG34_Giant_335` Theotar；`BG34_Giant_326` Tony；`BG34_Giant_010` Trickster；`BG34_Giant_105` Twirler；`BG34_Treasure_994` Ultralisk；`BG34_Treasure_990` Viper |

建议：历史池若要产品化，先逐卡决定是否进入候选池；没有实现的继续保留在历史开关后，并加显式 `implementation_status:blocked_historical_effect` 或等价标签，避免误以为已完成。

## P2 数据和验证问题

- `timewarpedTavernCards.json` 中 158 张卡全部仍带 `implementation_status:data_only` 标签。这个标签已经不能表达真实实现状态，建议改成更细的 `implemented`, `coverage_gap`, `historical_data_only`, `proxy_generated`, `integration_gap`。
- `Docs/research/timewarped-tavern/timewarped-tavern-remaining-completion-plan.md` 仍有旧句子说 Big Winner 使用 `darkmoon_prize_proxy` / Bounty proxy；实现和 status 文档已经更新，计划文档需要标记为已完成或修订。
- 全量默认 EditMode 之前出现过长时间停在 `test run started` 的问题。后续验证请用 `Tools/run-editmode-bisect.ps1` 分片/二分；Stress/Marathon 不放默认全量。

## 推荐修复顺序

1. 先补 P0 9 张当前池疑似漏实现卡，并给每张加 focused EditMode。
2. 接第二英雄技能使用入口，让 12 张 HeroPowerSpell 从“存储可见”变成“可选择使用”。
3. 补 Darkmoon Prize 5 张未测奖品的 focused tests。
4. 决定历史额外池产品化范围；未做逐卡实现的继续显式阻塞。
5. 清理数据状态标签和 stale 文档，再用 bisect 工具处理全量 EditMode 稳定性。
