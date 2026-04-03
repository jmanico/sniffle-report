import { Link } from 'react-router-dom'

import { validateAndSanitizeUrl } from '../../utils/validateAndSanitizeUrl'
import { useRegion } from '../../hooks/useRegion'

const regionOptions = [
  { id: 'travis-county-tx', label: 'Travis County, TX' },
  { id: 'cook-county-il', label: 'Cook County, IL' },
  { id: 'king-county-wa', label: 'King County, WA' },
]

export function RegionSelector() {
  const { regionId, regionLabel } = useRegion()

  return (
    <details className="region-picker">
      <summary className="region-picker__summary">Region</summary>
      <div className="region-picker__panel">
        <div className="region-picker__title">Current region</div>
        <strong>{regionLabel}</strong>
        <span className="region-picker__value">URL key: {regionId}</span>
        <div className="region-picker__list" role="list">
          {regionOptions.map((region) => (
            <Link
              className="region-picker__link"
              key={region.id}
              to={validateAndSanitizeUrl(`/region/${region.id}`)}
            >
              {region.label}
            </Link>
          ))}
        </div>
      </div>
    </details>
  )
}
