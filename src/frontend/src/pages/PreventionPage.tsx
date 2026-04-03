import { useRegion } from '../hooks/useRegion'

export function PreventionPage() {
  const { regionLabel } = useRegion()

  return (
    <section className="page-frame">
      <article className="page-hero">
        <span className="page-kicker">Prevention</span>
        <h1>Guidance and cost tiers for {regionLabel}</h1>
        <p>
          This page will host prevention guides and cost-tier displays once the
          API client layer and prevention UI components land.
        </p>
      </article>
    </section>
  )
}
