import { z } from 'zod'

import { fetchParsed, fetchParsedList } from './client'
import { trendSeriesSchema, type TrendSeries, type TrendsFilters } from './types'

function buildTrendParams(filters: TrendsFilters = {}) {
  return {
    disease: filters.disease,
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    page: filters.page,
    pageSize: filters.pageSize,
  }
}

export function getTrends(regionId: string, filters: TrendsFilters = {}, signal?: AbortSignal) {
  return fetchParsedList(`/regions/${regionId}/trends`, {
    schema: z.array(trendSeriesSchema),
    params: buildTrendParams(filters),
    signal,
    errorMessage: 'Unable to load trend data right now.',
  })
}

export function getTrendsByAlert(
  regionId: string,
  alertId: string,
  filters: TrendsFilters = {},
  signal?: AbortSignal,
): Promise<TrendSeries> {
  return fetchParsed(`/regions/${regionId}/alerts/${alertId}/trends`, {
    schema: trendSeriesSchema,
    params: buildTrendParams(filters),
    signal,
    errorMessage: 'Unable to load alert trend data right now.',
  })
}
