import { useRegion } from '../../hooks/useRegion'
import { RegionSearchPanel } from './RegionSearchPanel'

export function RegionSelector() {
  const { regionId, regionLabel } = useRegion()

  return (
    <details className="region-picker">
      <summary className="region-picker__summary">Region</summary>
      <div className="region-picker__panel">
        <RegionSearchPanel
          className="region-search region-search--header"
          currentRegionId={regionId}
          currentRegionLabel={regionLabel}
          description="Switch to another region from anywhere in the app."
          heading="Current region"
        />
      </div>
    </details>
  )
}
