import { lazy, Suspense } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'

import './App.css'
import { AppShell } from './components/layout/AppShell'
import { RegionProvider } from './components/region/RegionContext'
import { AlertDetailPage } from './pages/AlertDetailPage'
import { AlertsPage } from './pages/AlertsPage'
import { HomePage } from './pages/HomePage'
import { NewsPage } from './pages/NewsPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { PreventionPage } from './pages/PreventionPage'
import { RegionalDashboardPage } from './pages/RegionalDashboardPage'
import { ResourcesPage } from './pages/ResourcesPage'

const AdminRoutes = lazy(() => import('./admin/AdminRoutes'))

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route
          path="/region/:regionId"
          element={
            <RegionProvider>
              <AppShell />
            </RegionProvider>
          }
        >
          <Route index element={<RegionalDashboardPage />} />
          <Route path="alerts" element={<AlertsPage />} />
          <Route path="alerts/:alertId" element={<AlertDetailPage />} />
          <Route path="prevention" element={<PreventionPage />} />
          <Route path="resources" element={<ResourcesPage />} />
          <Route path="news" element={<NewsPage />} />
        </Route>
        <Route
          path="/admin/*"
          element={
            <Suspense fallback={<div className="route-loading">Loading admin interface…</div>}>
              <AdminRoutes />
            </Suspense>
          }
        />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
