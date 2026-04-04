import { useQuery } from '@tanstack/react-query'

import { getAlertById, getAlerts } from '../api/alerts'
import { queryKeys } from '../api/queryKeys'
import type { AlertsFilters } from '../api/types'

export function useAlerts(regionId: string, filters: AlertsFilters = {}) {
  return useQuery({
    queryKey: queryKeys.alerts(regionId, filters),
    queryFn: ({ signal }) => getAlerts(regionId, filters, signal),
    enabled: Boolean(regionId),
  })
}

export function useAlertById(regionId: string, alertId: string) {
  return useQuery({
    queryKey: queryKeys.alertById(regionId, alertId),
    queryFn: ({ signal }) => getAlertById(regionId, alertId, signal),
    enabled: Boolean(regionId && alertId),
  })
}
