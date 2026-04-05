import { lazy, Suspense } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Route, Routes } from 'react-router-dom'

import './App.css'
import { queryClient } from './api/queryClient'
import { DashboardErrorBoundary } from './components/dashboard/DashboardErrorBoundary'

const HomePage = lazy(() =>
  import('./pages/HomePage').then((module) => ({ default: module.HomePage })),
)
const StateBrowsePage = lazy(() =>
  import('./pages/StateBrowsePage').then((module) => ({ default: module.StateBrowsePage })),
)
const RegionalDashboardPage = lazy(() =>
  import('./pages/RegionalDashboardPage').then((module) => ({ default: module.RegionalDashboardPage })),
)
const StatusPage = lazy(() =>
  import('./pages/StatusPage').then((module) => ({ default: module.StatusPage })),
)
const NotFoundPage = lazy(() =>
  import('./pages/NotFoundPage').then((module) => ({ default: module.NotFoundPage })),
)

function RouteFallback() {
  return <div className="route-loading">Loading page...</div>
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Suspense fallback={<RouteFallback />}>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/states/:stateCode" element={<StateBrowsePage />} />
            <Route
              path="/region/:regionId"
              element={
                <DashboardErrorBoundary>
                  <RegionalDashboardPage />
                </DashboardErrorBoundary>
              }
            />
            <Route path="/status" element={<StatusPage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Routes>
        </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
