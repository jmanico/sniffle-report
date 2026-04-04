import { lazy, Suspense } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Route, Routes } from 'react-router-dom'

import './App.css'
import { queryClient } from './api/queryClient'
import { DashboardErrorBoundary } from './components/dashboard/DashboardErrorBoundary'
import { AppShell } from './components/layout/AppShell'
import { RegionProvider } from './components/region/RegionContext'

const AdminRoutes = lazy(() => import('./admin/AdminRoutes'))
const AlertDetailPage = lazy(() =>
  import('./pages/AlertDetailPage').then((module) => ({ default: module.AlertDetailPage })),
)
const AlertsPage = lazy(() =>
  import('./pages/AlertsPage').then((module) => ({ default: module.AlertsPage })),
)
const HomePage = lazy(() =>
  import('./pages/HomePage').then((module) => ({ default: module.HomePage })),
)
const NewsPage = lazy(() =>
  import('./pages/NewsPage').then((module) => ({ default: module.NewsPage })),
)
const NotFoundPage = lazy(() =>
  import('./pages/NotFoundPage').then((module) => ({ default: module.NotFoundPage })),
)
const PreventionDetailPage = lazy(() =>
  import('./pages/PreventionDetailPage').then((module) => ({ default: module.PreventionDetailPage })),
)
const PreventionPage = lazy(() =>
  import('./pages/PreventionPage').then((module) => ({ default: module.PreventionPage })),
)
const RegionalDashboardPage = lazy(() =>
  import('./pages/RegionalDashboardPage').then((module) => ({ default: module.RegionalDashboardPage })),
)
const ResourceDetailPage = lazy(() =>
  import('./pages/ResourceDetailPage').then((module) => ({ default: module.ResourceDetailPage })),
)
const ResourcesPage = lazy(() =>
  import('./pages/ResourcesPage').then((module) => ({ default: module.ResourcesPage })),
)

function RouteFallback() {
  return <div className="route-loading">Loading page…</div>
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Suspense fallback={<RouteFallback />}>
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
              <Route
                index
                element={
                  <DashboardErrorBoundary>
                    <RegionalDashboardPage />
                  </DashboardErrorBoundary>
                }
              />
              <Route path="alerts" element={<AlertsPage />} />
              <Route path="alerts/:alertId" element={<AlertDetailPage />} />
              <Route path="prevention" element={<PreventionPage />} />
              <Route path="prevention/:guideId" element={<PreventionDetailPage />} />
              <Route path="resources" element={<ResourcesPage />} />
              <Route path="resources/:resourceId" element={<ResourceDetailPage />} />
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
        </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
