import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

import { onRequest } from '../functions/unity/Build/_middleware.js'

const compressedBody = new Uint8Array([0xce, 0xb2, 0xcf, 0x81])

async function invoke(pathname, status = 200) {
  return onRequest({
    request: new Request(`https://example.test${pathname}`),
    next: async () => new Response(compressedBody, {
      status,
      headers: {
        'Content-Type': 'application/octet-stream',
        'Cache-Control': 'public, max-age=31536000, immutable',
      },
    }),
  })
}

test('Pages middleware restores Brotli headers for Unity build assets', async () => {
  const cases = [
    ['game.wasm.br', 'application/wasm'],
    ['game.framework.js.br', 'application/javascript'],
    ['game.data.br.part000.data-chunk.br', 'application/octet-stream'],
  ]

  for (const [file, contentType] of cases) {
    const response = await invoke(`/unity/Build/${file}`)
    assert.equal(response.headers.get('Content-Encoding'), 'br')
    assert.equal(response.headers.get('Content-Type'), contentType)
    assert.match(response.headers.get('Cache-Control'), /no-transform/)
    assert.deepEqual(new Uint8Array(await response.arrayBuffer()), compressedBody)
  }
})

test('Pages middleware leaves non-Brotli and failed asset responses unchanged', async () => {
  const loader = await invoke('/unity/Build/game.loader.js')
  assert.equal(loader.headers.get('Content-Encoding'), null)

  const missing = await invoke('/unity/Build/game.wasm.br', 404)
  assert.equal(missing.status, 404)
  assert.equal(missing.headers.get('Content-Encoding'), null)
})

test('Pages static headers do not compete with the Brotli middleware', async () => {
  const staticHeaders = await readFile(new URL('../public/_headers', import.meta.url), 'utf8')
  assert.doesNotMatch(staticHeaders, /Content-Encoding:\s*br/)
})
