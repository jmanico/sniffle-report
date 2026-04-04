import { z } from 'zod'

import { fetchParsed, fetchParsedList } from './client'
import {
  type NearbyResourcesFilters,
  type ResourceDetail,
  resourceDetailSchema,
  resourceListItemSchema,
  type ResourcesFilters,
} from './types'

function buildResourceParams(filters: ResourcesFilters = {}) {
  return {
    type: filters.type,
    page: filters.page,
    pageSize: filters.pageSize,
  }
}

export function getResources(regionId: string, filters: ResourcesFilters = {}, signal?: AbortSignal) {
  return fetchParsedList(`/regions/${regionId}/resources`, {
    schema: z.array(resourceListItemSchema),
    params: buildResourceParams(filters),
    signal,
    errorMessage: 'Unable to load local resources right now.',
  })
}

export function getResourceById(
  regionId: string,
  resourceId: string,
  signal?: AbortSignal,
): Promise<ResourceDetail> {
  return fetchParsed(`/regions/${regionId}/resources/${resourceId}`, {
    schema: resourceDetailSchema,
    signal,
    errorMessage: 'Unable to load this resource right now.',
  })
}

export function searchNearby(regionId: string, filters: NearbyResourcesFilters, signal?: AbortSignal) {
  return fetchParsedList(`/regions/${regionId}/resources/nearby`, {
    schema: z.array(resourceListItemSchema),
    params: {
      lat: filters.lat,
      lng: filters.lng,
      radius: filters.radius,
      type: filters.type,
      page: filters.page,
      pageSize: filters.pageSize,
    },
    signal,
    errorMessage: 'Unable to load nearby resources right now.',
  })
}
