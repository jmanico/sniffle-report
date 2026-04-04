import { Link, useParams } from 'react-router-dom'

import { CostTierBadge } from '../components/prevention/CostTierBadge'
import { usePreventionGuide } from '../hooks/usePrevention'
import { useRegion } from '../hooks/useRegion'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

function formatPrice(value: number) {
  if (value === 0) {
    return 'Free'
  }

  return `$${value.toFixed(2)}`
}

export function PreventionDetailPage() {
  const { guideId } = useParams()
  const { regionId, regionLabel, buildRegionPath } = useRegion()
  const guideQuery = usePreventionGuide(regionId, guideId ?? '')

  if (!guideId) {
    return null
  }

  return (
    <section className="page-frame">
      {guideQuery.isLoading ? (
        <div className="page-stack">
          <div className="dashboard-skeleton dashboard-skeleton--card" />
          <div className="dashboard-skeleton dashboard-skeleton--card" />
        </div>
      ) : null}

      {guideQuery.isError ? (
        <article className="page-panel">
          <span className="section-kicker">Guide unavailable</span>
          <strong>This prevention guide could not be loaded.</strong>
          <p>Return to the prevention list and choose another guide.</p>
        </article>
      ) : null}

      {!guideQuery.isLoading && !guideQuery.isError && guideQuery.data ? (
        <div className="page-stack">
          <article className="page-hero">
            <span className="page-kicker">Prevention guide</span>
            <h1>{guideQuery.data.title}</h1>
            <p>{guideQuery.data.content}</p>
            <div className="page-badges">
              <span className="page-badge">{guideQuery.data.disease}</span>
              <span className="page-badge">{regionLabel}</span>
            </div>
          </article>

          <section className="page-panel prevention-detail-panel">
            <div className="prevention-detail-panel__header">
              <div>
                <span className="section-kicker">Cost tiers</span>
                <strong>Compare free, insured, and out-of-pocket options</strong>
              </div>
              <Link className="dashboard-link" to={validateAndSanitizeUrl(buildRegionPath('prevention'))}>
                Back to guides
              </Link>
            </div>

            <div className="prevention-cost-table" role="table" aria-label="Prevention cost tiers">
              <div className="prevention-cost-table__head" role="row">
                <span role="columnheader">Type</span>
                <span role="columnheader">Price</span>
                <span role="columnheader">Provider</span>
                <span role="columnheader">Notes</span>
              </div>
              {guideQuery.data.costTiers.map((tier) => (
                <div className="prevention-cost-table__row" key={`${tier.type}-${tier.provider}-${tier.price}`} role="row">
                  <span role="cell">
                    <CostTierBadge type={tier.type} />
                  </span>
                  <span role="cell">{formatPrice(Number(tier.price))}</span>
                  <span role="cell">{tier.provider}</span>
                  <span role="cell">{tier.notes ?? 'No additional notes'}</span>
                </div>
              ))}
            </div>
          </section>
        </div>
      ) : null}
    </section>
  )
}
