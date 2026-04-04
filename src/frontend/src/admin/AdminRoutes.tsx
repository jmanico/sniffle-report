import { Route, Routes } from 'react-router-dom'

import { AdminDashboardPage } from './AdminDashboardPage'
import { AdminAlertsPage } from './AdminAlertsPage'
import { AdminLayout } from './AdminShared'
import { AdminNewsPage } from './AdminNewsPage'
import { AdminPreventionPage } from './AdminPreventionPage'
import { AdminResourcesPage } from './AdminResourcesPage'

export default function AdminRoutes() {
  return (
    <Routes>
      <Route index element={<AdminDashboardPage />} />
      <Route path="dashboard" element={<AdminDashboardPage />} />
      <Route path="alerts" element={<AdminAlertsPage />} />
      <Route path="resources" element={<AdminResourcesPage />} />
      <Route path="prevention" element={<AdminPreventionPage />} />
      <Route path="news" element={<AdminNewsPage />} />
      <Route
        path="*"
        element={
          <AdminLayout
            body="The requested admin route does not exist in the shell."
            kicker="404"
            title="Unknown admin route"
          >
            <section className="page-panel">
              <strong>Unknown admin route.</strong>
              <p>Use the navigation above to move between the available workspaces.</p>
            </section>
          </AdminLayout>
        }
      />
    </Routes>
  )
}
