import { Outlet } from 'react-router-dom'

import { Footer } from './Footer'
import { Header } from './Header'

export function AppShell() {
  return (
    <div className="shell">
      <Header />
      <main className="shell-content">
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}
