import axios, { AxiosError, type AxiosRequestConfig } from 'axios'
import { type ZodType } from 'zod'

import type { PaginatedResult, ProblemDetails } from './types'

const ApiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5001/api/v1'
const LoginRedirectPath = '/admin'

type RequestConfigWithRetry = AxiosRequestConfig & { _retry?: boolean }

let accessToken: string | null = null
let refreshRequest: Promise<string | null> | null = null

function extractAccessToken(payload: unknown) {
  if (!payload || typeof payload !== 'object') {
    return null
  }

  const maybeToken = (payload as Record<string, unknown>).accessToken
    ?? (payload as Record<string, unknown>).token
    ?? (payload as Record<string, unknown>).jwt

  return typeof maybeToken === 'string' && maybeToken.length > 0 ? maybeToken : null
}

function redirectToLogin() {
  if (typeof window === 'undefined') {
    return
  }

  if (window.location.pathname !== LoginRedirectPath) {
    window.location.assign(LoginRedirectPath)
  }
}

export function getAccessToken() {
  return accessToken
}

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function clearAccessToken() {
  accessToken = null
}

export function toFriendlyApiMessage(error: unknown, fallbackMessage: string) {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data as ProblemDetails | undefined
    return responseData?.detail ?? responseData?.title ?? fallbackMessage
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallbackMessage
}

async function refreshAccessToken() {
  if (!refreshRequest) {
    refreshRequest = refreshClient
      .post('/auth/refresh')
      .then((response) => {
        const token = extractAccessToken(response.data)
        setAccessToken(token)
        return token
      })
      .catch(() => {
        clearAccessToken()
        redirectToLogin()
        return null
      })
      .finally(() => {
        refreshRequest = null
      })
  }

  return refreshRequest
}

export const apiClient = axios.create({
  baseURL: ApiBaseUrl,
  withCredentials: true,
  headers: {
    Accept: 'application/json',
  },
})

const refreshClient = axios.create({
  baseURL: ApiBaseUrl,
  withCredentials: true,
})

apiClient.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RequestConfigWithRetry | undefined

    if (error.response?.status !== 401 || !config || config._retry || config.url?.includes('/auth/refresh')) {
      throw error
    }

    config._retry = true

    const refreshedToken = await refreshAccessToken()
    if (!refreshedToken) {
      throw error
    }

    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${refreshedToken}`

    return apiClient.request(config)
  },
)

interface FetchOptions<TResponse> {
  schema: ZodType<TResponse>
  params?: Record<string, string | number | boolean | undefined>
  signal?: AbortSignal
  errorMessage: string
}

function parseWithSchema<TResponse>(schema: ZodType<TResponse>, data: unknown, errorMessage: string) {
  const result = schema.safeParse(data)

  if (!result.success) {
    throw new Error(errorMessage)
  }

  return result.data
}

export async function fetchParsed<TResponse>(path: string, options: FetchOptions<TResponse>) {
  try {
    const response = await apiClient.get(path, {
      params: options.params,
      signal: options.signal,
    })

    return parseWithSchema(options.schema, response.data, options.errorMessage)
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, options.errorMessage))
  }
}

export async function fetchParsedList<TItem>(path: string, options: FetchOptions<TItem[]>) {
  try {
    const response = await apiClient.get(path, {
      params: options.params,
      signal: options.signal,
    })

    const items = parseWithSchema(options.schema, response.data, options.errorMessage)
    const totalCountHeader = response.headers['x-total-count']
    const totalCount = typeof totalCountHeader === 'string' ? Number.parseInt(totalCountHeader, 10) : items.length

    return {
      items,
      totalCount: Number.isNaN(totalCount) ? items.length : totalCount,
    } satisfies PaginatedResult<TItem>
  } catch (error) {
    throw new Error(toFriendlyApiMessage(error, options.errorMessage))
  }
}
