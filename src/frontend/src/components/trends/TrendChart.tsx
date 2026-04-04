import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import type { TrendDataPoint } from '../../api/types'

function formatAxisDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
  }).format(new Date(date))
}

function formatTooltipDate(date: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
}

type TrendChartProps = {
  dataPoints: TrendDataPoint[]
}

export function TrendChart({ dataPoints }: TrendChartProps) {
  const chartData = dataPoints.map((point) => ({
    ...point,
    shortDate: formatAxisDate(point.date),
  }))

  return (
    <div aria-label="Trend chart" className="trend-chart" role="img">
      <ResponsiveContainer height={280} width="100%">
        <LineChart data={chartData} margin={{ top: 16, right: 12, left: 0, bottom: 8 }}>
          <CartesianGrid stroke="rgba(16, 45, 61, 0.1)" strokeDasharray="4 4" />
          <XAxis dataKey="shortDate" stroke="#536774" tickLine={false} axisLine={false} />
          <YAxis allowDecimals={false} stroke="#536774" tickLine={false} axisLine={false} />
          <Tooltip
            contentStyle={{
              borderRadius: '12px',
              border: '1px solid rgba(16, 45, 61, 0.1)',
              background: 'rgba(255, 255, 255, 0.98)',
            }}
            formatter={(value, _name, payload) => {
              return [`${value} cases`, `${payload.payload.source}`]
            }}
            labelFormatter={(label, payload) => {
              const current = payload?.[0]?.payload
              return current ? `${formatTooltipDate(current.date)} · ${current.source}` : label
            }}
          />
          <Line
            type="monotone"
            dataKey="caseCount"
            dot={{ fill: '#be5b2a', r: 4 }}
            name="Case count"
            stroke="#be5b2a"
            strokeWidth={3}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}
