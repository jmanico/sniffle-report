import { Link } from 'react-router-dom'

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
          The app shell is now organized around shareable region URLs, a common
          layout, and route placeholders for every public and admin surface in
          the architecture. The next frontend steps wire these views to the API
          and fill them with real data.
        </p>
        <div className="landing-links">
          <Link
            className="landing-link landing-link--primary"
            to={validateAndSanitizeUrl('/region/travis-county-tx')}
          >
            Open a demo region
          </Link>
          <Link className="landing-link" to={validateAndSanitizeUrl('/admin')}>
            Admin entry
          </Link>
        </div>
      </section>

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
