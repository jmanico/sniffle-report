import { Link, Route, Routes } from 'react-router-dom'

import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

function AdminPage({
  kicker,
  title,
  body,
}: {
  kicker: string
  title: string
  body: string
}) {
  return (
    <main className="page-frame">
      <section className="admin-shell">
        <div className="page-hero">
          <span className="page-kicker">{kicker}</span>
          <h1>{title}</h1>
          <p>{body}</p>
          <Link className="landing-link" to={validateAndSanitizeUrl('/')}>
            Return to public landing
          </Link>
        </div>
      </section>
    </main>
  )
}

export default function AdminRoutes() {
  return (
    <Routes>
      <Route
        index
        element={
          <AdminPage
            body="Authentication, MFA, and admin state wiring land in later issues. This route is lazy-loaded now so it stays out of the public bundle."
            kicker="Admin login"
            title="Administrative entry"
          />
        }
      />
      <Route
        path="dashboard"
        element={
          <AdminPage
            body="Reserved for the management overview, pending the admin frontend workflow issues."
            kicker="Admin dashboard"
            title="Operations overview"
          />
        }
      />
      <Route
        path="alerts"
        element={
          <AdminPage
            body="This route will host alert CRUD after the auth and admin API slices are complete."
            kicker="Admin alerts"
            title="Manage health alerts"
          />
        }
      />
      <Route
        path="resources"
        element={
          <AdminPage
            body="This route will host resource CRUD and resource-map editing flows."
            kicker="Admin resources"
            title="Manage local resources"
          />
        }
      />
      <Route
        path="prevention"
        element={
          <AdminPage
            body="This route will host prevention guide management and cost-tier editing."
            kicker="Admin prevention"
            title="Manage prevention guidance"
          />
        }
      />
      <Route
        path="news"
        element={
          <AdminPage
            body="This route will host the editorial queue and fact-check workflow."
            kicker="Admin news"
            title="Manage health news"
          />
        }
      />
      <Route
        path="*"
        element={
          <AdminPage
            body="The requested admin route does not exist in the shell."
            kicker="404"
            title="Unknown admin route"
          />
        }
      />
    </Routes>
  )
}
