import { useRegion } from '../hooks/useRegion'

export function NewsPage() {
  const { regionLabel } = useRegion()

  return (
    <section className="page-frame">
      <article className="page-hero">
        <span className="page-kicker">News</span>
        <h1>Health news and fact-check state for {regionLabel}</h1>
        <p>
          This placeholder reserves the route for the later editorial and
          fact-check UI work without shipping dead links or unstable paths.
        </p>
      </article>
    </section>
  )
}
