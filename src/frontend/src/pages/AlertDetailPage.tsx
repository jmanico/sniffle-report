import { useParams } from 'react-router-dom'

import { useRegion } from '../hooks/useRegion'

export function AlertDetailPage() {
  const { alertId } = useParams()
  const { regionLabel } = useRegion()

  return (
    <section className="page-frame">
      <article className="page-hero">
        <span className="page-kicker">Alert detail</span>
        <h1>Alert {alertId}</h1>
        <p>
          The route is active and region-aware for {regionLabel}. A later issue
          will attach detail content and the associated trend chart.
        </p>
      </article>
    </section>
  )
}
