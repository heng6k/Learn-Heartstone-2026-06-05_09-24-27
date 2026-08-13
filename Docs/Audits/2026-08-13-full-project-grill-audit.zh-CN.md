# Learn Hearthstone 全项目 Grill 审查报告

> 审查日期：2026-08-13
>
> 审查基线：`02a6364 chore(checkpoint): save current product work`
>
> 优先平台：手机端、网页端、Windows；微信小游戏暂缓
>
> 审查方式：只输出报告，不修改产品逻辑，不发布到 Cloudflare，不替换网站下载包

## 1. 结论

当前版本已经具备完整训练器的主要骨架，最近讨论过的若干能力也确实已经存在：作者创建器可选择塑造法术类别，敌方战场已有接近全屏的编辑面板和删除按钮，龙虾已改为从正式定义创建，手牌通常按最右侧加入，S14 的黑暗赠礼与饰品主开关也基本对齐。

但现在还不适合作为新的正式下载包发布。发布前至少需要解决 6 个 P1 产品问题，并恢复一条可信的完整 Unity 测试门禁：

1. 手机横屏从网页深处进入训练场时保留旧滚动位置，工具栏和 iframe 会落到视口上方，用户表现为“卡住”。
2. 网页一图流把每个阵容的三种塑造法术全部展示出来，和 Unity 实际每档只绑定一种的规则矛盾。
3. 网页所选一图流/档位没有真正传入 Unity，玩家仍需在 Unity 内重新选择。
4. 简单/初级模式只放开了刷新，没有同样放开冻结，且不同指南配置不一致。
5. 黑暗赠礼与反射吊坠获得第 3 张同名随从时，不会立即触发三连。
6. 当前完整 Unity EditMode 审查任务长时间无结果；在恢复稳定、可重复的全量门禁前，不能把局部测试通过等同于版本可发布。

网页与小程序的现有自动测试均通过，网页生产构建通过，生产 npm 依赖未发现已知漏洞。这些是积极信号，但没有覆盖上述真实玩家路径与 Unity 全量门禁。

## 2. 审查边界与证据

### 已执行

- 固定并推送审查基线：`02a6364`，远端分支 `codex/mobile-onepage-rail-hotfix-20260813`。
- WebApp：`npm test`，12/12 通过。
- MiniProgram：`npm test`，8/8 通过。
- WebApp：`npm run build` 通过。
- WebApp 生产依赖：`npm audit --omit=dev --registry=https://registry.npmjs.org`，0 个已知漏洞。
- 以 Playwright 实测网页手机视口：390×844 竖屏、844×390 横屏。
- 静态检查 Unity 运行时、编辑器、数据目录、关键 EditMode/PlayMode 测试和发布文档。
- 对照暴雪官方 S14、三连、饰品和 Spellcraft 资料。

### 未完成或不在范围内

- 完整 Unity EditMode 审查任务运行超过 20 分钟后停止产出结果，Unity 进程仍忙，桥接请求连续超时。最后能观察到的测试栈靠近 `ContentSnapshotFallbackTests.Resolve_ValidV2RemotePromotesImmutableSnapshotAndRestoresActive`，但这不足以认定该测试就是根因。本报告将其列为“验证基础设施阻断”，不伪报通过或失败。
- 因 Unity 编辑器被上述任务占用，没有重复启动第二套全量 EditMode/PlayMode，以免污染结果或损坏编辑状态。
- 微信小游戏按本次约定不做功能验收；仅在架构与仓库体积部分记录其耦合影响。
- 未进行渗透测试、Unity 第三方二进制供应链审计或线上负载测试。

## 3. 缺陷清单

### P0 — 发布门禁不可用

#### P0-1 完整 Unity 测试任务卡住，当前没有可信的全量绿灯

**证据**

- 最后的有效测试日志靠近 `Assets/LearnHearthstone/Tests/EditMode/Catalogs/ContentSnapshotFallbackTests.cs:110`。
- 当时调用路径包含 `Assets/LearnHearthstone/Runtime/Adapters/Content/GameCatalogSnapshotResolver.cs:251`。
- 此后测试桥接连续超时，Unity 进程 CPU 继续增长但未返回最终结果。
- 最近受影响范围的分组测试曾有 393/393 通过、编译 0 错误；它只能证明该分组，不能替代本次全量结果。

**影响**

无法确认长时间运行、资源释放、测试隔离或某个内容快照路径是否存在死锁/极慢问题。此项本身未证明玩家功能错误，但它阻断发布判断。

**发布要求**

查明任务卡住位置，保证全量 EditMode 可在明确超时内重复完成；随后补跑关键 PlayMode 玩家路径。

### P1 — 发布前必须修复

#### P1-1 手机横屏进入网页训练场后控件移出视口

**证据**

- `WebApp/src/pages/PlayPage.vue:25` 的 `startUnity` 只切换页面状态，没有把窗口或容器滚回顶部。
- 844×390 实测：从 `/play` 深滚动后点击“窗口模式进入”，`scrollY=623`；新工具栏 y=-233，iframe y=-173，退出、重载、全屏按钮都在视口上方。
- 390×844 竖屏同样保留了旧 `scrollY=92`。
- `WebApp/tests/content.test.mjs:109` 只检查按钮和 Fullscreen API 文本存在，没有点击按钮或检查几何位置。

**影响**

与用户报告的“手机端卡在一图流、后续不能测试”直接吻合；它不是 Unity 战斗冻结，而是网页滚动上下文没有重置。

#### P1-2 网页一图流错误展示三种塑造法术

**证据**

- `WebApp/src/pages/GuidesPage.vue:201` 原样循环展示 `shapingSpells`。
- `WebApp/public/data/guides.json:299` 起的档位数据同时包含战吼、亡语、回合结束三张。
- Unity 实际发牌只读取 `ShapingSpellCardIds.FirstOrDefault()`，见 `Assets/LearnHearthstone/Runtime/Application/Services/StrategyGuideSession.cs:731`；规则是初始 2 张、以后每回合 1 张。
- 正式 Unity 指南源数据每档只绑定一种；网页导出结果丢失了这个约束。

**影响**

网页向玩家描述了一个不存在的“三类同时可用”策略，和此前确定的“制作时为该档位选择三类之一”相冲突。

#### P1-3 网页所选指南和档位没有传入 Unity

**证据**

- `WebApp/src/pages/GuidesPage.vue:259` 仅把 guide/profile 放到 URL 查询串。
- `WebApp/src/pages/PlayPage.vue:12` 读取查询串后只显示提示；`WebApp/src/pages/PlayPage.vue:139` 要求玩家进入 Unity 后重新选择。
- iframe 没有 postMessage、启动参数或 ready 后的配置注入，也没有端到端测试验证 Unity 已打开所选阵容。

**影响**

“一图流 → 操作训练”不是实际交接。手机端需要在更复杂的 Unity UI 中重做一次选择，既增加遮挡风险，也可能选错档位。

#### P1-4 简单/初级模式的冻结仍被锁定

**证据**

- `Assets/LearnHearthstone/Runtime/Application/Services/StrategyGuideSession.cs:404` 只对 `RerollShop` 做简单/引导模式豁免。
- 前三套指南的简单/初级 `AllowedCommands` 没有 `FreezeShop`，例如 `Assets/LearnHearthstone/Resources/Data/battlegroundsStrategyGuides.json:32`、`:72`。
- 较后的部分指南却包含相关命令，产生跨指南不一致。
- `Assets/LearnHearthstone/Tests/EditMode/Match/StrategyGuideSessionTests.cs:123` 只验证刷新；`Assets/LearnHearthstone/Tests/EditMode/UI/StrategyGuideUiTests.cs:864` 附近的旧断言仍期待简单模式刷新/冻结/升级全部锁定，测试基线互相矛盾。

**影响**

没有达到“简单模式和初级模式像正常对局一样能够刷新、冻结”的需求。

#### P1-5 黑暗赠礼获得第 3 张同名随从时不会立即三连

**证据**

- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:6510` 创建并放入发现随从。
- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:6542` 只调用手牌进入处理；选择完成路径 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:6461` 也没有调用 `ResolvePlayerTriples()`。
- 真正的三连入口位于 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:38290`。

**复现条件**

手牌/场上已有两张同名普通随从，在第 3 回合用黑暗赠礼发现第 3 张；三张不会当场合成。

**规则依据**

暴雪的[战棋基础说明](https://hearthstone.blizzard.com/en-gb/news/23156373/introducing-hearthstone-battlegrounds)规定三张相同随从会自动合成；S14 [黑暗赠礼公告](https://hearthstone.blizzard.com/en-us/news/24290433/announcing-battlegrounds-season-14-dark-gifts-of-dalaran)说明该机制会发现一个随从，因此仍应经过统一的“进入手牌后结算三连”路径。

#### P1-6 反射吊坠复制第 3 张同名随从时不会立即三连

**证据**

- 装备和回合开始入口位于 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:13663`、`:16745`。
- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:36165` 的 `AddPlainCopyOfRandomFriendlyMinionToHand` 添加手牌后只调用通用手牌处理，没有三连解析。

**影响**

与 P1-5 是同一架构根因、不同玩家路径，不能只为黑暗赠礼做点状修补。官方[反射吊坠说明](https://hearthstone.blizzard.com/en-us/news/24143781/more-trinkets-to-tinker-with-in-battlegrounds/)明确是取得普通复制，仍应遵守自动三连。

### P2 — 高风险或明显体验缺口

#### P2-1 `Gem Day / 宝石日` 元数据仍是等级 3、费用 1

**证据**

- `Assets/LearnHearthstone/Resources/Data/battlegroundsGameVersions.json:3423` 仍写 `tier: 3`、`cost: 1`。
- `Assets/LearnHearthstone/Resources/Data/battlegroundsSpells.json:5943` 同样是费用 1、等级 3。
- `Assets/LearnHearthstone/Tests/EditMode/Catalogs/Season14TavernSpellDiffCatalogTests.cs:24` 把旧值锁进测试。
- 运行时衍生物路径在 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:37386` 会把费用改为 0，所以部分实战看似正确，但卡面、目录和等级相关筛选仍错误。

**规则依据**

暴雪 S14 [Spell Updates](https://us.forums.blizzard.com/en/hearthstone/t/battlegrounds-season-14-spell-updates/163891)列出的新值为费用 0、无酒馆等级。

#### P2-2 版本身份仍是 `36.2-preview / Partial`

**证据**

- `Assets/LearnHearthstone/Resources/Data/battlegroundsGameVersions.json:15` 的 id 是 `36.2-preview`，同时标记 `Released`、`Partial`，规则集和内容仍使用 preview 名称。
- 网站主要标题显示 36.2，并只用较弱的“有限支持”文案提示；Windows 发布清单 `WebApp/public/releases/windows-release-manifest.json:8` 的 `gameVersionId` 仍是 `legacy-composite-sandbox-v1`。

**影响**

S14 已正式上线后，玩家可能把“部分实现的预览池”理解成完整正式卡池。此项既影响版本可信度，也会妨碍以后判断某张牌究竟是缺失还是有意未实现。

#### P2-3 版本成员表仍含双打卡牌

**证据**

- `Assets/LearnHearthstone/Resources/Data/battlegroundsGameVersions.json:850` 附近存在多项 `BGDUO_...` 成员。
- `Assets/LearnHearthstone/Runtime/Application/Content/GameVersionResolver.cs:186` 根据该成员表设置 `InPool`。
- 部分选择器另有 `CardPoolAvailability` 过滤，但直接消费 `Snapshot.Minions.Where(InPool)` 的路径仍可能泄漏。
- `Assets/LearnHearthstone/Tests/EditMode/Catalogs/Season14PoolMembershipTests.cs:68` 没有“单打池不得出现 BGDUO”的反向断言。

**影响**

与“新一图流的核心随从、酒馆法术不要出现双打内容”直接相关。由于下游有些路径会二次过滤，现象可能并非处处复现，但数据真源不干净。

#### P2-4 缺定义时的代理卡会回退英文或占位图

**证据**

- `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:35586` 起仍保留多种 definition 缺失时的手工 proxy。
- 例如 `CreateHackerfinProxy` 位于 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:36105`，名称是英文；`CreateTrinketProxyMinion` 位于 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:36120`，没有完整正式卡图/本地化元数据保证。

**影响**

这解释了中文环境中饰品、畸变、抉择或后加衍生物偶发英文/占位图的一类根因：不是单纯漏翻译，而是运行时绕过正式卡牌定义创建。

**已确认没有回归的关联项**

- 龙虾：`Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs:6521` 已通过正式 `BG36_202` 定义创建，正式定义在 `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json:12814`；测试已覆盖 DefinitionId、名称、图片与 sprite。
- 狼宝宝：正式定义在 `Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json:12871`，`inPool=false`、3/5，属于衍生物而非必选卡池。
- 鱼饵：正式定义和狮子鱼/鲨鱼的载体逻辑已经存在；现有测试主要验证逻辑，仍应增加名称、DefinitionId、ImagePath 与实际渲染覆盖。

#### P2-5 iframe 的“已连接”并不代表 Unity 可用

**证据**

- `WebApp/src/pages/PlayPage.vue:85` 的 iframe `load` 事件直接把状态标为 ready，并取消 90 秒超时。
- `WebApp/src/pages/PlayPage.vue:223` 的 iframe 只要 HTML 文档装载就会触发 load；Unity loader、内容快照或运行时随后失败也会显示“训练场页面已连接”。

**影响**

会把占位页、加载器初始页甚至后续崩溃误报为成功，使“项目打不开/卡住”的诊断信息失真。

#### P2-6 手机软键盘遮挡没有经过真实键盘验证

**证据**

- `Assets/LearnHearthstone/Runtime/Presentation/MainHub/StrategyGuideAuthoringEditorView.cs:2090` 在输入框被选择后，只按固定的顶部 18% 位置滚动两帧。
- 没有使用 `TouchScreenKeyboard.area/visible`，WebGL 侧也没有接入浏览器 `visualViewport` 的软键盘高度。
- `Assets/LearnHearthstone/Tests/EditMode/UI/StrategyGuideAuthoringEditorUiTests.cs:193` 只模拟 SelectEvent，没有键盘出现、输入、收起和重新布局。
- 卡牌选择弹窗 `Assets/LearnHearthstone/Runtime/Presentation/MainHub/StrategyGuideAuthoringPickerModalComponent.cs:158` 的搜索输入也没有同等的保持可见处理。

**影响**

现有修复能改善编辑器内的焦点滚动，但不能证明手机浏览器/真机键盘不会继续遮住文字输入。

#### P2-7 敌方战场“逻辑能删”，但缺少真实触控可达性证明

**证据**

- 面板已经接近全屏：`UnityTavernTrainerController.cs:2222` 使用约 1%–99% anchors。
- 选中后的常驻操作行和删除按钮位于 `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/UnityTavernTrainerController.cs:2314`。
- EditMode 测试 `Assets/LearnHearthstone/Tests/EditMode/UI/UnityTavernTrainerViewTests.cs:1988` 验证了选择后删除。
- PlayMode 玩家路径 `Assets/LearnHearthstone/Tests/PlayMode/Journeys/CorePlayerJourneyInputTests.cs:846` 仅打开后关闭，没有在 390×844/844×390 中执行“点敌方随从 → 删除”。

**影响**

实现层面已有删除，不应继续误报“没制作”；真正剩余风险是手机触控区域、滚动容器和操作行是否互相遮挡。

#### P2-8 手牌最右侧规则已基本实现，但入口分散，容易再次漏结算

**证据**

- 购买与三连重新置入当前都使用末尾添加，例如 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:20361`、`:38290`。
- `HandleCardsAddedToHand` 位于 `Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs:33164`，假定每个上游先直接 `Hand.Add`，再告诉它“刚加入 N 张”。
- 测试主要覆盖购买和一类三连，没有覆盖所有发现、奖励、英雄、饰品、多卡同时加入路径。
- P1-5/P1-6 已证明分散入口会漏掉三连解析；未来也可能漏掉最右侧、手牌上限或获得触发。

**影响**

这是“所有进入手牌都到最右侧”尚未完全形成架构不变量的问题。需要统一入口和参数化玩家路径测试，而不是继续逐卡修补。

### P3 — 体验、可维护性与仓库治理

#### P3-1 从训练场返回一图流会丢失上下文

`WebApp/src/pages/PlayPage.vue:186` 固定返回 `/guides`，没有保留 guide/profile。手机用户需要重新查找阵容和档位。

#### P3-2 手机端存在多处过小文字

`WebApp/src/styles.css:222`、`:436`、`:743` 等位置使用约 `.68rem`–`.73rem` 的文本。整体布局没有发现横向溢出，安全区和底部栏处理基本正确，但小字在移动端卡牌信息密集场景中可读性不足。

#### P3-3 核心类规模过大，放大回归和审查成本

- `MatchService.cs` 约 39,223 行。
- `UnityTavernTrainerController.cs` 约 14,046 行。
- `CombatEngine.cs` 约 11,297 行。

这些文件不是因为“大”就自动有错，但本次发现的三连遗漏、代理卡绕过正式定义、手牌入口分散，均体现了同一类问题：新机制容易在巨型服务中走另一条近似但不完整的路径。

#### P3-4 仓库包含大量重复宣传视频和暂缓平台 SDK

- `PromoVideo` 下存在相同或近似渲染物的多份副本，单个最终视频约 13 MB，原始录屏也有多个 3–9 MB 重复版本。
- 微信 SDK 约 576 个文件、131 MB。微信小游戏虽暂缓，但 Editor asmdef 仍硬引用 `WxEditor`，不能直接删除。

**影响**

克隆、LFS 上传、索引和审查都会变慢。微信内容应先隔离为独立 asmdef/条件包，再决定是否从默认仓库移出；宣传素材应只保留一个真源和一个正式成品。

## 4. 官方规则差异分类

这一部分专门回答“差异不一定都影响，有些反而是优化”。

| 类别 | 差异 | 审查结论 |
|---|---|---|
| 确定错误 | 黑暗赠礼、反射吊坠取得第 3 张同名随从不立即三连 | 必须修复，违反统一自动三连规则 |
| 确定错误 | 宝石日目录仍为 3 本 1 费 | 必须修复数据和锁死旧值的测试；运行时临时改 0 费不能代替真源修复 |
| 身份/覆盖风险 | 36.2 仍绑定 preview、Partial | 可以继续作为训练预览，但必须明确标识，不能包装成完整正式池 |
| 可接受训练简化 | 默认允许从完整合资格饰品库自由选择，而不是正式 4 选 1 | 有利于专项训练，不作为规则 Bug；正式模拟模式应关闭或清楚标记 |
| 可接受训练简化 | 自定义种族允许 5–10 种甚至全部种族 | 有利于组合练习，不作为规则 Bug；随机标准局仍应保持 5 种 |
| 可接受训练简化 | 可以手工编辑对手阵容、属性与饰品 | 是情景测试优势；由此得到的结论只能称为场景结果，不是天梯胜率 |
| 教学专属机制 | 三类“塑造法术”可主动触发战吼/亡语/回合结束 | 不是官方酒馆法术或 Spellcraft；机制可以保留，但建议对外称“教学塑形工具”避免混淆 |
| 有益优化 | 报价和刷新支持固定种子、结果可复现 | 对回归测试和复盘有利，不应误判为官方随机性错误 |
| 已对齐 | S14 当前规则集启用黑暗赠礼+饰品、关闭畸变 | 与本季方向一致；仓库保留旧畸变代码不等于本季实战开启 |
| 已对齐 | 黑暗赠礼第 3 回合、3 金、每回合 1 次、每局 3 次、3 选 1 | 当前实现基本对齐官方说明 |

## 5. 玩家路径覆盖评估

| 路径 | 当前状态 | 缺口 |
|---|---|---|
| 网页一图流浏览 | 基础展示可用，手机无横向溢出 | 塑造法术导出错误；返回上下文丢失 |
| 一图流进入训练 | 可打开 iframe | 不传所选档位；深滚动横屏会把控件带出视口；无 Unity ready 握手 |
| 简单/初级经营 | 刷新已放开 | 冻结仍锁；跨指南 AllowedCommands 不一致 |
| 作者创建器 | 已有模板、塑造类别、冻结诊断 | 真机软键盘遮挡未证明；选择器搜索也可能被挡 |
| 敌方战场编辑 | 已近全屏，已有删除逻辑 | 没有真实手机触控“选择→删除”PlayMode/E2E |
| 获得卡牌/三连 | 常规购买和部分三连到最右侧 | 黑暗赠礼、反射吊坠漏三连；缺少全入口参数化测试 |
| 中文资源 | 正式定义覆盖时总体可用 | proxy/缺定义路径仍会出现英文和占位图 |

真实玩家测试应至少包含：手机竖/横屏；先滚动再进入；打开软键盘输入并收起；选择敌方随从并删除；刷新、冻结、完成回合；通过购买/发现/英雄/饰品/三连分别获得牌；网页所选指南直接进入相同 Unity 档位。只测方法返回值不能证明这些路径可用。

## 6. Ponytail 全仓过度工程审查

本节只讨论“可删除/可缩小”，不与正确性、安全或性能缺陷混为一谈。

```text
delete: Assets/LearnHearthstone/Runtime/Adapters/Persistence/SaveRepositories.cs 中未被运行时或测试引用的 ISaveRepository/JsonSaveRepository。replacement: nothing.
delete: PromoVideo 中重复哈希或同一成品的多份渲染、截图与临时音频。replacement: 每个素材保留一个真源，每个版本保留一个正式成品。
delete: 未发现项目资产/源码使用证据的顶层 Unity 包候选（Aseprite、PSD Importer、SpriteShape、Tilemap Extras、Collab Proxy、Multiplayer Center、Timeline、Visual Scripting 等）。replacement: 先逐项移除并完成干净导入、编译和构建验证；间接编辑器依赖则保留。
shrink: MatchService 与 UnityTavernTrainerController 中继续增长的卡牌专用分支。replacement: 复用现有正式 Definition/Factory、统一 AddToHand/ResolveTriples 入口和数据驱动效果注册。
net: -54 lines, -8 deps possible.
```

其中只有 `SaveRepositories.cs` 的约 54 行属于高置信无引用删除项；包依赖数量是待干净构建验证的候选上限，不能直接批量删除。微信 SDK 当前仍被 `LearnHearthstone.Editor.asmdef` 和发布脚本引用，不计入可直接删除项。

## 7. 安全与发布链

### 安全

- WebApp 生产 npm 依赖未发现已知漏洞。
- 本次静态扫描未发现提交中的私钥、访问令牌或密码。
- 这不等于完成了 Unity SDK 二进制供应链或线上接口渗透审计。

### Git、Cloudflare 与下载包是三道独立动作

本次只完成 Git checkpoint，没有上线：

1. Cloudflare Pages 项目没有 Git Provider；仓库没有 GitHub Actions。push 当前分支不会自动更新正式站。
2. `Docs/WebGLUiChangeSyncAndDeploymentGuide.zh-CN.md:37` 明确使用 `wrangler pages deploy` 直传，且注明 push 不等于部署。
3. Windows ZIP 是独立 R2 不可变对象流程：构建 ZIP、上传新 objectKey、回读 SHA-256，再更新网站 manifest，最后重新 build 并手动部署 Pages。

因此用户所说的“全部提交”应固定解释为：

```text
Git 提交并推送
→ 构建 Unity/WebGL/Windows 与 WebApp dist
→ 上传新的 Windows ZIP 到 R2 不可变对象
→ 回读并核对 SHA-256、大小、公开 URL
→ 更新网站下载 manifest
→ 手动部署 Cloudflare Preview 并验收
→ 手动部署 Cloudflare Production
→ 从正式域名完成网页、Unity、下载包和校验值验收
```

未走完整链路时，不应把“已 push”描述为“已上线”。

## 8. 建议修复顺序

### 第一批：恢复发布可信度

1. 定位并修复全量 Unity 测试卡住，设定明确超时与测试日志产物。
2. 修复网页进入训练场的滚动复位、Unity ready 握手和失败状态。
3. 修复网页一图流塑造法术唯一性，并把 guide/profile 真正传入 Unity。
4. 统一简单/初级模式刷新和冻结权限，清理互相矛盾的旧测试。

### 第二批：统一游戏结算入口

1. 建立单一 `AddToHand` 事务：末尾加入、上限、获得触发、三连解析、UI 更新。
2. 迁移购买、发现、黑暗赠礼、饰品、英雄奖励、多卡加入和三连重新置入。
3. 用参数化测试覆盖每个入口，并至少增加一条真实玩家 PlayMode 路径。

### 第三批：数据真源与手机体验

1. 修正宝石日、36.2 正式/Partial 身份和单打成员表，增加无 `BGDUO_` 反向测试。
2. 把 proxy 衍生物迁回正式 Definition/Factory，并补中文、DefinitionId、ImagePath、sprite 测试。
3. 使用真实键盘高度/visualViewport 处理作者输入；补手机竖横屏的敌方删除和冻结测试。

### 第四批：控制复杂度

1. 从 `MatchService` 抽出统一手牌事务和各季机制处理器，优先处理本次已经造成漏结算的边界。
2. 清理高置信死代码、重复媒体；逐项验证并移除无用 Unity 包。
3. 将暂缓的微信渠道隔离为可选程序集/包，降低主线编译与仓库负担。

## 9. 最终发布判定

**当前判定：不建议发布新的正式网站和 Windows 下载包。**

允许继续内部测试和分支迭代。满足以下条件后再进入“Git → R2 → Cloudflare → 正式网站”的完整发布流程：

- P0-1 消除，完整 Unity EditMode 稳定通过；关键手机 PlayMode/E2E 通过。
- 6 个 P1 问题关闭并有回归测试。
- 网页/Unity 塑造法术数据一致；简单/初级刷新与冻结行为一致。
- 单打池无双打条目泄漏，关键衍生物不再走英文/缺图 proxy。
- Preview 域名完成手机竖屏、横屏、真实键盘、敌方删除和下载校验验收。
