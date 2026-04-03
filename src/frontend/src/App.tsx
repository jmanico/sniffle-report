import './App.css'

const statusCards = [
  {
    label: 'Frontend',
    value: 'Vite + React + TypeScript initialized',
  },
  {
    label: 'API Target',
    value: 'http://localhost:5000/api/v1',
  },
  {
    label: 'Next Focus',
    value: 'Region-driven public health dashboard',
  },
]

function App() {
  return (
    <main className="app-shell">
      <section className="hero">
        <span className="eyebrow">Sniffle Report</span>
        <h1>Regional health reporting, not a generic starter app.</h1>
        <p>
          This frontend is scaffolded for a region-scoped public health product:
          local alerts, trend views, prevention guidance, and admin-managed
          editorial workflows.
        </p>
        <div className="status-grid">
          {statusCards.map((card) => (
            <article className="status-card" key={card.label}>
              <strong>{card.label}</strong>
              <span>{card.value}</span>
            </article>
          ))}
        </div>
      </section>
    </main>
  )
}

export default App
