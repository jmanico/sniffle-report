const AllowedProtocols = new Set(['https:', 'mailto:', 'tel:'])

export function validateAndSanitizeUrl(candidate: string): string {
  if (!candidate) {
    return '/'
  }

  if (candidate.startsWith('/')) {
    return candidate
  }

  try {
    const url = new URL(candidate, window.location.origin)

    if (AllowedProtocols.has(url.protocol)) {
      return url.toString()
    }
  } catch {
    return '/'
  }

  return '/'
}
