import { useRegion } from '../hooks/useRegion'

export function AlertsPage() {
  const { regionLabel } = useRegion()

  return (
    <section className="page-frame">
      <div className="page-grid">
        <article className="page-hero">
          <span className="page-kicker">Alerts</span>
          <h1>Health alerts for {regionLabel}</h1>
          <p>
            This route is reserved for the alert list experience and connects to
            the public alert API already present on the backend.
          </p>
        </article>
        <aside className="page-panel">
          <span className="section-kicker">Next UI slice</span>
          <strong>Alert cards, severity badges, and detail deep-links</strong>
          <p>Issue #26 will turn this route into a browsable alert feed.</p>
        </aside>
      </div>
    </section>
  )
}
