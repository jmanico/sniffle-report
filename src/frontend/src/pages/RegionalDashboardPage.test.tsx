import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { RegionalDashboardPage } from './RegionalDashboardPage'

vi.mock('../hooks/useStaticData', () => ({
  useStaticDashboard: () => ({
    data: {
      regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
      computedAt: '2026-03-21T12:00:00Z',
      publishedAlertCount: 2,
      regionName: 'Travis County',
      state: 'TX',
      regionType: 'County',
      parentState: 'TX',
      parentName: 'Texas',
      topAlerts: [
        {
          alertId: '1f1ecb6b-c7dc-4142-bdc7-bff8cb6c57e4',
          disease: 'Influenza A',
          title: 'Seasonal flu activity is elevated',
          severity: 'High',
          caseCount: 123,
          sourceDate: '2026-03-20T00:00:00Z',
        },
        {
          alertId: 'cdf50eb8-cf20-4322-aa52-6e42c342f30d',
          disease: 'RSV',
          title: 'Pediatric RSV remains active',
          severity: 'Moderate',
          caseCount: 58,
          sourceDate: '2026-03-18T00:00:00Z',
        },
      ],
      trendHighlights: [
        {
          alertId: '1f1ecb6b-c7dc-4142-bdc7-bff8cb6c57e4',
          disease: 'Influenza A',
          latestCaseCount: 123,
          previousCaseCount: 77,
          wowChangePercent: 59.7,
          latestDate: '2026-03-20T00:00:00Z',
        },
      ],
      resourceCounts: {
        clinic: 12,
        pharmacy: 8,
        vaccinationSite: 3,
        hospital: 1,
        total: 24,
      },
      preventionHighlights: [
        {
          guideId: 'b7e9c781-13fe-4a0e-bb73-c5f3d063a213',
          disease: 'Influenza A',
          title: 'Flu shot options',
          hasCostTiers: true,
        },
      ],
      newsHighlights: [],
    },
    isLoading: false,
    isError: false,
  }),
}))

describe('RegionalDashboardPage', () => {
  it('renders dashboard summary sections from snapshot', () => {
    const queryClient = new QueryClient()

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b']}>
          <Routes>
            <Route path="/region/:regionId" element={<RegionalDashboardPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    )

    expect(screen.getByRole('heading', { name: 'Travis County, TX' })).toBeInTheDocument()
    expect(screen.getByText(/12 clinics/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /all alerts/i })).toHaveAttribute(
      'href',
      '/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b/alerts',
    )
    expect(screen.getByText(/seasonal flu activity is elevated/i)).toBeInTheDocument()
    expect(screen.getByText(/59.7% WoW/i)).toBeInTheDocument()
  })
})
