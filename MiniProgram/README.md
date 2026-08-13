# 炉石学习助手：一图流手机小程序

这是项目的原生微信小程序专版，面向手机用户提供“一图流浏览 + 操作训练”。它不运行 Unity、不重复实现战斗结算，也不使用 `web-view` 包装 WebGL。

当前内容：

- 8 套种族攻略，每套简单、初级、困难 3 个档位。
- 真实英雄/随从/法术/饰品牌图和移动缩略图。
- 回合、酒馆等级、金币、酒馆/战场/手牌起手局面。
- 核心路线、推荐大小饰品、三类教学塑造法术。
- 操作步骤、当前动作、完成/撤销、本地进度、收藏、分享和编号导入。
- 刘海/圆角安全区、96rpx 级触控目标和底部固定主操作。

## 数据真源与同步

小程序不手写第二套攻略。`Tools/Release/sync-mini-program-content.py` 从 Unity 权威攻略、卡牌目录和本地化生成：

- `fixtures/guides.js`：8 套攻略 × 3 档的移动投影。
- `assets/cards/*.jpg`：仅导出攻略实际引用的缩略图。

运行同步：

```powershell
Set-Location 'D:\unity project\Learn Heartstone'
python Tools\Release\sync-mini-program-content.py
```

生成器会拒绝“有真实素材定义但找不到牌图”的发布。三张项目专用塑造法术没有官方卡图，使用明确标注的教学牌壳，不伪造官方素材。

## 本地接口

`services/guide-api.js` 提供统一适配：

- `GET /api/guides`
- `GET /api/guides/{guideId}?profile=<profileId>`
- `POST /api/events`

`config.js` 的 `apiBaseUrl` 为空时读取发布 fixture；配置 HTTPS API 后走远端接口。没有部署远端服务时，不得把 fixture 模式描述成云端已上线。

导入支持：

- `guideId`
- `guideId:profileId`
- 带 `guideId/profileId` 的分享链接
- 兼容旧 20 位场景分享码

## 测试与预览

```powershell
Set-Location 'D:\unity project\Learn Heartstone\MiniProgram'
npm test
```

同步、测试通过后使用微信开发者工具 CLI：

```powershell
$cli = 'D:\Tencent\微信web开发者工具\cli.bat'
$project = 'D:\unity project\Learn Heartstone\MiniProgram'

& $cli open --project $project --port 28318 --lang zh
& $cli islogin --project $project

& $cli preview `
  --project $project `
  --port 28318 `
  --lang zh `
  --qr-format image `
  --qr-output '<预览二维码路径>' `
  --info-output '<预览信息 JSON 路径>'
```

`islogin` 必须返回 `{"login":true}`。未登录时由用户本人在开发者工具 GUI 完成登录，不反复执行上传命令。

模拟器自动化入口：

```powershell
& $cli auto `
  --project $project `
  --port 28318 `
  --auto-port 9420 `
  --lang zh `
  --trust-project
```

注意：`--port` 是开发者工具 HTTP 服务端口，`--auto-port` 才是 `miniprogram-automator` 的 WebSocket 端口。

## 上传与正式发布边界

上传开发版：

```powershell
& $cli upload `
  --project $project `
  --port 28318 `
  --lang zh `
  --version '<版本>' `
  --desc '<一图流版本摘要>' `
  --info-output '<上传信息 JSON 路径>'
```

CLI 上传成功只代表开发版已上传。正式上线仍需在微信管理后台提交审核；审核通过后由管理员点击全量或分阶段发布。状态统一使用：

- `local-verified`
- `dev-uploaded`
- `review-submitted`
- `approved`
- `published`

2026-08-11 基线：版本 `0.1.0` 已完成 8/8 Node 测试、开发者工具 preview、390×753 iPhone 模拟器首页/详情运行验收，并上传开发版；包体 1,259,604 Byte。当前状态为 `dev-uploaded`。

## 手机验收清单

- 首页首屏以“继续训练/选择阵容”为主，导入和扫码只占次级位置。
- 8 条路线均显示种族、名称、摘要、核心牌和真实牌图扇面。
- 详情三档切换可用，回合/酒馆/金币/进度清晰。
- 酒馆、战场、手牌只显示有牌位置；空逻辑槽不占视觉空间。
- 塑造法术明确显示“首回合 3 张，之后每回合 1 张；回合结束清除未使用项”。
- 操作顺序是页面主轴，底部“完成本步”不被安全区遮挡。
- 收藏、步骤进度和上次路线关闭后仍能从本地存储恢复。
- 中文长文本能换行，不依赖只用颜色传达状态。
- 页面没有 Unity 战斗、Web/Windows 交接按钮或假云端状态。

## 发布规范

三个渠道的分块提交、产物命名、验收和发布记录统一遵循 [../Docs/ThreeChannelReleaseSubmissionWorkflow.zh-CN.md](../Docs/ThreeChannelReleaseSubmissionWorkflow.zh-CN.md)。

