import { useState } from 'react'

import { FactCheckBadge } from '../components/news/FactCheckBadge'
import { useRegion } from '../hooks/useRegion'
import { useNews } from '../hooks/useNews'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

function formatDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
}

export function NewsPage() {
  const { regionId, regionLabel } = useRegion()
  const [page, setPage] = useState(1)
  const [headline, setHeadline] = useState('')

  const newsQuery = useNews(regionId, { headline: headline || undefined, page, pageSize: 20 })
  const items = newsQuery.data?.items ?? []
  const totalCount = newsQuery.data?.totalCount ?? 0
  const totalPages = Math.ceil(totalCount / 20)

  return (
    <section className="page-frame">
      <div className="page-stack">
        <article className="page-hero">
          <span className="page-kicker">Health news</span>
          <h1>News and alerts for {regionLabel}</h1>
          <p>
            Health news, food safety alerts, drug recalls, and outbreak reports
            relevant to this region. Items include fact-check verification status
            from official sources.
          </p>
          <div className="page-badges">
            <span className="page-badge">{totalCount} news items</span>
          </div>
        </article>

        <section className="page-panel">
          <div className="alerts-filter-panel">
            <div className="alerts-filter-grid">
              <label className="alerts-filter-field">
                <span>Search headlines</span>
                <input
                  type="text"
                  placeholder="e.g. salmonella, recall..."
                  value={headline}
                  onChange={(e) => { setHeadline(e.target.value); setPage(1) }}
                />
              </label>
            </div>
          </div>
        </section>

        {newsQuery.isError ? (
          <article className="page-panel dashboard-state-card">
            <span className="section-kicker">Data unavailable</span>
            <strong>News items could not be loaded.</strong>
            <p>Try refreshing the page or switching to another region.</p>
          </article>
        ) : null}

        {newsQuery.isLoading ? (
          <div className="dashboard-skeleton-group" aria-hidden="true">
            <div className="dashboard-skeleton dashboard-skeleton--card" />
            <div className="dashboard-skeleton dashboard-skeleton--card" />
            <div className="dashboard-skeleton dashboard-skeleton--card" />
          </div>
        ) : items.length ? (
          <div className="news-list">
            {items.map((item) => (
              <article className="news-card" key={item.id}>
                <div className="news-card__header">
                  <strong>{item.headline}</strong>
                  <FactCheckBadge status={item.factCheckStatus} />
                </div>
                <span className="news-card__meta">
                  {formatDate(item.publishedAt)}
                </span>
                {item.sourceUrl ? (
                  <a
                    className="news-card__source"
                    href={validateAndSanitizeUrl(item.sourceUrl)}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    View source
                  </a>
                ) : null}
              </article>
            ))}
          </div>
        ) : (
          <article className="page-panel">
            <p className="dashboard-empty">No health news available for this region.</p>
          </article>
        )}

        {totalPages > 1 ? (
          <div className="alerts-pagination">
            <button
              className="alerts-pagination__button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </button>
            <span className="alerts-pagination__info">
              Page {page} of {totalPages}
            </span>
            <button
              className="alerts-pagination__button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        ) : null}
      </div>
    </section>
  )
}
