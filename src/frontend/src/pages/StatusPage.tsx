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

function getFeedStatus(feed: {
  lastSyncStatus: string | null
  lastSyncCompletedAt: string | null
  lastRecordsFetched: number | null
}) {
  if (feed.lastSyncStatus === 'Failed') {
    return {
      label: 'Needs attention',
      className: 'status-sync--failed',
      detail: 'Latest refresh failed for this source.',
    }
  }

  if (feed.lastSyncStatus === 'PartialSuccess') {
    return {
      label: 'Partial',
      className: 'status-sync--partial',
      detail: 'Some records refreshed, but the source needs review.',
    }
  }

  if (feed.lastSyncCompletedAt) {
    return {
      label: 'Current',
      className: 'status-sync--success',
      detail: `Last refreshed ${formatRelative(feed.lastSyncCompletedAt)}.`,
    }
  }

  if ((feed.lastRecordsFetched ?? 0) > 0) {
    return {
      label: 'Snapshot available',
      className: 'status-sync--success',
      detail: 'This source is included in the current published snapshot.',
    }
  }

  return {
    label: 'Pending',
    className: 'status-sync--never',
    detail: 'No published snapshot has been generated for this source yet.',
  }
}

function formatFeedType(type: string) {
  switch (type) {
    case 'CdcSocrata':
      return 'CDC dataset'
    case 'CdcRss':
      return 'Public alert feed'
    case 'NpiRegistry':
      return 'Provider registry'
    case 'HrsaHealthCenter':
      return 'HRSA clinic site directory'
    case 'HrsaHpsa':
      return 'HRSA shortage-area dataset'
    case 'EpaSdwis':
      return 'EPA drinking water dataset'
    case 'OpenFda':
      return 'FDA enforcement feed'
    default:
      return type
  }
}

export function StatusPage() {
  const statusQuery = useSiteStatus()
  const statesQuery = useStates()
  const status = statusQuery.data
  const states = statesQuery.data ?? []

  const totalAlerts = states.reduce((sum, s) => sum + s.publishedAlertCount, 0)
  const totalResources = states.reduce((sum, s) => sum + s.resourceTotal, 0)
  const currentFeedCount = status?.feeds.filter((feed) => getFeedStatus(feed).label !== 'Needs attention').length ?? 0
  const totalFetched = status?.feeds.reduce((sum, feed) => sum + (feed.lastRecordsFetched ?? 0), 0) ?? 0

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
          <>
            <section className="page-panel">
              <span className="section-kicker">Source health</span>
              <strong>Published data sources in the current snapshot</strong>
              <div className="page-badges">
                <span className="page-badge">{status.feeds.length} active feeds</span>
                <span className="page-badge">{currentFeedCount} available in snapshot</span>
                <span className="page-badge">{totalFetched.toLocaleString()} records fetched</span>
              </div>
            </section>

            <section className="page-panel">
              <span className="section-kicker">Feed status</span>
              <strong>Coverage and freshness for each published source</strong>
              <div className="status-feed-grid">
                {status.feeds.map((feed) => {
                  const displayStatus = getFeedStatus(feed)

                  return (
                    <article className="status-feed-card" key={feed.name}>
                      <div className="status-feed-card__header">
                        <strong>{feed.name}</strong>
                        <span className={`status-badge ${displayStatus.className}`}>
                          {displayStatus.label}
                        </span>
                      </div>
                      <span className="status-feed-card__type">{formatFeedType(feed.type)}</span>
                      <p>{displayStatus.detail}</p>
                      <p className="status-feed-card__updated">
                        Last updated: {formatDateTime(feed.lastSyncCompletedAt)}
                      </p>
                      <div className="status-feed-card__metrics">
                        <span className="page-badge">
                          {(feed.lastRecordsFetched ?? 0).toLocaleString()} fetched
                        </span>
                        {feed.lastSyncCompletedAt ? (
                          <span className="page-badge" title={formatDateTime(feed.lastSyncCompletedAt)}>
                            {formatRelative(feed.lastSyncCompletedAt)}
                          </span>
                        ) : null}
                      </div>
                    </article>
                  )
                })}
              </div>
            </section>
          </>
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
