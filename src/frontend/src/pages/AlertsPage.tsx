import { useState } from 'react'

import type { AlertSeverity } from '../api/types'
import { AlertCard } from '../components/alerts/AlertCard'
import { useAlerts } from '../hooks/useAlerts'
import { useRegion } from '../hooks/useRegion'

const pageSize = 6

export function AlertsPage() {
  const { regionId, regionLabel, buildRegionPath } = useRegion()
  const [disease, setDisease] = useState('')
  const [severity, setSeverity] = useState<AlertSeverity | ''>('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [page, setPage] = useState(1)

  const alertsQuery = useAlerts(regionId, {
    status: 'Published',
    disease: disease.trim() || undefined,
    severity: severity || undefined,
    dateFrom: dateFrom || undefined,
    dateTo: dateTo || undefined,
    page,
    pageSize,
    sortBy: 'sourceDate',
    sortDirection: 'desc',
  })

  const totalCount = alertsQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  function resetPagination() {
    setPage(1)
  }

  return (
    <section className="page-frame">
      <div className="page-stack">
        <article className="page-hero">
          <span className="page-kicker">Alerts</span>
          <h1>Health alerts for {regionLabel}</h1>
          <p>
            Browse published alerts for the selected region, narrow them by
            severity and disease, and drill into source-backed detail pages.
          </p>
        </article>

        <section className="page-panel alerts-filter-panel">
          <div className="alerts-filter-grid">
            <label className="alerts-filter-field">
              <span>Disease search</span>
              <input
                onChange={(event) => {
                  setDisease(event.target.value)
                  resetPagination()
                }}
                placeholder="Influenza, RSV, measles…"
                type="search"
                value={disease}
              />
            </label>

            <label className="alerts-filter-field">
              <span>Severity</span>
              <select
                onChange={(event) => {
                  setSeverity((event.target.value as AlertSeverity | '') || '')
                  resetPagination()
                }}
                value={severity}
              >
                <option value="">All severities</option>
                <option value="Critical">Critical</option>
                <option value="High">High</option>
                <option value="Moderate">Moderate</option>
                <option value="Low">Low</option>
              </select>
            </label>

            <label className="alerts-filter-field">
              <span>Date from</span>
              <input
                max={dateTo || undefined}
                onChange={(event) => {
                  setDateFrom(event.target.value)
                  resetPagination()
                }}
                type="date"
                value={dateFrom}
              />
            </label>

            <label className="alerts-filter-field">
              <span>Date to</span>
              <input
                min={dateFrom || undefined}
                onChange={(event) => {
                  setDateTo(event.target.value)
                  resetPagination()
                }}
                type="date"
                value={dateTo}
              />
            </label>
          </div>
        </section>

        {alertsQuery.isLoading ? (
          <section className="alerts-list" aria-label="Loading alerts">
            <div className="dashboard-skeleton dashboard-skeleton--card" />
            <div className="dashboard-skeleton dashboard-skeleton--card" />
            <div className="dashboard-skeleton dashboard-skeleton--card" />
          </section>
        ) : null}

        {alertsQuery.isError ? (
          <section className="page-panel">
            <span className="section-kicker">Alerts unavailable</span>
            <strong>Health alerts could not be loaded right now.</strong>
            <p>Refresh the page or adjust the filters and try again.</p>
          </section>
        ) : null}

        {!alertsQuery.isLoading && !alertsQuery.isError && alertsQuery.data?.items.length === 0 ? (
          <section className="page-panel">
            <span className="section-kicker">No active alerts</span>
            <strong>No active health alerts for this region.</strong>
            <p>Try broadening the disease or date filters.</p>
          </section>
        ) : null}

        {!alertsQuery.isLoading && !alertsQuery.isError && alertsQuery.data?.items.length ? (
          <>
            <section className="alerts-list" aria-label="Alert results">
              {alertsQuery.data.items.map((alert) => (
                <AlertCard
                  alert={alert}
                  href={buildRegionPath(`alerts/${alert.id}`)}
                  key={alert.id}
                />
              ))}
            </section>

            <section className="page-panel alerts-pagination">
              <span className="alerts-pagination__summary">
                Page {page} of {totalPages} · {totalCount} total alerts
              </span>
              <div className="alerts-pagination__controls">
                <button
                  className="dashboard-link alerts-pagination__button"
                  disabled={page === 1}
                  onClick={() => {
                    setPage((current) => Math.max(1, current - 1))
                  }}
                  type="button"
                >
                  Previous
                </button>
                <button
                  className="dashboard-link alerts-pagination__button"
                  disabled={page >= totalPages}
                  onClick={() => {
                    setPage((current) => Math.min(totalPages, current + 1))
                  }}
                  type="button"
                >
                  Next
                </button>
              </div>
            </section>
          </>
        ) : null}
      </div>
    </section>
  )
}
