# 2026-08-12 手机版网页、完整 Unity 网页版与下载包发布记录

本记录对应 2026-08-12 已上线版本。构建与上传严格遵守“先提交 Git，再从干净工作树生成候选”的顺序；旧的 `cc03773-dirty` WebGL r13 候选及其 ZIP 不得复用。

## 交付结果

| 交付面 | 状态 | 说明 |
| --- | --- | --- |
| 共享攻略与卡牌数据 | 已发布 | 36.2 / Season 14 攻略、卡池、抉择与点金规则 |
| 手机版轻量网页 | 已发布 | 手机直达一图流；确认前不加载 Unity |
| 完整 Unity 网页版 | 已发布 | 新酒馆开门页、窗口模式、手机全屏、电脑浏览器全屏及完整 Unity 对局 |
| 网页下载包 | 已生成并复验 | 与线上使用同一 `ReleaseCandidate` |
| 原生微信小程序 | 本轮不发布 | 不提交、不构建、不上传 |
| Unity 微信小游戏 | 本轮不发布 | 不提交、不构建、不上传 |

正式入口：[https://jsoncool.com](https://jsoncool.com)

## Git 提交链

| 顺序 | 提交 | SHA |
| ---: | --- | --- |
| 1 | `feat(content): freeze season 14 guides and card rules` | `461ad2ff71c72d433a9685eeb75694febe603234` |
| 2 | `feat(web-mobile): ship lightweight guide experience` | `8e334454ed8bcbcc29494769fe7cf466a1cc0112` |
| 3 | `feat(web-unity): ship full browser play flow` | `6a9a5f3a93cbe871576331441d67ee970e23fb5d` |
| 4 | `build(web): package and configure Cloudflare release` | `d140380d349f89dee65afbb232628610f5c6fa2a` |
| 5 | `docs(release): record 2026-08-12 web delivery source` | `68b4922a04faeecd77edd6dacdd9532ce36f5479` |
| 6 | `feat(web): add responsive game fullscreen gate` | `be37bd59a17791830719246c3fe41ccd12570e5a` |
| 7 | `perf(webgl): load release data chunks concurrently` | `076893aee7a52bd2c1216767219b567095e47702` |
| 8 | `fix(webgl): retry chunk downloads on slow links` | `b96d5441e7ed1dce0afae16248fe2e6857944b07` |

最终发布源提交为 `b96d5441e7ed1dce0afae16248fe2e6857944b07`，分支为 `codex/wip-current-state`。文档证据提交位于它之后，不改变已发布产物的源码身份。

## 干净构建与发布身份

- Unity：`6000.4.10f1`
- `bundleVersion`：`0.1.0-alpha`
- WebGL 原始构建：`Builds/WebGL/LearnHeartstone_20260812-d140380`
- ReleaseCandidate：`Builds/ReleaseCandidate/LearnHeartstone_20260812-b96d544-clean`
- `buildId`：`20260812T082640Z-b96d544`
- `contentVersion`：`36.2-20260812-b96d544`
- `sourceCommit`：`b96d5441e7ed1dce0afae16248fe2e6857944b07`
- `sourceDirty`：`false`
- `packageFingerprint`：`8f98bc12a2d8580adebc07c5f07ed490f206889a6dbc62a35053ef1a3934a3af`
- WebGL 数据：12 个 Brotli 分片；线上加载器采用 6 路并发、每片最多 3 次重试和指数退避。

Unity 游戏二进制来自完整、非 `buildScriptsOnly` 的干净 WebGL 构建；后续提交只修改网页入口与浏览器分片加载器，因此没有重复编译未变化的 IL2CPP 游戏逻辑。

## 验证结果

### Unity 与发布工具

- Unity 全量 EditMode：128/130 在 `-nographics` 下通过；两个截图用例仅因无图形设备失败，随后在图形模式重跑 2/2 通过，合计 130/130。
- 最终 WebGL/Cloudflare 发布契约：3/3 通过。
- WebGL 分块往返：1/1 通过。
- WebApp：11/11 通过。
- `npm run build:with-unity` 通过，最终 `dist` 附加的是上述 `b96d544` 候选。
- `wrangler pages dev` 完整 Unity 启动：6.8 秒，0 页面错误，0 请求失败。

### 手机与电脑开门页

- 手机 390×844：横向溢出 0，页面错误 0，确认前 Unity 请求 0。
- 电脑 1440×900：横向溢出 0，页面错误 0，确认前 Unity 请求 0。
- 电脑点击“全屏进入训练场”后，浏览器原生 Fullscreen API 生效并创建 1 个 Unity iframe。
- 手机安装为 PWA 时使用 `display: fullscreen` 与横屏方向；普通手机浏览器受平台限制时提供清晰的全屏/添加到主屏幕指引。

### Production HTTP

- `/`、`/guides`、`/play`、`/manifest.webmanifest`、`/unity/content/content-manifest.json`：全部 200。
- 数据分片：`Content-Type: application/octet-stream`、`Content-Encoding: br`、`Cache-Control: public, max-age=31536000, immutable, no-transform`。
- `jsoncool.com/unity/release-meta.json` 与 Production deployment 均返回 `b96d544`、`sourceDirty: false`。

### 当前网络说明

本机到 `*.pages.dev` 的大文件连接出现过 `ERR_SSL_PROTOCOL_ERROR` 和 `ERR_CONNECTION_CLOSED`。实测单个 10.6 MiB 分片约 30.7 秒、约 348 KiB/s；Cloudflare 增量上传本身只需数秒。为避免把本地链路问题误判为 Unity 构建问题，本次同时采用以下证据：

1. 最终候选在本地 Wrangler Pages 运行时完整启动通过；
2. Production 发布身份、路由、响应头、手机/电脑 UI 与浏览器全屏均在正式域名复验通过；
3. 加载器加入 6 路限流和有界重试，慢链路断开时不再只依赖一次请求。

## Cloudflare Pages

- 项目：`learn-heartstone`
- 正式域名：[jsoncool.com](https://jsoncool.com)
- 最终 Preview：`5caa5210-8cb6-46f5-afb4-8de58aaf36b7`
- Preview URL：[https://5caa5210.learn-heartstone.pages.dev](https://5caa5210.learn-heartstone.pages.dev)
- Production：`4cb42d49-f32b-4069-8510-56e3bc315af1`
- Production URL：[https://4cb42d49.learn-heartstone.pages.dev](https://4cb42d49.learn-heartstone.pages.dev)
- Production source：`b96d544`
- 回滚目标：`c9ae9e3a-69f4-4a4a-b919-9f9cb7219a0a`（source `cc03773`）

## 网页下载包

- 文件：`Builds/DownloadPackage/LearnHeartstone-Web-20260812T082640Z-b96d544.zip`
- 字节数：`121285420`
- SHA-256：`5063257066232BB3B793759DAB7137EAE1AFF3CA533DA7345965E424214F24B5`
- 内容：最终 ReleaseCandidate、`serve_webgl.py`、中文使用说明。
- 解压复验：52 个文件；首页、`release-meta.json`、内容清单均为 200；数据分片 Brotli/MIME/immutable 响应头正确。

## 禁止事项与回滚

- 禁止上传 `0.1.0-alpha__20260811T133749Z-cc03773-dirty`、`d140380-dirty` 或其旧 ZIP。
- 禁止把 `076893a`、`b6b80ba` 中间候选当作最终下载包；最终身份必须为 `b96d544`。
- 禁止从包含微信 SDK、宣传视频或其他未提交工作的主工作区直接构建正式候选。
- 禁止把 AppID、AppSecret、平台缓存、本地构建目录或无关未提交文件混入发布提交。
- Production 出现版本身份、路由、响应头或主要入口回归时，回滚到 `c9ae9e3a-69f4-4a4a-b919-9f9cb7219a0a`。
