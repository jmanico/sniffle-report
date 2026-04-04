import type { RegionListItem } from '../../api/types'

export function formatRegionHierarchy(region: RegionListItem, regionMap: Map<string, RegionListItem>) {
  if (region.type === 'State') {
    return region.name
  }

  const path = [region.state]

  if (region.parentId) {
    const parentRegion = regionMap.get(region.parentId)

    if (parentRegion) {
      path.push(parentRegion.name)
    }
  }

  path.push(region.name)

  return path.join(' > ')
}
