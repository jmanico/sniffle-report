import { Link, useParams } from 'react-router-dom'

import type { SnapshotTrendHighlight } from '../api/types'
import { FactCheckBadge } from '../components/news/FactCheckBadge'
import { SeverityBadge } from '../components/dashboard/SeverityBadge'
import { useStaticDashboard } from '../hooks/useStaticData'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

const severityRank: Record<string, number> = {
  Critical: 4,
  High: 3,
  Moderate: 2,
  Low: 1,
}

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

function buildRegionPath(regionId: string, segment?: string) {
  return segment ? `/region/${regionId}/${segment}` : `/region/${regionId}`
}

export function RegionalDashboardPage() {
  const { regionId } = useParams<{ regionId: string }>()
  const dashboardQuery = useStaticDashboard(regionId ?? '')
  const dashboard = dashboardQuery.data

  const regionLabel = dashboard
    ? dashboard.regionType === 'State'
      ? dashboard.regionName
      : `${dashboard.regionName}, ${dashboard.state}`
    : 'Loading...'

  const isLoading = dashboardQuery.isLoading
  const hasError = dashboardQuery.isError

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
  const resourceParts = resourceCounts
    ? [
        resourceCounts.clinic > 0 ? `${resourceCounts.clinic} clinics` : null,
        resourceCounts.pharmacy > 0 ? `${resourceCounts.pharmacy} pharmacies` : null,
        resourceCounts.hospital > 0 ? `${resourceCounts.hospital} hospitals` : null,
        resourceCounts.vaccinationSite > 0 ? `${resourceCounts.vaccinationSite} vaccination sites` : null,
      ].filter(Boolean)
    : []
  const resourceSummary = resourceParts.length > 0
    ? resourceParts.join(', ')
    : 'No local resources indexed'

  const dashboardLinks = [
    { label: 'All alerts', segment: 'alerts' },
    { label: 'Prevention guides', segment: 'prevention' },
    { label: 'Resources', segment: 'resources' },
    { label: 'Health news', segment: 'news' },
  ]

  return (
    <section className="page-frame">
      <div className="page-stack dashboard-stack">
        <nav className="breadcrumb">
          <Link to="/">Home</Link>
          {dashboard?.parentState ? (
            <>
              <span className="breadcrumb__sep">/</span>
              <Link to={validateAndSanitizeUrl(`/states/${dashboard.parentState}`)}>
                {dashboard.parentName}
              </Link>
            </>
          ) : null}
          <span className="breadcrumb__sep">/</span>
          <span>{dashboard?.regionName ?? 'Loading'}</span>
        </nav>

        <article className="page-hero">
          <span className="page-kicker">Regional dashboard</span>
          <h1>{regionLabel}</h1>
          <p>
            {dashboard?.regionType ?? 'Region'} overview. Health alerts, disease trends,
            local resources, and prevention guidance for this region.
          </p>
          <div className="page-badges">
            <span className="page-badge">{dashboard?.publishedAlertCount ?? 0} published alerts</span>
            <span className="page-badge">{resourceCounts?.total ?? 0} local resources</span>
          </div>
        </article>

        {hasError ? (
          <article className="page-panel dashboard-state-card">
            <span className="section-kicker">Data unavailable</span>
            <strong>Dashboard data could not be loaded.</strong>
            <p>This region may not have data yet. <Link to="/">Return home</Link></p>
          </article>
        ) : null}

        <div className="dashboard-grid">
          <section className="page-panel dashboard-card">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Active alerts</span>
                <strong>Top published alerts by severity</strong>
              </div>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : topAlerts.length ? (
              <div className="dashboard-alert-list">
                {topAlerts.map((alert) => (
                  <article className="dashboard-alert-card" key={alert.alertId}>
                    <div className="dashboard-alert-card__row">
                      <SeverityBadge severity={alert.severity as 'Low' | 'Moderate' | 'High' | 'Critical'} />
                      <span className="dashboard-alert-card__cases">{alert.caseCount} cases</span>
                    </div>
                    <strong>{alert.title}</strong>
                    <span className="dashboard-alert-card__meta">
                      {alert.disease} · {formatDate(alert.sourceDate)}
                    </span>
                  </article>
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
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : dashboard?.preventionHighlights.length ? (
              <div className="dashboard-prevention-list">
                {dashboard.preventionHighlights.map((guide) => (
                  <article className="dashboard-prevention-card" key={guide.guideId}>
                    <strong>{guide.title}</strong>
                    <span className="dashboard-prevention-card__meta">{guide.disease}</span>
                    <p>{guide.hasCostTiers ? 'Cost guidance available' : 'Cost details unavailable'}</p>
                  </article>
                ))}
              </div>
            ) : (
              <p className="dashboard-empty">No prevention highlights are available for this region.</p>
            )}
          </section>

          <section className="page-panel dashboard-card">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Health news</span>
                <strong>Recent alerts and recalls</strong>
              </div>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : dashboard?.newsHighlights.length ? (
              <div className="news-list">
                {dashboard.newsHighlights.map((item) => (
                  <article className="news-card news-card--compact" key={item.newsItemId}>
                    <div className="news-card__header">
                      <strong>{item.headline}</strong>
                      <FactCheckBadge status={item.factCheckStatus as 'Verified' | 'Pending' | 'Disputed' | 'Unverified' | null} />
                    </div>
                    <span className="news-card__meta">{formatDate(item.publishedAt)}</span>
                  </article>
                ))}
              </div>
            ) : (
              <p className="dashboard-empty">No health news for this region yet.</p>
            )}
          </section>

          <section className="page-panel dashboard-card">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Local access</span>
                <strong>Nearby healthcare resources</strong>
              </div>
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
                to={validateAndSanitizeUrl(buildRegionPath(regionId ?? '', item.segment))}
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
