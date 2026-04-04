import type { TrendDataPoint } from '../../api/types'

function formatDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
}

export function CaseCountTable({ dataPoints }: { dataPoints: TrendDataPoint[] }) {
  return (
    <div className="alert-trend-table" role="table" aria-label="Case count table">
      <div className="alert-trend-table__head" role="row">
        <span role="columnheader">Date</span>
        <span role="columnheader">Cases</span>
        <span role="columnheader">Source</span>
      </div>
      {dataPoints.map((trend) => (
        <div className="alert-trend-table__row" key={`${trend.date}-${trend.source}`} role="row">
          <span role="cell">{formatDate(trend.date)}</span>
          <span role="cell">{trend.caseCount}</span>
          <span role="cell">{trend.source}</span>
        </div>
      ))}
    </div>
  )
}
