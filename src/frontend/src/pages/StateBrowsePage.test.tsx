import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'

import { StateBrowsePage } from './StateBrowsePage'

vi.mock('../hooks/useStaticData', () => ({
  useStateDetail: () => ({
    data: {
      id: '21d21acc-ea72-4015-afd4-f8415f09736d',
      name: 'Pennsylvania',
      code: 'PA',
      publishedAlertCount: 179,
      resourceTotal: 638,
      computedAt: '2026-04-05T21:45:20.061451Z',
      counties: [
        {
          id: '4156a173-433d-4823-b48a-deb80c0842fa',
          name: 'Chester County',
          type: 'County',
          state: 'PA',
          latitude: 39.973965,
          longitude: -75.749732,
          publishedAlertCount: 5,
          resourceTotal: 27,
          computedAt: '2026-04-05T21:45:20.061451Z',
        },
        {
          id: '934eeba8-0214-408e-a386-39cae7869459',
          name: 'Adams County',
          type: 'County',
          state: 'PA',
          latitude: 39.869471,
          longitude: -77.21773,
          publishedAlertCount: 0,
          resourceTotal: 3,
          computedAt: '2026-04-05T21:45:20.061451Z',
        },
      ],
    },
    isLoading: false,
  }),
  useStaticDashboard: () => ({
    data: {
      regionId: '21d21acc-ea72-4015-afd4-f8415f09736d',
      regionName: 'Pennsylvania',
      regionType: 'State',
      state: 'PA',
      parentName: null,
      parentId: null,
      parentState: null,
      computedAt: '2026-04-05T21:45:20.061451Z',
      publishedAlertCount: 179,
      topAlerts: [
        {
          alertId: 'a9e6d0ce-f25c-43d3-8fb7-290bced9595d',
          disease: 'Hepatitis C, chronic, Probable',
          title: 'Hepatitis C, chronic, Probable — data from CDC NNDSS Weekly Tables',
          summary: 'Surveillance data ingested from CDC NNDSS Weekly Tables.',
          severity: 'Low',
          caseCount: 0,
          sourceAttribution: 'CDC NNDSS Weekly Tables',
          sourceDate: '2026-04-04T18:03:43.313683Z',
          previousCaseCount: null,
          wowChangePercent: null,
          previousSourceDate: null,
        },
        {
          alertId: 'afcb0711-fe36-4777-87e4-088f59debd80',
          disease: 'Hepatitis C, perinatal, Confirmed',
          title: 'Hepatitis C, perinatal, Confirmed — data from CDC NNDSS Weekly Tables',
          summary: 'Surveillance data ingested from CDC NNDSS Weekly Tables.',
          severity: 'Low',
          caseCount: 0,
          sourceAttribution: 'CDC NNDSS Weekly Tables',
          sourceDate: '2026-04-04T18:03:43.300343Z',
          previousCaseCount: null,
          wowChangePercent: null,
          previousSourceDate: null,
        },
        {
          alertId: '7578ca9c-c677-468f-bbe3-ced82fb2ef59',
          disease: 'Influenza-associated pediatric mortality',
          title: 'Influenza-associated pediatric mortality — data from CDC NNDSS Weekly Tables',
          summary: 'Surveillance data ingested from CDC NNDSS Weekly Tables.',
          severity: 'Low',
          caseCount: 0,
          sourceAttribution: 'CDC NNDSS Weekly Tables',
          sourceDate: '2026-04-04T18:03:43.287101Z',
          previousCaseCount: null,
          wowChangePercent: null,
          previousSourceDate: null,
        },
      ],
      trendHighlights: [],
      resourceCounts: {
        clinic: 231,
        pharmacy: 202,
        vaccinationSite: 0,
        hospital: 205,
        total: 638,
      },
      accessSignals: [
        {
          designationId: '0a4da3dd-f7cd-4778-b456-c215c5ff2d8a',
          areaName: 'Philadelphia County mental health service area',
          discipline: 'Mental Health',
          designationType: 'Geographic area',
          status: 'Designated',
          populationGroup: null,
          hpsaScore: 17,
          populationToProviderRatio: 4280.4,
          sourceUpdatedAt: '2026-03-29T00:00:00Z',
        },
        {
          designationId: 'f7673426-ea21-40b2-9f78-f7d01c232916',
          areaName: 'Rural northwest Pennsylvania primary care service area',
          discipline: 'Primary Care',
          designationType: 'Geographic area',
          status: 'Designated',
          populationGroup: null,
          hpsaScore: 12,
          populationToProviderRatio: 3010.2,
          sourceUpdatedAt: '2026-03-25T00:00:00Z',
        },
      ],
      environmentalSignals: [
        {
          violationId: 'b563a47d-d33d-4c12-9126-25df5f467d96',
          waterSystemName: 'Philadelphia River Supply',
          violationCategory: 'Maximum contaminant level',
          ruleName: 'Total Trihalomethanes Rule',
          contaminantName: 'Total trihalomethanes',
          summary: 'Open maximum contaminant level violation for total trihalomethanes in a public water system serving Philadelphia County.',
          isOpen: true,
          populationServed: 346000,
          identifiedAt: '2026-01-23T00:00:00Z',
          sourceUpdatedAt: '2026-03-28T00:00:00Z',
        },
      ],
      nearbyResources: [],
      preventionHighlights: [],
      newsHighlights: [],
    },
  }),
}))

describe('StateBrowsePage', () => {
  it('groups repeated statewide alert feeds into a summary section', () => {
    const queryClient = new QueryClient()

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/states/PA']}>
          <Routes>
            <Route path="/states/:stateCode" element={<StateBrowsePage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    )

    expect(screen.getByRole('heading', { name: /counties in pennsylvania/i })).toBeInTheDocument()
    expect(screen.getByText(/grouped signals from repeated statewide feeds/i)).toBeInTheDocument()
    expect(screen.getByText(/cdc nndss weekly tables/i)).toBeInTheDocument()
    expect(screen.getByText(/3 notices/i)).toBeInTheDocument()
    expect(screen.getAllByText(/3 diseases/i)).toHaveLength(2)
    expect(screen.getByText(/surveillance-only/i)).toBeInTheDocument()
    expect(screen.getByText(/this feed contributes 3 statewide notices across 3 diseases/i)).toBeInTheDocument()
    expect(screen.getByText(/hepatitis c, chronic, probable/i)).toBeInTheDocument()
    expect(screen.getByText(/hepatitis c, perinatal, confirmed/i)).toBeInTheDocument()
    expect(screen.getByText(/influenza-associated pediatric mortality/i)).toBeInTheDocument()
    expect(screen.getByText(/provider shortages and drinking water issues across the state/i)).toBeInTheDocument()
    expect(screen.getByText(/mental health access constraints/i)).toBeInTheDocument()
    expect(screen.getAllByText(/1 shortage area/i)).toHaveLength(2)
    expect(screen.getByText(/top hpsa score 17/i)).toBeInTheDocument()
    expect(screen.getAllByText(/maximum contaminant level/i)).toHaveLength(2)
    expect(screen.getByText(/346,000 served/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /chester county/i })).toHaveAttribute('href', '/region/4156a173-433d-4823-b48a-deb80c0842fa')
  })
})
