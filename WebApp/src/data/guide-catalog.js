let catalogPromise

export function loadGuideCatalog() {
  catalogPromise ||= fetch('/data/guides.json', { cache: 'no-cache' }).then(async (response) => {
    if (!response.ok) throw new Error(`一图流数据加载失败（${response.status}）`)
    const catalog = await response.json()
    if (!Array.isArray(catalog.guides)) throw new Error('一图流数据格式不正确')
    return catalog
  })
  return catalogPromise
}

