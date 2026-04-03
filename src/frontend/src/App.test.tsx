import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import App from './App'

describe('App', () => {
  it('renders the Sniffle Report heading', () => {
    render(<App />)

    expect(
      screen.getByRole('heading', {
        name: /regional health reporting, not a generic starter app/i,
      }),
    ).toBeInTheDocument()
  })
})
