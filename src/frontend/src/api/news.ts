import { z } from 'zod'

import { fetchParsedList } from './client'
import { newsListItemSchema, type NewsFilters } from './types'

export function getNews(regionId: string, filters: NewsFilters = {}, signal?: AbortSignal) {
  return fetchParsedList(`/regions/${regionId}/news`, {
    schema: z.array(newsListItemSchema),
    params: {
      headline: filters.headline,
      page: filters.page,
      pageSize: filters.pageSize,
    },
    signal,
    errorMessage: 'Unable to load health news right now.',
  })
}
