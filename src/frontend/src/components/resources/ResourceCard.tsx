import { Link } from 'react-router-dom'

import type { ResourceListItem, ResourceType } from '../../api/types'
import { validateAndSanitizeUrl } from '../../utils/validateAndSanitizeUrl'

const phonePattern = /^\+?[0-9()\-\s.]{7,20}$/

function getSafeTelLink(phone: string | null) {
  if (!phone || !phonePattern.test(phone)) {
    return null
  }

  const sanitized = phone.replace(/[^\d+]/g, '')
  return validateAndSanitizeUrl(`tel:${sanitized}`)
}

const resourceTypeLabel: Record<ResourceType, string> = {
  Clinic: 'Clinic',
  Pharmacy: 'Pharmacy',
  VaccinationSite: 'Vaccination Site',
  Hospital: 'Hospital',
}

type ResourceCardProps = {
  resource: ResourceListItem
  href: string
}

export function ResourceCard({ resource, href }: ResourceCardProps) {
  const websiteHref = resource.website ? validateAndSanitizeUrl(resource.website) : null
  const phoneHref = getSafeTelLink(resource.phone)

  return (
    <article className="resource-card">
      <div className="resource-card__header">
        <strong>{resource.name}</strong>
        <span className="resource-type-badge">{resourceTypeLabel[resource.type]}</span>
      </div>
      <p className="resource-card__address">{resource.address}</p>
      <div className="resource-card__meta">
        {phoneHref ? (
          <a href={phoneHref}>{resource.phone}</a>
        ) : (
          <span>{resource.phone ?? 'Phone unavailable'}</span>
        )}
        {websiteHref ? (
          <a href={websiteHref} rel="noreferrer" target="_blank">
            Website
          </a>
        ) : (
          <span>Website unavailable</span>
        )}
      </div>
      <div className="resource-card__footer">
        <span>
          {resource.distanceMiles !== null && resource.distanceMiles !== undefined
            ? `${resource.distanceMiles.toFixed(2)} miles away`
            : 'Distance unavailable'}
        </span>
        <Link className="dashboard-link" to={validateAndSanitizeUrl(href)}>
          Details
        </Link>
      </div>
    </article>
  )
}
