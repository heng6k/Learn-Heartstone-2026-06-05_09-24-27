# 2026-08-12 手机版网页、完整 Unity 网页版与下载包发布记录

本记录对应 2026-08-12 的正确提交与发布链。构建和上传必须先完成 Git 提交，再从记录的源提交创建干净工作树；旧的 `cc03773-dirty` WebGL r13 候选与下载包不得复用。

## 交付范围

| 交付面 | 状态 | 说明 |
| --- | --- | --- |
| 共享攻略与卡牌数据 | committed | 36.2 / Season 14 攻略、卡池、抉择与点金规则 |
| 手机版轻量网页 | committed | 手机直达一图流、8 套攻略 × 3 档，不预加载 Unity |
| 完整 Unity 网页版 | committed | Unity 准备阶段、一图流创建/查看、牌库与抉择交互 |
| 网页下载包 | packaging-pending | 必须与上线 WebGL 使用同一 ReleaseCandidate |
| 原生微信小程序 | not-in-scope | 本轮不提交、不构建、不上传 |
| Unity 微信小游戏 | not-in-scope | 本轮不提交、不构建、不上传 |

## Git 提交链

| 顺序 | 提交 | SHA |
| ---: | --- | --- |
| 1 | `feat(content): freeze season 14 guides and card rules` | `461ad2ff71c72d433a9685eeb75694febe603234` |
| 2 | `feat(web-mobile): ship lightweight guide experience` | `8e334454ed8bcbcc29494769fe7cf466a1cc0112` |
| 3 | `feat(web-unity): ship full browser play flow` | `6a9a5f3a93cbe871576331441d67ee970e23fb5d` |
| 4 | `build(web): package and configure Cloudflare release` | `d140380d349f89dee65afbb232628610f5c6fa2a` |

本轮产物以第 4 块 `d140380d349f89dee65afbb232628610f5c6fa2a` 为发布源提交；本记录和上传后的证据各自形成后续文档提交，不改变已上传产物的源码身份。

## 已完成的提交前验证

- `WebApp`：`npm test`，9/9 通过。
- `WebApp`：`npm run build` 通过。
- WebGL 数据分块：`node --test Tools/Release/webgl-data-chunks.test.mjs`，1/1 通过。
- 攻略投影：8 套攻略、24 个难度档、121 张移动端缩略图。
- 四个提交块均通过 `git diff --cached --check` 和凭据关键字扫描。

## 干净构建与发布身份

- 发布源提交：`d140380d349f89dee65afbb232628610f5c6fa2a`
- 分支：`codex/wip-current-state`
- Unity 版本：`PENDING`
- Unity `bundleVersion`：`PENDING`
- WebGL 原始构建：`PENDING`
- ReleaseCandidate：`PENDING`
- `buildId`：`PENDING`
- `contentVersion`：`PENDING`
- `packageFingerprint`：`PENDING`
- 源状态：必须为 `sourceDirty: false`
- 回滚目标：发布前 `jsoncool.com` 当前 Production deployment

## 浏览器与发布结果

### 手机版轻量网页

- 状态：`committed`
- Preview：`PENDING`
- Production：`PENDING`
- 390×844 / 430×932：`PENDING`
- 攻略：预期 8 套 × 3 档
- 确认前 Unity 请求：预期 0
- 横向溢出 / 浏览器错误：`PENDING`

### 完整 Unity 网页版

- 状态：`committed`
- Preview：`PENDING`
- Production：`PENDING`
- 1280×720 / 1600×900：`PENDING`
- Brotli 与数据分片：`PENDING`
- 冷启动与 300 秒门限：`PENDING`
- 一图流 PNG 浏览器下载：`PENDING`
- 浏览器错误：`PENDING`

### 网页下载包

- 状态：`packaging-pending`
- 文件：`PENDING`
- 字节数：`PENDING`
- SHA-256：`PENDING`
- 解压 HTTP 复验：`PENDING`

## Cloudflare Pages

- 项目：`learn-heartstone`
- 正式域名：[jsoncool.com](https://jsoncool.com)
- Preview deployment：`PENDING`
- Production deployment：`PENDING`
- Production 发布后复验：`PENDING`

## 禁止事项与回滚

- 禁止上传 `0.1.0-alpha__20260811T133749Z-cc03773-dirty` 或它生成的旧 ZIP。
- 禁止从当前含有其他未提交工作的主工作区直接构建正式候选。
- 禁止把微信 SDK、AppID、AppSecret、平台缓存、宣传视频或本地构建目录混入本轮提交。
- Preview 任一必须门禁失败时保持当前 Production；Production 复验失败时回滚到记录的上一 deployment。
