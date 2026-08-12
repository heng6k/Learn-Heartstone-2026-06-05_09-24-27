# Learn Heartstone Web

精简 Vue 3 产品外壳。它只负责静态产品内容、版本边界、Unity 延迟加载和真实下载状态；玩法规则仍由 Unity 执行。

## 页面

- `/`：产品首页与当前版本牌轨
- `/versions`：支持状态、已知差异、未支持项、更新时间与旅法师营地资讯
- `/guides`：手机优先的一图流阵容列表，不加载 Unity
- `/guides/:guideId`：阵容档位、核心牌、操作顺序、开局位置与目标阵容
- `/play`：明确确认后才创建 Unity iframe
- `/download`：当前 Windows 候选与验收状态

本阶段不提供 `/s/:shareCode`。从一图流进入 `/play` 时会保留阵容与档位查询参数，Unity 仍由用户确认后才加载。

## 同步一图流内容

```powershell
python ../Tools/Release/sync-mini-program-content.py
```

脚本复用 Unity 权威攻略数据，同时生成 `public/data/guides.json` 与 `public/assets/cards` 手机缩略图；不要手工编辑这些生成文件。

## 本地开发

```powershell
npm install
npm run dev
```

普通 `npm run build` 只构建静态外壳，并保留一个不会下载 Unity 资源的本地占位页。

## 附加已验收的 Unity 候选

```powershell
npm run build:with-unity -- "../Builds/ReleaseCandidate/<candidate>"
```

脚本只接受同时含 `index.html`、`Build`、`TemplateData`、`content` 与 `release-meta.json` 的候选，并替换 `dist/unity`。部署输入是 `WebApp/dist`。

Cloudflare Pages 在没有顶层 `404.html` 时会自动使用 SPA 回退，因此项目不添加会吞掉 `/unity/*` 静态资源的全局 `_redirects`。`public/_headers` 只承担安全头、指纹资源缓存和 WebGL 预压缩文件类型/编码声明。
