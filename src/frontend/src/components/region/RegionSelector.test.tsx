import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { RegionSelector } from './RegionSelector'

vi.mock('../../hooks/useRegion', () => ({
  useRegion: () => ({
    regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
    regionLabel: 'Travis County, TX',
    buildRegionPath: (segment = '') =>
      segment ? `/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b/${segment}` : '/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
  }),
}))

vi.mock('../../hooks/useRegions', () => ({
  useRegionSearch: () => ({
    data: {
      items: [
        {
          id: '11111111-1111-1111-1111-111111111111',
          name: 'Travis County',
          type: 'County',
          state: 'TX',
          parentId: null,
        },
      ],
    },
    isLoading: false,
    isError: false,
  }),
  useRegions: () => ({
    data: {
      items: [
        {
          id: '11111111-1111-1111-1111-111111111111',
          name: 'Travis County',
          type: 'County',
          state: 'TX',
          parentId: null,
        },
      ],
    },
  }),
}))

describe('RegionSelector', () => {
  it('renders the current region and searchable result links', () => {
    render(
      <MemoryRouter>
        <RegionSelector />
      </MemoryRouter>,
    )

    expect(screen.getByText('Travis County, TX')).toBeInTheDocument()
    expect(screen.getByRole('searchbox', { name: /search regions/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /travis county tx > travis county/i })).toHaveAttribute(
      'href',
      '/region/11111111-1111-1111-1111-111111111111',
    )
  })
})
