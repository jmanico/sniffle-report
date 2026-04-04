import { render, screen } from '@testing-library/react'
import { beforeAll, describe, expect, it } from 'vitest'

import { TrendChart } from './TrendChart'

beforeAll(() => {
  class ResizeObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
  }

  globalThis.ResizeObserver = ResizeObserverMock as typeof ResizeObserver
})

describe('TrendChart', () => {
  it('renders a chart container with trend data', () => {
    render(
      <TrendChart
        dataPoints={[
          {
            date: '2026-03-01T00:00:00Z',
            caseCount: 77,
            source: 'Austin Public Health',
            sourceDate: '2026-03-01T00:00:00Z',
          },
          {
            date: '2026-03-20T00:00:00Z',
            caseCount: 123,
            source: 'Austin Public Health',
            sourceDate: '2026-03-20T00:00:00Z',
          },
        ]}
      />,
    )

    expect(screen.getByRole('img', { name: /trend chart/i })).toBeInTheDocument()
    expect(document.querySelector('.recharts-responsive-container')).not.toBeNull()
  })
})
