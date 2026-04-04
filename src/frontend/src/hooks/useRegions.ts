import { useQuery } from '@tanstack/react-query'

import { getRegionById, getRegions, searchRegions } from '../api/regions'
import { queryKeys } from '../api/queryKeys'
import type { RegionsFilters, SearchRegionsFilters } from '../api/types'

export function useRegions(filters: RegionsFilters = {}, enabled = true) {
  return useQuery({
    queryKey: queryKeys.regions(filters),
    queryFn: ({ signal }) => getRegions(filters, signal),
    enabled,
  })
}

export function useRegionById(regionId: string) {
  return useQuery({
    queryKey: queryKeys.regionById(regionId),
    queryFn: ({ signal }) => getRegionById(regionId, signal),
    enabled: Boolean(regionId),
  })
}

export function useRegionSearch(filters: SearchRegionsFilters, enabled = true) {
  return useQuery({
    queryKey: queryKeys.searchRegions(filters),
    queryFn: ({ signal }) => searchRegions(filters, signal),
    enabled: enabled && filters.q.trim().length > 0,
  })
}
