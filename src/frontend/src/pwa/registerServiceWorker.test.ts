import { afterEach, describe, expect, it, vi } from 'vitest'

import { registerServiceWorker } from './registerServiceWorker'

describe('registerServiceWorker', () => {
  const originalEnablePwa = import.meta.env.VITE_ENABLE_PWA

  afterEach(() => {
    import.meta.env.VITE_ENABLE_PWA = originalEnablePwa
    vi.restoreAllMocks()
  })

  it('registers the service worker after window load', async () => {
    import.meta.env.VITE_ENABLE_PWA = 'true'
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

  it('unregisters existing service workers when pwa is disabled', async () => {
    import.meta.env.VITE_ENABLE_PWA = 'false'
    const unregister = vi.fn().mockResolvedValue(true)
    const getRegistrations = vi.fn().mockResolvedValue([{ unregister }])
    Object.defineProperty(window.navigator, 'serviceWorker', {
      configurable: true,
      value: { getRegistrations, register: vi.fn() },
    })

    registerServiceWorker()
    window.dispatchEvent(new Event('load'))
    await Promise.resolve()
    await Promise.resolve()

    expect(getRegistrations).toHaveBeenCalled()
    expect(unregister).toHaveBeenCalled()
  })
})
