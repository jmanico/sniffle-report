import { Link } from 'react-router-dom'

import type { AlertListItem } from '../../api/types'
import { SeverityBadge } from '../dashboard/SeverityBadge'
import { validateAndSanitizeUrl } from '../../utils/validateAndSanitizeUrl'

function formatDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
}

type AlertCardProps = {
  alert: AlertListItem
  href: string
}

export function AlertCard({ alert, href }: AlertCardProps) {
  return (
    <Link className="alert-card" to={validateAndSanitizeUrl(href)}>
      <div className="alert-card__header">
        <SeverityBadge severity={alert.severity} />
        <span className="alert-card__metric">{alert.caseCount} cases</span>
      </div>
      <strong>{alert.title}</strong>
      <p>{alert.summary}</p>
      <div className="alert-card__meta">
        <span>{alert.disease}</span>
        <span>{formatDate(alert.sourceDate)}</span>
      </div>
      <span className="alert-card__source">{alert.sourceAttribution}</span>
    </Link>
  )
}
