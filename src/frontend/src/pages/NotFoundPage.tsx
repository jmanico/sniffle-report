import { Link } from 'react-router-dom'

import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

export function NotFoundPage() {
  return (
    <main className="page-frame">
      <section className="not-found">
        <span className="page-kicker">404</span>
        <h1>That route does not belong to this app shell.</h1>
        <p>
          The URL did not match a known public or admin path. Use the landing
          page to pick a region-driven route instead.
        </p>
        <Link className="landing-link landing-link--primary" to={validateAndSanitizeUrl('/')}>
          Back to landing page
        </Link>
      </section>
    </main>
  )
}
