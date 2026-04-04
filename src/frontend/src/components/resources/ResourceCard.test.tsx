import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'

import { ResourceCard } from './ResourceCard'

describe('ResourceCard', () => {
  it('renders validated resource details', () => {
    render(
      <MemoryRouter>
        <ResourceCard
          href="/region/test/resources/1"
          resource={{
            id: '1f1ecb6b-c7dc-4142-bdc7-bff8cb6c57e4',
            regionId: '8fb8cb1d-6622-4c13-a0b8-f6ccebc5454b',
            name: 'Austin Public Health Clinic',
            type: 'Clinic',
            address: '123 Main St',
            phone: '(512) 555-0199',
            website: 'https://example.org',
            latitude: 30.2672,
            longitude: -97.7431,
            distanceMiles: 2.4,
          }}
        />
      </MemoryRouter>,
    )

    expect(screen.getByText(/austin public health clinic/i)).toBeInTheDocument()
    expect(screen.getByText('Clinic')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /website/i })).toHaveAttribute('href', 'https://example.org/')
    expect(screen.getByRole('link', { name: /\(512\) 555-0199/i })).toHaveAttribute('href', 'tel:5125550199')
  })
})
