# 图片资源缺失审计（中文注释版）

审计日期：2026-06-18

这份文档是 `ImageAssetAudit.md` 的中文解释版。原文档偏技术清单，这里换成“你实际找图、补图时怎么用”的写法。

## 先看结论

项目不是整体缺图，基础资源已经不少：

| 类型 | 当前已有数量 | 目录 |
| - | - | - |
| 随从/普通卡图 | 253 | `Assets/LearnHearthstone/Resources/CardImages` |
| 酒馆法术图 | 57 | `Assets/LearnHearthstone/Resources/CardImages/TavernSpells` |
| 英雄头像 | 114 | `Assets/LearnHearthstone/Resources/HeroBuddyImages/heroes` |
| 英雄技能图 | 114 | `Assets/LearnHearthstone/Resources/HeroBuddyImages/heroPowers` |
| 伙伴图 | 108 | `Assets/LearnHearthstone/Resources/HeroBuddyImages/buddies` |

真正需要处理的是下面几类：

1. **最优先补：16 张酒馆法术图。**功能都已实现；图片没有，所以现在会直接显示成占位卡面。
2. **优先检查：7 张非双打随从/衍生随从图。**功能都已实现或有战斗逻辑；图片没有。`BG26_800` 功能有，但数据里不进池。
3. **不要急着补：29 张 `BGDUO` 双打随从图。**图片没有；功能按当前项目范围不实现，建议从数据里排除或标记为 OutOfScope。
4. **容易漏：代码临时生成的卡。**功能大多已实现，其中一部分是代理/简化实现；图片没有，所以运行时会出现占位。
5. **不是图片文件缺失：6 个英雄缺伙伴映射。**英雄图和技能图都有，但伙伴映射没有；伙伴卡展示/伙伴效果没有完整实现。
6. **低优先级：280 张金色随从图。**金色专属图片没有；金色数值/三连逻辑有实现，但当前仍复用普通图。

## 功能实现状态怎么看

下面表格把“图片有没有”和“功能有没有”分开写：

| 标记 | 意思 |
| - | - |
| `图片：没有` | 当前资源目录里没有对应图片，界面会显示占位卡面。 |
| `功能：已实现` | 代码里已经有明确处理逻辑，主要缺的是卡图。 |
| `功能：部分/代理实现` | 运行时会生成或使用这张卡，但逻辑是项目里的简化/代理版本，不一定等同官方完整规则。 |
| `功能：不在当前范围` | 例如双打卡，当前单人酒馆项目不打算实现。 |
| `功能：没有` | 没找到对应功能或映射，需要补数据/补逻辑，不是只放图片能解决。 |

## 为什么缺图会变难看

所有卡图最后都会经过：

`Assets/LearnHearthstone/Runtime/Adapters/Images/CardImageProvider.cs`

它会按资源路径去 `Resources` 里找图片。找不到时，UI 会退回到占位视觉：

| 界面 | 缺图时看到什么 |
| - | - |
| UnityStyle 卡牌组件 | 彩色块 + 简短文字 |
| Realistic 卡牌视图 | 棕色肖像占位块 |

相关代码：

- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernCardComponent.cs`
- `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/Realistic/TavernCardView.cs`

所以你找图时，只要把图片放到文档里写的目标路径，并让文件名匹配，体验就会明显好一截。

## P0：最先补的 16 张酒馆法术图

这些是最明确的缺图。处理方式很简单：找到对应卡图后，放到：

`Assets/LearnHearthstone/Resources/CardImages/TavernSpells/`

文件名用表里的数字，比如 `131152.png`。

| 文件名 | 英文名 | 图片/功能状态 | 找图时看这个效果/身份 | 防找错提示 |
| - | - | - | - | - |
| `131152.png` | Might of Stormwind | 图片：没有；功能：已实现。`TavernSpellEngine` 会让最多四个友方随从获得 +1/+2。 | 2费、酒馆等级2；效果是“随机使四个友方随从获得 +1/+2”。图应偏暴风城/联盟增益感。 | 搜 `Might of Stormwind 131152 Hearthstone Battlegrounds`。不要拿成普通构筑法术或“暴风城勇士”随从图。 |
| `122182.png` | Healthy Bounty | 图片：没有；功能：已实现。官方 ID 和旧本地 ID 都接入，效果是四个友方随从加生命。 | 2费、等级3；Bounty 系列之一，效果是“使四个友方随从获得 +4生命值”。 | 搜 `Healthy Bounty 122182 Battlegrounds`。重点区分它是生命值版本，不是攻击力版本。 |
| `122183.png` | Hostile Bounty | 图片：没有；功能：已实现。官方 ID 和旧本地 ID 都接入，效果是四个友方随从加攻击。 | 2费、等级3；Bounty 系列之一，效果是“使四个友方随从获得 +4攻击力”。 | 搜 `Hostile Bounty 122183 Battlegrounds`。不要和 `Healthy Bounty` 混，Hostile 是攻击。 |
| `122184.png` | Selfish Bounty | 图片：没有；功能：已实现。代码会强化第一个友方随从 +6/+6。 | 2费、等级3；Bounty 系列之一，效果是“使你最左边的随从获得 +6/+6”。 | 搜 `Selfish Bounty 122184 Battlegrounds`。识别点是只强化最左边一个随从。 |
| `122185.png` | Friendly Bounty | 图片：没有；功能：已实现。代码会按你最多的随从类型获取同类型随从。 | 2费、等级3；Bounty 系列之一，效果是“随机获取一个与你最多随从类型相同的随从”。 | 搜 `Friendly Bounty 122185 Battlegrounds`。识别点是获取同类型随从，不是加属性。 |
| `122186.png` | Wealthy Bounty | 图片：没有；功能：已实现。代码会获得 2 枚铸币。 | 2费、等级3；Bounty 系列之一，效果是“获得2枚铸币”。 | 搜 `Wealthy Bounty 122186 Battlegrounds`。识别点是金币/财富，不是随从增益。 |
| `110401.png` | Boon of Beetles | 图片：没有；功能：已实现。代码会记录下场战斗召唤两只甲虫。 | 1费、等级4；效果是“战斗中有空位时，召唤两只 1/1 甲虫并使其获得嘲讽”。 | 搜 `Boon of Beetles 110401 Battlegrounds`。图应和甲虫、召唤、嘲讽有关。 |
| `130310.png` | Conflagration | 图片：没有；功能：已实现。代码会按本回合元素使用数量缩放加成。 | 2费、等级4；效果是“使一个随从获得 +2/+2；你本回合每使用过一个元素都会强化效果”。 | 搜 `Conflagration 130310 Battlegrounds`。识别点是火焰/元素成长，不是普通火焰法术。 |
| `130311.png` | Arcane Absorption | 图片：没有；功能：已实现。代码会让友方元素获得酒馆最高生命随从一半属性。 | 1费、等级4；效果是“使一个友方元素获得酒馆中最高生命值随从一半的属性”。 | 搜 `Arcane Absorption 130311 Battlegrounds`。识别点是奥术吸收、元素吃属性。 |
| `130312.png` | Eonar's Favor | 图片：没有；功能：已实现。代码会给同类型酒馆随从建立本局成长。 | 2费、等级4；效果是“选择一个随从，本局中酒馆里该类型随从获得 +3/+3”。 | 搜 `Eonar's Favor 130312 Battlegrounds`。识别点是伊欧娜/Eonar、类型成长。 |
| `131153.png` | Back to Back | 图片：没有；功能：已实现。代码会记录同名法术成长，让后续加成变高。 | 1费、等级4；效果是“使一个随从获得 +2/+2；以后你的 Back to Back 额外再多 +2/+2”。 | 搜 `Back to Back 131153 Battlegrounds`。识别点是同名法术会越用越强。 |
| `131218.png` | Deepwater Clan | 图片：没有；功能：已实现。代码会给目标和所有友方鱼人 +2/+2。 | 2费、等级4；效果是“使一个随从获得 +2/+2，并使你的鱼人获得 +2/+2”。 | 搜 `Deepwater Clan 131218 Battlegrounds`。识别点是鱼人/深水族群。 |
| `110412.png` | Butchering | 图片：没有；功能：已实现。代码会消灭友方亡灵并提高亡灵攻击成长。 | 2费、等级5；效果是“消灭一个友方亡灵；本局中你的亡灵无论在哪获得 +4攻击力”。 | 搜 `Butchering 110412 Battlegrounds`。识别点是亡灵牺牲换全局攻击成长。 |
| `130713.png` | Queen's Command | 图片：没有；功能：已实现。代码会全体加成，纳迦额外加成。 | 2费、等级5；效果是“使你的随从获得 +2/+2；你的纳迦额外再获得 +2/+2”。 | 搜 `Queen's Command 130713 Battlegrounds`。识别点是纳迦女王/纳迦全体增益。 |
| `113902.png` | Knockoff Wisdomball | 图片：没有；功能：部分/简化实现，需要后续补完整机制。当前代码只记录接下来 2 次 helpful refresh；每次刷新后给酒馆里的随从 +2/+2。后续应实现更接近官方的“智能刷新/更有帮助的酒馆内容”，而不是只做属性加成。 | 4费、等级6；效果是“接下来2次刷新会更有帮助”。 | 搜 `Knockoff Wisdomball 113902 Battlegrounds`。识别点是仿制智慧之球/刷新辅助。 |
| `130527.png` | Menagerie Tableware | 图片：没有；功能：已实现。代码会按友方随从类型数量重复全体 +3/+3。 | 4费、等级7；效果是“使你的随从获得 +3/+3；每有一种不同友方随从类型就重复一次”。 | 搜 `Menagerie Tableware 130527 Battlegrounds`。识别点是混合流/餐具/多类型重复增益。 |

目标路径示例：

`Assets/LearnHearthstone/Resources/CardImages/TavernSpells/131152.png`

## P0：优先补的非双打随从图

这些不是 `BGDUO`，更可能真的会进入当前单人玩法。建议优先找。

| CardId / 文件名 | 名称 | 池子 | 图片/功能状态 | 身材/类型 | 效果描述 | 找图提示 |
| - | - | - | - | - | - | - |
| `BG25_013.png` | 腐皮豺狼人 | 是 | 图片：没有；功能：已实现。战斗引擎会在友方死亡后给它加攻击，金色按 2 点处理。 | 1星，1/4，亡灵 | 本场战斗中，每有一个友方随从死亡，便拥有 +1攻击力。 | 搜 `BG25_013 腐皮豺狼人` 或 `Rot Hide Gnoll BG25_013`。看图时应是亡灵/豺狼人，不要拿成普通野兽豺狼人。 |
| `BG26_800.png` | 魔刃豹 | 否 | 图片：没有；功能：已实现但不在池。战斗引擎有亡语召唤两只嘲讽豹宝宝，数据里 `InPool = 否`。 | 1星，4/1，野兽 | 亡语：召唤两只 0/1 并具有嘲讽的豹宝宝。 | 搜 `BG26_800 魔刃豹` 或 `Manasaber BG26_800`。识别点是紫色/魔法豹，别拿成普通猎人职业的“魔泉山猫”等相似野兽。 |
| `BG34_638t.png` | 红色多彩幼龙 | 是 | 图片：没有；功能：已实现。战吼会提高本局酒馆法术攻击加成。 | 3星，6/4，龙 | 战吼：本局中，你的酒馆法术使随从额外获得 +1攻击力。 | 这是多彩幼龙 token 的红色版本。搜 `BG34_638t Red Chromawhelp`。红色版本偏攻击力。 |
| `BG34_636t.png` | 绿色多彩幼龙 | 是 | 图片：没有；功能：已实现。战吼会给其他友方龙 +2/+4。 | 3星，3/5，龙 | 战吼：使你的其他龙获得 +2/+4。 | 搜 `BG34_636t Green Chromawhelp`。绿色版本偏生命值/防御，不要和红色、黑色混。 |
| `BG34_634t.png` | 蓝色多彩幼龙 | 是 | 图片：没有；功能：已实现。战吼会随机获取一张 2 费酒馆法术。 | 3星，4/4，龙 | 战吼：随机获取一张消耗2枚铸币的酒馆法术牌。 | 搜 `BG34_634t Blue Chromawhelp`。蓝色版本和“获取酒馆法术”有关。 |
| `BG34_637t.png` | 青铜多彩幼龙 | 是 | 图片：没有；功能：已实现。战吼会给其他友方龙 +4/+2。 | 3星，5/3，龙 | 战吼：使你的其他龙获得 +4/+2。 | 搜 `BG34_637t Bronze Chromawhelp`。青铜版本偏攻击力和龙群增益。 |
| `BG34_635t.png` | 黑色多彩幼龙 | 是 | 图片：没有；功能：已实现。战吼会提高本局酒馆法术生命加成。 | 3星，4/6，龙 | 战吼：本局中，你的酒馆法术使随从额外获得 +1生命值。 | 搜 `BG34_635t Black Chromawhelp`。黑色版本偏生命值成长，容易和红色版本搞反。 |

注释：

- `InPool = 是` 表示它可能被正常酒馆池抽到，优先级更高。
- `BG34_63xt` 这一组像是衍生幼龙，通常比普通随从更容易被漏掉。

## P1：双打卡图，建议先别补

下面这些是 `BGDUO` 或 `BGDUO31`、`BGDUO33` 开头的卡。项目根目录的 `PROJECT_SCOPE.md` 明确写了当前只做单人酒馆，不做双打系统。

所以这批有两个选择：

1. 推荐：从当前可用数据里排除，或标记为 `OutOfScope`。
2. 备选：如果你决定界面里仍然要展示双打卡，再补这些图。

统一状态：图片：没有；功能：不在当前范围。代码里有一部分双打卡被注册为 `OutOfScope`，项目当前目标是单人酒馆，所以建议先排除，而不是逐张补实现。

| CardId | 名称 | 等级 | 目标文件 |
| - | - | - | - |
| `BGDUO_114` | 过路旅客 | 1 | `CardImages/BGDUO_114.png` |
| `BGDUO_104` | 热心的沙龙酒保 | 2 | `CardImages/BGDUO_104.png` |
| `BGDUO_100` | 远行者周卓 | 2 | `CardImages/BGDUO_100.png` |
| `BGDUO31_201` | 聚积风暴 | 2 | `CardImages/BGDUO31_201.png` |
| `BGDUO_111` | 大方的地卜师 | 2 | `CardImages/BGDUO_111.png` |
| `BGDUO_119` | 兽人指挥 | 3 | `CardImages/BGDUO_119.png` |
| `BGDUO_115` | 跳跳娃娃 | 3 | `CardImages/BGDUO_115.png` |
| `BGDUO_118` | 打劫共犯 | 3 | `CardImages/BGDUO_118.png` |
| `BGDUO31_207` | 摩托水手 | 3 | `CardImages/BGDUO31_207.png` |
| `BGDUO33_140` | 底栖饲育者 | 3 | `CardImages/BGDUO33_140.png` |
| `BGDUO_117` | 滩涂跳跳鱼 | 3 | `CardImages/BGDUO_117.png` |
| `BGDUO_107` | 护雏的龙希尔 | 3 | `CardImages/BGDUO_107.png` |
| `BGDUO31_212` | 螳螂妖国王 | 4 | `CardImages/BGDUO31_212.png` |
| `BGDUO_112` | 墓后解说员 | 4 | `CardImages/BGDUO_112.png` |
| `BGDUO31_208` | 萨莱因铭文师 | 4 | `CardImages/BGDUO31_208.png` |
| `BGDUO_110` | 活泼的淡水元素 | 4 | `CardImages/BGDUO_110.png` |
| `BGDUO_108` | 镜中鬼怪 | 4 | `CardImages/BGDUO_108.png` |
| `BGDUO31_209` | 私房主厨 | 4 | `CardImages/BGDUO31_209.png` |
| `BGDUO31_203` | 狡捷灵蛇 | 4 | `CardImages/BGDUO31_203.png` |
| `BGDUO_120` | 井边许愿者 | 5 | `CardImages/BGDUO_120.png` |
| `BGDUO_121` | 堕落者信使 | 5 | `CardImages/BGDUO_121.png` |
| `BGDUO_109` | 增援系统 | 5 | `CardImages/BGDUO_109.png` |
| `BGDUO_122` | 风暴分流者 | 5 | `CardImages/BGDUO_122.png` |
| `BGDUO_105` | 宽厚的驼鹿 | 5 | `CardImages/BGDUO_105.png` |
| `BGDUO31_205` | 无私的观光客 | 5 | `CardImages/BGDUO31_205.png` |
| `BGDUO33_150` | 黑暗炫魔 | 6 | `CardImages/BGDUO33_150.png` |
| `BGDUO31_211` | 转运反应堆 | 6 | `CardImages/BGDUO31_211.png` |
| `BGDUO31_202` | 忠实的帮凶 | 6 | `CardImages/BGDUO31_202.png` |
| `BGDUO_125` | 流沙幻象 | 7 | `CardImages/BGDUO_125.png` |

## P1：代码生成卡，也需要补图或指定路径

这类最容易被忽略，因为它们不是从 `battlegroundsMinions.json` 或 `battlegroundsSpells.json` 正常读出来的，而是代码临时创建的。

典型来源：

- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/HeroEffectEngine.cs`
- `Assets/LearnHearthstone/Runtime/Domain/Engine/TavernSpellEngine.cs`

处理方式有两种：

1. 直接按下面路径补图。
2. 或者在代码里给这些生成卡设置明确的 `ImagePath`，让多个同类生成卡共用一张图。

| CardId | 显示名 | 图片/功能状态 | 它在游戏里代表什么 | 找图/占位建议 | 建议图片路径 |
| - | - | - | - | - | - |
| `MOONSTEEL_SATELLITE` | Moonsteel Satellite | 图片：没有；功能：已实现。`MatchService` 会由 Moonsteel Juggernaut 生成机械卫星到手牌。 | 月钢卫星，代码生成的机械/星际风格随从。 | 如果找不到官方图，可用机械卫星、月钢、星际科技感临时代替。 | `CardImages/MOONSTEEL_SATELLITE.png` |
| `TAUGHT_MURLOC` | Taught Murloc | 图片：没有；功能：已实现。`MatchService` 会生成 1/1 鱼人衍生随从。 | 被教学/训练出来的鱼人衍生随从。 | 优先找鱼人学徒、训练鱼人、低阶鱼人风格图。 | `CardImages/TAUGHT_MURLOC.png` |
| `GENERATED_ELEMENTAL` | 商贩元素 | 图片：没有；功能：部分/代理实现。它是代码生成的通用 3/3 元素，不是完整官方卡。 | 代码生成的元素随从，用于补位或效果生成。 | 可以用普通小型元素图，重点看起来像“元素”，不必是唯一官方卡。 | `CardImages/GENERATED_ELEMENTAL.png` |
| `DEMON_FODDER` | 恶魔饲料 | 图片：没有；功能：部分/代理实现。它是刷新/恶魔效果用的 1/1 恶魔饲料单位。 | 恶魔相关效果生成的牺牲/饲料单位。 | 用低阶恶魔、献祭材料、暗色恶魔衍生物更合适。 | `CardImages/DEMON_FODDER.png` |
| `NZOTH_FISH` | Fish of N'Zoth | 图片：没有；功能：已实现。恩佐斯英雄开局会生成 2/2 鱼。 | 恩佐斯相关的鱼，英雄/伙伴效果会生成。 | 搜 `Fish of N'Zoth Battlegrounds`。不要拿普通鱼人图，它应该更像恩佐斯的鱼。 | `CardImages/NZOTH_FISH.png` |
| `SNEED_SHREDDER` | Sneed's Shredder | 图片：没有；功能：部分/代理实现。斯尼德开局生成 2/1 机械，并带有项目内死亡语标签。 | 斯尼德相关的机械伐木机/切割机代理随从。 | 搜 `Sneed's Shredder Hearthstone`，机械伐木机外形最匹配。 | `CardImages/SNEED_SHREDDER.png` |
| `CURATOR_AMALGAM` | Amalgam | 图片：没有；功能：已实现。馆长开局生成融合怪，并带全随从类型标签。 | 馆长英雄自带的融合怪。 | 搜 `Curator Amalgam Battlegrounds` 或 `Amalgam Curator Hearthstone`。识别点是多种族融合。 | `CardImages/CURATOR_AMALGAM.png` |
| `UNDEAD_CREATION_PROXY` | Undead Creation | 图片：没有；功能：部分/代理实现。找不到合适亡灵候选时用 4/4 代理亡灵兜底。 | 普崔塞德/亡灵创造类效果的代理亡灵。 | 找亡灵造物、拼接怪、实验体风格，避免普通骷髅太弱。 | `CardImages/UNDEAD_CREATION_PROXY.png` |
| `ZERG_MINION_PROXY` | Zerg minion proxy | 图片：没有；功能：部分/代理实现。星际/虫族相关效果用它当生成随从代理。 | 星灵/虫族联动英雄效果生成的虫族代理随从。 | 用虫族、异虫、尖刺生物风格图。它不是炉石常规随从，找不到官方图可用临时统一代理图。 | `CardImages/ZERG_MINION_PROXY.png` |
| `OZUMAT_TENTACLE` | Tentacle | 图片：没有；功能：已实现。厄祖玛特战斗中会生成带嘲讽的触须，属性会随计数成长。 | 厄祖玛特生成的触须。 | 搜 `Ozumat Tentacle Battlegrounds`。识别点是海怪触手，不是普通纳迦。 | `CardImages/OZUMAT_TENTACLE.png` |
| `BLOOD_GEM` | 鲜血宝石 | 图片：没有；功能：已实现。酒馆法术引擎会给友方随从 +1/+1，并吃野猪人宝石成长。 | 野猪人核心法术，通常效果是给一个友方随从 +1/+1。 | 搜 `Blood Gem Hearthstone Battlegrounds`。这是最常见生成卡之一，建议优先补。 | `CardImages/BLOOD_GEM.png` |
| `BRISTLEBACK_BLOOD_GEM` | 特殊鲜血宝石 | 图片：没有；功能：已实现。给 +1/+1，目标是野猪人时额外给嘲讽。 | 棘背/野猪人相关的特殊鲜血宝石版本。 | 可先和普通鲜血宝石共用图；如果要区分，可加棘刺或野猪人视觉。 | `CardImages/BRISTLEBACK_BLOOD_GEM.png` |
| `REBORN_BLOOD_GEM` | 复生鲜血宝石 | 图片：没有；功能：已实现。按鲜血宝石成长给属性，目标是野猪人时额外给复生。 | 带复生/亡灵语义的特殊鲜血宝石。 | 可先和普通鲜血宝石共用图；精修时加绿色亡灵/复生气息。 | `CardImages/REBORN_BLOOD_GEM.png` |
| `SLIMY_SHIELD` | 黏黏盾 | 图片：没有；功能：已实现。生成法术会给目标 +1/+1 和嘲讽。 | 软泥角斗士生成的法术，通常用于给随从圣盾/防护感。 | 搜 `Slimy Shield Battlegrounds` 或用软泥盾牌图。识别点是“黏液 + 盾”。 | `CardImages/SLIMY_SHIELD.png` |
| `100596` | Pointy Arrow | 图片：没有；功能：已实现。生成法术会给目标 +4 攻击力。 | 一张箭矢/尖箭类增益法术，来自随从生成。 | 搜 `Pointy Arrow 100596 Hearthstone Battlegrounds`。不要拿成猎人奥秘或武器。 | `CardImages/100596.png` |
| `REEF_RIFFER_SPELL` | Reef Riff | 图片：没有；功能：已实现。Spellcraft 会按当前酒馆等级给临时属性。 | 深海/纳迦/鱼人相关的临时法术。 | 用海礁、音乐 riff、海洋法术感图片；也可后续映射到官方 Spellcraft 图。 | `CardImages/REEF_RIFFER_SPELL.png` |
| `SURF_N_SURF_SPELL` | Surf n' Surf | 图片：没有；功能：已实现。Spellcraft 会给目标临时亡语：召唤螃蟹。 | 冲浪主题临时法术。 | 用海浪、冲浪、纳迦/鱼人法术风格，避免和普通水元素法术混。 | `CardImages/SURF_N_SURF_SPELL.png` |
| `DEEP_SEA_ANGLER_SPELL` | Deep Sea Angling | 图片：没有；功能：已实现。Spellcraft 会给 +2/+6 和嘲讽直到下回合。 | 深海垂钓/钓鱼主题临时法术。 | 用鱼钩、深海、垂钓感图。 | `CardImages/DEEP_SEA_ANGLER_SPELL.png` |
| `DEEP_BLUE_SPELL` | Deep Blue | 图片：没有；功能：已实现。Spellcraft 会给可成长的临时 +2/+2，并提升后续 Deep Blue。 | Deep Blue 系列法术，通常是纳迦/塑造法术相关增益。 | 搜 `Deep Blue Hearthstone Battlegrounds`。注意不要找成普通蓝色背景法术。 | `CardImages/DEEP_BLUE_SPELL.png` |
| `VOLCANIC_VISITOR_ATTACK_SPELL` | 火山访客攻击法术 | 图片：没有；功能：已实现。Spellcraft 攻击版本会给临时 +4 攻击。 | 火山访客生成的攻击力版本法术。 | 找火山/熔岩 + 攻击增益感；可和生命版本同底图不同角标。 | `CardImages/VOLCANIC_VISITOR_ATTACK_SPELL.png` |
| `VOLCANIC_VISITOR_HEALTH_SPELL` | 火山访客生命法术 | 图片：没有；功能：已实现。Spellcraft 生命版本会给临时 +4 生命。 | 火山访客生成的生命值版本法术。 | 找火山/熔岩 + 生命/防御增益感；注意和攻击版本区分。 | `CardImages/VOLCANIC_VISITOR_HEALTH_SPELL.png` |
| `FROSTLING_PRIESTESS_SPELL` | Frostling Priestess | 图片：没有；功能：已实现。Spellcraft 会获取随机属性型酒馆法术。 | 霜寒女祭司相关临时法术。 | 用冰霜、祭司、蓝白法术感图。 | `CardImages/FROSTLING_PRIESTESS_SPELL.png` |
| `RAKANISHU_LANTERN_LIGHT` | Lantern Light | 图片：没有；功能：已实现。拉卡尼休相关法术会按酒馆等级给 +N/+N。 | 拉卡尼休相关“灯火”增益法术。 | 搜 `Rakanishu Lantern Light Battlegrounds`，识别点是灯笼/元素火光。 | `CardImages/RAKANISHU_LANTERN_LIGHT.png` |
| `MUKLA_BANANA` | Banana | 图片：没有；功能：已实现。穆克拉会生成香蕉，使用后给友方随从 +1/+1。 | 穆克拉的香蕉，通常给友方随从 +1/+1。 | 搜 `Mukla Banana Hearthstone`。这是很常见的香蕉法术，建议优先补。 | `CardImages/TavernSpells/MUKLA_BANANA.png` |
| `BATTLECRUISER_UPGRADE` | Battlecruiser Upgrade | 图片：没有；功能：旧兼容代理。主路径已改为官方 `BG31_HERO_801pt*` 战列巡航舰升级；旧代理不再回退给最左随从。 | 旧战列巡航舰升级兼容资源。 | 优先补官方 `BG31_HERO_801pt*` 升级图片；旧代理可用统一科幻升级占位图。 | `CardImages/TavernSpells/BATTLECRUISER_UPGRADE.png` |
| `BETTER_SECRET_PROXY` | Better Secret | 图片：没有；功能：部分/代理实现。代码明示完整奥秘战场支持暂缓，目前代理为最左随从 +2/+2。 | 奥秘代理法术，用来表示“更好的奥秘”效果。 | 如果没有官方图，用奥秘问号/紫色神秘法术图；后续可统一成代理图。 | `CardImages/TavernSpells/BETTER_SECRET_PROXY.png` |
| `TRIPLE_REWARD` | Triple Reward | 图片：没有；功能：已实现。三连后生成奖励卡，打出时进入三选一发现奖励。 | 三连奖励卡，三连后给奖励。 | 搜 `Triple Reward Battlegrounds` 或用金色三连奖励/发现奖励图。这个出现频率高，建议补。 | `CardImages/TavernSpells/TRIPLE_REWARD.png` |

完整路径要加上资源目录前缀，例如：

`Assets/LearnHearthstone/Resources/CardImages/BLOOD_GEM.png`

## P1：缺伙伴映射的英雄

这不是“图片文件没放进去”，而是“英雄没有对应伙伴数据”。所以你单纯放图片不一定能解决，需要补数据映射。

| 英雄 ID | 英雄 | 状态 |
| - | - | - |
| `BG34_HERO_004` | Morchie | 缺伙伴映射 |
| `BG34_HERO_002` | Mister Clocksworth | 缺伙伴映射 |
| `BG34_HERO_000` | Murozond, Unbounded | 缺伙伴映射 |
| `BG31_HERO_003` | Farseer Nobundo | 缺伙伴映射 |
| `BG35_HERO_001` | Genn, Worgen King | 缺伙伴映射 |
| `BG34_HERO_001` | Time Twister Chromie | 缺伙伴映射 |

注释：

- 英雄头像：已有。
- 英雄技能图：已有。
- 伙伴映射：没有。
- 伙伴图片文件：这里不是主要问题；主要问题是数据没有连到伙伴。
- 功能：没有完整实现。因为伙伴字段为空，伙伴相关 UI、伙伴卡展示和伙伴效果都无法完整走通；只补图片不能解决。

## P2：金色随从图，先不用急

数据里有 280 个金色随从 ID，但项目里没有对应金色图片。

不过当前代码创建金色随从时，仍然使用普通随从的 `ImagePath`。也就是说：

- 金色专属图片：没有。
- 功能：已实现一部分。金色数值、三连/金色状态相关逻辑有实现；但专属金色卡图加载没有实现。
- 现在不会因为缺金色图而变成占位图，因为仍然复用普通图。
- 但金色随从看起来不会有真正的金色卡图差异。

后面如果想提升体验，有两个方向：

1. 补全金色图，并让代码在 `MinionInstance.Golden == true` 时加载金色图。
2. 不补金色图，只在 UI 上加金色边框、金色光效或金色遮罩。

缺失数量：

| 等级 | 缺少金色图数量 |
| - | - |
| 1 | 24 |
| 2 | 36 |
| 3 | 48 |
| 4 | 60 |
| 5 | 61 |
| 6 | 38 |
| 7 | 13 |

## 推荐处理顺序

按体验提升效率，建议这样做：

1. 先补 16 张酒馆法术图。
2. 再补 7 张非双打随从/衍生随从图。
3. 然后处理代码生成卡，尤其是鲜血宝石、香蕉、三连奖励、衍生随从这类常见卡。
4. 决定双打卡到底是排除还是展示。如果仍然只做单人，优先排除，不要浪费时间找图。
5. 补 6 个英雄的伙伴映射。
6. 最后再考虑金色随从图或金色 UI 特效。

## 找图时的小规则

- 放在 `Resources` 下面的图片，文件名必须和代码/数据里的路径一致。
- 酒馆法术优先放到 `CardImages/TavernSpells`。
- 普通随从、衍生随从、普通法术代理优先放到 `CardImages`。
- 英雄、英雄技能、伙伴图目前覆盖良好，除非新增英雄，否则不用先碰。
- 如果你找到的是 `.jpg`，通常也能加载；但为了清单一致，建议统一转成 `.png`。
