const IMMUTABLE_NO_TRANSFORM = 'public, max-age=31536000, immutable, no-transform'

function brotliContentType(pathname) {
  if (pathname.endsWith('.wasm.br')) return 'application/wasm'
  if (pathname.endsWith('.framework.js.br')) return 'application/javascript'
  if (pathname.endsWith('.data-chunk.br')) return 'application/octet-stream'
  return null
}

export async function onRequest(context) {
  const response = await context.next()
  const contentType = brotliContentType(new URL(context.request.url).pathname)
  if (!contentType || !response.ok) return response

  const headers = new Headers(response.headers)
  headers.set('Content-Type', contentType)
  headers.set('Content-Encoding', 'br')
  headers.set('Cache-Control', IMMUTABLE_NO_TRANSFORM)

  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers,
    encodeBody: 'manual',
  })
}
