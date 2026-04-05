import { fetchParsed } from './client'
import { regionDashboardSchema, type RegionDashboard } from './types'

export function getDashboard(regionId: string, signal?: AbortSignal): Promise<RegionDashboard> {
  return fetchParsed(`/regions/${regionId}/dashboard`, {
    schema: regionDashboardSchema,
    signal,
    errorMessage: 'Unable to load the dashboard right now.',
  })
}
