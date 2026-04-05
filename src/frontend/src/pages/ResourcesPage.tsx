import { useState } from 'react'

import type { ResourceType } from '../api/types'
import { ResourceCard } from '../components/resources/ResourceCard'
import { useResources } from '../hooks/useResources'
import { useRegion } from '../hooks/useRegion'

const pageSize = 8

const tabs: Array<{ label: string; value: ResourceType | 'All' }> = [
  { label: 'All', value: 'All' },
  { label: 'Clinics', value: 'Clinic' },
  { label: 'Pharmacies', value: 'Pharmacy' },
  { label: 'Hospitals', value: 'Hospital' },
  { label: 'Vaccination Sites', value: 'VaccinationSite' },
]

export function ResourcesPage() {
  const { regionId, regionLabel, buildRegionPath } = useRegion()
  const [selectedType, setSelectedType] = useState<ResourceType | 'All'>('All')
  const [page, setPage] = useState(1)

  const resourcesQuery = useResources(regionId, {
    type: selectedType === 'All' ? undefined : selectedType,
    page,
    pageSize,
  })

  const totalCount = resourcesQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  return (
    <section className="page-frame">
      <div className="page-stack">
        <article className="page-hero">
          <span className="page-kicker">Resources</span>
          <h1>Clinics, pharmacies, and nearby access for {regionLabel}</h1>
          <p>
            Browse clinics, pharmacies, vaccination sites, and hospitals for the
            selected region. The list view stays primary and the map stays lightweight.
          </p>
        </article>

        <section className="page-panel">
          <div className="resource-tabs" role="tablist" aria-label="Resource type filters">
            {tabs.map((tab) => (
              <button
                aria-selected={selectedType === tab.value}
                className={selectedType === tab.value ? 'trend-range-button trend-range-button--active' : 'trend-range-button'}
                key={tab.value}
                onClick={() => {
                  setSelectedType(tab.value)
                  setPage(1)
                }}
                role="tab"
                type="button"
              >
                {tab.label}
              </button>
            ))}
          </div>
        </section>

        {resourcesQuery.isLoading ? (
          <section className="resource-list" aria-label="Loading resources">
            <div className="dashboard-skeleton dashboard-skeleton--card" />
            <div className="dashboard-skeleton dashboard-skeleton--card" />
          </section>
        ) : null}

        {resourcesQuery.isError ? (
          <section className="page-panel">
            <span className="section-kicker">Resources unavailable</span>
            <strong>Local resources could not be loaded right now.</strong>
            <p>Refresh the page and try again.</p>
          </section>
        ) : null}

        {!resourcesQuery.isLoading && !resourcesQuery.isError && resourcesQuery.data?.items.length === 0 ? (
          <section className="page-panel">
            <span className="section-kicker">No resources found</span>
            <strong>No resources found in this region.</strong>
            <p>Try another resource type tab.</p>
          </section>
        ) : null}

        {!resourcesQuery.isLoading && !resourcesQuery.isError && resourcesQuery.data?.items.length ? (
          <>
            <section className="resource-map-shell">
              <article className="page-panel resource-map-panel">
                <span className="section-kicker">Map view</span>
                <strong>Static location overview</strong>
                <p>
                  {resourcesQuery.data.items[0].latitude ?? 'Unknown'}, {resourcesQuery.data.items[0].longitude ?? 'Unknown'} is the first visible resource coordinate.
                </p>
              </article>
            </section>

            <section className="resource-list" aria-label="Resource results">
              {resourcesQuery.data.items.map((resource) => (
                <ResourceCard
                  href={buildRegionPath(`resources/${resource.id}`)}
                  key={resource.id}
                  resource={resource}
                />
              ))}
            </section>

            <section className="page-panel alerts-pagination">
              <span className="alerts-pagination__summary">
                Page {page} of {totalPages} · {totalCount} total resources
              </span>
              <div className="alerts-pagination__controls">
                <button
                  className="dashboard-link alerts-pagination__button"
                  disabled={page === 1}
                  onClick={() => {
                    setPage((current) => Math.max(1, current - 1))
                  }}
                  type="button"
                >
                  Previous
                </button>
                <button
                  className="dashboard-link alerts-pagination__button"
                  disabled={page >= totalPages}
                  onClick={() => {
                    setPage((current) => Math.min(totalPages, current + 1))
                  }}
                  type="button"
                >
                  Next
                </button>
              </div>
            </section>
          </>
        ) : null}
      </div>
    </section>
  )
}
