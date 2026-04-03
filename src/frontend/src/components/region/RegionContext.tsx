import { type ReactNode } from 'react'
import { useParams } from 'react-router-dom'

import { RegionContext, type RegionContextValue } from './region-context'

const KnownRegions: Record<string, string> = {
  'travis-county-tx': 'Travis County, TX',
  'cook-county-il': 'Cook County, IL',
  'king-county-wa': 'King County, WA',
}

export function RegionProvider({ children }: { children: ReactNode }) {
  const { regionId } = useParams()

  if (!regionId) {
    throw new Error('RegionProvider requires a regionId route param.')
  }

  const regionLabel = KnownRegions[regionId] ?? regionId.replaceAll('-', ' ')

  const value: RegionContextValue = {
    regionId,
    regionLabel,
    buildRegionPath: (segment = '') => {
      const normalizedSegment = segment.replace(/^\/+/, '')
      return normalizedSegment
        ? `/region/${regionId}/${normalizedSegment}`
        : `/region/${regionId}`
    },
  }

  return <RegionContext.Provider value={value}>{children}</RegionContext.Provider>
}
