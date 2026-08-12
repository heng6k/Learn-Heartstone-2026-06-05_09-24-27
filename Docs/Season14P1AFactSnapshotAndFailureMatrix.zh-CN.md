# Season 14 P1-A：事实快照与失败矩阵

## 1. 结论

P1-A 已完成“先冻结事实和正确行为，再修改运行时”的准备工作。本阶段没有修改任何运行时代码。

- 事实快照：`Docs/data/season14-p1a-facts-20260806.json`
- 前置契约测试：`Assets/LearnHearthstone/Tests/EditMode/Mechanics/Season14P1AContractTests.cs`
- 目标版本：`36.2-preview`
- Ruleset：`ruleset-36.2-preview-v1`
- 当前 resolver fingerprint：`f7034ea5890544b7a04b9edd29ed5f809cc0411a0ff89c18554b3673c8c02184`
- 版本状态：`Partial`；不能升为 `Verified`

事实快照含 8 个来源和 25 条事实。每条事实均记录 `sourceLevel`、`revisionId`、`contentFingerprint`、获取时间、当前行为、36.2 目标与旧版本基线。

## 2. 来源与裁决

来源优先级遵循实施文档：正式客户端/API、正式补丁、官方赛季预览、社区交叉核对、单次观察。

- [Blizzard Season 14 公告](https://hearthstone.blizzard.com/en-us/news/24290433/announcing-battlegrounds-season-14-dark-gifts-of-dalaran)：赛季身份、黑暗之赐、新英雄和内容方向。
- [Blizzard 36.2 随从更新](https://us.forums.blizzard.com/en/hearthstone/t/battlegrounds-season-14-minion-updates/163700)：夺金健将新版的正式预览事实。
- [Blizzard Season 14 饰品更新](https://us.forums.blizzard.com/en/hearthstone/t/battlegrounds-season-14-trinket-updates/163710)：相关饰品文本与触发阶段。
- [Blizzard Season 14 酒馆法术更新](https://us.forums.blizzard.com/en/hearthstone/t/battlegrounds-season-14-spell-updates/163891)：酒馆法术事实。
- [Blizzard 夺金健将卡牌库](https://hearthstone.blizzard.com/en-us/battlegrounds/119996-aureate-laureate/)：仍显示旧 1/1 战吼形态，只作为历史基线，不能覆盖 36.2 目标。
- [Battle.net Hearthstone Game Data API 文档](https://community.developer.battle.net/documentation/hearthstone/game-data-apis)：官方 API 契约；没有可追溯 BG36 payload 时不擅自补外部 cardId。
- [营地 36.2 英雄整理](https://www.iyingdi.com/tz/post/5675745)：六名旧英雄调整和护甲表；当前只标记 `CommunityObserved/Preview`。

夺金健将的 36.2 目标冻结为：1 本海盗、2/2、圣盾、始终金色、不提供三连奖励；金色定义仍为 2/2 且没有战吼。旧版本继续保持 1/1、战吼使自身变金。

两名新英雄当前仅冻结为 Partial：萨维斯 12 甲、特莱斯塔斯 10 甲，已有英雄图和行为框架，但英雄/技能 DBF 仍为 0，技能图为空。护甲数据也尚未建立可区分 `SoloLow`、`SoloHigh` 与双打的正式 profile。

六名旧英雄的 36.2 覆盖已进入 Preview 清单，但没有写入运行时：

| 英雄 | 旧版本基线 | 36.2 Preview 目标 | 来源等级 |
| --- | --- | --- | --- |
| 艾德温 | 购买 5 张后提升 | 购买 4 张后提升 | CommunityObserved |
| 拉卡尼休 | 1 费获得 Lantern Light | 酒馆法术额外 +1/+1；每 3 回合提升 | CommunityObserved |
| 凯瑞尔 | 技能 1 费 | 技能 0 费 | CommunityObserved |
| 拉格纳罗斯 | 购买 16 张后解锁 | 购买 12 张后解锁 | CommunityObserved |
| 萨鲁法尔 | 购买 4 个随从后提升 | 购买 3 个后提升 | CommunityObserved |
| 强化机器人 | 每次刷新触发一次 | 每次刷新独立触发两次 | CommunityObserved |

## 3. 新增契约测试结果

运行 fixture：`LearnHearthstone.Tests.EditMode.Season14P1AContractTests`

P1-C 完成后的稳定结果为 7 项完成，5 项通过、2 项按设计失败。剩余失败仍是 P1-A 的交付物，后续 P1-D/P1-F 应逐项将其转绿，不能删除断言。

### 已通过

| 测试 | 证明内容 |
| --- | --- |
| `FactSnapshot_BindsEveryRecordToResolvedSeason14FingerprintAndReviewMetadata` | 事实文件可解析；来源、日期、revision 完整；25 条事实均绑定当前 resolver fingerprint |
| `ChooseOneCombinedTag_AllianceFlagResolvesBothBranchesOnce` | Alliance Flag 只消耗一次，从 1/1 正确变为 5/5，两项按当前统一解析器结算 |
| `TribeSelection_ManualSixthChoiceRemainsSelectableAndCanContinue` | P1-B 已转绿：手选第 6 个仍可交互，摘要为 6/10，并可原样进入启动参数 |
| `Lockbox_TurnEndUsesStrongestDrakkariOccurrenceCount(False,3)` | P1-C 已转绿：普通达卡莱产生两个独立、幂等的 `TurnEnded` occurrence，宝箱 5→3 |
| `Lockbox_TurnEndUsesStrongestDrakkariOccurrenceCount(True,2)` | P1-C 已转绿：金色达卡莱产生三个独立、幂等的 `TurnEnded` occurrence，宝箱 5→2 |

### 预期失败

| 后续阶段 | 测试 | 当前结果 | 目标结果 |
| --- | --- | --- | --- |
| P1-D | `AureateLaureate_Season14TargetDoesNotRewriteLegacyDefinition` | 36.2 仍为 1 攻 | 36.2 为 2/2、非战吼、始终金色且无三连奖励；legacy 保持旧定义 |
| P1-F | `RecruitPhaseRally_PropagatesToBoundDarkGiftObserversWithoutDuplicatingSelfEffect` | Glim Guardian 自身进击生效，但黑赐获得 0 个鲜血宝石 | 自身只触发一次，同时向绑定的 Consanguinity 传播并获得 2 个鲜血宝石 |

## 4. 复用的现有绿色证据

本轮另外聚焦运行 4/4 通过：

- `HeroSelectionModal_SearchFiltersAndDirectChooseUpdatesOpeningStrip`：完整英雄列表、搜索和手选入口。
- `Season14_ShowsOnlyDarkGiftsAndTrinketsAndClampsLegacyMechanics`：36.2 机制入口只有黑暗之赐与饰品，并钳制旧机制状态。
- `QuietCourier_BattlecryAddsGoldenTierFourCardsWithoutTripleRewards(False,1)`。
- `QuietCourier_BattlecryAddsGoldenTierFourCardsWithoutTripleRewards(True,2)`。

因此无需重复实现或重复测试完整英雄选择、Season 14 机制白名单、静默投递者生成金色 4 本的无三连奖励行为。

## 5. 下一步入口

P1-B/P1-C 已完成：开局策略、36.2 机制白名单以及 Lockbox `TurnEnded`/达卡莱/幂等/保存恢复均已收口。下一步严格进入 P1-D 金色来源与三连奖励；Rally 红灯继续保留到 P1-F。
