import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { queryClient } from '../../api/queryClient'
import { RegionProvider } from './RegionContext'
import { useRegion } from '../../hooks/useRegion'

const mockRegion = {
  id: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
  name: 'Travis County',
  state: 'TX',
  type: 'County',
}

vi.mock('../../hooks/useRegions', () => ({
  useRegionById: () => ({
    data: mockRegion,
  }),
}))

function RegionProbe() {
  const { regionId, regionLabel, buildRegionPath } = useRegion()

  return (
    <div>
      <span>{regionId}</span>
      <span>{regionLabel}</span>
      <span>{buildRegionPath('alerts')}</span>
    </div>
  )
}

describe('RegionProvider', () => {
  it('derives region state from the URL param', () => {
    Object.assign(mockRegion, {
      id: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
      name: 'Travis County',
      state: 'TX',
      type: 'County',
    })

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
    expect(screen.getByText('Travis County, TX')).toBeInTheDocument()
    expect(screen.getByText('/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b/alerts')).toBeInTheDocument()
  })

  it('omits the state code for state-level regions', () => {
    Object.assign(mockRegion, {
      id: '53890ccc-91f7-4ac4-b2b8-4ab49e92da40',
      name: 'California',
      state: 'CA',
      type: 'State',
    })

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/region/53890ccc-91f7-4ac4-b2b8-4ab49e92da40']}>
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

    expect(screen.getByText('California')).toBeInTheDocument()
    expect(screen.queryByText('California, CA')).not.toBeInTheDocument()
  })
})
