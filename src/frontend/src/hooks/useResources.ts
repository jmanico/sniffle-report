import { useQuery } from '@tanstack/react-query'

import { queryKeys } from '../api/queryKeys'
import { getResourceById, getResources, searchNearby } from '../api/resources'
import type { NearbyResourcesFilters, ResourcesFilters } from '../api/types'

export function useResources(regionId: string, filters: ResourcesFilters = {}) {
  return useQuery({
    queryKey: queryKeys.resources(regionId, filters),
    queryFn: ({ signal }) => getResources(regionId, filters, signal),
    enabled: Boolean(regionId),
  })
}

export function useResourceById(regionId: string, resourceId: string) {
  return useQuery({
    queryKey: queryKeys.resourceById(regionId, resourceId),
    queryFn: ({ signal }) => getResourceById(regionId, resourceId, signal),
    enabled: Boolean(regionId && resourceId),
  })
}

export function useNearbyResources(regionId: string, filters: NearbyResourcesFilters) {
  return useQuery({
    queryKey: queryKeys.nearbyResources(regionId, filters),
    queryFn: ({ signal }) => searchNearby(regionId, filters, signal),
    enabled: Boolean(regionId),
  })
}
