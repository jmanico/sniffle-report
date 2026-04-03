import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'

import { RegionProvider } from './RegionContext'
import { useRegion } from '../../hooks/useRegion'

function RegionProbe() {
  const { regionId, buildRegionPath } = useRegion()

  return (
    <div>
      <span>{regionId}</span>
      <span>{buildRegionPath('alerts')}</span>
    </div>
  )
}

describe('RegionProvider', () => {
  it('derives region state from the URL param', () => {
    render(
      <MemoryRouter initialEntries={['/region/travis-county-tx']}>
        <Routes>
          <Route
            element={
              <RegionProvider>
                <RegionProbe />
              </RegionProvider>
            }
            path="/region/:regionId"
          />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByText('travis-county-tx')).toBeInTheDocument()
    expect(screen.getByText('/region/travis-county-tx/alerts')).toBeInTheDocument()
  })
})
