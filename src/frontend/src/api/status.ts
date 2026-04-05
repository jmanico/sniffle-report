import { z } from 'zod'

import { fetchParsed } from './client'
import { feedStatusSchema, regionStatusSchema, type FeedStatus, type RegionStatus } from './types'

export function getRegionStatus(signal?: AbortSignal): Promise<RegionStatus[]> {
  return fetchParsed('/status/regions', {
    schema: z.array(regionStatusSchema),
    signal,
    errorMessage: 'Unable to load region status.',
  })
}

export function getFeedStatus(signal?: AbortSignal): Promise<FeedStatus[]> {
  return fetchParsed('/status/feeds', {
    schema: z.array(feedStatusSchema),
    signal,
    errorMessage: 'Unable to load feed status.',
  })
}
