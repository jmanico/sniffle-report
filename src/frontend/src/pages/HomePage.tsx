import { Link } from 'react-router-dom'

import { SniffleReportLogo } from '../components/layout/SniffleReportLogo'
import { useStates } from '../hooks/useStaticData'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

export function HomePage() {
  const statesQuery = useStates()
  const states = statesQuery.data ?? []

  return (
    <section className="page-frame">
      <div className="page-stack">
        <article className="page-hero">
          <div className="home-hero-brand">
            <SniffleReportLogo />
            <div className="home-hero-brand__copy">
              <span className="page-kicker">Sniffle Report</span>
              <span className="home-hero-brand__title">Regional health intelligence</span>
            </div>
          </div>
          <h1>Community health data for every US county</h1>
          <p>
            Regional health trends, disease surveillance, local clinics and pharmacies,
            prevention guidance, and drug safety alerts — sourced from CDC, FDA, and CMS.
            Updated automatically from 12 public data feeds.
          </p>
          <div className="page-badges">
            <span className="page-badge">50 states + DC</span>
            <span className="page-badge">
              {states.reduce((sum, s) => sum + s.countyCount, 0).toLocaleString()} counties
            </span>
            <Link className="page-badge page-badge--link" to="/status">
              System status
            </Link>
          </div>
        </article>

        <section className="page-panel">
          <span className="section-kicker">Browse by state</span>
          <strong>Select a state to see counties, alerts, and local resources.</strong>

          {statesQuery.isLoading ? (
            <div className="dashboard-skeleton-group" aria-hidden="true">
              <div className="dashboard-skeleton dashboard-skeleton--card" />
            </div>
          ) : (
            <div className="state-grid">
              {states.filter((s) => s.countyCount > 0).map((state) => (
                <Link
                  className="state-card"
                  key={state.code}
                  to={validateAndSanitizeUrl(`/states/${state.code}`)}
                >
                  <strong className="state-card__name">{state.name}</strong>
                  <span className="state-card__meta">
                    {state.countyCount} counties
                  </span>
                  <span className="state-card__stats">
                    {state.publishedAlertCount > 0 ? `${state.publishedAlertCount} alerts` : 'No alerts'}
                    {state.resourceTotal > 0 ? ` · ${state.resourceTotal} resources` : ''}
                  </span>
                </Link>
              ))}
            </div>
          )}
        </section>
      </div>
    </section>
  )
}
