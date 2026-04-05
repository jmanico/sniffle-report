import type { ZodType } from 'zod'

const DATA_BASE = '/data'

export async function fetchStaticJson<T>(path: string, schema: ZodType<T>): Promise<T> {
  const response = await fetch(`${DATA_BASE}/${path}`)

  if (!response.ok) {
    throw new Error(`Failed to load ${path}: ${response.status}`)
  }

  const data = await response.json()
  const result = schema.safeParse(data)

  if (!result.success) {
    throw new Error(`Data validation failed for ${path}`)
  }

  return result.data
}
