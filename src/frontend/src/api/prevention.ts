import { z } from 'zod'

import { fetchParsed, fetchParsedList } from './client'
import {
  type PreventionDetail,
  preventionDetailSchema,
  preventionListItemSchema,
  type PreventionFilters,
} from './types'

export function getPreventionGuides(regionId: string, filters: PreventionFilters = {}, signal?: AbortSignal) {
  return fetchParsedList(`/regions/${regionId}/prevention`, {
    schema: z.array(preventionListItemSchema),
    params: {
      disease: filters.disease,
      page: filters.page,
      pageSize: filters.pageSize,
    },
    signal,
    errorMessage: 'Unable to load prevention guides right now.',
  })
}

export function getGuideById(regionId: string, guideId: string, signal?: AbortSignal): Promise<PreventionDetail> {
  return fetchParsed(`/regions/${regionId}/prevention/${guideId}`, {
    schema: preventionDetailSchema,
    signal,
    errorMessage: 'Unable to load this prevention guide right now.',
  })
}
