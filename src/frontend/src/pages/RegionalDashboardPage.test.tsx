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
          summary: 'Emergency department visits and lab-confirmed cases are increasing across the county.',
          severity: 'High',
          caseCount: 123,
          sourceAttribution: 'County health department surveillance',
          sourceDate: '2026-03-20T00:00:00Z',
          previousCaseCount: 77,
          wowChangePercent: 59.7,
          previousSourceDate: '2026-03-13T00:00:00Z',
        },
        {
          alertId: '951bfbd1-3074-44ed-a753-7273c8105e2f',
          disease: 'Influenza A',
          title: 'Seasonal flu activity is elevated',
          summary: 'Emergency department visits and lab-confirmed cases are increasing across the county.',
          severity: 'High',
          caseCount: 123,
          sourceAttribution: 'County health department surveillance',
          sourceDate: '2026-03-19T00:00:00Z',
        },
        {
          alertId: 'cdf50eb8-cf20-4322-aa52-6e42c342f30d',
          disease: 'RSV',
          title: 'Pediatric RSV remains active',
          summary: 'RSV activity remains elevated for young children and urgent care visits are up.',
          severity: 'Moderate',
          caseCount: 58,
          sourceAttribution: 'Regional pediatric hospital network',
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
      nearbyResources: [
        {
          id: '917cc0d1-9ea0-4f11-b0bb-0f1894b60f1f',
          regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
          name: 'Travis Pharmacy',
          type: 'Pharmacy',
          address: '123 Congress Ave, Austin, TX',
          phone: '(512) 555-0110',
          website: 'https://example.org/pharmacy',
        },
      ],
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
    expect(screen.getByText(/travis pharmacy/i)).toBeInTheDocument()
    expect(screen.getByText(/123 congress ave, austin, tx/i)).toBeInTheDocument()
    expect(screen.getByText(/this page is the complete published view for this region/i)).toBeInTheDocument()
    expect(screen.getByText(/emergency department visits and lab-confirmed cases are increasing/i)).toBeInTheDocument()
    expect(screen.getByText(/county health department surveillance/i)).toBeInTheDocument()
    expect(screen.getByText(/77 previous cases · \+59.7% WoW/i)).toBeInTheDocument()
    expect(screen.getByText(/2 updates/i)).toBeInTheDocument()
    expect(screen.getByText(/county health department surveillance · mar 18-20, 2026/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /back to texas/i })).toHaveAttribute(
      'href',
      '/states/TX',
    )
    expect(screen.getByText(/seasonal flu activity is elevated/i)).toBeInTheDocument()
    expect(screen.getAllByText(/seasonal flu activity is elevated/i)).toHaveLength(1)
    expect(screen.getByText(/123 cases \(\+59.7% WoW\)/i)).toBeInTheDocument()
  })
})
