import { NavLink } from 'react-router-dom'

import { useRegion } from '../../hooks/useRegion'
import { validateAndSanitizeUrl } from '../../utils/validateAndSanitizeUrl'
import { RegionSelector } from '../region/RegionSelector'

const navItems = [
  { label: 'Dashboard', segment: '' },
  { label: 'Alerts', segment: 'alerts' },
  { label: 'Prevention', segment: 'prevention' },
  { label: 'Resources', segment: 'resources' },
  { label: 'News', segment: 'news' },
]

export function Header() {
  const { buildRegionPath } = useRegion()

  return (
    <header className="site-header">
      <div className="site-header__inner">
        <NavLink className="brand-lockup" to={validateAndSanitizeUrl(buildRegionPath())}>
          <span className="brand-lockup__eyebrow">Sniffle Report</span>
          <span className="brand-lockup__title">Region-scoped health intelligence</span>
          <span className="brand-lockup__subtitle">Alerts, trends, prevention, and local care access</span>
        </NavLink>

        <div className="nav-cluster">
          <nav className="desktop-nav" aria-label="Primary">
            {navItems.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  isActive ? 'nav-pill nav-pill--active' : 'nav-pill'
                }
                key={item.label}
                to={validateAndSanitizeUrl(buildRegionPath(item.segment))}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <RegionSelector />

          <details className="mobile-nav">
            <summary className="mobile-nav__summary">Menu</summary>
            <div className="mobile-nav__panel">
              <div className="mobile-nav__title">Mobile navigation placeholder</div>
              <div className="mobile-nav__links" role="list">
                {navItems.map((item) => (
                  <NavLink
                    className="mobile-nav__link"
                    key={item.label}
                    to={validateAndSanitizeUrl(buildRegionPath(item.segment))}
                  >
                    {item.label}
                  </NavLink>
                ))}
              </div>
            </div>
          </details>
        </div>
      </div>
    </header>
  )
}
