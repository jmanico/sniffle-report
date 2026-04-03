import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import App from './App'

describe('App', () => {
  it('renders the landing page headline', () => {
    render(<App />)

    expect(
      screen.getByRole('heading', {
        name: /regional health intelligence shaped by the place you actually live in/i,
      }),
    ).toBeInTheDocument()
  })
})
