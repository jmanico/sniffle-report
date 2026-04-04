import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'

import { PwaStatusBanner } from './PwaStatusBanner'

vi.mock('../../pwa/useInstallPrompt', () => ({
  useInstallPrompt: () => ({
    canInstall: false,
    promptToInstall: vi.fn(),
  }),
}))

describe('PwaStatusBanner', () => {
  it('renders offline messaging when the browser is offline', () => {
    const originalOnline = navigator.onLine

    Object.defineProperty(window.navigator, 'onLine', {
      configurable: true,
      value: false,
    })

    render(<PwaStatusBanner />)

    expect(screen.getByText(/you are offline/i)).toBeInTheDocument()

    Object.defineProperty(window.navigator, 'onLine', {
      configurable: true,
      value: originalOnline,
    })
  })
})
