import { useQuery } from '@tanstack/react-query'

import { queryKeys } from '../api/queryKeys'
import { getTrends, getTrendsByAlert } from '../api/trends'
import type { TrendsFilters } from '../api/types'

export function useTrends(regionId: string, filters: TrendsFilters = {}) {
  return useQuery({
    queryKey: queryKeys.trends(regionId, filters),
    queryFn: ({ signal }) => getTrends(regionId, filters, signal),
    enabled: Boolean(regionId),
  })
}

export function useAlertTrends(regionId: string, alertId: string, filters: TrendsFilters = {}) {
  return useQuery({
    queryKey: queryKeys.trendsByAlert(regionId, alertId, filters),
    queryFn: ({ signal }) => getTrendsByAlert(regionId, alertId, filters, signal),
    enabled: Boolean(regionId && alertId),
  })
}
