import { useRegion } from '../hooks/useRegion'

export function RegionalDashboardPage() {
  const { regionLabel } = useRegion()

  return (
    <section className="page-frame">
      <div className="page-stack">
        <article className="page-hero">
          <span className="page-kicker">Regional dashboard</span>
          <h1>{regionLabel}</h1>
          <p>
            This route is the public dashboard anchor for the selected region.
            Upcoming frontend issues will connect alert summaries, trend charts,
            prevention guidance, and resource cards to the backend APIs.
          </p>
          <div className="page-badges">
            <span className="page-badge">URL-driven region context</span>
            <span className="page-badge">Shared header and footer</span>
            <span className="page-badge">Ready for API wiring</span>
          </div>
        </article>
      </div>
    </section>
  )
}
