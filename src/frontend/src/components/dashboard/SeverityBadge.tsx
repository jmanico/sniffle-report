import type { AlertSeverity } from '../../api/types'

const severityClassNames: Record<AlertSeverity, string> = {
  Critical: 'severity-badge severity-badge--critical',
  High: 'severity-badge severity-badge--high',
  Moderate: 'severity-badge severity-badge--moderate',
  Low: 'severity-badge severity-badge--low',
}

export function SeverityBadge({ severity }: { severity: AlertSeverity }) {
  return <span className={severityClassNames[severity]}>{severity}</span>
}
