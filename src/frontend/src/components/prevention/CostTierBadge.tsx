import type { CostTierType } from '../../api/types'

const costTierClassNames: Record<CostTierType, string> = {
  Free: 'cost-tier-badge cost-tier-badge--free',
  Insured: 'cost-tier-badge cost-tier-badge--insured',
  OutOfPocket: 'cost-tier-badge cost-tier-badge--out-of-pocket',
  Promotional: 'cost-tier-badge cost-tier-badge--promotional',
}

export function CostTierBadge({ type }: { type: CostTierType }) {
  return <span className={costTierClassNames[type]}>{type}</span>
}
