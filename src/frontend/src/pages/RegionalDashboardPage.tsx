import { Link, useParams } from 'react-router-dom'

import type { RegionDashboard, SnapshotTrendHighlight } from '../api/types'
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

function formatNumber(value: number) {
  return new Intl.NumberFormat('en-US').format(value)
}

function formatDateRange(startDate: string, endDate: string) {
  const start = new Date(startDate)
  const end = new Date(endDate)
  const sameYear = start.getUTCFullYear() === end.getUTCFullYear()
  const sameMonth = sameYear && start.getUTCMonth() === end.getUTCMonth()

  if (sameMonth) {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
    }).format(start) + `-${end.getUTCDate()}, ${end.getUTCFullYear()}`
  }

  if (sameYear) {
    return `${new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
    }).format(start)}-${new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
    }).format(end)}, ${end.getUTCFullYear()}`
  }

  return `${formatDate(startDate)}-${formatDate(endDate)}`
}

function formatWowChange(highlight: SnapshotTrendHighlight) {
  const sign = highlight.wowChangePercent >= 0 ? '+' : ''
  return `${sign}${highlight.wowChangePercent.toFixed(1)}%`
}

function formatAlertWowChange(wowChangePercent: number) {
  const sign = wowChangePercent >= 0 ? '+' : ''
  return `${sign}${wowChangePercent.toFixed(1)}%`
}

function buildAlertSummary(alert: {
  summary: string
  caseCount: number
  disease: string
  severity: string
}) {
  if (alert.summary.trim()) {
    return alert.summary
  }

  return `${alert.caseCount} reported case${alert.caseCount === 1 ? '' : 's'} for ${alert.disease}. Severity is ${alert.severity.toLowerCase()}.`
}

function getSafeTelLink(phone: string | null) {
  if (!phone) {
    return null
  }

  const sanitized = phone.replace(/[^\d+]/g, '')
  return sanitized ? validateAndSanitizeUrl(`tel:${sanitized}`) : null
}

type DashboardAlert = RegionDashboard['topAlerts'][number]

type DisplayAlert = DashboardAlert & {
  latestSourceDate: string
  earliestSourceDate: string
  occurrenceCount: number
}

function isCommunityHealthAlert(alert: { disease: string }) {
  return alert.disease.startsWith('[Community Health]')
}

function getDisplayDiseaseName(disease: string) {
  return disease.replace(/^\[Community Health\]\s*/, '').trim()
}

function buildAlertMetricLabel(alert: { caseCount: number; disease: string }) {
  if (isCommunityHealthAlert(alert)) {
    return `${alert.caseCount}% indicator`
  }

  return `${alert.caseCount} cases`
}

function createAlertGroupKey(alert: DashboardAlert) {
  return [
    alert.disease.trim().toLowerCase(),
    alert.title.trim().toLowerCase(),
    alert.summary.trim().toLowerCase(),
    alert.severity.trim().toLowerCase(),
    alert.caseCount,
    alert.sourceAttribution.trim().toLowerCase(),
  ].join('::')
}

function dedupeAlerts(alerts: DashboardAlert[]) {
  const groupedAlerts = new Map<string, DisplayAlert>()

  alerts.forEach((alert) => {
    const key = createAlertGroupKey(alert)
    const existing = groupedAlerts.get(key)

    if (!existing) {
      groupedAlerts.set(key, {
        ...alert,
        latestSourceDate: alert.sourceDate,
        earliestSourceDate: alert.sourceDate,
        occurrenceCount: 1,
      })
      return
    }

    const sourceTime = new Date(alert.sourceDate).getTime()
    const latestTime = new Date(existing.latestSourceDate).getTime()
    const earliestTime = new Date(existing.earliestSourceDate).getTime()

    existing.occurrenceCount += 1

    if (sourceTime > latestTime) {
      existing.latestSourceDate = alert.sourceDate
      existing.sourceDate = alert.sourceDate
      existing.alertId = alert.alertId
      existing.previousCaseCount = alert.previousCaseCount
      existing.wowChangePercent = alert.wowChangePercent
      existing.previousSourceDate = alert.previousSourceDate
    }

    if (sourceTime < earliestTime) {
      existing.earliestSourceDate = alert.sourceDate
    }
  })

  return [...groupedAlerts.values()]
}

function buildAlertDateLabel(alert: DisplayAlert) {
  if (alert.occurrenceCount <= 1) {
    return formatDate(alert.latestSourceDate)
  }

  return formatDateRange(alert.earliestSourceDate, alert.latestSourceDate)
}

function buildAlertOccurrencesLabel(alert: DisplayAlert) {
  if (alert.occurrenceCount <= 1) {
    return null
  }

  return `${alert.occurrenceCount} updates`
}

function buildDisplayAlertMeta(alert: DisplayAlert) {
  const dateLabel = buildAlertDateLabel(alert)
  const sourceLabel = alert.sourceAttribution.trim() ? alert.sourceAttribution : 'Snapshot date'

  return `${sourceLabel} · ${dateLabel}`
}

function buildAlertHeading(alert: DisplayAlert) {
  if (!isCommunityHealthAlert(alert)) {
    return alert.title
  }

  return getDisplayDiseaseName(alert.disease)
}

function buildAlertTypeLabel(alert: DisplayAlert) {
  if (!isCommunityHealthAlert(alert)) {
    return null
  }

  return 'Community health indicator'
}

function buildCommunityHealthSummary(alert: DisplayAlert) {
  return alert.summary
}

function buildCommunityHealthMetricLabel(alert: DisplayAlert) {
  const percentageMatch = alert.title.match(/:\s*([0-9.]+)%/)
  const displayValue = percentageMatch ? `${percentageMatch[1]}%` : `${alert.caseCount}%`

  if (/prevalence/i.test(alert.summary)) {
    return `${displayValue} prevalence`
  }

  return `${displayValue} rate`
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

  const topAlerts = dedupeAlerts([...(dashboard?.topAlerts ?? [])])
    .sort((left, right) => {
      const severityDiff = (severityRank[right.severity] ?? 0) - (severityRank[left.severity] ?? 0)
      if (severityDiff !== 0) return severityDiff
      const occurrenceDiff = right.occurrenceCount - left.occurrenceCount
      if (occurrenceDiff !== 0) return occurrenceDiff
      return right.caseCount - left.caseCount
    })
    .slice(0, 5)

  const trendHighlights = [...(dashboard?.trendHighlights ?? [])]
    .sort((left, right) => Math.abs(right.wowChangePercent) - Math.abs(left.wowChangePercent))
    .slice(0, 3)

  const resourceCounts = dashboard?.resourceCounts
  const nearbyResources = dashboard?.nearbyResources ?? []
  const accessSignals = dashboard?.accessSignals ?? []
  const environmentalSignals = dashboard?.environmentalSignals ?? []
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
                      <span className="dashboard-alert-card__cases">
                        {isCommunityHealthAlert(alert) ? buildCommunityHealthMetricLabel(alert) : buildAlertMetricLabel(alert)}
                      </span>
                      {buildAlertOccurrencesLabel(alert) ? (
                        <span className="dashboard-alert-card__updates">{buildAlertOccurrencesLabel(alert)}</span>
                      ) : null}
                    </div>
                    <strong>{buildAlertHeading(alert)}</strong>
                    {buildAlertTypeLabel(alert) ? (
                      <span className="dashboard-alert-card__indicator">{buildAlertTypeLabel(alert)}</span>
                    ) : null}
                    <p className="dashboard-alert-card__summary">
                      {isCommunityHealthAlert(alert) ? buildCommunityHealthSummary(alert) : buildAlertSummary(alert)}
                    </p>
                    {alert.previousCaseCount != null && alert.wowChangePercent != null ? (
                      <span className="dashboard-alert-card__trend">
                        {alert.previousCaseCount} previous cases · {formatAlertWowChange(alert.wowChangePercent)} WoW
                      </span>
                    ) : null}
                    <span className="dashboard-alert-card__meta">
                      {buildDisplayAlertMeta(alert)}
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
                <span className="section-kicker">Access & safety</span>
                <strong>Provider shortages and drinking water issues</strong>
              </div>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : accessSignals.length || environmentalSignals.length ? (
              <div className="dashboard-signal-list">
                {accessSignals.map((signal) => (
                  <article className="dashboard-signal-card" key={signal.designationId}>
                    <div className="dashboard-alert-card__row">
                      <span className="page-badge">{signal.discipline}</span>
                      {signal.hpsaScore != null ? (
                        <span className="dashboard-alert-card__cases">HPSA score {signal.hpsaScore}</span>
                      ) : null}
                    </div>
                    <strong>{signal.areaName}</strong>
                    <span className="dashboard-alert-card__indicator">{signal.designationType}</span>
                    <p className="dashboard-alert-card__summary">
                      {signal.populationToProviderRatio != null
                        ? `${signal.status}. Estimated population-to-provider ratio: ${signal.populationToProviderRatio.toLocaleString('en-US', { maximumFractionDigits: 1 })}.`
                        : `${signal.status}. HRSA shortage-area designation for this region.`}
                    </p>
                    <span className="dashboard-alert-card__meta">
                      HRSA HPSA{signal.sourceUpdatedAt ? ` · ${formatDate(signal.sourceUpdatedAt)}` : ''}
                    </span>
                  </article>
                ))}
                {environmentalSignals.map((signal) => (
                  <article className="dashboard-signal-card" key={signal.violationId}>
                    <div className="dashboard-alert-card__row">
                      <span className="page-badge">Water safety</span>
                      {signal.populationServed != null ? (
                        <span className="dashboard-alert-card__cases">{formatNumber(signal.populationServed)} served</span>
                      ) : null}
                    </div>
                    <strong>{signal.waterSystemName}</strong>
                    <span className="dashboard-alert-card__indicator">{signal.violationCategory}</span>
                    <p className="dashboard-alert-card__summary">{signal.summary}</p>
                    <span className="dashboard-alert-card__meta">
                      {signal.ruleName}
                      {signal.identifiedAt ? ` · Open since ${formatDate(signal.identifiedAt)}` : ''}
                    </span>
                  </article>
                ))}
              </div>
            ) : (
              <p className="dashboard-empty">No provider-shortage or drinking-water safety signals for this region yet.</p>
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
                <strong>Indexed healthcare resources</strong>
              </div>
            </div>

            {isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : nearbyResources.length ? (
              <div className="dashboard-resource-list">
                {nearbyResources.map((resource) => {
                  const websiteHref = resource.website
                    ? validateAndSanitizeUrl(resource.website)
                    : null
                  const phoneHref = getSafeTelLink(resource.phone)

                  return (
                    <article className="dashboard-resource-item" key={resource.id}>
                      <div className="dashboard-resource-item__header">
                        <strong>{resource.name}</strong>
                        <span className="page-badge">{resource.type}</span>
                      </div>
                      <p>{resource.address}</p>
                      <div className="dashboard-resource-item__links">
                        {websiteHref && websiteHref !== '/' ? (
                          <a href={websiteHref} rel="noreferrer" target="_blank">
                            Website
                          </a>
                        ) : null}
                        {phoneHref && phoneHref !== '/' ? <a href={phoneHref}>{resource.phone}</a> : null}
                      </div>
                    </article>
                  )
                })}
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
          <span className="section-kicker">Static snapshot</span>
          <strong>This page is the complete published view for this region.</strong>
          <p className="dashboard-empty">
            Detailed subpages for alerts, prevention, resources, and news were part of the old
            dynamic UI and are not included in the static site.
          </p>
          <div className="page-badges">
            <span className="page-badge">
              Exported {dashboard?.computedAt ? formatDate(dashboard.computedAt) : 'with the latest snapshot'}
            </span>
            {dashboard?.parentState ? (
              <Link
                className="page-badge page-badge--link"
                to={validateAndSanitizeUrl(`/states/${dashboard.parentState}`)}
              >
                Back to {dashboard.parentName}
              </Link>
            ) : (
              <Link className="page-badge page-badge--link" to={validateAndSanitizeUrl('/')}>
                Back to home
              </Link>
            )}
          </div>
        </section>
      </div>
    </section>
  )
}
