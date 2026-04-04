import { Link } from 'react-router-dom'

import type { AlertListItem, CostTier, PreventionListItem, TrendSeries } from '../api/types'
import { SeverityBadge } from '../components/dashboard/SeverityBadge'
import { useRegion } from '../hooks/useRegion'
import { useAlerts } from '../hooks/useAlerts'
import { usePrevention } from '../hooks/usePrevention'
import { useResources } from '../hooks/useResources'
import { useRegionById } from '../hooks/useRegions'
import { useTrends } from '../hooks/useTrends'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

const severityRank: Record<AlertListItem['severity'], number> = {
  Critical: 4,
  High: 3,
  Moderate: 2,
  Low: 1,
}

const dashboardLinks = [
  { label: 'All alerts', segment: 'alerts' },
  { label: 'Prevention guides', segment: 'prevention' },
  { label: 'Resources', segment: 'resources' },
  { label: 'Health news', segment: 'news' },
]

function formatDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
}

function getCheapestCostTier(costTiers: CostTier[]) {
  return [...costTiers].sort((left, right) => Number(left.price) - Number(right.price))[0] ?? null
}

function formatCostSummary(guide: PreventionListItem) {
  const cheapestTier = getCheapestCostTier(guide.costTiers)

  if (!cheapestTier) {
    return 'Cost details unavailable'
  }

  if (cheapestTier.type === 'Free') {
    return `Free through ${cheapestTier.provider}`
  }

  return `From $${Number(cheapestTier.price).toFixed(2)} via ${cheapestTier.provider}`
}

function getLatestTrendCount(series: TrendSeries) {
  const latestPoint = [...series.dataPoints].sort((left, right) => {
    return new Date(right.date).getTime() - new Date(left.date).getTime()
  })[0]

  return latestPoint ?? null
}

export function RegionalDashboardPage() {
  const { regionId, regionLabel, buildRegionPath } = useRegion()
  const regionQuery = useRegionById(regionId)
  const alertsQuery = useAlerts(regionId, { status: 'Published', page: 1, pageSize: 25 })
  const trendsQuery = useTrends(regionId, { page: 1, pageSize: 6 })
  const preventionQuery = usePrevention(regionId, { page: 1, pageSize: 3 })
  const resourcesQuery = useResources(regionId, { page: 1, pageSize: 1 })
  const clinicsQuery = useResources(regionId, { type: 'Clinic', page: 1, pageSize: 1 })
  const pharmaciesQuery = useResources(regionId, { type: 'Pharmacy', page: 1, pageSize: 1 })

  const topAlerts = [...(alertsQuery.data?.items ?? [])]
    .sort((left, right) => {
      const severityDifference = severityRank[right.severity] - severityRank[left.severity]

      if (severityDifference !== 0) {
        return severityDifference
      }

      return right.caseCount - left.caseCount
    })
    .slice(0, 5)

  const trendHighlights = [...(trendsQuery.data?.items ?? [])]
    .map((series) => ({
      series,
      latestPoint: getLatestTrendCount(series),
    }))
    .filter((item) => item.latestPoint !== null)
    .sort((left, right) => right.latestPoint.caseCount - left.latestPoint.caseCount)
    .slice(0, 3)

  const regionTypeLabel = regionQuery.data?.type ?? 'Region'
  const resourceSummary = `${clinicsQuery.data?.totalCount ?? 0} clinics, ${pharmaciesQuery.data?.totalCount ?? 0} pharmacies near you`
  const isLoading =
    regionQuery.isLoading ||
    alertsQuery.isLoading ||
    trendsQuery.isLoading ||
    preventionQuery.isLoading ||
    resourcesQuery.isLoading ||
    clinicsQuery.isLoading ||
    pharmaciesQuery.isLoading
  const hasError =
    regionQuery.isError ||
    alertsQuery.isError ||
    trendsQuery.isError ||
    preventionQuery.isError ||
    resourcesQuery.isError ||
    clinicsQuery.isError ||
    pharmaciesQuery.isError

  return (
    <section className="page-frame">
      <div className="page-stack dashboard-stack">
        <article className="page-hero">
          <span className="page-kicker">Regional dashboard</span>
          <h1>{regionLabel}</h1>
          <p>
            {regionTypeLabel} overview for the selected region. This dashboard
            surfaces the highest-signal alert, trend, prevention, and local
            care access summaries without forcing users through every sub-page first.
          </p>
          <div className="page-badges">
            <span className="page-badge">{alertsQuery.data?.totalCount ?? 0} published alerts</span>
            <span className="page-badge">{resourcesQuery.data?.totalCount ?? 0} local resources</span>
            <span className="page-badge">{preventionQuery.data?.totalCount ?? 0} prevention guides</span>
          </div>
        </article>

        {hasError ? (
          <article className="page-panel dashboard-state-card">
            <span className="section-kicker">Data unavailable</span>
            <strong>Some dashboard sections could not be loaded.</strong>
            <p>Try refreshing the page or switching to another region.</p>
          </article>
        ) : null}

        <div className="dashboard-grid">
          <section className="page-panel dashboard-card">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Active alerts</span>
                <strong>Top published alerts by severity</strong>
              </div>
              <Link className="dashboard-link" to={validateAndSanitizeUrl(buildRegionPath('alerts'))}>
                View all alerts
              </Link>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : topAlerts.length ? (
              <div className="dashboard-alert-list">
                {topAlerts.map((alert) => (
                  <Link
                    className="dashboard-alert-card"
                    key={alert.id}
                    to={validateAndSanitizeUrl(buildRegionPath(`alerts/${alert.id}`))}
                  >
                    <div className="dashboard-alert-card__row">
                      <SeverityBadge severity={alert.severity} />
                      <span className="dashboard-alert-card__cases">{alert.caseCount} cases</span>
                    </div>
                    <strong>{alert.title}</strong>
                    <p>{alert.summary}</p>
                    <span className="dashboard-alert-card__meta">
                      {alert.disease} · {formatDate(alert.sourceDate)}
                    </span>
                  </Link>
                ))}
              </div>
            ) : (
              <p className="dashboard-empty">No active health alerts for this region.</p>
            )}
          </section>

          <section className="page-panel dashboard-card">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Trend summary</span>
                <strong>Latest case counts for leading diseases</strong>
              </div>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : trendHighlights.length ? (
              <div className="dashboard-trend-list">
                {trendHighlights.map(({ series, latestPoint }) => (
                  <article className="dashboard-trend-card" key={series.alertId}>
                    <strong>{series.disease}</strong>
                    <span className="dashboard-trend-card__count">{latestPoint.caseCount} latest reported cases</span>
                    <span className="dashboard-trend-card__meta">
                      {formatDate(latestPoint.date)} · {series.sourceAttribution}
                    </span>
                  </article>
                ))}
              </div>
            ) : (
              <p className="dashboard-empty">Trend data is not available for this region yet.</p>
            )}
          </section>

          <section className="page-panel dashboard-card">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Prevention highlights</span>
                <strong>Quick access to lower-cost guidance</strong>
              </div>
              <Link className="dashboard-link" to={validateAndSanitizeUrl(buildRegionPath('prevention'))}>
                Browse prevention
              </Link>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : preventionQuery.data?.items.length ? (
              <div className="dashboard-prevention-list">
                {preventionQuery.data.items.map((guide) => (
                  <Link
                    className="dashboard-prevention-card"
                    key={guide.id}
                    to={validateAndSanitizeUrl(buildRegionPath(`prevention/${guide.id}`))}
                  >
                    <strong>{guide.title}</strong>
                    <span className="dashboard-prevention-card__meta">{guide.disease}</span>
                    <p>{formatCostSummary(guide)}</p>
                  </Link>
                ))}
              </div>
            ) : (
              <p className="dashboard-empty">No prevention highlights are available for this region.</p>
            )}
          </section>

          <section className="page-panel dashboard-card">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Local access</span>
                <strong>Nearby clinics and pharmacies</strong>
              </div>
              <Link className="dashboard-link" to={validateAndSanitizeUrl(buildRegionPath('resources'))}>
                Explore resources
              </Link>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : (
              <div className="dashboard-resource-summary">
                <strong>{resourceSummary}</strong>
                <p>
                  {resourcesQuery.data?.totalCount ?? 0} clinics, pharmacies, hospitals, and vaccination sites are indexed for this region.
                </p>
              </div>
            )}
          </section>
        </div>

        <section className="page-panel dashboard-nav-panel">
          <span className="section-kicker">Explore this region</span>
          <strong>Jump deeper into the region-scoped views.</strong>
          <div className="dashboard-nav-links">
            {dashboardLinks.map((item) => (
              <Link
                className="dashboard-nav-link"
                key={item.segment}
                to={validateAndSanitizeUrl(buildRegionPath(item.segment))}
              >
                {item.label}
              </Link>
            ))}
          </div>
        </section>
      </div>
    </section>
  )
}
