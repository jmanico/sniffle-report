import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'

import { getRegionStatus, getFeedStatus } from '../api/status'
import { queryKeys } from '../api/queryKeys'
import type { RegionStatus, FeedStatus } from '../api/types'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

type SortKey = 'name' | 'type' | 'state' | 'computedAt' | 'publishedAlertCount'
type SortDirection = 'asc' | 'desc'

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

function formatRelative(date: string | null) {
  if (!date) return 'never'
  const diff = Date.now() - new Date(date).getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return 'just now'
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  return `${days}d ago`
}

function stalenessClass(date: string | null): string {
  if (!date) return 'status-stale--never'
  const hours = (Date.now() - new Date(date).getTime()) / 3600000
  if (hours < 1) return 'status-stale--fresh'
  if (hours < 24) return 'status-stale--recent'
  return 'status-stale--stale'
}

function syncStatusClass(status: string | null): string {
  if (!status) return ''
  switch (status) {
    case 'Success': return 'status-sync--success'
    case 'PartialSuccess': return 'status-sync--partial'
    case 'Failed': return 'status-sync--failed'
    case 'NeverRun': return 'status-sync--never'
    default: return ''
  }
}

function FeedStatusSection({ feeds, isLoading }: { feeds: FeedStatus[]; isLoading: boolean }) {
  if (isLoading) return <p>Loading feed status...</p>

  return (
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
              <th>Unmappable</th>
              <th>Failures</th>
            </tr>
          </thead>
          <tbody>
            {feeds.map((feed) => (
              <tr key={feed.id}>
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
                <td>{formatRelative(feed.lastSyncCompletedAt)}</td>
                <td>{feed.lastRecordsFetched ?? '-'}</td>
                <td>{feed.lastRecordsCreated ?? '-'}</td>
                <td>{feed.lastRecordsSkippedUnmappable ?? '-'}</td>
                <td>{feed.consecutiveFailureCount > 0 ? feed.consecutiveFailureCount : '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

function RegionStatusSection({
  regions,
  isLoading,
}: {
  regions: RegionStatus[]
  isLoading: boolean
}) {
  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState<string>('')
  const [sortKey, setSortKey] = useState<SortKey>('name')
  const [sortDir, setSortDir] = useState<SortDirection>('asc')

  const filtered = useMemo(() => {
    let result = regions

    if (search) {
      const q = search.toLowerCase()
      result = result.filter(
        (r) => r.name.toLowerCase().includes(q) || r.state.toLowerCase().includes(q),
      )
    }

    if (typeFilter) {
      result = result.filter((r) => r.type === typeFilter)
    }

    result = [...result].sort((a, b) => {
      let cmp = 0
      switch (sortKey) {
        case 'name': cmp = a.name.localeCompare(b.name); break
        case 'type': cmp = a.type.localeCompare(b.type); break
        case 'state': cmp = a.state.localeCompare(b.state); break
        case 'computedAt':
          cmp = (a.computedAt ?? '').localeCompare(b.computedAt ?? '')
          break
        case 'publishedAlertCount':
          cmp = a.publishedAlertCount - b.publishedAlertCount
          break
      }
      return sortDir === 'asc' ? cmp : -cmp
    })

    return result
  }, [regions, search, typeFilter, sortKey, sortDir])

  function toggleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortKey(key)
      setSortDir('asc')
    }
  }

  function sortIndicator(key: SortKey) {
    if (sortKey !== key) return ''
    return sortDir === 'asc' ? ' \u25B2' : ' \u25BC'
  }

  const withData = regions.filter((r) => r.publishedAlertCount > 0).length
  const withSnapshots = regions.filter((r) => r.computedAt).length

  if (isLoading) return <p>Loading region status...</p>

  return (
    <section className="page-panel">
      <span className="section-kicker">Region data coverage</span>
      <strong>
        {regions.length.toLocaleString()} regions, {withSnapshots.toLocaleString()} with snapshots,{' '}
        {withData.toLocaleString()} with alert data
      </strong>

      <div className="status-filters">
        <input
          className="status-search"
          type="text"
          placeholder="Search regions..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          className="status-type-filter"
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value)}
        >
          <option value="">All types</option>
          <option value="State">States</option>
          <option value="County">Counties</option>
          <option value="Metro">Metros</option>
        </select>
        <span className="status-result-count">{filtered.length.toLocaleString()} shown</span>
      </div>

      <div className="status-table-wrap">
        <table className="status-table status-table--regions">
          <thead>
            <tr>
              <th className="status-sortable" onClick={() => toggleSort('name')}>
                Region{sortIndicator('name')}
              </th>
              <th className="status-sortable" onClick={() => toggleSort('type')}>
                Type{sortIndicator('type')}
              </th>
              <th className="status-sortable" onClick={() => toggleSort('state')}>
                State{sortIndicator('state')}
              </th>
              <th>Parent</th>
              <th className="status-sortable" onClick={() => toggleSort('computedAt')}>
                Last updated{sortIndicator('computedAt')}
              </th>
              <th className="status-sortable" onClick={() => toggleSort('publishedAlertCount')}>
                Alerts{sortIndicator('publishedAlertCount')}
              </th>
              <th>Resources</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((region) => (
              <tr key={region.regionId}>
                <td>
                  <Link to={validateAndSanitizeUrl(`/region/${region.regionId}`)}>
                    {region.name}
                  </Link>
                </td>
                <td>{region.type}</td>
                <td>{region.state}</td>
                <td>{region.parentName ?? '-'}</td>
                <td>
                  <span
                    className={`status-stale-dot ${stalenessClass(region.computedAt)}`}
                    title={formatDateTime(region.computedAt)}
                  />
                  {formatRelative(region.computedAt)}
                </td>
                <td>{region.publishedAlertCount}</td>
                <td>{region.resourceTotal}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export function StatusPage() {
  const regionsQuery = useQuery({
    queryKey: queryKeys.statusRegions(),
    queryFn: ({ signal }) => getRegionStatus(signal),
  })

  const feedsQuery = useQuery({
    queryKey: queryKeys.statusFeeds(),
    queryFn: ({ signal }) => getFeedStatus(signal),
  })

  return (
    <section className="page-frame">
      <div className="page-stack">
        <article className="page-hero">
          <span className="page-kicker">System status</span>
          <h1>Data Coverage Status</h1>
          <p>
            Live view of all regions and their data freshness. Updated automatically
            after each feed sync cycle.
          </p>
        </article>

        <FeedStatusSection feeds={feedsQuery.data ?? []} isLoading={feedsQuery.isLoading} />
        <RegionStatusSection regions={regionsQuery.data ?? []} isLoading={regionsQuery.isLoading} />
      </div>
    </section>
  )
}
