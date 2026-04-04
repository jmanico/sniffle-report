import { useParams } from 'react-router-dom'

import { useResourceById } from '../hooks/useResources'
import { useRegion } from '../hooks/useRegion'
import { validateAndSanitizeUrl } from '../utils/validateAndSanitizeUrl'

const days: Array<keyof NonNullable<Awaited<ReturnType<typeof useResourceById>>['data']>['hours']> = [
  'mon',
  'tue',
  'wed',
  'thu',
  'fri',
  'sat',
  'sun',
]

const dayLabels: Record<(typeof days)[number], string> = {
  mon: 'Mon',
  tue: 'Tue',
  wed: 'Wed',
  thu: 'Thu',
  fri: 'Fri',
  sat: 'Sat',
  sun: 'Sun',
}

const phonePattern = /^\+?[0-9()\-\s.]{7,20}$/

function getSafeTelLink(phone: string | null) {
  if (!phone || !phonePattern.test(phone)) {
    return null
  }

  return validateAndSanitizeUrl(`tel:${phone.replace(/[^\d+]/g, '')}`)
}

export function ResourceDetailPage() {
  const { resourceId } = useParams()
  const { regionId, regionLabel } = useRegion()
  const resourceQuery = useResourceById(regionId, resourceId ?? '')

  if (!resourceId) {
    return null
  }

  return (
    <section className="page-frame">
      {resourceQuery.isLoading ? (
        <div className="page-stack">
          <div className="dashboard-skeleton dashboard-skeleton--card" />
          <div className="dashboard-skeleton dashboard-skeleton--card" />
        </div>
      ) : null}

      {resourceQuery.isError ? (
        <article className="page-panel">
          <span className="section-kicker">Resource unavailable</span>
          <strong>This resource could not be loaded.</strong>
          <p>Return to the resource list and choose another location.</p>
        </article>
      ) : null}

      {!resourceQuery.isLoading && !resourceQuery.isError && resourceQuery.data ? (
        <div className="page-stack">
          <article className="page-hero">
            <span className="page-kicker">Resource detail</span>
            <h1>{resourceQuery.data.name}</h1>
            <p>
              {resourceQuery.data.address} in {regionLabel}. This resource is categorized as{' '}
              {resourceQuery.data.type}.
            </p>
          </article>

          <div className="alert-detail-grid">
            <section className="page-panel">
              <span className="section-kicker">Contact</span>
              <strong>{resourceQuery.data.address}</strong>
              <div className="resource-detail-links">
                {getSafeTelLink(resourceQuery.data.phone) ? (
                  <a href={getSafeTelLink(resourceQuery.data.phone)!}>{resourceQuery.data.phone}</a>
                ) : (
                  <span>Phone unavailable</span>
                )}
                {resourceQuery.data.website ? (
                  <a href={validateAndSanitizeUrl(resourceQuery.data.website)} rel="noreferrer" target="_blank">
                    Visit website
                  </a>
                ) : (
                  <span>Website unavailable</span>
                )}
              </div>
            </section>

            <section className="page-panel">
              <span className="section-kicker">Map view</span>
              <strong>Simple location preview</strong>
              <div className="resource-map-placeholder">
                <span>Lat: {resourceQuery.data.latitude ?? 'Unknown'}</span>
                <span>Lng: {resourceQuery.data.longitude ?? 'Unknown'}</span>
              </div>
            </section>
          </div>

          <section className="page-panel">
            <span className="section-kicker">Hours</span>
            <strong>Weekly availability</strong>
            <div className="resource-hours-table" role="table" aria-label="Resource hours">
              {days.map((day) => (
                <div className="resource-hours-row" key={day} role="row">
                  <span role="columnheader">{dayLabels[day]}</span>
                  <span role="cell">{resourceQuery.data.hours[day] ?? 'Closed / unavailable'}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="page-panel">
            <span className="section-kicker">Services</span>
            <strong>Available support</strong>
            <div className="resource-services-list">
              {resourceQuery.data.services.map((service) => (
                <span className="page-badge" key={service}>
                  {service}
                </span>
              ))}
            </div>
          </section>
        </div>
      ) : null}
    </section>
  )
}
