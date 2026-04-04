import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { CostTierBadge } from './CostTierBadge'

describe('CostTierBadge', () => {
  it.each([
    ['Free'],
    ['Insured'],
    ['OutOfPocket'],
    ['Promotional'],
  ] as const)('renders badge for %s tiers', (type) => {
    render(<CostTierBadge type={type} />)

    expect(screen.getByText(type)).toBeInTheDocument()
  })
})
