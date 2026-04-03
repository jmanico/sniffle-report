import { useRegion } from '../hooks/useRegion'

export function ResourcesPage() {
  const { regionLabel } = useRegion()

  return (
    <section className="page-frame">
      <article className="page-hero">
        <span className="page-kicker">Resources</span>
        <h1>Clinics, pharmacies, and nearby access for {regionLabel}</h1>
        <p>
          The route shell is in place for the resource list and eventual map
          view, backed by the region-scoped resource API and nearby search.
        </p>
      </article>
    </section>
  )
}
