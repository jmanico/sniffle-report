import { createContext } from 'react'

export type RegionContextValue = {
  regionId: string
  regionLabel: string
  buildRegionPath: (segment?: string) => string
}

export const RegionContext = createContext<RegionContextValue | null>(null)
