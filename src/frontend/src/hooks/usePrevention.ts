import { useQuery } from '@tanstack/react-query'

import { getGuideById, getPreventionGuides } from '../api/prevention'
import { queryKeys } from '../api/queryKeys'
import type { PreventionFilters } from '../api/types'

export function usePrevention(regionId: string, filters: PreventionFilters = {}) {
  return useQuery({
    queryKey: queryKeys.prevention(regionId, filters),
    queryFn: ({ signal }) => getPreventionGuides(regionId, filters, signal),
    enabled: Boolean(regionId),
  })
}

export function usePreventionGuide(regionId: string, guideId: string) {
  return useQuery({
    queryKey: queryKeys.preventionById(regionId, guideId),
    queryFn: ({ signal }) => getGuideById(regionId, guideId, signal),
    enabled: Boolean(regionId && guideId),
  })
}
