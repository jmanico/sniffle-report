import { useQuery } from '@tanstack/react-query'

import { getDashboard } from '../api/dashboard'
import { queryKeys } from '../api/queryKeys'

export function useDashboard(regionId: string) {
  return useQuery({
    queryKey: queryKeys.dashboard(regionId),
    queryFn: ({ signal }) => getDashboard(regionId, signal),
    enabled: Boolean(regionId),
  })
}
