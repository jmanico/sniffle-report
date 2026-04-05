import type { FactCheckStatus } from '../../api/types'

const statusConfig: Record<string, { label: string; className: string }> = {
  Verified: { label: 'Verified', className: 'fact-check-badge--verified' },
  Pending: { label: 'Pending', className: 'fact-check-badge--pending' },
  Disputed: { label: 'Disputed', className: 'fact-check-badge--disputed' },
  Unverified: { label: 'Unverified', className: 'fact-check-badge--unverified' },
}

interface FactCheckBadgeProps {
  status: FactCheckStatus | null
}

export function FactCheckBadge({ status }: FactCheckBadgeProps) {
  if (!status) return null

  const config = statusConfig[status] ?? { label: status, className: '' }

  return (
    <span className={`fact-check-badge ${config.className}`}>
      {config.label}
    </span>
  )
}
