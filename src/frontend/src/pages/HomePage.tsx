import { Link } from 'react-router-dom'

import { PwaStatusBanner } from '../components/layout/PwaStatusBanner'
import { RegionSearchPanel } from '../components/region/RegionSearchPanel'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

const spotlightCards = [
  {
    kicker: 'Alert lanes',
    title: 'Track what is active nearby without drifting into national noise.',
  },
  {
    kicker: 'Trend framing',
    title: 'Case curves stay attached to a specific region and specific alert context.',
  },
  {
    kicker: 'Access answers',
    title: 'Prevention cost tiers and local resource directories sit next to the news cycle.',
  },
]

export function HomePage() {
  return (
    <main className="landing-page">
      <section className="landing-hero">
        <span className="section-kicker">Public health, scoped correctly</span>
        <h1>Regional health intelligence shaped by the place you actually live in.</h1>
        <p>
          Start by selecting a real region from the backend dataset. Search is
          debounced, region routes are shareable, and the selected region
          becomes the scope for every public health view in the app.
        </p>
        <div className="landing-links">
          <Link className="landing-link" to={validateAndSanitizeUrl('/admin')}>
            Admin entry
          </Link>
        </div>
      </section>

      <PwaStatusBanner />

      <RegionSearchPanel
        className="region-search region-search--landing"
        description="Search counties, metros, ZIP regions, and states, then jump directly into that dashboard."
        heading="Choose a region"
        inputLabel="Find your region"
      />

      <section className="spotlight-grid" aria-label="Application shell highlights">
        {spotlightCards.map((card) => (
          <article className="spotlight-card" key={card.kicker}>
            <span className="section-kicker">{card.kicker}</span>
            <strong>{card.title}</strong>
          </article>
        ))}
      </section>
    </main>
  )
}
