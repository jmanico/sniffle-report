import { z } from 'zod'

import { fetchParsed, fetchParsedList } from './client'
import { queryKeys } from './queryKeys'
import {
  type RegionDetail,
  regionDetailSchema,
  regionListItemSchema,
  type RegionsFilters,
  type SearchRegionsFilters,
} from './types'

function buildRegionParams(filters: RegionsFilters = {}) {
  return {
    type: filters.type,
    page: filters.page,
    pageSize: filters.pageSize,
  }
}

export function getRegions(filters: RegionsFilters = {}, signal?: AbortSignal) {
  return fetchParsedList('/regions', {
    schema: z.array(regionListItemSchema),
    params: buildRegionParams(filters),
    signal,
    errorMessage: 'Unable to load regions right now.',
  })
}

export function getRegionById(regionId: string, signal?: AbortSignal): Promise<RegionDetail> {
  return fetchParsed(`/regions/${regionId}`, {
    schema: regionDetailSchema,
    signal,
    errorMessage: 'Unable to load this region right now.',
  })
}

export function searchRegions(filters: SearchRegionsFilters, signal?: AbortSignal) {
  return fetchParsedList('/regions/search', {
    schema: z.array(regionListItemSchema),
    params: {
      q: filters.q,
      page: filters.page,
      pageSize: filters.pageSize,
    },
    signal,
    errorMessage: 'Unable to search regions right now.',
  })
}

export { queryKeys }
