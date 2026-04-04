import { afterEach, describe, expect, it, vi } from 'vitest'

import { registerServiceWorker } from './registerServiceWorker'

describe('registerServiceWorker', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('registers the service worker after window load', async () => {
    const register = vi.fn().mockResolvedValue(undefined)
    Object.defineProperty(window.navigator, 'serviceWorker', {
      configurable: true,
      value: { register },
    })

    registerServiceWorker()
    window.dispatchEvent(new Event('load'))
    await Promise.resolve()

    expect(register).toHaveBeenCalledWith('/sw.js', { scope: '/' })
  })
})
