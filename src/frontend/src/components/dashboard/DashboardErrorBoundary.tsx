import { Component, type ReactNode } from 'react'

type DashboardErrorBoundaryProps = {
  children: ReactNode
}

type DashboardErrorBoundaryState = {
  hasError: boolean
}

export class DashboardErrorBoundary extends Component<
  DashboardErrorBoundaryProps,
  DashboardErrorBoundaryState
> {
  public state: DashboardErrorBoundaryState = {
    hasError: false,
  }

  public static getDerivedStateFromError() {
    return { hasError: true }
  }

  public render() {
    if (this.state.hasError) {
      return (
        <section className="page-frame">
          <article className="page-panel dashboard-state-card">
            <span className="section-kicker">Dashboard error</span>
            <strong>Something broke while rendering this dashboard.</strong>
            <p>Reload the page or switch regions to try again.</p>
          </article>
        </section>
      )
    }

    return this.props.children
  }
}
