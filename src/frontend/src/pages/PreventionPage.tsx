import { useState } from 'react'
import { Link } from 'react-router-dom'

import { CostTierBadge } from '../components/prevention/CostTierBadge'
import { usePrevention } from '../hooks/usePrevention'
import { useRegion } from '../hooks/useRegion'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

function formatPrice(value: number) {
  if (value === 0) {
    return 'Free'
  }

  return `$${value.toFixed(2)}`
}

export function PreventionPage() {
  const { regionId, regionLabel, buildRegionPath } = useRegion()
  const [disease, setDisease] = useState('')

  const guidesQuery = usePrevention(regionId, {
    disease: disease.trim() || undefined,
    page: 1,
    pageSize: 24,
  })

  return (
    <section className="page-frame">
      <div className="page-stack">
        <article className="page-hero">
          <span className="page-kicker">Prevention</span>
          <h1>Guidance and cost tiers for {regionLabel}</h1>
          <p>
            Browse prevention guides for the selected region and compare the
            lowest-cost path before drilling into full pricing detail.
          </p>
        </article>

        <section className="page-panel alerts-filter-panel">
          <label className="alerts-filter-field">
            <span>Disease filter</span>
            <input
              onChange={(event) => {
                setDisease(event.target.value)
              }}
              placeholder="Influenza, RSV, measles…"
              type="search"
              value={disease}
            />
          </label>
        </section>

        {guidesQuery.isLoading ? (
          <section className="prevention-list" aria-label="Loading prevention guides">
            <div className="dashboard-skeleton dashboard-skeleton--card" />
            <div className="dashboard-skeleton dashboard-skeleton--card" />
          </section>
        ) : null}

        {guidesQuery.isError ? (
          <section className="page-panel">
            <span className="section-kicker">Guides unavailable</span>
            <strong>Prevention guides could not be loaded right now.</strong>
            <p>Refresh the page and try again.</p>
          </section>
        ) : null}

        {!guidesQuery.isLoading && !guidesQuery.isError && guidesQuery.data?.items.length === 0 ? (
          <section className="page-panel">
            <span className="section-kicker">No guides</span>
            <strong>No prevention guides available for this region.</strong>
            <p>Try broadening the disease filter.</p>
          </section>
        ) : null}

        {!guidesQuery.isLoading && !guidesQuery.isError && guidesQuery.data?.items.length ? (
          <section className="prevention-list" aria-label="Prevention guides">
            {guidesQuery.data.items.map((guide) => {
              const cheapestTier =
                [...guide.costTiers].sort((left, right) => Number(left.price) - Number(right.price))[0] ?? null

              return (
                <Link
                  className="prevention-card"
                  key={guide.id}
                  to={validateAndSanitizeUrl(buildRegionPath(`prevention/${guide.id}`))}
                >
                  <span className="section-kicker">{guide.disease}</span>
                  <strong>{guide.title}</strong>
                  {cheapestTier ? (
                    <div className="prevention-card__summary">
                      <CostTierBadge type={cheapestTier.type} />
                      <span>
                        Cheapest option: {formatPrice(Number(cheapestTier.price))} via {cheapestTier.provider}
                      </span>
                    </div>
                  ) : (
                    <p>Cost details unavailable.</p>
                  )}
                </Link>
              )
            })}
          </section>
        ) : null}
      </div>
    </section>
  )
}
