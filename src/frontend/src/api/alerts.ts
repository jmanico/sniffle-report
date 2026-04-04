import { z } from 'zod'

import { fetchParsed, fetchParsedList } from './client'
import {
  alertDetailSchema,
  alertListItemSchema,
  type AlertDetail,
  type AlertsFilters,
} from './types'

export function getAlerts(regionId: string, filters: AlertsFilters = {}, signal?: AbortSignal) {
  return fetchParsedList(`/regions/${regionId}/alerts`, {
    schema: z.array(alertListItemSchema),
    params: {
      severity: filters.severity,
      disease: filters.disease,
      status: filters.status,
      dateFrom: filters.dateFrom,
      dateTo: filters.dateTo,
      page: filters.page,
      pageSize: filters.pageSize,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
    },
    signal,
    errorMessage: 'Unable to load health alerts right now.',
  })
}

export function getAlertById(regionId: string, alertId: string, signal?: AbortSignal): Promise<AlertDetail> {
  return fetchParsed(`/regions/${regionId}/alerts/${alertId}`, {
    schema: alertDetailSchema,
    signal,
    errorMessage: 'Unable to load this alert right now.',
  })
}
