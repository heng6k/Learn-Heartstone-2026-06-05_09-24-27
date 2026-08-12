炉石学习助手 WebGL 下载包
===========================

本压缩包包含：

1. LearnHeartstone ReleaseCandidate 目录：网页游戏本体及 release-meta.json。
2. serve_webgl.py：带 Unity WebGL Brotli 与 MIME 响应头的本地服务器。

本地启动
--------

1. 安装 Python 3。
2. 解压本压缩包，不要只在压缩软件中直接打开 index.html。
3. 在解压目录打开 PowerShell，执行：

   python .\serve_webgl.py ".\<ReleaseCandidate 目录名>" --port 8080

4. 浏览器打开 http://127.0.0.1:8080/ 。
5. 结束时在 PowerShell 中按 Ctrl+C。

自行托管
--------

- 上传完整 ReleaseCandidate 目录，不要漏掉 Build、content、TemplateData、_headers 与 release-meta.json。
- 托管平台必须按 _headers 为 .br 和数据分块返回正确的 Content-Encoding、Content-Type 与缓存头。
- 每次发布先在 HTTP 环境完成首页、主路径和一图流 PNG 下载验收；不要用 file:// 双击验收。

版本身份与完整性
----------------

- 版本、内容快照、源码提交和 dirty 状态见 ReleaseCandidate/release-meta.json。
- 发布方提供的 ZIP SHA-256 用于校验下载包是否完整。
