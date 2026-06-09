# 官方 API 接入说明

> 本文用于后续接入和核对 Hearthstone 官方数据。所有真实卡牌文本、类型、关键词、酒馆等级、图片等，优先以 Blizzard / Battle.net 官方 Hearthstone Game Data API 为准；HearthstoneJSON 只能作为离线检索和辅助对照。

## 官方入口

- 官方开发者门户：[Battle.net Community Developer Portal](https://community.developer.battle.net/)
- Hearthstone Game Data API 页面：[Hearthstone Game Data APIs](https://community.developer.battle.net/documentation/hearthstone/game-data-apis)
- 旧入口会跳转到新门户：[develop.battle.net Hearthstone Game Data APIs](https://develop.battle.net/documentation/hearthstone/game-data-apis)
- OAuth / 认证文档：[Battle.net OAuth APIs](https://community.developer.battle.net/documentation/battle-net/oauth-apis)
- Getting Started：[Getting Started](https://community.developer.battle.net/documentation/guides/getting-started)
- 地域说明：[Regionality and APIs](https://community.developer.battle.net/documentation/guides/regionality-and-apis)

## 认证怎么接

Hearthstone Game Data API 需要 Battle.net OAuth token。

开发者账号准备：

- 登录或创建 Battle.net 开发者账号。
- 在 API Access 创建 client。
- 为 client 生成 secret。
- Battle.net 开发者门户要求账号启用 Authenticator，并接受开发者 API 条款。

项目建议用 Application Authentication，也就是 client credentials flow：

```text
POST https://oauth.battle.net/token
Authorization: Basic base64(client_id:client_secret)
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
```

返回的 `access_token` 用于后续 Hearthstone API 请求：

```text
Authorization: Bearer {access_token}
```

也可以用 query parameter：

```text
?access_token={access_token}
```

但项目代码里建议统一用 `Authorization` header，避免 token 出现在日志、URL 或复制记录里。

中国区认证 host：

```text
https://oauth.battlenet.com.cn/token
```

当前项目主要做本地酒馆战棋训练器，默认先接全局分区，不接中国区。

## Host 和地区

非中国区 API host：

```text
https://{region}.api.blizzard.com
```

官方支持的非中国区 region：

- `us`
- `eu`
- `kr`
- `tw`

中国区 API host：

```text
https://gateway.battlenet.com.cn
```

项目建议默认：

```text
region = us
locale = zh_CN 或 en_US
```

说明：

- 如果要核对中文文本，用 `locale=zh_CN`。
- 如果要对照英文社区资料或 HearthstoneJSON，用 `locale=en_US`。
- 如果请求不传 `locale`，部分接口会返回全部翻译，数据量更大，不适合训练器运行时常规请求。

## 项目优先接入顺序

项目不是炉石百科站，不需要一开始接所有官方接口。建议按这个顺序：

1. `GET /hearthstone/metadata`
   - 先拿官方分类表，建立 slug 对照。
2. `GET /hearthstone/cards?gameMode=battlegrounds`
   - 拉酒馆战棋卡牌列表。
3. `GET /hearthstone/cards/:idorslug?gameMode=battlegrounds`
   - 单卡精确核对文本、图片和分类。
4. 非官方 HearthstoneJSON
   - 只做离线全文检索、批量 diff、断网缓存候选源。

暂时不优先接：

- `cardbacks`：卡背，和酒馆训练器无关。
- `deck`：构筑套牌，和酒馆训练器无关。
- `mercenaries`：佣兵模式，和酒馆训练器无关。

## Metadata：分类元数据

### All metadata

```text
GET /hearthstone/metadata
```

用途：

- 返回卡牌分类信息。
- 官方说明包括：card set、set group、rarity、class、card type、minion type、keywords。
- 项目里用它建立官方 slug 到本地枚举/标签的映射。

参数：

| 参数 | 必填 | 用途 |
| --- | --- | --- |
| `locale` | 否 | 本地化语言，默认 `en_US`。 |

项目接入点：

- `CardTag` / `EffectTag` 的官方关键词对照。
- 随从种族 slug，例如 beast、murloc、dragon、undead、quilboar、naga、mech、demon、elemental、pirate。
- 卡牌类型 slug，例如 minion、spell、hero、location 等。
- 稀有度、职业、扩展包等暂时只存原始 metadata，训练器首版不依赖。

### Specific metadata

```text
GET /hearthstone/metadata/:type
```

用途：

- 只取某一种 metadata，适合调试或局部刷新。

官方有效 `:type`：

- `sets`
- `setGroups`
- `types`
- `rarities`
- `classes`
- `minionTypes`
- `keywords`

参数：

| 参数 | 必填 | 用途 |
| --- | --- | --- |
| `:type` | 是 | 要取的 metadata 类型。 |
| `locale` | 否 | 本地化语言，默认 `en_US`。 |

项目怎么用：

- 新增关键词或种族时，优先用 `metadata/keywords` 和 `metadata/minionTypes` 核对 slug。
- 不要手写英文显示名当逻辑键，逻辑里应使用官方 slug 或项目自己的稳定枚举。

## Cards：卡牌数据

### Card search

```text
GET /hearthstone/cards
```

用途：

- 按条件搜索卡牌。
- 官方描述是返回匹配搜索条件的最新卡牌列表。
- 对本项目最重要，是拉取酒馆战棋卡牌池的主接口。

通用参数：

| 参数 | 必填 | 用途 |
| --- | --- | --- |
| `locale` | 否 | 本地化语言。文本核对建议 `zh_CN`，英文 diff 建议 `en_US`。 |
| `set` | 否 | 扩展包 slug。酒馆训练器通常不用它过滤。 |
| `class` | 否 | 职业 slug。酒馆战棋通常不用它做主过滤。 |
| `manaCost` | 否 | 法力值，支持逗号分隔多个数字。酒馆战棋通常不靠它。 |
| `attack` | 否 | 攻击力，支持逗号分隔。可用于调试筛选。 |
| `health` | 否 | 生命值，支持逗号分隔。可用于调试筛选。 |
| `collectible` | 否 | `1` 只返回可收藏，`0` 只返回不可收藏，`0,1` 返回全部。酒馆战棋应谨慎使用，很多衍生物和特殊牌可能不是收藏牌。 |
| `rarity` | 否 | 稀有度 slug，来自 metadata。酒馆训练器通常不作为核心条件。 |
| `type` | 否 | 卡牌类型 slug，例如 minion、spell。区分随从、法术、酒馆法术时有用。 |
| `minionType` | 否 | 随从种族 slug，来自 metadata。 |
| `keyword` | 否 | 关键词 slug，例如 battlecry、deathrattle。用于机制池分类。 |
| `textFilter` | 否 | 文本搜索。官方要求同时传 `locale`。适合核对“亡语”“塑造法术”等文本。 |
| `gameMode` | 否 | 游戏模式。酒馆战棋必须传 `battlegrounds`。默认是 constructed。 |
| `spellSchool` | 否 | 法术派系 slug。对普通法术有用，对酒馆法术不是主要分类。 |
| `page` | 否 | 页码。 |
| `pageSize` | 否 | 每页数量；不传时官方会自动选择，超过最大值也会被官方限制。 |
| `sort` | 否 | 排序。构筑搜索支持 `manaCost`、`attack`、`health`、`class`、`groupByClass`、`name` 等。 |
| `order` | 否 | 已废弃。不要在新代码里使用，用 `sort` 替代。 |

项目接入建议：

```text
GET https://us.api.blizzard.com/hearthstone/cards
  ?gameMode=battlegrounds
  &locale=zh_CN
  &page=1
  &pageSize=500
```

接到项目哪里：

- 卡池导入器：生成或校验 `battlegroundsMinions.json` / 后续 spell 数据文件。
- 卡牌搜索面板：按名称、种族、关键词、等级过滤。
- 机制池标签：根据 `keyword`、`type`、`minionType`、文本关键词生成初始候选标签，再人工校正。
- 文本核对工具：对比本地描述和官方描述。

注意：

- 不要只用 `type=spell` 判断“酒馆法术”。酒馆法术是 Battlegrounds 里的特殊牌，需要结合官方返回的类型、gameMode 数据、文本和本地规则标签。
- 鲜血宝石、黏黏盾、尖利箭矢、塑造法术等不是酒馆刷新里的 Tavern Spell，不应占用酒馆法术刷新格。

### Battlegrounds card search

```text
GET /hearthstone/cards?gameMode=battlegrounds
```

用途：

- 官方专门列出的酒馆战棋搜索方式。
- 这是本项目最核心的卡牌入口。

酒馆战棋专用参数：

| 参数 | 必填 | 用途 |
| --- | --- | --- |
| `gameMode=battlegrounds` | 建议必传 | 指定酒馆战棋。 |
| `tier` | 否 | 酒馆等级。有效值是 `1` 到 `6`，也可以传 `hero`。支持逗号分隔。 |

酒馆战棋搜索还支持：

- `attack`
- `health`
- `minionType`
- `keyword`
- `textFilter`
- `page`
- `pageSize`
- `sort`
- `order`，已废弃

酒馆战棋排序值：

- `tier:asc`
- `tier:desc`
- `attack:asc`
- `attack:desc`
- `health:asc`
- `health:desc`
- `name:asc`
- `name:desc`

项目常用请求：

```text
# 查一本
GET /hearthstone/cards?gameMode=battlegrounds&tier=1&locale=zh_CN

# 查二本
GET /hearthstone/cards?gameMode=battlegrounds&tier=2&locale=zh_CN

# 查英雄
GET /hearthstone/cards?gameMode=battlegrounds&tier=hero&locale=zh_CN

# 查带亡语的酒馆战棋牌
GET /hearthstone/cards?gameMode=battlegrounds&keyword=deathrattle&locale=zh_CN

# 查文本里包含塑造法术的牌
GET /hearthstone/cards?gameMode=battlegrounds&textFilter=塑造法术&locale=zh_CN
```

接到项目哪里：

- 酒馆等级卡池：`tier` 对应本地 tavern tier。
- 对手战场编辑器：按等级、种族、关键词搜索并添加卡。
- 后续三本扩展：先拉 `tier=3`，再按机制池分组实现。
- 黑盒测试场景：保存官方 ID / slug，避免本地名称变化导致测试找不到牌。

### Detailed card search example

官方文档还有一个 Detailed card search example，本质仍是：

```text
GET /hearthstone/cards
```

它只是展示多个参数组合搜索，比如 `set`、`class`、`manaCost`、`rarity`、`type`、`minionType`、`keyword`、`textFilter`、`sort`。

项目里不用单独做接口，只需要让卡牌导入工具支持组合 query。

### Fetch one card

```text
GET /hearthstone/cards/:idorslug
```

用途：

- 用 card ID 或 slug 获取单张卡牌详情。
- `:idorslug` 可以从 `GET /hearthstone/cards` 的搜索结果中发现。

参数：

| 参数 | 必填 | 用途 |
| --- | --- | --- |
| `:idorslug` | 是 | 卡牌 ID 或 slug，例如官方示例 `52119-arch-villain-rafaam`。 |
| `locale` | 否 | 本地化语言。 |
| `gameMode` | 否 | 游戏模式；核对酒馆牌时传 `battlegrounds`。 |

项目接入点：

- 单卡详情面板。
- “官方核对”按钮。
- 修卡牌逻辑时查真实文本、图片、种族、关键词、酒馆等级。
- 测试失败时根据本地 card id 反查官方数据。

项目建议：

- 本地卡牌数据存官方 `id` 和 `slug`。
- 展示名称可以随 locale 变，逻辑不要依赖展示名称。

## Card Backs：卡背

### Card Back Search

```text
GET /hearthstone/cardbacks
```

用途：

- 搜索卡背。
- 支持按分类、文本、排序、分页查询。

参数：

| 参数 | 必填 | 用途 |
| --- | --- | --- |
| `locale` | 否 | 本地化语言。 |
| `cardBackCategory` | 否 | 卡背分类。 |
| `textFilter` | 否 | 文本搜索；官方要求同时传 `locale`。 |
| `sort` | 否 | 支持 `name:asc`、`name:desc`、`dateAdded:asc`、`dateAdded:desc`。默认按日期倒序。 |
| `page` | 否 | 页码。 |
| `pageSize` | 否 | 每页数量。 |
| `order` | 否 | 已废弃。 |

本项目怎么处理：

- 当前酒馆训练器不需要接。
- 只有未来做账号收藏、外观、卡背预览时才接。

### Fetch one card back

```text
GET /hearthstone/cardbacks/:idorslug
```

用途：

- 用卡背 ID 或 slug 获取单个卡背。

本项目暂不接。

## Decks：构筑套牌

### Get deck by code

```text
GET /hearthstone/deck
```

用途：

- 用套牌代码解析构筑套牌。

参数：

| 参数 | 必填 | 用途 |
| --- | --- | --- |
| `locale` | 否 | 本地化语言，默认 `en_US`。 |
| `code` | 否 | 套牌代码，需要 URL encode。 |
| `ids` | 否 | 卡牌 ID 列表；如果同时有 `code`，官方会忽略 `ids`。 |
| `hero` | 否 | 英雄 card ID。和 `ids` 一起使用；不传时 API 会尝试根据卡组自动添加默认英雄和职业。 |

本项目怎么处理：

- 当前酒馆训练器不接。
- 如果未来做构筑练习器或卡组导入，可以复用这个接口。

### Get deck by card list

同样是：

```text
GET /hearthstone/deck
```

区别只是用 `ids` + `hero` 构造 deck，而不是传 `code`。

本项目暂不接。

## Mercenaries：佣兵搜索

官方 Cards 资源里还有 `gameMode=mercenaries` 的搜索示例。

本项目不接佣兵模式。原因：

- 数据结构和酒馆战棋无关。
- 参数里有 `mercenaryId`、`mercenaryRole`、`defaultMercenary` 等佣兵专属字段。
- 接入会增加噪音，不利于保持酒馆训练器卡池干净。

## 酒馆法术、普通法术、塑造法术怎么区分

官方 API 能给我们真实卡牌类型、gameMode、关键词、文本、图片和 ID，但项目逻辑仍需要本地标签层。

建议本地标签分三层：

1. 官方原始字段
   - `id`
   - `slug`
   - `name`
   - `text`
   - `type`
   - `minionType`
   - `keywordIds` / `keywords`
   - `battlegrounds.tier` 等官方返回字段
2. 项目逻辑标签
   - `CardTag.Battlecry`
   - `CardTag.Deathrattle`
   - `CardTag.Spellcraft`
   - `CardTag.TavernSpell`
   - `CardTag.NormalSpell`
   - `CardTag.Token`
   - `CardTag.CombatReward`
3. 规则池标签
   - 战吼池
   - 亡语池
   - 进击池
   - 复仇池
   - 光环池
   - 指向型法术池
   - 发现型法术池
   - 法强/增益法术池
   - 塑造法术池

关键规则：

- 酒馆刷新中出现的是 Tavern Spell。
- 鲜血宝石、黏黏盾、尖利箭矢、塑造法术等按普通法术或衍生特殊法术处理。
- 普通法术不占酒馆“每次刷新几个随从、几个酒馆法术”的法术刷新格，除非它本身就是官方酒馆法术。
- 不要只靠中文名字判断类型，最终要回到官方 ID / slug + 本地规则标签。

## 导入器建议

后续如果做官方数据导入器，建议流程：

1. 获取 token。
2. 拉 `GET /hearthstone/metadata?locale=en_US`。
3. 拉 `GET /hearthstone/metadata?locale=zh_CN`。
4. 拉 `GET /hearthstone/cards?gameMode=battlegrounds&locale=en_US&pageSize=500`。
5. 拉 `GET /hearthstone/cards?gameMode=battlegrounds&locale=zh_CN&pageSize=500`。
6. 将英文和中文结果按官方 `id` 合并。
7. 保留官方原始 JSON 快照。
8. 生成项目用精简数据。
9. 对缺失逻辑的卡牌打上 `NeedsManualRule`。
10. 人工补规则池标签和具体效果实现。

不要让运行时每次打开项目都请求官方 API。更适合做成编辑器工具或离线导入命令，生成本地 JSON 后进入测试。

## HearthstoneJSON 的位置

非官方镜像：

- [HearthstoneJSON latest cards](https://api.hearthstonejson.com/v1/latest/enUS/cards.json)

只能用于：

- 快速全文搜索。
- 离线比较。
- 辅助批量查字段。
- 官方 API 暂时不可访问时做候选数据源。

不能用于：

- 最终确认真实文本。
- 最终确认酒馆法术/普通法术边界。
- 替代 Blizzard 官方 API。

## 本项目当前最该用的 API

当前阶段是战斗可视化和三本前置，最有价值的官方 API 使用方式是：

```text
# 查三本全部酒馆战棋牌
GET /hearthstone/cards?gameMode=battlegrounds&tier=3&locale=zh_CN

# 查三本英文数据用于和社区资料/HearthstoneJSON diff
GET /hearthstone/cards?gameMode=battlegrounds&tier=3&locale=en_US

# 查亡语牌
GET /hearthstone/cards?gameMode=battlegrounds&keyword=deathrattle&locale=zh_CN

# 查塑造法术相关文本
GET /hearthstone/cards?gameMode=battlegrounds&textFilter=塑造法术&locale=zh_CN

# 查单卡详情
GET /hearthstone/cards/{id-or-slug}?gameMode=battlegrounds&locale=zh_CN
```

接入后优先服务这些功能：

- 三本机制池分类。
- 对手战场可视化编辑器的卡牌搜索。
- 单卡官方文本核对。
- 本地卡牌数据和官方数据 diff。
- 后续白盒测试场景绑定官方 ID。
