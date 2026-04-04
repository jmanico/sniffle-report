import { describe, expect, it } from 'vitest'

import {
  alertListItemSchema,
  regionListItemSchema,
  resourceListItemSchema,
} from './types'

describe('API schemas', () => {
  it('normalizes numeric enum responses into named frontend values', () => {
    const parsed = regionListItemSchema.parse({
      id: '3f9e8070-9f83-463b-bd41-0ac2fa9c8cb4',
      name: 'Travis County',
      type: 1,
      state: 'TX',
      parentId: null,
    })

    expect(parsed.type).toBe('County')
  })

  it('sanitizes external resource websites before components receive them', () => {
    const parsed = resourceListItemSchema.parse({
      id: '3f9e8070-9f83-463b-bd41-0ac2fa9c8cb4',
      regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
      name: 'Austin Public Health',
      type: 0,
      address: '123 Main St',
      phone: '512-555-0199',
      website: 'http://unsafe.example.com',
      latitude: 30.2672,
      longitude: -97.7431,
      distanceMiles: 3.4,
    })

    expect(parsed.website).toBeNull()
  })

  it('rejects malformed alert payloads', () => {
    const result = alertListItemSchema.safeParse({
      id: '3f9e8070-9f83-463b-bd41-0ac2fa9c8cb4',
      regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
      disease: 'Influenza A',
      severity: 2,
      caseCount: 12,
      sourceAttribution: 'Austin Public Health',
      sourceDate: '2026-03-01T00:00:00Z',
      createdAt: '2026-03-02T00:00:00Z',
    })

    expect(result.success).toBe(false)
  })
})
