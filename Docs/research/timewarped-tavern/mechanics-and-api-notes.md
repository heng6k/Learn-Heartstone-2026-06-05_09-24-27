# 扭曲时空酒馆机制与 API 调研

## 关键结论

- 当前 Firestone 页面对应的扭曲时空随从池可以从 Firestone 静态卡牌数据复现，过滤条件是 `type == "Minion" && premium != true && mechanics contains "BACON_TIMEWARPED" && isBaconPool == true`。来源: https://www.firestoneapp.com/battlegrounds/cards?time=all-time&type=timewarped 和 https://static.firestoneapp.com/data/cards/cards_enUS.gz.json
- 当前池共 125 个普通随从，其中 Minor 为 `techLevel = 3`，共 55 个；Major 为 `techLevel = 5`，共 70 个。完整结果见 `timewarped-tavern-research.json`。
- Firestone 静态数据里还有 33 个非当前池扭曲时空普通随从，`isBaconPool` 不为 `true` 且 `techLevel = 0`。这些更适合当作上线版本/历史池候选，不应默认进入当前版本。来源: https://static.firestoneapp.com/data/cards/cards_enUS.gz.json
- 图片可按 `https://static.zerotoheroes.com/hearthstone/cardart/256x/{cardId}.jpg` 拉取；本次已下载当前池 125 张、全 Firestone 扭曲时空随从 158 张、历史额外池 33 张，失败 0 张。来源: Firestone bundle 中的 ZeroToHeroes cardart 路径和实际下载验证。
- Blizzard 官方 Hearthstone Game Data API 应作为卡牌基础字段、图片和本地化文本的权威校验源，但需要 Battle.net OAuth token；当前仓库 `.env` 没有 Blizzard client 变量，所以本轮没有做 authenticated API 拉取。官方文档: https://community.developer.battle.net/documentation/hearthstone/game-data-apis 和 https://community.developer.battle.net/documentation/battle-net/oauth-apis

## 已生成文件

- `Tools/scrape-timewarped-tavern.mjs`: 可复现抓取脚本。
- `Docs/research/timewarped-tavern/timewarped-tavern-research.json`: 结构化结果，含当前池、全 Firestone 扭曲时空池、HearthstoneJSON fallback。
- `Docs/research/timewarped-tavern/timewarped-tavern-research.md`: 当前池和全池 Markdown 表。
- `Docs/research/timewarped-tavern/timewarped-tavern-system-mechanics.md`: 扭曲时空酒馆总机制文档，含触发回合、Chronum、饰品同回合顺序、普通酒馆边界、状态机和测试口径。
- `Docs/research/timewarped-tavern/timewarped-tavern-production-plan.md`: 扭曲时空酒馆与卡牌效果的详细制作顺序计划，含数据、运行时、UI、测试和卡牌机制批次。
- `Docs/research/timewarped-tavern/timewarped-minion-mechanisms.json`: 158 个扭曲时空随从的逐随从机制结构化清单。
- `Docs/research/timewarped-tavern/timewarped-minion-mechanisms.md`: 158 个扭曲时空随从的逐随从机制 Markdown 清单。
- `Docs/research/timewarped-tavern/images-current/*.jpg`: 当前池 125 张卡图。
- `Docs/research/timewarped-tavern/images-all/*.jpg`: 全 Firestone 扭曲时空随从 158 张卡图。
- `Docs/research/timewarped-tavern/images-historical-extra/*.jpg`: 历史/上线版本额外池 33 张卡图。
- `Docs/research/timewarped-tavern/image-download-failures.json`: 图片失败列表，当前三类均为空数组。

## 数据源判断

### Firestone 当前池

Firestone 静态卡牌对象有这些关键字段:

- `mechanics` 包含 `BACON_TIMEWARPED`: 标记扭曲时空卡。
- `isBaconPool == true`: 当前酒馆战棋池内卡。
- `premium == true`: 金色卡，应该用 `battlegroundsNormalDbfId` 回指普通卡，不作为普通池条目。
- `battlegroundsPremiumDbfId`: 普通卡指向金色卡。
- `techLevel`: 对扭曲时空随从可映射为 Minor/Major，当前数据中 Minor 为 3，Major 为 5。
- `cost`: Timewarped Tavern 独立购买成本。当前随从中 76 张成本 1，49 张成本 2。

### 历史/上线版本池

本轮用两个来源辅助识别历史候选:

- Firestone 静态数据中 `mechanics contains BACON_TIMEWARPED` 但 `isBaconPool != true` 的普通随从，共 33 张。
- HearthstoneJSON 中名称或文本包含 `Timewarped` 的普通非金随从，共 159 条。来源: https://api.hearthstonejson.com/v1/latest/enUS/cards.json

这两类适合用来对照营地“时空扭曲”筛选结果，但默认实现当前版本时应优先用 Firestone 当前池条件。

## 机制草案

### 触发回合

Firestone 数据中的系统牌 `BG34_BlackMarket` 名为 `Timewarped Tavern System`，文本为 “On Turns 6 and 9, visit the Timewarped Tavern.”，并带有 `TAG_SCRIPT_DATA_NUM_1 = 6`。来源: https://static.firestoneapp.com/data/cards/cards_enUS.gz.json

实现建议:

- 第 6 回合进入 Minor Timewarped Tavern，只从 `techLevel = 3` 的当前池卡和对应 Minor 宝藏/法术中生成。
- 第 9 回合进入 Major Timewarped Tavern，只从 `techLevel = 5` 的当前池卡和对应 Major 宝藏/法术中生成。
- Timewarped Tavern 购买使用独立成本字段 `cost`，不要走普通随从 3 金购买逻辑。
- Timewarped Tavern 卡应默认视为复制/特殊来源，不消耗普通随从池副本，除非后续确认官方也扣池。
- `BG34_BlackMarket_Skip` 是 `Exit the Timewarped Tavern`，文本说明可保存 Chronum 到下一次 Timewarp；因此需要保存未花完的 Timewarped Tavern 货币。

### 和饰品的同回合时序

用户指定规则: 如果饰品和扭曲时空在同一回合，先饰品后扭曲时空酒馆。

当前工程中 `NextTurn()` 的相关顺序是:

- 回合结束触发、刷新新回合商店、设置金币。
- `DispatchTrinketTurnStarted()`
- `MaybeOfferScheduledTrinketChoice()`
- `AddSpellcraftFromBoard()`
- `DispatchBoardEvent(TurnStarted)`
- `DispatchHeroEffect(TurnStarted)`
- `DispatchQuestRewardTurnStarted()`
- `HandleTurnStartedForTierThreeMinions()`

代码参考: `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`

实现建议:

- 第 6/9 回合先执行现有饰品 turn-start 和 scheduled trinket choice。
- 如果饰品选择产生了 `AdvancedMechanics.PendingChoice`，不要覆盖它。记录 `timewarp:pending-round = State.Round` 和 `timewarp:pending-kind = minor/major`，等饰品 `ChooseMechanicOption()` 清空 pending 后再打开 Timewarped Tavern。
- 如果没有饰品 pending，立即打开 Timewarped Tavern。
- 长期更稳的做法是把 `AdvancedMechanicState.PendingChoice` 升级为队列；短期可用 `Counters` 暂存 Timewarp due 状态。

### 和普通酒馆的关系

- Timewarped Tavern 是特殊访问，不应直接替换普通 `RefreshShopFromPoolPreservingFrozen()` 的结果，除非 UI 明确进入 Timewarp 面板。
- 进入 Timewarped Tavern 时应保存当前普通商店，退出后恢复或保持普通商店不变。
- Timewarped Tavern 的“刷新/购买”不应触发普通酒馆刷新相关效果，除非卡牌文本写明 `Refresh` 或 `Casts When Bought`。
- 带 `Casts When Bought` 的 Timewarped spell 买入即施放，不进手牌。

### 和异常 `Oathstone's Summoning` 的关系

`BG34_Anomaly_805` 文本是 “Minor Timewarped minions enter the Tavern pool on Turn 7, and Major ones on Turn 10.” 来源: https://static.firestoneapp.com/data/cards/cards_enUS.gz.json

这不是第 6/9 回合的 Timewarped Tavern 访问，而是把 Timewarped 随从注入普通酒馆池的异常规则。实现时应拆开:

- Timewarped Tavern visit: 第 6/9 回合的特殊商店。
- Oathstone anomaly: 第 7/10 回合把 Minor/Major Timewarped minions 加入普通刷新池。

## 官方 API 接入结论

官方 Battle.net Hearthstone Game Data API 适合做这些事:

- `GET /hearthstone/metadata`: 获取官方分类、关键字、种族等元数据。
- `GET /hearthstone/cards?gameMode=battlegrounds&locale=en_US&pageSize=500`: 拉取酒馆战棋卡牌基础数据。
- `GET /hearthstone/cards/{id-or-slug}?gameMode=battlegrounds&locale=zh_CN`: 单卡校验本地化文本和图片。

限制:

- 需要 OAuth client credentials token。官方 OAuth 文档: https://community.developer.battle.net/documentation/battle-net/oauth-apis
- 官方 API 是否暴露 `BACON_TIMEWARPED`、`isBaconPool` 这类 Firestone 静态数据字段，本轮无法在无 token 状态下确认。
- 当前工程应将官方 API 作为基础字段校验源，将 Firestone/ZeroToHeroes 作为 Timewarped 当前池和图片抓取源，将 HearthstoneJSON 作为离线 fallback 和历史池 diff 源。

## 待确认项

- 每次 Timewarp 初始 Chronum 数量，以及未花完 Chronum 的精确结转规则。
- 每次 Timewarped Tavern 展示多少格、是否固定含 `Exit the Timewarped Tavern`。
- Timewarped Tavern 是否按当前禁用种族过滤。第一版建议按 `TribeAvailabilityRules` 过滤，`ALL` 和 `NONE` 永远可用。
- Timewarped spell 是否也要进入第一版，还是先只做随从购买。完整体验应包含 spell，因为 Firestone 数据中有 38 张带 `BACON_TIMEWARPED` 的非随从牌。

## Confidence

Medium-high. 当前池、图片 URL、Firestone 字段和工程时序都已本地验证；官方 API authenticated response 和 Chronum 细则仍需后续用 Battle.net client credentials 或官方补丁说明确认。
