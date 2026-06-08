# 官方 API 入口

## Hearthstone Game Data API

- 官方文档：https://develop.battle.net/documentation/hearthstone/game-data-apis
- 卡牌搜索：https://develop.battle.net/documentation/hearthstone/game-data-apis#card-search
- 卡牌详情：https://develop.battle.net/documentation/hearthstone/game-data-apis#card
- 认证文档：https://develop.battle.net/documentation/guides/getting-started

使用要点：

- 这是 Blizzard / Battle.net 官方 Hearthstone Game Data API。
- 需要 Battle.net Client ID / Client Secret，通过 OAuth client credentials 获取 token。
- 查真实卡牌文本、类型、关键字、随从等级时，优先用这个 API。
- 区分 `TavernSpell` 和普通 `Spell` 时，以官方返回的卡牌类型和 Battlegrounds 文本为准。

## 非官方镜像

- HearthstoneJSON：https://api.hearthstonejson.com/v1/latest/enUS/cards.json

HearthstoneJSON 适合快速全文检索和离线比对，但不是官方 API。需要最终确认时回到 Battle.net 官方文档和接口。
