import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { RegionalDashboardPage } from './RegionalDashboardPage'

let mockDashboard = {
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
      previousCaseCount: null,
      wowChangePercent: null,
      previousSourceDate: null,
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
      previousCaseCount: null,
      wowChangePercent: null,
      previousSourceDate: null,
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
  accessSignals: [
    {
      designationId: '3f75bb6a-d4d1-4dc5-9f23-cf79278ca73d',
      areaName: 'Travis County primary care service area',
      discipline: 'Primary Care',
      designationType: 'Geographic area',
      status: 'Designated',
      populationGroup: null,
      hpsaScore: 14,
      populationToProviderRatio: 3550.2,
      sourceUpdatedAt: '2026-03-11T00:00:00Z',
    },
  ],
  environmentalSignals: [
    {
      violationId: 'fce8d336-e7f2-442b-9ed1-71577f1eff4d',
      waterSystemName: 'Central Travis Water Authority',
      violationCategory: 'Monitoring and reporting',
      ruleName: 'Lead and Copper Rule',
      contaminantName: 'Lead',
      summary: 'Open monitoring and reporting violation under the Lead and Copper Rule for a community water system serving central Travis County.',
      isOpen: true,
      populationServed: 182000,
      identifiedAt: '2026-02-18T00:00:00Z',
      sourceUpdatedAt: '2026-03-21T00:00:00Z',
    },
  ],
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
}

vi.mock('../hooks/useStaticData', () => ({
  useStaticDashboard: () => ({
    data: mockDashboard,
    isLoading: false,
    isError: false,
  }),
}))

function renderPage(initialEntry: string) {
  const queryClient = new QueryClient()

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/region/:regionId" element={<RegionalDashboardPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('RegionalDashboardPage', () => {
  beforeEach(() => {
    mockDashboard = {
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
          previousCaseCount: null,
          wowChangePercent: null,
          previousSourceDate: null,
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
          previousCaseCount: null,
          wowChangePercent: null,
          previousSourceDate: null,
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
      accessSignals: [
        {
          designationId: '3f75bb6a-d4d1-4dc5-9f23-cf79278ca73d',
          areaName: 'Travis County primary care service area',
          discipline: 'Primary Care',
          designationType: 'Geographic area',
          status: 'Designated',
          populationGroup: null,
          hpsaScore: 14,
          populationToProviderRatio: 3550.2,
          sourceUpdatedAt: '2026-03-11T00:00:00Z',
        },
      ],
      environmentalSignals: [
        {
          violationId: 'fce8d336-e7f2-442b-9ed1-71577f1eff4d',
          waterSystemName: 'Central Travis Water Authority',
          violationCategory: 'Monitoring and reporting',
          ruleName: 'Lead and Copper Rule',
          contaminantName: 'Lead',
          summary: 'Open monitoring and reporting violation under the Lead and Copper Rule for a community water system serving central Travis County.',
          isOpen: true,
          populationServed: 182000,
          identifiedAt: '2026-02-18T00:00:00Z',
          sourceUpdatedAt: '2026-03-21T00:00:00Z',
        },
      ],
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
    }
  })

  it('renders dashboard summary sections from snapshot', () => {
    renderPage('/region/8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b')

    expect(screen.getByRole('heading', { name: 'Travis County, TX' })).toBeInTheDocument()
    expect(screen.getByText(/travis pharmacy/i)).toBeInTheDocument()
    expect(screen.getByText(/123 congress ave, austin, tx/i)).toBeInTheDocument()
    expect(screen.getByText(/this page is the complete published view for this region/i)).toBeInTheDocument()
    expect(screen.getByText(/emergency department visits and lab-confirmed cases are increasing/i)).toBeInTheDocument()
    expect(screen.getByText(/county health department surveillance/i)).toBeInTheDocument()
    expect(screen.getByText(/77 previous cases · \+59.7% WoW/i)).toBeInTheDocument()
    expect(screen.getByText(/2 updates/i)).toBeInTheDocument()
    expect(screen.getByText(/county health department surveillance · mar 18-20, 2026/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /back to texas/i })).toHaveAttribute('href', '/states/TX')
    expect(screen.getByText(/seasonal flu activity is elevated/i)).toBeInTheDocument()
    expect(screen.getAllByText(/seasonal flu activity is elevated/i)).toHaveLength(1)
    expect(screen.getByText(/123 cases \(\+59.7% WoW\)/i)).toBeInTheDocument()
    expect(screen.getByText(/hpsa score 14/i)).toBeInTheDocument()
    expect(screen.getByText(/central travis water authority/i)).toBeInTheDocument()
    expect(screen.getByText(/182,000 served/i)).toBeInTheDocument()
  })

  it('renders community health alerts as indicators instead of outbreak cases', () => {
    mockDashboard = {
      ...mockDashboard,
      regionId: '560440d4-2f60-4880-a8fa-b2299aeca2f4',
      regionName: 'Arkansas County',
      state: 'AR',
      parentState: 'AR',
      parentName: 'Arkansas',
      publishedAlertCount: 23,
      topAlerts: [
        {
          alertId: 'bd992032-7cf1-4c2c-8a75-6a7ccd9064ee',
          disease: '[Community Health] Visits to doctor for routine checkup within the past year among adults',
          title: 'Visits to doctor for routine checkup within the past year among adults: 78.1% (age-adjusted)',
          summary: 'Prevention - Visits to doctor for routine checkup within the past year among adults. Age-adjusted prevalence: 78.1%.',
          severity: 'Low',
          caseCount: 78,
          sourceAttribution: 'CDC PLACES County Health',
          sourceDate: '2026-04-05T19:25:10.72226Z',
          previousCaseCount: null,
          wowChangePercent: null,
          previousSourceDate: null,
        },
      ],
      trendHighlights: [],
      accessSignals: [],
      environmentalSignals: [],
      nearbyResources: [],
      preventionHighlights: [],
      newsHighlights: [],
    }

    renderPage('/region/560440d4-2f60-4880-a8fa-b2299aeca2f4')

    expect(screen.getByText(/78.1% prevalence/i)).toBeInTheDocument()
    expect(screen.getByText(/community health indicator/i)).toBeInTheDocument()
    expect(screen.getByText(/age-adjusted prevalence: 78.1%/i)).toBeInTheDocument()
    expect(screen.getByText(/^Visits to doctor for routine checkup within the past year among adults$/i)).toBeInTheDocument()
  })
})
