import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'

import { useStateDetail } from '../hooks/useStaticData'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

type SortKey = 'name' | 'publishedAlertCount' | 'resourceTotal'
type SortDirection = 'asc' | 'desc'

function formatRelative(date: string | null) {
  if (!date) return 'never'
  const diff = Date.now() - new Date(date).getTime()
  const hours = Math.floor(diff / 3600000)
  if (hours < 1) return 'just now'
  if (hours < 24) return `${hours}h ago`
  return `${Math.floor(hours / 24)}d ago`
}

export function StateBrowsePage() {
  const { stateCode } = useParams<{ stateCode: string }>()
  const stateQuery = useStateDetail(stateCode ?? '')
  const state = stateQuery.data

  const [sortKey, setSortKey] = useState<SortKey>('name')
  const [sortDir, setSortDir] = useState<SortDirection>('asc')

  const counties = useMemo(() => {
    if (!state) return []

    return [...state.counties].sort((a, b) => {
      let cmp = 0
      switch (sortKey) {
        case 'name': cmp = a.name.localeCompare(b.name); break
        case 'publishedAlertCount': cmp = a.publishedAlertCount - b.publishedAlertCount; break
        case 'resourceTotal': cmp = a.resourceTotal - b.resourceTotal; break
      }
      return sortDir === 'asc' ? cmp : -cmp
    })
  }, [state, sortKey, sortDir])

  function toggleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortKey(key)
      setSortDir(key === 'name' ? 'asc' : 'desc')
    }
  }

  function sortIndicator(key: SortKey) {
    if (sortKey !== key) return ''
    return sortDir === 'asc' ? ' \u25B2' : ' \u25BC'
  }

  if (stateQuery.isLoading) {
    return (
      <section className="page-frame">
        <div className="page-stack">
          <div className="dashboard-skeleton-group" aria-hidden="true">
            <div className="dashboard-skeleton dashboard-skeleton--card" />
          </div>
        </div>
      </section>
    )
  }

  if (!state) {
    return (
      <section className="page-frame">
        <article className="page-hero">
          <h1>State not found</h1>
          <p><Link to="/">Return home</Link></p>
        </article>
      </section>
    )
  }

  return (
    <section className="page-frame">
      <div className="page-stack">
        <nav className="breadcrumb">
          <Link to="/">Home</Link>
          <span className="breadcrumb__sep">/</span>
          <span>{state.name}</span>
        </nav>

        <article className="page-hero">
          <span className="page-kicker">{state.name}</span>
          <h1>Counties in {state.name}</h1>
          <p>
            Browse all {state.counties.length} counties and county-equivalents.
            Each county page shows health alerts, disease trends, local resources,
            and prevention guidance.
          </p>
          <div className="page-badges">
            <span className="page-badge">{state.publishedAlertCount} alerts statewide</span>
            <span className="page-badge">{state.resourceTotal} resources statewide</span>
            <Link
              className="page-badge page-badge--link"
              to={validateAndSanitizeUrl(`/region/${state.id}`)}
            >
              State dashboard
            </Link>
          </div>
        </article>

        <section className="page-panel">
          <div className="status-table-wrap">
            <table className="status-table status-table--regions">
              <thead>
                <tr>
                  <th className="status-sortable" onClick={() => toggleSort('name')}>
                    County{sortIndicator('name')}
                  </th>
                  <th className="status-sortable" onClick={() => toggleSort('publishedAlertCount')}>
                    Alerts{sortIndicator('publishedAlertCount')}
                  </th>
                  <th className="status-sortable" onClick={() => toggleSort('resourceTotal')}>
                    Resources{sortIndicator('resourceTotal')}
                  </th>
                  <th>Last updated</th>
                </tr>
              </thead>
              <tbody>
                {counties.map((county) => (
                  <tr key={county.id}>
                    <td>
                      <Link to={validateAndSanitizeUrl(`/region/${county.id}`)}>
                        {county.name}
                      </Link>
                    </td>
                    <td>{county.publishedAlertCount}</td>
                    <td>{county.resourceTotal}</td>
                    <td>{formatRelative(county.computedAt)}</td>
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
