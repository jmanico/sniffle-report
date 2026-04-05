import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import App from './App'

describe('App', () => {
  it('renders the landing page headline', async () => {
    render(<App />)

    expect(
      await screen.findByRole('heading', {
        name: /community health data for every us county/i,
      }),
    ).toBeInTheDocument()
  })
})
