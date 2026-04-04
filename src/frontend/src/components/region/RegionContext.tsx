import { type ReactNode } from 'react'
import { useParams } from 'react-router-dom'

import { useRegionById } from '../../hooks/useRegions'
import { RegionContext, type RegionContextValue } from './region-context'

export function RegionProvider({ children }: { children: ReactNode }) {
  const { regionId } = useParams()

  if (!regionId) {
    throw new Error('RegionProvider requires a regionId route param.')
  }

  const regionQuery = useRegionById(regionId)
  const regionLabel = regionQuery.data
    ? `${regionQuery.data.name}, ${regionQuery.data.state}`
    : regionId

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
