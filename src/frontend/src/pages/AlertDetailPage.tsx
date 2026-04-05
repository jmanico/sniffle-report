import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'

import { CaseCountTable } from '../components/trends/CaseCountTable'
import { SeverityBadge } from '../components/dashboard/SeverityBadge'
import { TrendChart } from '../components/trends/TrendChart'
import { useAlertById } from '../hooks/useAlerts'
import { useAlertTrends } from '../hooks/useTrends'
import { useRegion } from '../hooks/useRegion'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

function formatDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
}

const rangeOptions = [
  { label: '30 days', days: 30 },
  { label: '90 days', days: 90 },
  { label: '6 months', days: 180 },
  { label: '1 year', days: 365 },
] as const

function toQueryDate(date: Date) {
  return date.toISOString()
}

export function AlertDetailPage() {
  const { alertId } = useParams()
  const { regionId, regionLabel, buildRegionPath } = useRegion()
  const resolvedAlertId = alertId ?? ''
  const alertQuery = useAlertById(regionId, resolvedAlertId)
  const [selectedRangeDays, setSelectedRangeDays] = useState<(typeof rangeOptions)[number]['days']>(90)

  const anchorDate = alertQuery.data?.trends[0]?.date
    ? new Date(alertQuery.data.trends[0].date)
    : new Date()
  const rangeStart = new Date(anchorDate)
  rangeStart.setUTCDate(rangeStart.getUTCDate() - selectedRangeDays)

  const alertTrendsQuery = useAlertTrends(regionId, resolvedAlertId, {
    dateFrom: toQueryDate(rangeStart),
    dateTo: toQueryDate(anchorDate),
    page: 1,
    pageSize: 100,
  })

  if (!alertId) {
    return null
  }

  return (
    <section className="page-frame">
      {alertQuery.isLoading ? (
        <div className="page-stack">
          <div className="dashboard-skeleton dashboard-skeleton--card" />
          <div className="dashboard-skeleton dashboard-skeleton--card" />
        </div>
      ) : null}

      {alertQuery.isError ? (
        <article className="page-panel">
          <span className="section-kicker">Alert unavailable</span>
          <strong>This alert could not be loaded.</strong>
          <p>Return to the alert list and choose another item.</p>
        </article>
      ) : null}

      {!alertQuery.isLoading && !alertQuery.isError && alertQuery.data ? (
        <div className="page-stack">
          <article className="page-hero alert-detail-hero">
            <span className="page-kicker">Alert detail</span>
            <h1>{alertQuery.data.title}</h1>
            <p>{alertQuery.data.summary}</p>
            <div className="page-badges">
              <SeverityBadge severity={alertQuery.data.severity} />
              <span className="page-badge">{alertQuery.data.caseCount} reported cases</span>
              <span className="page-badge">Status: {alertQuery.data.status}</span>
            </div>
          </article>

          <div className="alert-detail-grid">
            <section className="page-panel">
              <span className="section-kicker">Source attribution</span>
              <strong>{alertQuery.data.sourceAttribution}</strong>
              <p>Data collected on {formatDate(alertQuery.data.sourceDate)} for {regionLabel}.</p>
              <div className="alert-detail-facts">
                <span>Disease: {alertQuery.data.disease}</span>
                <span>Published: {formatDate(alertQuery.data.createdAt)}</span>
              </div>
            </section>

            <section className="page-panel">
              <span className="section-kicker">Trend chart</span>
              <strong>Interactive case-count trend for this alert</strong>
              <p>
                Switch between bounded time ranges up to one year. The data table
                below remains available as an accessible fallback.
              </p>
              <Link className="dashboard-link" to={validateAndSanitizeUrl(`${buildRegionPath(`alerts/${alertId}`)}#trend-data`)}>
                Jump to trend details
              </Link>
            </section>
          </div>

          <section className="page-panel" id="trend-data">
            <span className="section-kicker">Trend chart</span>
            <strong>Case counts over time</strong>

            <div className="trend-range-picker" role="tablist" aria-label="Trend date range selector">
              {rangeOptions.map((option) => (
                <button
                  aria-selected={selectedRangeDays === option.days}
                  className={
                    selectedRangeDays === option.days
                      ? 'trend-range-button trend-range-button--active'
                      : 'trend-range-button'
                  }
                  key={option.days}
                  onClick={() => {
                    setSelectedRangeDays(option.days)
                  }}
                  role="tab"
                  type="button"
                >
                  {option.label}
                </button>
              ))}
            </div>

            {alertTrendsQuery.isLoading ? (
              <div className="dashboard-skeleton-group" aria-hidden="true">
                <div className="dashboard-skeleton dashboard-skeleton--line" />
                <div className="dashboard-skeleton dashboard-skeleton--card" />
              </div>
            ) : null}

            {alertTrendsQuery.isError ? (
              <p>Trend data could not be loaded for the selected date range.</p>
            ) : null}

            {!alertTrendsQuery.isLoading && !alertTrendsQuery.isError && alertTrendsQuery.data?.dataPoints.length ? (
              <>
                <TrendChart dataPoints={alertTrendsQuery.data.dataPoints} />
                <p className="trend-chart__source">
                  Data from {alertTrendsQuery.data.sourceAttribution}, last updated{' '}
                  {formatDate(alertTrendsQuery.data.dataPoints[alertTrendsQuery.data.dataPoints.length - 1].sourceDate)}.
                </p>
                <CaseCountTable dataPoints={alertTrendsQuery.data.dataPoints} />
              </>
            ) : (
              !alertTrendsQuery.isLoading &&
              !alertTrendsQuery.isError && <p>No trend records are attached to this alert yet.</p>
            )}
          </section>
        </div>
      ) : null}
    </section>
  )
}
