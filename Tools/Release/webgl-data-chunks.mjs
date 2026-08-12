import { createHash } from "node:crypto";
import { readFile, unlink, writeFile } from "node:fs/promises";
import path from "node:path";
import { promisify } from "node:util";
import { brotliCompress, brotliDecompress, constants } from "node:zlib";

export const defaultRawChunkBytes = 16 * 1024 * 1024;

const compress = promisify(brotliCompress);
const decompress = promisify(brotliDecompress);

function sha256Hex(bytes) {
  return createHash("sha256").update(bytes).digest("hex");
}

function positiveInteger(value, description) {
  if (!Number.isSafeInteger(value) || value <= 0) {
    throw new Error(`${description} must be a positive integer`);
  }
  return value;
}

export async function replaceBrotliDataWithChunks(dataFilePath, options = {}) {
  const rawChunkBytes = positiveInteger(
    options.rawChunkBytes ?? defaultRawChunkBytes,
    "rawChunkBytes",
  );
  const maxCompressedChunkBytes = positiveInteger(
    options.maxCompressedChunkBytes ?? 25 * 1024 * 1024,
    "maxCompressedChunkBytes",
  );
  const quality = options.quality ?? 11;
  if (!Number.isInteger(quality) || quality < 0 || quality > 11) {
    throw new Error("quality must be an integer from 0 through 11");
  }

  const sourceBytes = await readFile(dataFilePath);
  const uncompressedBytes = await decompress(sourceBytes);
  const directory = path.dirname(dataFilePath);
  const originalFile = path.basename(dataFilePath);
  const chunks = [];

  for (let offset = 0, index = 0; offset < uncompressedBytes.byteLength; offset += rawChunkBytes, index += 1) {
    const rawPart = uncompressedBytes.subarray(offset, Math.min(offset + rawChunkBytes, uncompressedBytes.byteLength));
    const compressedPart = await compress(rawPart, {
      params: { [constants.BROTLI_PARAM_QUALITY]: quality },
    });
    if (compressedPart.byteLength > maxCompressedChunkBytes) {
      throw new Error(
        `Compressed WebGL data chunk exceeds ${maxCompressedChunkBytes} bytes: part ${index} (${compressedPart.byteLength} bytes)`,
      );
    }

    const file = `${originalFile}.part${String(index).padStart(3, "0")}.data-chunk.br`;
    await writeFile(path.join(directory, file), compressedPart);
    chunks.push({
      file,
      compressedBytes: compressedPart.byteLength,
      uncompressedBytes: rawPart.byteLength,
      sha256: sha256Hex(compressedPart),
    });
  }

  const manifest = {
    schemaVersion: 1,
    originalFile,
    sourceCompressedBytes: sourceBytes.byteLength,
    sourceCompressedSha256: sha256Hex(sourceBytes),
    uncompressedBytes: uncompressedBytes.byteLength,
    uncompressedSha256: sha256Hex(uncompressedBytes),
    rawChunkBytes,
    chunks,
  };
  const manifestPath = `${dataFilePath}.chunks.json`;
  await writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
  await unlink(dataFilePath);
  return { manifest, manifestPath };
}
