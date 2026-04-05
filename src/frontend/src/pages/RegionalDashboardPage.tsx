import { Link } from 'react-router-dom'

import type { SnapshotTrendHighlight } from '../api/types'
import { SeverityBadge } from '../components/dashboard/SeverityBadge'
import { useRegion } from '../hooks/useRegion'
import { useDashboard } from '../hooks/useDashboard'
import { useRegionById } from '../hooks/useRegions'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

const severityRank: Record<string, number> = {
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

function formatWowChange(highlight: SnapshotTrendHighlight) {
  const sign = highlight.wowChangePercent >= 0 ? '+' : ''
  return `${sign}${highlight.wowChangePercent.toFixed(1)}%`
}

export function RegionalDashboardPage() {
  const { regionId, regionLabel, buildRegionPath } = useRegion()
  const regionQuery = useRegionById(regionId)
  const dashboardQuery = useDashboard(regionId)

  const dashboard = dashboardQuery.data
  const regionTypeLabel = regionQuery.data?.type ?? 'Region'
  const isLoading = regionQuery.isLoading || dashboardQuery.isLoading
  const hasError = regionQuery.isError || dashboardQuery.isError

  const topAlerts = [...(dashboard?.topAlerts ?? [])]
    .sort((left, right) => {
      const severityDiff = (severityRank[right.severity] ?? 0) - (severityRank[left.severity] ?? 0)
      if (severityDiff !== 0) return severityDiff
      return right.caseCount - left.caseCount
    })
    .slice(0, 5)

  const trendHighlights = [...(dashboard?.trendHighlights ?? [])]
    .sort((left, right) => Math.abs(right.wowChangePercent) - Math.abs(left.wowChangePercent))
    .slice(0, 3)

  const resourceCounts = dashboard?.resourceCounts
  const resourceSummary = resourceCounts
    ? `${resourceCounts.clinic} clinics, ${resourceCounts.pharmacy} pharmacies near you`
    : '0 clinics, 0 pharmacies near you'

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
            <span className="page-badge">{dashboard?.publishedAlertCount ?? 0} published alerts</span>
            <span className="page-badge">{resourceCounts?.total ?? 0} local resources</span>
            <span className="page-badge">{dashboard?.preventionHighlights.length ?? 0} prevention guides</span>
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
                    key={alert.alertId}
                    to={validateAndSanitizeUrl(buildRegionPath(`alerts/${alert.alertId}`))}
                  >
                    <div className="dashboard-alert-card__row">
                      <SeverityBadge severity={alert.severity as 'Low' | 'Moderate' | 'High' | 'Critical'} />
                      <span className="dashboard-alert-card__cases">{alert.caseCount} cases</span>
                    </div>
                    <strong>{alert.title}</strong>
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
                <strong>Week-over-week changes for leading diseases</strong>
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
                {trendHighlights.map((highlight) => (
                  <article className="dashboard-trend-card" key={highlight.alertId}>
                    <strong>{highlight.disease}</strong>
                    <span className="dashboard-trend-card__count">
                      {highlight.latestCaseCount} cases ({formatWowChange(highlight)} WoW)
                    </span>
                    <span className="dashboard-trend-card__meta">
                      {formatDate(highlight.latestDate)} · Previous: {highlight.previousCaseCount} cases
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
                <strong>Quick access to guidance</strong>
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
            ) : dashboard?.preventionHighlights.length ? (
              <div className="dashboard-prevention-list">
                {dashboard.preventionHighlights.map((guide) => (
                  <Link
                    className="dashboard-prevention-card"
                    key={guide.guideId}
                    to={validateAndSanitizeUrl(buildRegionPath(`prevention/${guide.guideId}`))}
                  >
                    <strong>{guide.title}</strong>
                    <span className="dashboard-prevention-card__meta">{guide.disease}</span>
                    <p>{guide.hasCostTiers ? 'Cost guidance available' : 'Cost details unavailable'}</p>
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
                  {resourceCounts?.total ?? 0} clinics, pharmacies, hospitals, and vaccination sites are indexed for this region.
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
