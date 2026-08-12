# 如何提交手机版网页、完整网页版和下载包

本文件是 Learn Heartstone 后续网页版本提交、验收与发布记录的统一入口。默认路线先交付不加载 Unity 的手机版轻量网页，再验收完整 Unity 网页版，最后从同一候选生成下载包；微信小程序和微信小游戏仅在版本范围明确包含时执行。

## 1. 先确定本次交付面

每次开始前，逐项填写 `in-scope`、`not-in-scope` 或当前状态。不要用“网页版已完成”同时代替手机版网页和完整网页版的结论。

| 交付面 | 面向用户 | 必须包含 | 不包含 |
| --- | --- | --- | --- |
| 共享攻略与卡牌数据 | 所有前端 | 权威攻略、真实牌图、三档训练数据、版本修订号 | 渠道专用布局 |
| 手机版轻量网页 | 手机浏览器用户 | `/guides`、`/guides/:guideId`、8 套攻略、每套 3 档、触控清晰布局 | Unity 下载、完整对局模拟 |
| 完整 Unity 网页版 | 桌面浏览器与主动进入试玩的用户 | Vue 产品壳、确认后加载 Unity、WebGL 内容包、一图流创建/查看/试玩 | 未验收的临时构建 |
| 网页下载包 | 需要离线保存或自行托管的用户 | 与完整网页版同源的候选、启动说明、SHA-256 | Unity 工程、`Library`、日志、凭据 |
| 原生微信小程序 | 微信手机用户 | 原生一图流浏览与训练 | `web-view` 包装 WebGL、Unity 对局 |
| Unity 微信小游戏 | 微信游戏用户 | Unity 一图流查看、创建、导入与试玩入口 | 未配置远程资源的完整模拟器 |

当前产品默认把前四项作为网页发布主线。微信渠道暂停时仍写 `not-in-scope`，不要删除历史状态。

## 2. 统一版本身份

一次发布记录必须写明以下身份：

- Git 源提交；脏工作区只能标记为 `dirty-local-acceptance`
- 攻略目录 `catalogRevisionId`
- Web 产品壳版本和构建时间
- Unity `bundleVersion`
- WebGL `buildId`、`contentVersion`、`packageFingerprint`
- Preview 与 Production 地址及 deployment 标识
- 下载包文件名、字节数和 SHA-256
- 本轮包含的提交块及每块提交 SHA
- 微信渠道版本号、AppID、包体和平台状态，仅在纳入范围时填写

正式候选必须来自干净且已提交的源状态。通过本地验收的 dirty 候选不能标记为 Production。

## 3. 按固定块提交

每块只暂存表中列出的范围。禁止使用 `git add .`，也不要把 `WebApp/dist`、`Builds`、`Library`、`Temp` 或平台凭据提交到仓库。

| 顺序 | 建议提交主题 | 典型路径 | 放行条件 |
| ---: | --- | --- | --- |
| 1 | `feat(content): freeze shared guide and card data` | Unity 权威攻略、卡牌数据、共享规则和测试 | Unity 编译与精确规则测试通过 |
| 2 | `feat(web-mobile): ship lightweight guide experience` | `WebApp/src`、`WebApp/public/data`、手机牌图、Web 测试 | `npm test`、轻量构建和手机浏览器验收通过 |
| 3 | `feat(web-unity): ship full browser play flow` | Unity UI、`Assets/WebGLTemplates`、`.jslib`、`WebApp/scripts/attach-unity.mjs`、WebGL 测试 | WebGL 构建、完整网页 smoke 和 PNG 下载通过 |
| 4 | `build(web): package and configure web release` | `Tools/Release`、`Tools/WebGL`、`WebApp/functions`、响应头和托管配置 | 候选自校验、预压缩响应、ZIP 和 SHA-256 通过 |
| 5 | `feat(miniprogram): update native one-sheet client` | `MiniProgram`、移动投影脚本和测试 | 仅在纳入范围时执行 |
| 6 | `feat(wxgame): update Unity mini-game client` | 微信 Unity SDK、模板和渠道构建器 | 仅在纳入范围时执行 |
| 7 | `docs(release): record delivery surfaces` | 本文件、文档索引和版本记录 | 每个交付面状态、证据和回滚目标完整 |

共享数据先进入第 1 块。手机与桌面共用的 Web 组件进入第 2 块，并在提交说明中列出两个受影响页面；不要复制实现来制造两个提交。

每块执行以下检查：

```powershell
git status --short
git diff --check
git add -- <本块明确文件列表>
git diff --cached --check
git diff --cached --stat
git commit -m "<本块主题>"
```

提交、push、部署和 Production 晋升是四个独立动作。未获得对应授权时，只完成被授权的动作。

## 4. 执行共享预检

共享预检先冻结内容，再生成手机版网页使用的投影。

1. 写明本轮目标、交付面和回滚版本。
2. 保护工作区已有修改；正式发布前完成分块提交。
3. 运行受影响的 Unity EditMode 与 PlayMode 测试。
4. 同步 Web 与小程序共用的一图流移动投影：

```powershell
python Tools\Release\sync-mini-program-content.py
```

5. 确认 `WebApp/public/data/guides.json` 可解析，包含 8 套攻略且每套包含 3 档。
6. 确认生成的牌图都能从攻略条目解析到本地资源。
7. 执行 `git diff --check`，记录源状态是否为 dirty。

## 5. 验收手机版轻量网页

手机版网页必须在不下载 Unity 的情况下完成浏览、选档和操作步骤阅读。

### 5.1 构建轻量站点

```powershell
Push-Location WebApp
npm test
npm run build
npm run preview -- --host 127.0.0.1
Pop-Location
```

普通 `npm run build` 不附加 Unity 候选。使用真实手机浏览器或 Chromium 设备模拟器验收，不要只检查 Vite 构建退出码。

### 5.2 检查手机用户路径

至少检查 390×844 和 430×932 两个视口：

1. 从 `/` 直接进入一图流。
2. 打开 `/guides`，确认 8 套攻略可见。
3. 直接访问一个 `/guides/:guideId` 深链接，确认刷新后仍能打开。
4. 切换同一攻略的 3 个难度档。
5. 检查标题、核心卡、操作顺序、开局位置和目标阵容是否清晰。
6. 检查页面无横向溢出，底部操作不会遮挡正文。
7. 检查可点击目标在触控下可用，文本不依赖悬停显示。
8. 检查浏览器错误为 0，卡图和数据请求没有非预期失败。
9. 检查 `/unity/` 请求数为 0，页面未创建 Unity `iframe`。

发布记录还要写入首屏传输量和主要资源数量。若它们高于上一 Production，写明原因和是否接受，不要用“性能正常”代替数字。

## 6. 验收完整 Unity 网页版

完整网页版只有在用户明确确认后才加载 Unity，并且必须验证真实 WebGL 数据传输与一图流下载。

### 6.1 生成和附加候选

1. 使用现有 Unity Editor 的 `Temp/WebGLReleaseBuild.request` 入口生成 `Builds/WebGL/<版本>`，不要启动第二个 Editor。
2. 使用 `Tools/Release/assemble-release-candidate.mjs` 组装 `Builds/ReleaseCandidate/<buildId>`。
3. 从 `WebApp` 附加已经验收结构的候选：

```powershell
Push-Location WebApp
npm test
npm run build:with-unity -- "../Builds/ReleaseCandidate/<candidate>"
npm run preview -- --host 127.0.0.1
Pop-Location
```

4. 通过 HTTP 验收 `WebApp/dist`。不要双击 `index.html`。

### 6.2 检查完整浏览器路径

至少检查 1280×720 和 1600×900 两个视口：

1. 打开 `/play`，确认页面在点击确认前不请求 Unity。
2. 点击确认，检查 Unity loader、WASM、framework、data 和内容分片全部成功。
3. 检查 Brotli 资源返回正确的 `Content-Encoding: br` 和 MIME 类型。
4. 检查一图流查看、创建、阵容码导入和目标档位试玩入口。
5. 检查 Console、page error 和 request failure；阻断性错误必须为 0。
6. 从一图流查看页点击下载，确认浏览器产生 download 事件。
7. 检查下载文件为非空 PNG，尺寸为 1600×900。

如果完整 Unity 冷启动超过本轮门限，完整网页版不得晋升 Production。本轮使用的门限是 300s；后续调整门限时必须在发布记录中写明原因。

## 7. 决定 Preview 与 Production

手机版网页和完整网页版可以位于同一站点，但必须分别记录状态。按以下规则放行：

- **两者都在范围内**：两者都通过后，才可把同一 deployment 晋升 Production。
- **只发布手机版网页**：使用 `npm run build` 生成不含 Unity 的站点，并在页面与发布记录中标明完整试玩不在本轮范围。
- **手机版通过、完整网页版失败**：可以保留 Preview 供排查，不能把包含失败交付面的 deployment 标记为 Production。
- **dirty 候选**：只能标记 `local-verified` 或 `preview`，不能标记 Production。
- **Preview 通过**：不等于 Production；晋升后还要在正式域名复验同一用户路径。

网页状态统一使用 `not-in-scope`、`local-verified`、`preview` 或 `production`。

## 8. 生成和验收网页下载包

下载包必须来自已经通过完整浏览器验收的同一个 ReleaseCandidate，不要重新构建 Unity。

```powershell
$candidate = '<已验收 ReleaseCandidate 绝对路径>'
$version = '<版本或 buildId>'
$outputRoot = 'Builds\DownloadPackage'
$zip = Join-Path $outputRoot "LearnHeartstone-Web-$version.zip"
$packageFiles = @(
    $candidate,
    'Tools\WebGL\serve_webgl.py',
    'Tools\Release\WebGLDownloadPackage-README.zh-CN.txt'
)

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
Compress-Archive -LiteralPath $packageFiles -DestinationPath $zip -CompressionLevel Optimal
Get-FileHash -LiteralPath $zip -Algorithm SHA256
```

把 ZIP 解压到新的临时目录，再用包内 `serve_webgl.py` 完成 HTTP smoke。检查 `release-meta.json`、`content/content-manifest.json`、部署头、全部分片、服务器脚本和启动说明；不要只检查压缩命令退出码。

## 9. 使用统一验收矩阵

任一“必须”项失败，对应交付面不得标记为完成。

| 门禁 | 手机版轻量网页 | 完整 Unity 网页版 | 网页下载包 |
| --- | :---: | :---: | :---: |
| 权威攻略与卡牌数据可解析 | 必须 | 必须 | 必须 |
| 8 套攻略 × 3 档 | 必须 | 必须 | 继承完整网页版 |
| 390×844 与 430×932 | 必须 | 补充检查 | 不适用 |
| 无横向溢出与遮挡 | 必须 | 必须 | 继承完整网页版 |
| 确认前 0 Unity 请求 | 必须 | 必须 | 必须 |
| Unity 测试与 WebGL 数据加载 | 不适用 | 必须 | 继承完整网页版 |
| Brotli 响应头与分片完整 | 不适用 | 必须 | 解压后必须 |
| 1600×900 PNG 浏览器下载 | 不适用 | 必须 | 解压后必须 |
| 浏览器错误为 0 | 必须 | 必须 | 解压后必须 |
| 版本、哈希和证据记录 | 必须 | 必须 | 必须 |
| 外部状态 | Preview/Production | Preview/Production | 下载地址 |

微信渠道纳入范围时，继续按 [原生小程序交接文档](../MiniProgram/README.md) 和对应 Unity 微信渠道计划验收。它们不能继承网页状态，也不能把开发版上传写成正式发布。

## 10. 复制发布记录模板

每次交付把以下模板复制到版本记录或本轮计划目录。保留所有交付面；未包含的交付面填写 `not-in-scope`。

```markdown
## <版本> 网页发布记录

### 共同身份

- 源提交：<sha / dirty-local-acceptance>
- 提交块：<提交主题与 sha>
- 攻略目录：<catalogRevisionId>
- Web 产品壳：<版本与构建时间>
- Unity：<bundleVersion>
- 回滚目标：<上一已知良好版本>

### 手机版轻量网页

- 状态：not-in-scope / local-verified / preview / production
- 地址：<URL>
- 视口：<390×844、430×932 结果>
- 攻略：<套数 × 档数>
- Unity 请求：<数量>
- 横向溢出：<数量>
- 浏览器错误：<数量>
- 首屏传输：<字节数与主要资源数>
- 证据：<截图、日志或报告路径>

### 完整 Unity 网页版

- 状态：not-in-scope / local-verified / preview / production
- 地址：<URL>
- WebGL：<路径>
- ReleaseCandidate：<路径>
- buildId / contentVersion / fingerprint：<值>
- 确认前 Unity 请求：<数量>
- 分片与 Brotli：<结果>
- 冷启动：<耗时与门限>
- PNG 下载：<文件、尺寸与结果>
- 浏览器错误：<数量>
- 证据：<截图、日志或报告路径>

### 网页下载包

- 状态：not-in-scope / packaged / published
- 文件：<路径或 URL>
- 字节数：<值>
- SHA-256：<值>
- 解压复验：<结果>

### 微信渠道

- 原生小程序：not-in-scope / local-verified / dev-uploaded / review-submitted / approved / published
- Unity 微信小游戏：not-in-scope / local-verified / dev-uploaded / review-submitted / approved / published
- 平台待办：<审核、发布、资质或无>

### 已知边界

- 未通过项：<交付面、现象和影响>
- 暂缓项：<原因>
- Production 决策：<晋升、保持原版本或回滚>
```

## 11. 上轮错误候选记录（禁止复用）

这次网页交付发生在 2026-08-11 至 2026-08-12，源状态仍是 `dirty-local-acceptance`，因此没有晋升 Production。该候选现已被 2026-08-12 的干净提交链取代；它只作为失败证据保留，后续构建、部署和下载包不得复用。

### 11.1 手机版轻量网页

- 状态：`preview`
- Preview：[Learn Heartstone Cloudflare Preview](https://769b15fd.learn-heartstone.pages.dev)
- 390×844：8 套攻略、每套 3 档、0 Unity 请求、0 横向溢出、0 浏览器错误
- 路线：从首页直达一图流，`/guides` 与攻略详情可用
- 结论：手机版轻量页通过本轮 Preview 门禁

### 11.2 完整 Unity 网页版

- 状态：`preview`
- WebGL：r13
- ReleaseCandidate：`Builds/ReleaseCandidate/0.1.0-alpha__20260811T133749Z-cc03773-dirty`
- 本地 Chromium：确认前不加载 Unity、12/12 Brotli 分片、10,387,346 字节 WASM、入口、预览和 1600×900 PNG 下载均通过，浏览器错误为 0
- Cloudflare Preview：Pages Function 使用 `encodeBody: manual` 传递预压缩资源，实测分片返回 `Content-Encoding: br`
- 阻断项：本机到 Preview 的完整约 120 MB Unity 冷启动连续超过 300s 门限
- 结论：保留 Preview，`jsoncool.com` Production 不变

### 11.3 网页下载包

- 状态：`packaged`
- 文件：`Builds/DownloadPackage/LearnHeartstone-Web-20260811T133749Z-cc03773-dirty-r2.zip`
- 大小：120,779,352 字节
- SHA-256：`07F51461F86F312FFDC85499400D26FF6FBB609381008A38BD3A2C4D7B868470`
- 解压复验：52 个文件、加载器 8 路并发配置、入口、预览和 1600×900 PNG 下载通过

### 11.4 微信渠道

- 本轮状态：`not-in-scope`
- 原生小程序上一状态：`0.1.0` 已上传开发版，不代表审核或正式发布
- 当前方向：优先维护手机版网页；重新纳入微信渠道时再检查资质、包体与平台审核状态

## 12. 回滚与禁止事项

回滚必须使用已记录的上一良好版本，并保留本轮失败证据：

- 网页只回滚到已知良好的同源 deployment，不现场修改候选目录。
- 下载包保留上一版本 ZIP 与 SHA-256；发现问题时恢复上一下载链接。
- 不提交构建物、临时目录、预览二维码、AppSecret、API Token 或自动化运行时缓存。
- 不把 Preview 写成 Production，不把 dirty 候选写成正式发布。
- 不因某个渠道暂缓而改写共享攻略规则；渠道只负责投影和呈现。
- 不删除失败记录；写明失败门禁、影响范围和下一次复验条件。

## 13. 当前发布记录

- [2026-08-12 手机版网页、完整 Unity 网页版与下载包发布记录](Releases/2026-08-12-web-release.md)
- 本轮必须从记录中的 Git 源提交创建干净工作树并构建；不得从主工作区的未提交文件或第 11 节旧候选上传。
- Preview、Production、下载包和发布后复验结果统一回填到该记录，后续发布沿用同一结构新建文件。
