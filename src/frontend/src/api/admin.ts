import { z } from 'zod'

import { apiClient, toFriendlyApiMessage } from './client'
import {
  adminAlertDetailSchema,
  adminAlertInputSchema,
  adminAlertListItemSchema,
  adminAlertStatusInputSchema,
  adminNewsItemDetailSchema,
  adminNewsItemInputSchema,
  adminNewsItemListItemSchema,
  adminPreventionGuideDetailSchema,
  adminPreventionGuideInputSchema,
  adminPreventionGuideListItemSchema,
  adminResourceDetailSchema,
  adminResourceInputSchema,
  adminResourceListItemSchema,
  type AdminAlertDetail,
  type AdminAlertInput,
  type AdminAlertsFilters,
  type AdminAlertStatusInput,
  type AdminNewsFilters,
  type AdminNewsItemDetail,
  type AdminNewsItemInput,
  type AdminPreventionFilters,
  type AdminPreventionGuideDetail,
  type AdminPreventionGuideInput,
  type AdminResourcesFilters,
  type AdminResourceDetail,
  type AdminResourceInput,
} from './types'

function parseResponse<T>(schema: z.ZodType<T>, data: unknown, errorMessage: string) {
  const result = schema.safeParse(data)

  if (!result.success) {
    throw new Error(errorMessage)
  }

  return result.data
}

function getTotalCount(header: unknown, fallbackCount: number) {
  return typeof header === 'string' ? Number.parseInt(header, 10) || fallbackCount : fallbackCount
}

function buildDeletePayload(justification: string) {
  return {
    data: {
      justification,
    },
  }
}

export async function getAdminPreventionGuides(filters: AdminPreventionFilters = {}, signal?: AbortSignal) {
  try {
    const response = await apiClient.get('/admin/prevention', {
      params: filters,
      signal,
    })

    const items = parseResponse(
      z.array(adminPreventionGuideListItemSchema),
      response.data,
      'Unable to load prevention guides right now.',
    )

    return {
      items,
      totalCount: getTotalCount(response.headers['x-total-count'], items.length),
    }
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load prevention guides right now.'))
  }
}

export async function getAdminAlerts(filters: AdminAlertsFilters = {}, signal?: AbortSignal) {
  try {
    const response = await apiClient.get('/admin/alerts', {
      params: filters,
      signal,
    })

    const items = parseResponse(
      z.array(adminAlertListItemSchema),
      response.data,
      'Unable to load admin alerts right now.',
    )

    return {
      items,
      totalCount: getTotalCount(response.headers['x-total-count'], items.length),
    }
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load admin alerts right now.'))
  }
}

export async function getAdminAlertById(alertId: string, signal?: AbortSignal): Promise<AdminAlertDetail> {
  try {
    const response = await apiClient.get(`/admin/alerts/${alertId}`, { signal })
    return parseResponse(adminAlertDetailSchema, response.data, 'Unable to load this alert right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load this alert right now.'))
  }
}

export async function createAdminAlert(input: AdminAlertInput): Promise<AdminAlertDetail> {
  const payload = adminAlertInputSchema.parse(input)

  try {
    const response = await apiClient.post('/admin/alerts', payload)
    return parseResponse(adminAlertDetailSchema, response.data, 'Unable to create this alert right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to create this alert right now.'))
  }
}

export async function updateAdminAlert(alertId: string, input: AdminAlertInput): Promise<AdminAlertDetail> {
  const payload = adminAlertInputSchema.parse(input)

  try {
    const response = await apiClient.put(`/admin/alerts/${alertId}`, payload)
    return parseResponse(adminAlertDetailSchema, response.data, 'Unable to update this alert right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to update this alert right now.'))
  }
}

export async function updateAdminAlertStatus(alertId: string, input: AdminAlertStatusInput): Promise<AdminAlertDetail> {
  const payload = adminAlertStatusInputSchema.parse(input)

  try {
    const response = await apiClient.put(`/admin/alerts/${alertId}/status`, payload)
    return parseResponse(adminAlertDetailSchema, response.data, 'Unable to update alert status right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to update alert status right now.'))
  }
}

export async function deleteAdminAlert(alertId: string, justification: string) {
  try {
    await apiClient.delete(`/admin/alerts/${alertId}`, buildDeletePayload(justification))
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to delete this alert right now.'))
  }
}

export async function getAdminPreventionGuideById(guideId: string, signal?: AbortSignal): Promise<AdminPreventionGuideDetail> {
  try {
    const response = await apiClient.get(`/admin/prevention/${guideId}`, { signal })
    return parseResponse(adminPreventionGuideDetailSchema, response.data, 'Unable to load this prevention guide right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load this prevention guide right now.'))
  }
}

export async function createAdminPreventionGuide(input: AdminPreventionGuideInput): Promise<AdminPreventionGuideDetail> {
  const payload = adminPreventionGuideInputSchema.parse(input)

  try {
    const response = await apiClient.post('/admin/prevention', payload)
    return parseResponse(adminPreventionGuideDetailSchema, response.data, 'Unable to create this prevention guide right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to create this prevention guide right now.'))
  }
}

export async function updateAdminPreventionGuide(
  guideId: string,
  input: AdminPreventionGuideInput,
): Promise<AdminPreventionGuideDetail> {
  const payload = adminPreventionGuideInputSchema.parse(input)

  try {
    const response = await apiClient.put(`/admin/prevention/${guideId}`, payload)
    return parseResponse(adminPreventionGuideDetailSchema, response.data, 'Unable to update this prevention guide right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to update this prevention guide right now.'))
  }
}

export async function deleteAdminPreventionGuide(guideId: string, justification: string) {
  try {
    await apiClient.delete(`/admin/prevention/${guideId}`, buildDeletePayload(justification))
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to delete this prevention guide right now.'))
  }
}

export async function getAdminResources(filters: AdminResourcesFilters = {}, signal?: AbortSignal) {
  try {
    const response = await apiClient.get('/admin/resources', {
      params: filters,
      signal,
    })

    const items = parseResponse(
      z.array(adminResourceListItemSchema),
      response.data,
      'Unable to load resources right now.',
    )

    return {
      items,
      totalCount: getTotalCount(response.headers['x-total-count'], items.length),
    }
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load resources right now.'))
  }
}

export async function getAdminResourceById(resourceId: string, signal?: AbortSignal): Promise<AdminResourceDetail> {
  try {
    const response = await apiClient.get(`/admin/resources/${resourceId}`, { signal })
    return parseResponse(adminResourceDetailSchema, response.data, 'Unable to load this resource right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load this resource right now.'))
  }
}

export async function createAdminResource(input: AdminResourceInput): Promise<AdminResourceDetail> {
  const payload = adminResourceInputSchema.parse(input)

  try {
    const response = await apiClient.post('/admin/resources', payload)
    return parseResponse(adminResourceDetailSchema, response.data, 'Unable to create this resource right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to create this resource right now.'))
  }
}

export async function updateAdminResource(resourceId: string, input: AdminResourceInput): Promise<AdminResourceDetail> {
  const payload = adminResourceInputSchema.parse(input)

  try {
    const response = await apiClient.put(`/admin/resources/${resourceId}`, payload)
    return parseResponse(adminResourceDetailSchema, response.data, 'Unable to update this resource right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to update this resource right now.'))
  }
}

export async function deleteAdminResource(resourceId: string, justification: string) {
  try {
    await apiClient.delete(`/admin/resources/${resourceId}`, buildDeletePayload(justification))
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to delete this resource right now.'))
  }
}

export async function getAdminNewsItems(filters: AdminNewsFilters = {}, signal?: AbortSignal) {
  try {
    const response = await apiClient.get('/admin/news', {
      params: filters,
      signal,
    })

    const items = parseResponse(
      z.array(adminNewsItemListItemSchema),
      response.data,
      'Unable to load news items right now.',
    )

    return {
      items,
      totalCount: getTotalCount(response.headers['x-total-count'], items.length),
    }
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load news items right now.'))
  }
}

export async function getAdminNewsItemById(newsItemId: string, signal?: AbortSignal): Promise<AdminNewsItemDetail> {
  try {
    const response = await apiClient.get(`/admin/news/${newsItemId}`, { signal })
    return parseResponse(adminNewsItemDetailSchema, response.data, 'Unable to load this news item right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to load this news item right now.'))
  }
}

export async function createAdminNewsItem(input: AdminNewsItemInput): Promise<AdminNewsItemDetail> {
  const payload = adminNewsItemInputSchema.parse(input)

  try {
    const response = await apiClient.post('/admin/news', payload)
    return parseResponse(adminNewsItemDetailSchema, response.data, 'Unable to create this news item right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to create this news item right now.'))
  }
}

export async function updateAdminNewsItem(newsItemId: string, input: AdminNewsItemInput): Promise<AdminNewsItemDetail> {
  const payload = adminNewsItemInputSchema.parse(input)

  try {
    const response = await apiClient.put(`/admin/news/${newsItemId}`, payload)
    return parseResponse(adminNewsItemDetailSchema, response.data, 'Unable to update this news item right now.')
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to update this news item right now.'))
  }
}

export async function deleteAdminNewsItem(newsItemId: string, justification: string) {
  try {
    await apiClient.delete(`/admin/news/${newsItemId}`, buildDeletePayload(justification))
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, 'Unable to delete this news item right now.'))
  }
}
