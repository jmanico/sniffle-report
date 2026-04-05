import { useMemo, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'

import type { RegionDashboard } from '../api/types'
import { SeverityBadge } from '../components/dashboard/SeverityBadge'
import { useStateDetail, useStaticDashboard } from '../hooks/useStaticData'
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

function formatDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
}

type StatewidePattern = {
  key: string
  sourceAttribution: string
  severity: string
  alertCount: number
  diseaseCount: number
  maxCaseCount: number
  latestDate: string
  diseases: string[]
}

function groupStatewideAlerts(topAlerts: RegionDashboard['topAlerts']) {
  const patterns = new Map<string, StatewidePattern>()

  topAlerts.forEach((alert) => {
    const key = `${alert.sourceAttribution.trim().toLowerCase()}::${alert.severity.trim().toLowerCase()}`
    const existing = patterns.get(key)

    if (!existing) {
      patterns.set(key, {
        key,
        sourceAttribution: alert.sourceAttribution || 'Snapshot feed',
        severity: alert.severity,
        alertCount: 1,
        diseaseCount: 1,
        maxCaseCount: alert.caseCount,
        latestDate: alert.sourceDate,
        diseases: [alert.disease],
      })
      return
    }

    existing.alertCount += 1
    existing.maxCaseCount = Math.max(existing.maxCaseCount, alert.caseCount)

    if (new Date(alert.sourceDate).getTime() > new Date(existing.latestDate).getTime()) {
      existing.latestDate = alert.sourceDate
    }

    if (!existing.diseases.includes(alert.disease)) {
      existing.diseases.push(alert.disease)
      existing.diseaseCount = existing.diseases.length
    }
  })

  return [...patterns.values()]
    .sort((left, right) => {
      if (right.alertCount !== left.alertCount) return right.alertCount - left.alertCount
      if (right.maxCaseCount !== left.maxCaseCount) return right.maxCaseCount - left.maxCaseCount
      return new Date(right.latestDate).getTime() - new Date(left.latestDate).getTime()
    })
    .slice(0, 3)
}

function buildPatternSummary(pattern: StatewidePattern) {
  if (pattern.maxCaseCount > 0) {
    return `This feed contributes ${pattern.alertCount} statewide notices across ${pattern.diseaseCount} diseases. Highest reported count in this group is ${pattern.maxCaseCount} cases.`
  }

  return `This feed contributes ${pattern.alertCount} statewide notices across ${pattern.diseaseCount} diseases. These are surveillance notices in the current statewide snapshot.`
}

function buildPatternExamples(pattern: StatewidePattern) {
  return pattern.diseases.slice(0, 4)
}

function buildPatternExamplesRemainder(pattern: StatewidePattern) {
  return pattern.diseaseCount - buildPatternExamples(pattern).length
}

export function StateBrowsePage() {
  const { stateCode } = useParams<{ stateCode: string }>()
  const stateQuery = useStateDetail(stateCode ?? '')
  const state = stateQuery.data
  const stateDashboardQuery = useStaticDashboard(state?.id ?? '')

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

  const statewidePatterns = useMemo(() => {
    if (!stateDashboardQuery.data) return []
    return groupStatewideAlerts(stateDashboardQuery.data.topAlerts)
  }, [stateDashboardQuery.data])

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

  // Single-county states (e.g., DC) — go straight to the dashboard
  if (state.counties.length === 1) {
    return <Navigate to={`/region/${state.counties[0].id}`} replace />
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

        {statewidePatterns.length ? (
          <section className="page-panel">
            <div className="dashboard-card__header">
              <div>
                <span className="section-kicker">Statewide alert patterns</span>
                <strong>Grouped signals from repeated statewide feeds</strong>
              </div>
            </div>
            <div className="statewide-pattern-list">
              {statewidePatterns.map((pattern) => (
                <article className="statewide-pattern-card" key={pattern.key}>
                  <div className="statewide-pattern-card__header">
                    <SeverityBadge severity={pattern.severity as 'Low' | 'Moderate' | 'High' | 'Critical'} />
                    <span className="state-alert-pill">
                      {pattern.alertCount} notice{pattern.alertCount === 1 ? '' : 's'}
                    </span>
                  </div>
                  <strong>{pattern.sourceAttribution}</strong>
                  <div className="statewide-pattern-card__stats">
                    <span className="page-badge">{pattern.diseaseCount} diseases</span>
                    {pattern.maxCaseCount > 0 ? (
                      <span className="page-badge">{pattern.maxCaseCount} max cases</span>
                    ) : (
                      <span className="page-badge">Surveillance-only</span>
                    )}
                  </div>
                  <p>{buildPatternSummary(pattern)}</p>
                  <div className="statewide-pattern-card__examples">
                    {buildPatternExamples(pattern).map((disease) => (
                      <span className="statewide-pattern-card__example" key={disease}>{disease}</span>
                    ))}
                    {buildPatternExamplesRemainder(pattern) > 0 ? (
                      <span className="statewide-pattern-card__example statewide-pattern-card__example--more">
                        +{buildPatternExamplesRemainder(pattern)} more
                      </span>
                    ) : null}
                  </div>
                  <span className="statewide-pattern-card__meta">
                    Updated {formatDate(pattern.latestDate)}
                  </span>
                </article>
              ))}
            </div>
          </section>
        ) : null}

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
                  <tr
                    className={
                      county.publishedAlertCount > 0
                        ? 'state-region-row state-region-row--with-alerts'
                        : 'state-region-row'
                    }
                    key={county.id}
                  >
                    <td>
                      <Link to={validateAndSanitizeUrl(`/region/${county.id}`)}>
                        {county.name}
                      </Link>
                    </td>
                    <td>
                      {county.publishedAlertCount > 0 ? (
                        <span className="state-alert-pill">
                          {county.publishedAlertCount} alert{county.publishedAlertCount === 1 ? '' : 's'}
                        </span>
                      ) : (
                        <span className="state-alert-pill state-alert-pill--quiet">0 alerts</span>
                      )}
                    </td>
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
