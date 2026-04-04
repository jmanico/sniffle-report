import { Link } from 'react-router-dom'

import { AdminLayout } from './AdminShared'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

const cards = [
  {
    href: '/admin/prevention',
    kicker: 'Prevention guides',
    title: 'Manage disease guidance and cost tiers',
    body: 'Edit long-form guidance, add pricing tiers, and keep the prevention catalog current.',
  },
  {
    href: '/admin/resources',
    kicker: 'Local resources',
    title: 'Maintain clinics, pharmacies, and service metadata',
    body: 'Update addresses, hours, websites, services, and geolocation for local resource listings.',
  },
  {
    href: '/admin/news',
    kicker: 'Health news',
    title: 'Publish editorial items with fact-check status visibility',
    body: 'Create and update news items while keeping the current fact-check status visible to editors.',
  },
]

export function AdminDashboardPage() {
  return (
    <AdminLayout
      body="This admin shell is still pre-auth, but the content management routes are now wired to the live backend APIs."
      kicker="Admin dashboard"
      title="Editorial operations"
    >
      <section className="spotlight-grid">
        {cards.map((card) => (
          <article className="spotlight-card" key={card.href}>
            <span className="section-kicker">{card.kicker}</span>
            <strong>{card.title}</strong>
            <p>{card.body}</p>
            <Link className="dashboard-link" to={validateAndSanitizeUrl(card.href)}>
              Open workspace
            </Link>
          </article>
        ))}
      </section>
    </AdminLayout>
  )
}
