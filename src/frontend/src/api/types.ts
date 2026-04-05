import { z } from 'zod'

import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

function createEnumSchema<const TValues extends readonly [string, ...string[]]>(values: TValues) {
  return z.union([z.number().int(), z.enum(values)]).transform((value, context) => {
    if (typeof value === 'string') {
      return value
    }

    const mappedValue = values[value]

    if (mappedValue) {
      return mappedValue
    }

    context.issues.push({
      code: z.ZodIssueCode.custom,
      message: `Unsupported enum value: ${value}`,
      input: value,
    })

    return z.NEVER
  })
}

function sanitizeExternalUrl(value: string | null | undefined) {
  if (!value) {
    return null
  }

  const sanitized = validateAndSanitizeUrl(value)
  return sanitized === '/' ? null : sanitized
}

export const regionTypeSchema = createEnumSchema(['Zip', 'County', 'Metro', 'State'] as const)
export const alertSeveritySchema = createEnumSchema(['Low', 'Moderate', 'High', 'Critical'] as const)
export const alertStatusSchema = createEnumSchema(['Draft', 'Published', 'Archived'] as const)
export const resourceTypeSchema = createEnumSchema(
  ['Clinic', 'Pharmacy', 'VaccinationSite', 'Hospital'] as const,
)
export const costTierTypeSchema = createEnumSchema(
  ['Free', 'Insured', 'OutOfPocket', 'Promotional'] as const,
)
export const factCheckStatusSchema = createEnumSchema(
  ['Pending', 'Verified', 'Disputed', 'Unverified'] as const,
)

export type RegionType = z.infer<typeof regionTypeSchema>
export type AlertSeverity = z.infer<typeof alertSeveritySchema>
export type AlertStatus = z.infer<typeof alertStatusSchema>
export type ResourceType = z.infer<typeof resourceTypeSchema>
export type CostTierType = z.infer<typeof costTierTypeSchema>
export type FactCheckStatus = z.infer<typeof factCheckStatusSchema>

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  type?: string
  instance?: string
  errors?: Record<string, string[]>
}

export interface PaginatedResult<TItem> {
  items: TItem[]
  totalCount: number
}

export interface RegionsFilters {
  type?: RegionType
  page?: number
  pageSize?: number
}

export interface SearchRegionsFilters extends Omit<RegionsFilters, 'type'> {
  q: string
}

export interface AlertsFilters {
  severity?: AlertSeverity
  disease?: string
  status?: AlertStatus
  dateFrom?: string
  dateTo?: string
  page?: number
  pageSize?: number
  sortBy?: 'createdAt' | 'sourceDate' | 'caseCount'
  sortDirection?: 'asc' | 'desc'
}

export interface TrendsFilters {
  disease?: string
  dateFrom?: string
  dateTo?: string
  page?: number
  pageSize?: number
}

export interface ResourcesFilters {
  type?: ResourceType
  page?: number
  pageSize?: number
}

export interface NearbyResourcesFilters extends ResourcesFilters {
  lat: number
  lng: number
  radius?: number
}

export interface PreventionFilters {
  disease?: string
  page?: number
  pageSize?: number
}

export interface AdminPreventionFilters {
  regionId?: string
  disease?: string
  page?: number
  pageSize?: number
}

export interface AdminResourcesFilters {
  regionId?: string
  type?: ResourceType
  name?: string
  page?: number
  pageSize?: number
}

export interface AdminNewsFilters {
  regionId?: string
  headline?: string
  page?: number
  pageSize?: number
}

export interface AdminAlertsFilters {
  regionId?: string
  disease?: string
  status?: AlertStatus
  page?: number
  pageSize?: number
}

const isoDateTimeSchema = z.string().datetime({ offset: true })
const guidSchema = z.string().uuid()

export const regionParentSchema = z.object({
  id: guidSchema,
  name: z.string().min(1),
  type: regionTypeSchema,
})

export const regionListItemSchema = z.object({
  id: guidSchema,
  name: z.string().min(1),
  type: regionTypeSchema,
  state: z.string().min(1),
  parentId: guidSchema.nullable(),
})

export const regionDetailSchema = z.object({
  id: guidSchema,
  name: z.string().min(1),
  type: regionTypeSchema,
  state: z.string().min(1),
  latitude: z.number().nullable(),
  longitude: z.number().nullable(),
  childCount: z.number().int().nonnegative(),
  parent: regionParentSchema.nullable(),
})

export const diseaseTrendSchema = z.object({
  date: isoDateTimeSchema,
  caseCount: z.number().int().nonnegative(),
  source: z.string().min(1),
  sourceDate: isoDateTimeSchema,
  notes: z.string().nullable(),
})

export const alertListItemSchema = z.object({
  id: guidSchema,
  regionId: guidSchema,
  disease: z.string().min(1),
  title: z.string().min(1),
  summary: z.string(),
  severity: alertSeveritySchema,
  caseCount: z.number().int().nonnegative(),
  sourceAttribution: z.string().min(1),
  sourceDate: isoDateTimeSchema,
  createdAt: isoDateTimeSchema,
})

export const alertDetailSchema = alertListItemSchema.extend({
  status: alertStatusSchema,
  trends: z.array(diseaseTrendSchema),
})

export const trendDataPointSchema = z.object({
  date: isoDateTimeSchema,
  caseCount: z.number().int().nonnegative(),
  source: z.string().min(1),
  sourceDate: isoDateTimeSchema,
})

export const trendSeriesSchema = z.object({
  alertId: guidSchema,
  regionId: guidSchema,
  disease: z.string().min(1),
  alertTitle: z.string().min(1),
  sourceAttribution: z.string().min(1),
  dataPoints: z.array(trendDataPointSchema),
})

export const resourceHoursSchema = z.object({
  mon: z.string().nullable(),
  tue: z.string().nullable(),
  wed: z.string().nullable(),
  thu: z.string().nullable(),
  fri: z.string().nullable(),
  sat: z.string().nullable(),
  sun: z.string().nullable(),
})

export const resourceListItemSchema = z.object({
  id: guidSchema,
  regionId: guidSchema,
  name: z.string().min(1),
  type: resourceTypeSchema,
  address: z.string().min(1),
  phone: z.string().nullable(),
  website: z.string().nullable().transform((value) => sanitizeExternalUrl(value)),
  latitude: z.number().nullable(),
  longitude: z.number().nullable(),
  distanceMiles: z.number().nullable(),
})

export const resourceDetailSchema = resourceListItemSchema.extend({
  hours: resourceHoursSchema,
  services: z.array(z.string().min(1)),
})

export const costTierSchema = z.object({
  type: costTierTypeSchema,
  price: z.number().nonnegative(),
  provider: z.string().min(1),
  notes: z.string().nullable(),
})

export const preventionListItemSchema = z.object({
  id: guidSchema,
  regionId: guidSchema,
  disease: z.string().min(1),
  title: z.string().min(1),
  createdAt: isoDateTimeSchema,
  costTiers: z.array(costTierSchema),
})

export const preventionDetailSchema = preventionListItemSchema.extend({
  content: z.string().min(1),
})

export const adminPreventionGuideListItemSchema = preventionListItemSchema.extend({
  isDeleted: z.boolean(),
})

export const adminAlertListItemSchema = z.object({
  id: guidSchema,
  regionId: guidSchema,
  disease: z.string().min(1),
  title: z.string().min(1),
  severity: alertSeveritySchema,
  caseCount: z.number().int().nonnegative(),
  sourceAttribution: z.string().min(1),
  sourceDate: isoDateTimeSchema,
  status: alertStatusSchema,
  createdAt: isoDateTimeSchema,
})

export const adminAlertDetailSchema = adminAlertListItemSchema.extend({
  summary: z.string().min(1),
  updatedAt: isoDateTimeSchema,
})

export const adminPreventionGuideDetailSchema = preventionDetailSchema.extend({
  isDeleted: z.boolean(),
  deletedAt: isoDateTimeSchema.nullable(),
})

export const adminResourceListItemSchema = resourceListItemSchema.omit({
  distanceMiles: true,
})

export const adminResourceDetailSchema = resourceDetailSchema
  .omit({
    distanceMiles: true,
  })

export const adminNewsItemListItemSchema = z.object({
  id: guidSchema,
  regionId: guidSchema,
  headline: z.string().min(1),
  sourceUrl: z.string().transform((value) => validateAndSanitizeUrl(value)),
  publishedAt: isoDateTimeSchema,
  createdAt: isoDateTimeSchema,
  isDeleted: z.boolean(),
  factCheckStatus: factCheckStatusSchema.nullable(),
})

export const adminNewsItemDetailSchema = adminNewsItemListItemSchema.extend({
  content: z.string().min(1),
  deletedAt: isoDateTimeSchema.nullable(),
})

export const adminCostTierInputSchema = z.object({
  type: costTierTypeSchema,
  price: z.number().nonnegative(),
  provider: z.string().trim().min(1).max(200),
  notes: z.string().trim().max(1000).nullable().optional().transform((value) => value || null),
})

export const adminPreventionGuideInputSchema = z.object({
  regionId: guidSchema,
  disease: z.string().trim().min(1).max(120),
  title: z.string().trim().min(1).max(200),
  content: z.string().trim().min(1).max(10_000),
  costTiers: z.array(adminCostTierInputSchema),
})

export const adminResourceInputSchema = z.object({
  regionId: guidSchema,
  name: z.string().trim().min(1).max(200),
  type: resourceTypeSchema,
  address: z.string().trim().min(1).max(300),
  phone: z.string().trim().max(40).optional().or(z.literal('')).transform((value) => value || undefined),
  website: z.string().trim().optional().or(z.literal('')).transform((value) => value || undefined),
  latitude: z.number().min(-90).max(90).nullable().optional(),
  longitude: z.number().min(-180).max(180).nullable().optional(),
  hours: resourceHoursSchema,
  services: z.array(z.string().trim().min(1).max(100)),
}).superRefine((value, context) => {
  if ((value.latitude == null) !== (value.longitude == null)) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Latitude and longitude must both be provided together.',
      path: ['latitude'],
    })
  }

  if (value.website) {
    const sanitized = validateAndSanitizeUrl(value.website)
    if (sanitized === '/') {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Website must be a valid https URL.',
        path: ['website'],
      })
    }
  }
})

export const adminNewsItemInputSchema = z.object({
  regionId: guidSchema,
  headline: z.string().trim().min(1).max(300),
  content: z.string().trim().min(1).max(10_000),
  sourceUrl: z.string().trim().min(1),
  publishedAt: z.string().min(1),
}).superRefine((value, context) => {
  const sanitized = validateAndSanitizeUrl(value.sourceUrl)
  if (sanitized === '/') {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Source URL must be a valid https URL.',
      path: ['sourceUrl'],
    })
  }
})

export const snapshotAlertSummarySchema = z.object({
  alertId: guidSchema,
  disease: z.string().min(1),
  title: z.string().min(1),
  severity: z.string().min(1),
  caseCount: z.number().int().nonnegative(),
  sourceDate: isoDateTimeSchema,
})

export const snapshotTrendHighlightSchema = z.object({
  alertId: guidSchema,
  disease: z.string().min(1),
  latestCaseCount: z.number().int().nonnegative(),
  previousCaseCount: z.number().int().nonnegative(),
  wowChangePercent: z.number(),
  latestDate: isoDateTimeSchema,
})

export const snapshotResourceCountsSchema = z.object({
  clinic: z.number().int().nonnegative(),
  pharmacy: z.number().int().nonnegative(),
  vaccinationSite: z.number().int().nonnegative(),
  hospital: z.number().int().nonnegative(),
  total: z.number().int().nonnegative(),
})

export const snapshotPreventionSummarySchema = z.object({
  guideId: guidSchema,
  disease: z.string().min(1),
  title: z.string().min(1),
  hasCostTiers: z.boolean(),
})

export const snapshotNewsSummarySchema = z.object({
  newsItemId: guidSchema,
  headline: z.string().min(1),
  publishedAt: isoDateTimeSchema,
  factCheckStatus: z.string().nullable(),
})

export const regionDashboardSchema = z.object({
  regionId: guidSchema,
  computedAt: isoDateTimeSchema,
  publishedAlertCount: z.number().int().nonnegative(),
  topAlerts: z.array(snapshotAlertSummarySchema),
  trendHighlights: z.array(snapshotTrendHighlightSchema),
  resourceCounts: snapshotResourceCountsSchema,
  preventionHighlights: z.array(snapshotPreventionSummarySchema),
  newsHighlights: z.array(snapshotNewsSummarySchema),
})

export type SnapshotAlertSummary = z.infer<typeof snapshotAlertSummarySchema>
export type SnapshotTrendHighlight = z.infer<typeof snapshotTrendHighlightSchema>
export type SnapshotResourceCounts = z.infer<typeof snapshotResourceCountsSchema>
export type SnapshotPreventionSummary = z.infer<typeof snapshotPreventionSummarySchema>
export type SnapshotNewsSummary = z.infer<typeof snapshotNewsSummarySchema>
export type RegionDashboard = z.infer<typeof regionDashboardSchema>

export interface NewsFilters {
  headline?: string
  page?: number
  pageSize?: number
}

export const newsListItemSchema = z.object({
  id: guidSchema,
  regionId: guidSchema,
  headline: z.string().min(1),
  sourceUrl: z.string(),
  publishedAt: isoDateTimeSchema,
  createdAt: isoDateTimeSchema,
  factCheckStatus: factCheckStatusSchema.nullable(),
  sourceAttribution: z.string().nullable(),
})

export type NewsListItem = z.infer<typeof newsListItemSchema>

export const regionStatusSchema = z.object({
  regionId: guidSchema,
  name: z.string().min(1),
  type: z.string().min(1),
  state: z.string().min(1),
  parentName: z.string().nullable(),
  computedAt: isoDateTimeSchema.nullable(),
  publishedAlertCount: z.number().int().nonnegative(),
  resourceTotal: z.number().int().nonnegative(),
})

export const feedStatusSchema = z.object({
  id: guidSchema,
  name: z.string().min(1),
  type: z.string().min(1),
  isEnabled: z.boolean(),
  lastSyncStatus: z.string().nullable(),
  lastSyncCompletedAt: isoDateTimeSchema.nullable(),
  consecutiveFailureCount: z.number().int().nonnegative(),
  lastRecordsCreated: z.number().int().nullable(),
  lastRecordsFetched: z.number().int().nullable(),
  lastRecordsSkippedUnmappable: z.number().int().nullable(),
  lastSyncError: z.string().nullable(),
})

export type RegionStatus = z.infer<typeof regionStatusSchema>
export type FeedStatus = z.infer<typeof feedStatusSchema>

export const adminAlertInputSchema = z.object({
  regionId: guidSchema,
  disease: z.string().trim().min(1).max(120),
  title: z.string().trim().min(1).max(200),
  summary: z.string().trim().min(1).max(2000),
  severity: alertSeveritySchema,
  caseCount: z.number().int().nonnegative(),
  sourceAttribution: z.string().trim().min(1).max(300),
  sourceDate: z.string().min(1),
  status: alertStatusSchema,
})

export const adminAlertStatusInputSchema = z.object({
  status: alertStatusSchema,
  justification: z.string().trim().max(2000).optional().or(z.literal('')).transform((value) => value || undefined),
}).superRefine((value, context) => {
  if ((value.status === 'Archived' || value.status === 'Draft') && !value.justification) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Justification is required when moving an alert to Draft or Archived.',
      path: ['justification'],
    })
  }
})

export type RegionParent = z.infer<typeof regionParentSchema>
export type RegionListItem = z.infer<typeof regionListItemSchema>
export type RegionDetail = z.infer<typeof regionDetailSchema>
export type DiseaseTrend = z.infer<typeof diseaseTrendSchema>
export type AlertListItem = z.infer<typeof alertListItemSchema>
export type AlertDetail = z.infer<typeof alertDetailSchema>
export type TrendDataPoint = z.infer<typeof trendDataPointSchema>
export type TrendSeries = z.infer<typeof trendSeriesSchema>
export type ResourceHours = z.infer<typeof resourceHoursSchema>
export type ResourceListItem = z.infer<typeof resourceListItemSchema>
export type ResourceDetail = z.infer<typeof resourceDetailSchema>
export type CostTier = z.infer<typeof costTierSchema>
export type PreventionListItem = z.infer<typeof preventionListItemSchema>
export type PreventionDetail = z.infer<typeof preventionDetailSchema>
export type AdminAlertListItem = z.infer<typeof adminAlertListItemSchema>
export type AdminAlertDetail = z.infer<typeof adminAlertDetailSchema>
export type AdminPreventionGuideListItem = z.infer<typeof adminPreventionGuideListItemSchema>
export type AdminPreventionGuideDetail = z.infer<typeof adminPreventionGuideDetailSchema>
export type AdminResourceListItem = z.infer<typeof adminResourceListItemSchema>
export type AdminResourceDetail = z.infer<typeof adminResourceDetailSchema>
export type AdminNewsItemListItem = z.infer<typeof adminNewsItemListItemSchema>
export type AdminNewsItemDetail = z.infer<typeof adminNewsItemDetailSchema>
export type AdminCostTierInput = z.infer<typeof adminCostTierInputSchema>
export type AdminPreventionGuideInput = z.infer<typeof adminPreventionGuideInputSchema>
export type AdminResourceInput = z.infer<typeof adminResourceInputSchema>
export type AdminNewsItemInput = z.infer<typeof adminNewsItemInputSchema>
export type AdminAlertInput = z.infer<typeof adminAlertInputSchema>
export type AdminAlertStatusInput = z.infer<typeof adminAlertStatusInputSchema>
