import { useQuery } from '@tanstack/react-query'
import { z } from 'zod'

import { fetchStaticJson } from '../api/staticClient'
import {
  snapshotAlertSummarySchema,
  snapshotTrendHighlightSchema,
  snapshotResourceCountsSchema,
  snapshotAccessSignalSummarySchema,
  snapshotEnvironmentalSignalSummarySchema,
  snapshotPreventionSummarySchema,
  snapshotNewsSummarySchema,
  resourceTypeSchema,
} from '../api/types'

// --- State index schema ---

const stateEntrySchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  code: z.string().min(1),
  latitude: z.number().nullable(),
  longitude: z.number().nullable(),
  countyCount: z.number().int().nonnegative(),
  publishedAlertCount: z.number().int().nonnegative(),
  resourceTotal: z.number().int().nonnegative(),
  computedAt: z.string().nullable(),
})

export type StateEntry = z.infer<typeof stateEntrySchema>

// --- State detail schema (with counties) ---

const countyEntrySchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  type: z.string().min(1),
  state: z.string().min(1),
  latitude: z.number().nullable(),
  longitude: z.number().nullable(),
  publishedAlertCount: z.number().int().nonnegative(),
  resourceTotal: z.number().int().nonnegative(),
  computedAt: z.string().nullable(),
})

export type CountyEntry = z.infer<typeof countyEntrySchema>

const stateDetailSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1),
  code: z.string().min(1),
  publishedAlertCount: z.number().int().nonnegative(),
  resourceTotal: z.number().int().nonnegative(),
  computedAt: z.string().nullable(),
  counties: z.array(countyEntrySchema),
})

export type StateDetail = z.infer<typeof stateDetailSchema>

// --- Static dashboard schema (slightly different from API version — includes region metadata) ---

const staticDashboardSchema = z.object({
  regionId: z.string().uuid(),
  regionName: z.string().min(1),
  regionType: z.string().min(1),
  state: z.string().min(1),
  parentName: z.string().nullable(),
  parentId: z.string().uuid().nullable(),
  parentState: z.string().nullable(),
  computedAt: z.string(),
  publishedAlertCount: z.number().int().nonnegative(),
  topAlerts: z.array(snapshotAlertSummarySchema),
  trendHighlights: z.array(snapshotTrendHighlightSchema),
  resourceCounts: snapshotResourceCountsSchema,
  accessSignals: z.array(snapshotAccessSignalSummarySchema),
  environmentalSignals: z.array(snapshotEnvironmentalSignalSummarySchema),
  nearbyResources: z.array(
    z.object({
      id: z.string().uuid(),
      regionId: z.string().uuid(),
      name: z.string().min(1),
      type: resourceTypeSchema,
      address: z.string().min(1),
      phone: z.string().nullable(),
      website: z.string().nullable(),
    }),
  ),
  preventionHighlights: z.array(snapshotPreventionSummarySchema),
  newsHighlights: z.array(snapshotNewsSummarySchema),
})

export type StaticDashboard = z.infer<typeof staticDashboardSchema>

// --- Status schema ---

const statusFeedSchema = z.object({
  name: z.string().min(1),
  type: z.string().min(1),
  isEnabled: z.boolean(),
  lastSyncStatus: z.string().nullable(),
  lastSyncCompletedAt: z.string().nullable(),
  lastRecordsCreated: z.number().int().nullable(),
  lastRecordsFetched: z.number().int().nullable(),
})

const statusSchema = z.object({
  exportedAt: z.string(),
  totalRegions: z.number().int().nonnegative(),
  totalSnapshots: z.number().int().nonnegative(),
  regionsWithAlerts: z.number().int().nonnegative(),
  feeds: z.array(statusFeedSchema),
})

export type SiteStatus = z.infer<typeof statusSchema>

// --- News schema ---

const newsEntrySchema = z.object({
  id: z.string().uuid(),
  headline: z.string().min(1),
  sourceUrl: z.string(),
  publishedAt: z.string(),
  factCheckStatus: z.string().nullable(),
})

export type NewsEntry = z.infer<typeof newsEntrySchema>

// --- Hooks ---

export function useStates() {
  return useQuery({
    queryKey: ['static', 'states'],
    queryFn: () => fetchStaticJson('states.json', z.array(stateEntrySchema)),
    staleTime: Infinity,
  })
}

export function useStateDetail(stateCode: string) {
  return useQuery({
    queryKey: ['static', 'states', stateCode],
    queryFn: () => fetchStaticJson(`states/${stateCode}.json`, stateDetailSchema),
    enabled: Boolean(stateCode),
    staleTime: Infinity,
  })
}

export function useStaticDashboard(regionId: string) {
  return useQuery({
    queryKey: ['static', 'dashboard', regionId],
    queryFn: () => fetchStaticJson(`regions/${regionId}.json`, staticDashboardSchema),
    enabled: Boolean(regionId),
    staleTime: Infinity,
  })
}

export function useSiteStatus() {
  return useQuery({
    queryKey: ['static', 'status'],
    queryFn: () => fetchStaticJson('status.json', statusSchema),
    staleTime: Infinity,
  })
}

export function useStaticNews() {
  return useQuery({
    queryKey: ['static', 'news'],
    queryFn: () => fetchStaticJson('news.json', z.array(newsEntrySchema)),
    staleTime: Infinity,
  })
}
