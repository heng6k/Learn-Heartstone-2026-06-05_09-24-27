# 2026-08-13 手机端一图流阵容栏热修发布记录

## 共同身份

- 源提交：`b191236323e63d4b329df51f6500c295b233fe9c`
- 源分支：`codex/mobile-onepage-rail-hotfix-20260813`
- 修复提交：`fix(web-unity): contain mobile one-page guide rail`
- `buildId`：`20260813T005958Z-b191236`
- 内容版本：`36.2-20260813-b191236`
- `packageFingerprint`：`9778fbb1b131bb6952677b40e2b4151b40b6c9511f4d91b442cccd1843760f49`
- `sourceDirty`：`false`
- Unity：`6000.4.10f1`
- 回滚目标：`1bd34e47-db91-46af-b884-b39464e7c45f`

## 修复范围

- 手机紧凑视口的一图流阵容栏由 226 高度调整为 176。
- 阵容列表使用现有 UGUI `ScrollRect + Mask`，不再溢出覆盖详情或拦截后续触摸。
- 桌面非 Compact 布局保持不变。
- 新增 844×390 回归测试，确保阵容按钮位于带遮罩的滚动内容中。

## 构建与测试

- `StrategyGuideUiTests`：23/23 通过。
- WebApp Node 测试：12/12 通过。
- 完整 WebGL：117,817,429 字节，构建耗时 14分34秒。
- ReleaseCandidate：12 个 data 分片，结构与哈希校验通过。
- 最终 `dist`：195 个文件，125,443,948 字节。
- `dist` 树哈希：`dfb55edb633ea3ff2910eaea2d45832ad0128bb9dd5f09a46d3df2db1e33ce78`。

## Preview

- 状态：通过。
- Deployment：`ed85ffac-7a2f-4fb4-b525-c37b03cc6207`
- 地址：<https://ed85ffac.learn-heartstone.pages.dev>
- 390×844：8 套攻略、每套 3 档、0 横向溢出、确认前 0 Unity 请求。
- 浏览器：0 控制台错误、0 真实失败请求。
- Unity：metadata 为干净 `b191236`；12 个分片；首片 `Content-Encoding: br`。

## Production

- 状态：`production`。
- Deployment：`9691901d-20de-475c-b2cf-8b27630edc6d`
- 部署地址：<https://9691901d.learn-heartstone.pages.dev>
- 正式域名：<https://jsoncool.com>
- Cloudflare 从 Preview 同一份冻结 `dist` 创建 Production；194 个资产全部复用，未上传新静态文件。
- 正式域名复验：8 套攻略、3 档、0 横向溢出、确认前 0 Unity 请求、0 控制台错误、0 真实失败请求。
- 正式域名回读：`buildId=20260813T005958Z-b191236`、`sourceCommit=b191236...`、`sourceDirty=false`、12 分片、Brotli 头正确。

## 渠道状态

- 手机版轻量网页：`production`
- 完整 Unity 网页版：`production`
- 网页下载包：本轮未重新生成
- 原生微信小程序：`not-in-scope`
- Unity 微信小游戏：`not-in-scope`

## 已知边界

- 本地 `wrangler pages dev` 在此机器监听端口后首包超时；同一 `dist` 已通过 Vite 本地浏览器验收，并在真实 Cloudflare Preview 与 Production 上完成 Functions/Brotli 验收。
- 页面切换时浏览器会取消尚未完成的懒加载卡图请求；被取消资源已逐个直接请求，全部返回 HTTP 200。
