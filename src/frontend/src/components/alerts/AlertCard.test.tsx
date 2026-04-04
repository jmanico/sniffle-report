import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it } from 'vitest'

import { AlertCard } from './AlertCard'

const baseAlert = {
  id: '1f1ecb6b-c7dc-4142-bdc7-bff8cb6c57e4',
  regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
  disease: 'Influenza A',
  title: 'Seasonal flu activity is elevated',
  summary: 'Cases are rising across the county.',
  caseCount: 123,
  sourceAttribution: 'Austin Public Health',
  sourceDate: '2026-03-20T00:00:00Z',
  createdAt: '2026-03-21T00:00:00Z',
}

describe('AlertCard', () => {
  afterEach(() => {
    cleanup()
  })

  it.each([
    ['Critical'],
    ['High'],
    ['Moderate'],
    ['Low'],
  ] as const)('renders severity badge for %s alerts', (severity) => {
    render(
      <MemoryRouter>
        <AlertCard alert={{ ...baseAlert, severity }} href="/region/test/alerts/1" />
      </MemoryRouter>,
    )

    expect(screen.getByText(severity)).toBeInTheDocument()
    expect(screen.getByText(/seasonal flu activity is elevated/i)).toBeInTheDocument()
  })
})
