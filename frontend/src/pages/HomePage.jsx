import { useNavigate } from 'react-router-dom'

const recentAnalyses = [
  {
    name: 'Retinol test',
    date: '9 Jul 2026',
    ratings: [
      { label: 'Amber', count: 1, tone: 'amber' },
      { label: 'Green', count: 2, tone: 'green' },
      { label: 'Grey', count: 4, tone: 'grey' }
    ]
  },
  {
    name: 'Retinol serum',
    date: 'Today',
    ratings: [
      { label: 'Amber', count: 1, tone: 'amber' },
      { label: 'Green', count: 2, tone: 'green' },
      { label: 'Grey', count: 4, tone: 'grey' }
    ]
  }
]

function HomePage() {
  const navigate = useNavigate()

  return (
    <div className="home-page">
      <section className="hero-section">
        <h1>Know what’s really in your products.</h1>
        <p>
          Scan a barcode or paste ingredients to check them against your selected skin profile.
        </p>
      </section>

      <section className="action-grid" aria-label="Start a product analysis">
        <article className="action-card">
          <div>
            <h2>Scan a product</h2>
            <p>Use your camera to read barcode instantly.</p>
          </div>
          <button type="button" onClick={() => navigate('/scan')}>
            Open scanner →
          </button>
        </article>

        <article className="action-card">
          <div>
            <h2>Enter ingredients manually</h2>
            <p>Paste or type an ingredient list instead.</p>
          </div>
          <button type="button" onClick={() => navigate('/manual')}>
            Enter list →
          </button>
        </article>
      </section>

      <section className="recent-section">
        <div className="section-header">
          <h2>Recent analyses</h2>
          <button className="show-all-button" type="button">
            Show all →
          </button>
        </div>

        <div className="recent-list">
          {recentAnalyses.map((item) => (
            <article className="recent-row" key={`${item.name}-${item.date}`}>
              <div className="recent-product">
                <h3>{item.name}</h3>
                <p>{item.date}</p>
              </div>

              <div className="rating-pills" aria-label={`Rating summary for ${item.name}`}>
                {item.ratings.map((rating) => (
                  <span className={`rating-pill ${rating.tone}`} key={rating.label}>
                    {rating.label} {rating.count}
                  </span>
                ))}
              </div>

              <button
                className="text-button"
                type="button"
                onClick={() => navigate('/manual')}
              >
                View result →
              </button>
            </article>
          ))}
        </div>

      </section>
    </div>
  )
}

export default HomePage
