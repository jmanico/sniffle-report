import { useQuery } from '@tanstack/react-query'

import {
  getAdminAlertById,
  getAdminAlerts,
  getAdminNewsItemById,
  getAdminNewsItems,
  getAdminPreventionGuideById,
  getAdminPreventionGuides,
  getAdminResourceById,
  getAdminResources,
} from '../api/admin'
import { queryKeys } from '../api/queryKeys'
import type { AdminAlertsFilters, AdminNewsFilters, AdminPreventionFilters, AdminResourcesFilters } from '../api/types'

export function useAdminAlerts(filters: AdminAlertsFilters = {}) {
  return useQuery({
    queryKey: queryKeys.adminAlerts(filters),
    queryFn: ({ signal }) => getAdminAlerts(filters, signal),
  })
}

export function useAdminAlert(alertId: string) {
  return useQuery({
    queryKey: queryKeys.adminAlertById(alertId),
    queryFn: ({ signal }) => getAdminAlertById(alertId, signal),
    enabled: Boolean(alertId),
  })
}

export function useAdminPrevention(filters: AdminPreventionFilters = {}) {
  return useQuery({
    queryKey: queryKeys.adminPrevention(filters),
    queryFn: ({ signal }) => getAdminPreventionGuides(filters, signal),
  })
}

export function useAdminPreventionGuide(guideId: string) {
  return useQuery({
    queryKey: queryKeys.adminPreventionById(guideId),
    queryFn: ({ signal }) => getAdminPreventionGuideById(guideId, signal),
    enabled: Boolean(guideId),
  })
}

export function useAdminResources(filters: AdminResourcesFilters = {}) {
  return useQuery({
    queryKey: queryKeys.adminResources(filters),
    queryFn: ({ signal }) => getAdminResources(filters, signal),
  })
}

export function useAdminResource(resourceId: string) {
  return useQuery({
    queryKey: queryKeys.adminResourceById(resourceId),
    queryFn: ({ signal }) => getAdminResourceById(resourceId, signal),
    enabled: Boolean(resourceId),
  })
}

export function useAdminNews(filters: AdminNewsFilters = {}) {
  return useQuery({
    queryKey: queryKeys.adminNews(filters),
    queryFn: ({ signal }) => getAdminNewsItems(filters, signal),
  })
}

export function useAdminNewsItem(newsItemId: string) {
  return useQuery({
    queryKey: queryKeys.adminNewsById(newsItemId),
    queryFn: ({ signal }) => getAdminNewsItemById(newsItemId, signal),
    enabled: Boolean(newsItemId),
  })
}
