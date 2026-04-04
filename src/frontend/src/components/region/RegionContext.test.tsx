import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { queryClient } from '../../api/queryClient'
import { RegionProvider } from './RegionContext'
import { useRegion } from '../../hooks/useRegion'

vi.mock('../../hooks/useRegions', () => ({
  useRegionById: () => ({
    data: {
      id: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
      name: 'Travis County',
      state: 'TX',
    },
  }),
}))

function RegionProbe() {
  const { regionId, buildRegionPath } = useRegion()

  return (
    <div>
      <span>{regionId}</span>
      <span>{buildRegionPath('alerts')}</span>
    </div>
  )
}

describe('RegionProvider', () => {
  it('derives region state from the URL param', () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b']}>
          <Routes>
            <Route
              element={
                <RegionProvider>
                  <RegionProbe />
                </RegionProvider>
              }
              path="/region/:regionId"
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    )

    expect(screen.getByText('8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b')).toBeInTheDocument()
    expect(screen.getByText('/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b/alerts')).toBeInTheDocument()
  })
})
