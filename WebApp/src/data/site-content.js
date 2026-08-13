export const currentVersion = Object.freeze({
  id: '36.2-preview',
  label: '36.2',
  season: '第 14 赛季',
  officialStatus: '已上线',
  trainerStatus: '已上线',
  supportLabel: '限定训练范围',
  updatedAt: '2026-08-13',
  contentSnapshotId: '36.2-20260813-f61e7bb',
  rulesetId: 'ruleset-legacy-composite-v1',
  summary: '当前训练范围只启用黑暗之赐与饰品；其他历史机制不会混入这套版本规则。',
})

export const mechanics = Object.freeze([
  {
    id: 'dark-gifts',
    name: '黑暗之赐',
    kicker: '三次抉择 · 与酒馆等级无关',
    status: '已上线可试玩',
    description: '候选、付费、回合范围、选择状态与实际效果均进入当前内容快照。',
  },
  {
    id: 'trinkets',
    name: '饰品',
    kicker: '小型与大型饰品',
    status: '已上线可试玩',
    description: '新饰品与池差异进入版本隔离内容集，并保持与旧版本互不污染。',
  },
])

export const productCapabilities = Object.freeze([
  {
    title: '四步开局',
    detail: '选择版本、英雄与种族、版本机制和高级卡池，再进入训练。',
  },
  {
    title: '版本锁定',
    detail: '存档、回放与场景恢复都绑定同一内容快照和规则集。',
  },
  {
    title: '确定性复现',
    detail: '固定种子与场景状态让同一局面可以被重复验证。',
  },
])

export const knownDifferences = Object.freeze([
  '六名旧英雄调整仍包含 CommunityObserved 证据，尚未完成完整官方快照差异核对。',
  '36.2 训练范围已锁定为黑暗之赐、饰品和当前一图流方案；历史版本仍按各自快照独立保留。',
])

export const unsupportedEffects = Object.freeze([
  '六名旧英雄调整的完整官方快照差异复核',
  '尚未取得官方证据的极端触发优先级',
])

export const communityNews = Object.freeze([
  {
    title: '炉石 36.2 补丁正式前瞻（三）：战棋大更新汇总',
    date: '2026-08-04',
    source: '旅法师营地',
    href: 'https://www.iyingdi.com/tz/post/5675744',
    summary: '正式补丁日志、战棋汇总与 Bug 修复入口。',
  },
  {
    title: '36.2 战棋预览（十六）：英雄调整一览',
    date: '2026-08-04',
    source: '旅法师营地',
    href: 'https://www.iyingdi.com/tz/post/5675745',
    summary: '第 14 赛季新英雄与旧英雄调整的社区整理。',
  },
  {
    title: '战棋 S14 大更新：黑暗之赐、发动与饰品总览',
    date: '2026-07-28',
    source: '旅法师营地',
    href: 'https://www.iyingdi.com/tz/post/5712622',
    summary: '新赛季机制、牌池、饰品和时间线的集中入口。',
  },
])

export const unityRelease = Object.freeze({
  path: '/unity/',
  sourceDataBytes: 107357298,
  sourceDataLabel: '约 102.4 MiB 压缩数据',
  chunkCount: 12,
  recommendedMemory: '建议桌面浏览器、8 GB 及以上内存',
})

export const windowsRelease = Object.freeze({
  available: true,
  candidateBuilt: true,
  version: '36.2-preview',
  status: 'Windows 36.2 预览版',
  reason: '下载后解压，运行 Learn Heartstone.exe 即可开始训练。',
  buildJobId: 'build-f61e7bb-r1',
  contentSnapshotId: '36.2-20260813-f61e7bb',
  artifactBytes: 185071735,
  artifactLabel: '176.50 MiB',
  sha256: '4909f564dd5bb9637d17805596e92aaf30f90ae31a9fdf43dc049e2b0870c5c5',
  downloadUrl: 'https://downloads.jsoncool.com/windows/36.2-preview/0.1.0-alpha__36.2-20260813-f61e7bb__build-f61e7bb-r1/LearnHeartstone-Windows-x64-0.1.0-alpha__36.2-20260813-f61e7bb__build-f61e7bb-r1.zip',
  manifestPath: '/releases/windows-release-manifest.json',
})
