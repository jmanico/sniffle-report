import type {
  AdminAlertsFilters,
  AdminNewsFilters,
  AdminPreventionFilters,
  AdminResourcesFilters,
  AlertsFilters,
  NearbyResourcesFilters,
  NewsFilters,
  PreventionFilters,
  RegionsFilters,
  ResourcesFilters,
  SearchRegionsFilters,
  TrendsFilters,
} from './types'

export const queryKeys = {
  regions: (filters: RegionsFilters = {}) => ['regions', filters] as const,
  regionById: (regionId: string) => ['regions', regionId] as const,
  searchRegions: (filters: SearchRegionsFilters) => ['regions', 'search', filters] as const,
  statusRegions: () => ['status', 'regions'] as const,
  statusFeeds: () => ['status', 'feeds'] as const,
  dashboard: (regionId: string) => ['dashboard', regionId] as const,
  alerts: (regionId: string, filters: AlertsFilters = {}) => ['alerts', regionId, filters] as const,
  alertById: (regionId: string, alertId: string) => ['alerts', regionId, alertId] as const,
  trends: (regionId: string, filters: TrendsFilters = {}) => ['trends', regionId, filters] as const,
  trendsByAlert: (regionId: string, alertId: string, filters: TrendsFilters = {}) =>
    ['trends', regionId, alertId, filters] as const,
  resources: (regionId: string, filters: ResourcesFilters = {}) =>
    ['resources', regionId, filters] as const,
  resourceById: (regionId: string, resourceId: string) => ['resources', regionId, resourceId] as const,
  nearbyResources: (regionId: string, filters: NearbyResourcesFilters) =>
    ['resources', regionId, 'nearby', filters] as const,
  news: (regionId: string, filters: NewsFilters = {}) => ['news', regionId, filters] as const,
  prevention: (regionId: string, filters: PreventionFilters = {}) =>
    ['prevention', regionId, filters] as const,
  preventionById: (regionId: string, guideId: string) => ['prevention', regionId, guideId] as const,
  adminPrevention: (filters: AdminPreventionFilters = {}) => ['admin', 'prevention', filters] as const,
  adminPreventionById: (guideId: string) => ['admin', 'prevention', guideId] as const,
  adminAlerts: (filters: AdminAlertsFilters = {}) => ['admin', 'alerts', filters] as const,
  adminAlertById: (alertId: string) => ['admin', 'alerts', alertId] as const,
  adminResources: (filters: AdminResourcesFilters = {}) => ['admin', 'resources', filters] as const,
  adminResourceById: (resourceId: string) => ['admin', 'resources', resourceId] as const,
  adminNews: (filters: AdminNewsFilters = {}) => ['admin', 'news', filters] as const,
  adminNewsById: (newsItemId: string) => ['admin', 'news', newsItemId] as const,
}
