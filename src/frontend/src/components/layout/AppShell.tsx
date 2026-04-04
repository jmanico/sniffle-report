import { Outlet } from 'react-router-dom'

import { Footer } from './Footer'
import { Header } from './Header'
import { PwaStatusBanner } from './PwaStatusBanner'

export function AppShell() {
  return (
    <div className="shell">
      <Header />
      <main className="shell-content">
        <div className="page-frame">
          <PwaStatusBanner />
        </div>
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}
