import { useContext } from 'react'

import { RegionContext } from '../components/region/region-context'

export function useRegion() {
  const context = useContext(RegionContext)

  if (!context) {
    throw new Error('useRegion must be used within RegionProvider.')
  }

  return context
}
