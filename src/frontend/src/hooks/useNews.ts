import { useQuery } from '@tanstack/react-query'

import { getNews } from '../api/news'
import { queryKeys } from '../api/queryKeys'
import type { NewsFilters } from '../api/types'

export function useNews(regionId: string, filters: NewsFilters = {}) {
  return useQuery({
    queryKey: queryKeys.news(regionId, filters),
    queryFn: ({ signal }) => getNews(regionId, filters, signal),
    enabled: Boolean(regionId),
  })
}
