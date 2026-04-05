import { Link } from 'react-router-dom'

import { useSiteStatus, useStates } from '../hooks/useStaticData'

function formatRelative(date: string | null) {
  if (!date) return 'never'
  const diff = Date.now() - new Date(date).getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return 'just now'
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  return `${Math.floor(hours / 24)}d ago`
}

function formatDateTime(date: string | null) {
  if (!date) return 'Never'
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))
}

function syncStatusClass(status: string | null): string {
  switch (status) {
    case 'Success': return 'status-sync--success'
    case 'PartialSuccess': return 'status-sync--partial'
    case 'Failed': return 'status-sync--failed'
    case 'NeverRun': return 'status-sync--never'
    default: return ''
  }
}

export function StatusPage() {
  const statusQuery = useSiteStatus()
  const statesQuery = useStates()
  const status = statusQuery.data
  const states = statesQuery.data ?? []

  const totalAlerts = states.reduce((sum, s) => sum + s.publishedAlertCount, 0)
  const totalResources = states.reduce((sum, s) => sum + s.resourceTotal, 0)

  return (
    <section className="page-frame">
      <div className="page-stack">
        <nav className="breadcrumb">
          <Link to="/">Home</Link>
          <span className="breadcrumb__sep">/</span>
          <span>Status</span>
        </nav>

        <article className="page-hero">
          <span className="page-kicker">System status</span>
          <h1>Data Coverage Status</h1>
          <p>
            Static snapshot of all data sources and region coverage.
            {status?.exportedAt ? ` Last exported ${formatRelative(status.exportedAt)}.` : ''}
          </p>
          <div className="page-badges">
            <span className="page-badge">{status?.totalRegions ?? 0} regions</span>
            <span className="page-badge">{status?.regionsWithAlerts ?? 0} with alerts</span>
            <span className="page-badge">{totalAlerts.toLocaleString()} total alerts</span>
            <span className="page-badge">{totalResources.toLocaleString()} total resources</span>
          </div>
        </article>

        {statusQuery.isLoading ? (
          <p>Loading status...</p>
        ) : status ? (
          <section className="page-panel">
            <span className="section-kicker">Feed sync status</span>
            <strong>Data sources and their latest sync results</strong>
            <div className="status-table-wrap">
              <table className="status-table">
                <thead>
                  <tr>
                    <th>Feed</th>
                    <th>Type</th>
                    <th>Status</th>
                    <th>Last sync</th>
                    <th>Fetched</th>
                    <th>Created</th>
                  </tr>
                </thead>
                <tbody>
                  {status.feeds.map((feed) => (
                    <tr key={feed.name}>
                      <td>
                        <strong>{feed.name}</strong>
                        {!feed.isEnabled && <span className="status-badge status-badge--disabled"> disabled</span>}
                      </td>
                      <td>{feed.type}</td>
                      <td>
                        <span className={`status-badge ${syncStatusClass(feed.lastSyncStatus)}`}>
                          {feed.lastSyncStatus ?? 'Unknown'}
                        </span>
                      </td>
                      <td title={formatDateTime(feed.lastSyncCompletedAt)}>
                        {formatRelative(feed.lastSyncCompletedAt)}
                      </td>
                      <td>{feed.lastRecordsFetched ?? '-'}</td>
                      <td>{feed.lastRecordsCreated ?? '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        ) : null}

        <section className="page-panel">
          <span className="section-kicker">Coverage by state</span>
          <strong>{states.length} states and territories</strong>
          <div className="status-table-wrap">
            <table className="status-table status-table--regions">
              <thead>
                <tr>
                  <th>State</th>
                  <th>Counties</th>
                  <th>Alerts</th>
                  <th>Resources</th>
                </tr>
              </thead>
              <tbody>
                {states.map((state) => (
                  <tr key={state.code}>
                    <td>
                      <Link to={`/states/${state.code}`}>{state.name}</Link>
                    </td>
                    <td>{state.countyCount}</td>
                    <td>{state.publishedAlertCount}</td>
                    <td>{state.resourceTotal}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </section>
  )
}
