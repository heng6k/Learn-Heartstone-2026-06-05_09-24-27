# Learn Heartstone WebGL

这是可直接静态托管的完整网页版产物。

Vercel 导入仓库时使用：

- Framework Preset：`Other`
- Root Directory：`WebDeploy`
- Build Command：留空
- Output Directory：`.`

`vercel.json` 已配置 Unity WebGL Brotli 文件所需的 MIME、`Content-Encoding` 和缓存响应头。

部署配置的唯一人工真源是 `Deploy/Vercel/vercel.json`。本目录中的同名文件只为迁移期旧 Vercel Root Directory 保留，修改时必须从真源同步，不能直接单独编辑。
