import assert from "node:assert/strict";
import { mkdtemp, readFile, stat, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";
import test from "node:test";
import { promisify } from "node:util";
import { brotliCompress, brotliDecompress, constants } from "node:zlib";

import { replaceBrotliDataWithChunks } from "./webgl-data-chunks.mjs";

const compress = promisify(brotliCompress);
const decompress = promisify(brotliDecompress);

test("replaceBrotliDataWithChunks round-trips independent Brotli parts", async (context) => {
  const directory = await mkdtemp(path.join(tmpdir(), "learn-heartstone-webgl-chunks-"));
  context.after(async () => {
    const { rm } = await import("node:fs/promises");
    await rm(directory, { recursive: true, force: true });
  });

  const rawBytes = Buffer.allocUnsafe(1_310_733);
  for (let index = 0; index < rawBytes.length; index += 1) {
    rawBytes[index] = (index * 31 + Math.floor(index / 997)) % 256;
  }

  const sourceBytes = await compress(rawBytes, {
    params: { [constants.BROTLI_PARAM_QUALITY]: 4 },
  });
  const dataFile = path.join(directory, "fixture.data.br");
  await writeFile(dataFile, sourceBytes);

  const result = await replaceBrotliDataWithChunks(dataFile, {
    rawChunkBytes: 256 * 1024,
    maxCompressedChunkBytes: 512 * 1024,
    quality: 4,
  });

  await assert.rejects(stat(dataFile), { code: "ENOENT" });
  assert.equal(result.manifest.schemaVersion, 1);
  assert.equal(result.manifest.originalFile, "fixture.data.br");
  assert.equal(result.manifest.uncompressedBytes, rawBytes.byteLength);
  assert.equal(result.manifest.chunks.length, Math.ceil(rawBytes.byteLength / (256 * 1024)));

  const persistedManifest = JSON.parse(await readFile(result.manifestPath, "utf8"));
  assert.deepEqual(persistedManifest, result.manifest);

  const reconstructedParts = [];
  for (const chunk of result.manifest.chunks) {
    const compressedPart = await readFile(path.join(directory, chunk.file));
    assert.equal(compressedPart.byteLength, chunk.compressedBytes);
    assert.ok(compressedPart.byteLength <= 512 * 1024);
    const uncompressedPart = await decompress(compressedPart);
    assert.equal(uncompressedPart.byteLength, chunk.uncompressedBytes);
    reconstructedParts.push(uncompressedPart);
  }

  assert.deepEqual(Buffer.concat(reconstructedParts), rawBytes);
});
