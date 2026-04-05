import type { ReactNode } from 'react'
import { Link, NavLink } from 'react-router-dom'

import type { RegionListItem } from '../api/types'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'
import type { AdminNoticeState } from './AdminFormUtils'

export function AdminLayout({
  kicker,
  title,
  body,
  actions,
  children,
}: {
  kicker: string
  title: string
  body: string
  actions?: ReactNode
  children: ReactNode
}) {
  return (
    <main className="page-frame">
      <section className="admin-stack">
        <article className="page-hero">
          <span className="page-kicker">{kicker}</span>
          <h1>{title}</h1>
          <p>{body}</p>
          <div className="admin-top-actions">
            <Link className="landing-link" to={validateAndSanitizeUrl('/')}>
              Return to public landing
            </Link>
            {actions}
          </div>
        </article>
        <AdminNav />
        {children}
      </section>
    </main>
  )
}

export function AdminNav() {
  const links = [
    { href: '/admin/dashboard', label: 'Dashboard' },
    { href: '/admin/alerts', label: 'Alerts' },
    { href: '/admin/resources', label: 'Resources' },
    { href: '/admin/prevention', label: 'Prevention' },
    { href: '/admin/news', label: 'News' },
  ]

  return (
    <nav className="admin-nav page-panel" aria-label="Admin navigation">
      {links.map((link) => (
        <NavLink
          className={({ isActive }) =>
            isActive ? 'dashboard-nav-link admin-nav-link admin-nav-link--active' : 'dashboard-nav-link admin-nav-link'
          }
          key={link.href}
          to={validateAndSanitizeUrl(link.href)}
        >
          {link.label}
        </NavLink>
      ))}
    </nav>
  )
}

export function AdminNotice({ notice }: { notice: AdminNoticeState }) {
  if (!notice) {
    return null
  }

  return (
    <section className={`page-panel admin-notice admin-notice--${notice.tone}`} role="status">
      <strong>{notice.tone === 'success' ? 'Saved' : 'Action failed'}</strong>
      <p>{notice.message}</p>
    </section>
  )
}

export function RegionSelect({
  regions,
  value,
  onChange,
}: {
  regions: RegionListItem[]
  value: string
  onChange: (value: string) => void
}) {
  return (
    <label className="admin-field">
      <span>Region</span>
      <select onChange={(event) => onChange(event.target.value)} value={value}>
        <option value="">Select a region</option>
        {regions.map((region) => (
          <option key={region.id} value={region.id}>
            {region.name} ({region.state})
          </option>
        ))}
      </select>
    </label>
  )
}
