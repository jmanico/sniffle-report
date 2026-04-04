import { Link } from 'react-router-dom'

import { useRegions, useRegionSearch } from '../../hooks/useRegions'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { validateAndSanitizeUrl } from '../../utils/validateAndSanitizeUrl'
import { formatRegionHierarchy } from './formatRegionHierarchy'
import { useState } from 'react'

type RegionSearchPanelProps = {
  currentRegionId?: string
  currentRegionLabel?: string
  heading: string
  description: string
  className?: string
  inputLabel?: string
}

export function RegionSearchPanel({
  currentRegionId,
  currentRegionLabel,
  heading,
  description,
  className,
  inputLabel = 'Search regions',
}: RegionSearchPanelProps) {
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query, 300)
  const searchEnabled = debouncedQuery.trim().length > 0
  const regionSearch = useRegionSearch(
    {
      q: debouncedQuery.trim(),
      page: 1,
      pageSize: 8,
    },
    searchEnabled,
  )
  const regionIndex = useRegions(
    {
      page: 1,
      pageSize: 100,
    },
    searchEnabled,
  )

  const regionMap = new Map(regionIndex.data?.items.map((region) => [region.id, region]))

  return (
    <section className={className}>
      <div className="region-search__intro">
        <span className="section-kicker">{heading}</span>
        <strong>{description}</strong>
        {currentRegionLabel ? (
          <p className="region-search__current">
            Current region: <span>{currentRegionLabel}</span>
          </p>
        ) : null}
      </div>

      <label className="region-search__field">
        <span className="region-search__label">{inputLabel}</span>
        <input
          autoComplete="off"
          className="region-search__input"
          name="region-search"
          onChange={(event) => {
            setQuery(event.target.value)
          }}
          placeholder="Type a county, metro, zip, or state"
          type="search"
          value={query}
        />
      </label>

      <div aria-live="polite" className="region-search__results">
        {!searchEnabled ? (
          <p className="region-search__hint">
            Search results appear after a short 300ms debounce so the app does not hit the API on every keystroke.
          </p>
        ) : null}

        {regionSearch.isLoading ? <p className="region-search__state">Searching regions…</p> : null}

        {regionSearch.isError ? (
          <p className="region-search__state region-search__state--error">
            Unable to load regions right now. Try again in a moment.
          </p>
        ) : null}

        {searchEnabled && !regionSearch.isLoading && !regionSearch.isError && regionSearch.data?.items.length === 0 ? (
          <p className="region-search__state">No regions matched that search.</p>
        ) : null}

        {regionSearch.data?.items.length ? (
          <div className="region-search__list" role="list">
            {regionSearch.data.items.map((region) => {
              const isCurrentRegion = region.id === currentRegionId

              return (
                <Link
                  aria-current={isCurrentRegion ? 'page' : undefined}
                  className={isCurrentRegion ? 'region-search__option region-search__option--active' : 'region-search__option'}
                  key={region.id}
                  to={validateAndSanitizeUrl(`/region/${region.id}`)}
                >
                  <span className="region-search__name">{region.name}</span>
                  <span className="region-search__meta">{formatRegionHierarchy(region, regionMap)}</span>
                </Link>
              )
            })}
          </div>
        ) : null}
      </div>
    </section>
  )
}
