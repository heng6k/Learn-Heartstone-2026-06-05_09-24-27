const SHARE_CODE_LENGTH = 20
const SHARE_CODE_ALPHABET = '23456789ABCDEFGHJKLMNPQRSTUVWXYZ'

function normalizeShareCode(value) {
  const source = String(value || '')
  let normalized = ''

  for (const character of source) {
    if (character === '-' || /\s/.test(character)) {
      continue
    }
    const upper = character.toUpperCase()
    if (SHARE_CODE_ALPHABET.indexOf(upper) < 0) {
      throw new Error('分享码包含不支持的字符')
    }
    normalized += upper
  }

  if (normalized.length !== SHARE_CODE_LENGTH) {
    throw new Error('分享码应为 20 位')
  }
  return normalized
}

function tryNormalize(value) {
  try {
    return normalizeShareCode(value)
  } catch (_) {
    return ''
  }
}

function extractShareCode(value) {
  let decoded
  try {
    decoded = decodeURIComponent(String(value || ''))
  } catch (_) {
    return ''
  }
  const queryMatch = decoded.match(/(?:shareCode|scene|code)=([^&#]+)/i)
  if (queryMatch) {
    const fromQuery = tryNormalize(queryMatch[1])
    if (fromQuery) {
      return fromQuery
    }
  }

  const direct = tryNormalize(decoded)
  if (direct) {
    return direct
  }

  for (const part of decoded.split(/[/?#&=]/)) {
    const fromPart = tryNormalize(part)
    if (fromPart) {
      return fromPart
    }
  }
  return ''
}

module.exports = {
  SHARE_CODE_ALPHABET,
  SHARE_CODE_LENGTH,
  extractShareCode,
  normalizeShareCode
}
