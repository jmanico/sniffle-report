import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { RegionalDashboardPage } from './RegionalDashboardPage'

vi.mock('../hooks/useRegion', () => ({
  useRegion: () => ({
    regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
    regionLabel: 'Travis County, TX',
    buildRegionPath: (segment = '') =>
      segment ? `/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b/${segment}` : '/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
  }),
}))

vi.mock('../hooks/useRegions', () => ({
  useRegionById: () => ({
    data: {
      id: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
      name: 'Travis County',
      state: 'TX',
      type: 'County',
      childCount: 12,
      latitude: 30.2672,
      longitude: -97.7431,
      parent: null,
    },
  }),
}))

vi.mock('../hooks/useAlerts', () => ({
  useAlerts: () => ({
    data: {
      totalCount: 2,
      items: [
        {
          id: '1f1ecb6b-c7dc-4142-bdc7-bff8cb6c57e4',
          regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
          disease: 'Influenza A',
          title: 'Seasonal flu activity is elevated',
          summary: 'Cases are rising.',
          severity: 'High',
          caseCount: 123,
          sourceAttribution: 'Austin Public Health',
          sourceDate: '2026-03-20T00:00:00Z',
          createdAt: '2026-03-21T00:00:00Z',
        },
        {
          id: 'cdf50eb8-cf20-4322-aa52-6e42c342f30d',
          regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
          disease: 'RSV',
          title: 'Pediatric RSV remains active',
          summary: 'Clinics are seeing more cases.',
          severity: 'Moderate',
          caseCount: 58,
          sourceAttribution: 'Austin Public Health',
          sourceDate: '2026-03-18T00:00:00Z',
          createdAt: '2026-03-19T00:00:00Z',
        },
      ],
    },
    isLoading: false,
    isError: false,
  }),
}))

vi.mock('../hooks/useTrends', () => ({
  useTrends: () => ({
    data: {
      totalCount: 1,
      items: [
        {
          alertId: '1f1ecb6b-c7dc-4142-bdc7-bff8cb6c57e4',
          regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
          disease: 'Influenza A',
          alertTitle: 'Seasonal flu activity is elevated',
          sourceAttribution: 'Austin Public Health',
          dataPoints: [
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
          ],
        },
      ],
    },
    isLoading: false,
    isError: false,
  }),
}))

vi.mock('../hooks/usePrevention', () => ({
  usePrevention: () => ({
    data: {
      totalCount: 1,
      items: [
        {
          id: 'b7e9c781-13fe-4a0e-bb73-c5f3d063a213',
          regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
          disease: 'Influenza A',
          title: 'Flu shot options',
          createdAt: '2026-03-20T00:00:00Z',
          costTiers: [
            {
              type: 'Free',
              price: 0,
              provider: 'Austin Public Health',
              notes: 'Walk-in clinic',
            },
          ],
        },
      ],
    },
    isLoading: false,
    isError: false,
  }),
}))

vi.mock('../hooks/useResources', () => ({
  useResources: ({}, filters?: { type?: string }) => {
    const totalCount =
      filters?.type === 'Clinic' ? 12 : filters?.type === 'Pharmacy' ? 8 : 24

    return {
      data: {
        totalCount,
        items: [],
      },
      isLoading: false,
      isError: false,
    }
  },
}))

describe('RegionalDashboardPage', () => {
  it('renders live dashboard summary sections', () => {
    render(
      <MemoryRouter>
        <RegionalDashboardPage />
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: 'Travis County, TX' })).toBeInTheDocument()
    expect(screen.getByText(/12 clinics, 8 pharmacies near you/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /view all alerts/i })).toHaveAttribute(
      'href',
      '/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b/alerts',
    )
    expect(screen.getByText(/seasonal flu activity is elevated/i)).toBeInTheDocument()
  })
})
