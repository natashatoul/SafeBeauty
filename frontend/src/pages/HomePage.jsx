import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AnalysisHistoryRow from '../components/AnalysisHistoryRow'
import { getHistory, removeAnalysis } from '../utils/analysisHistory'

function HomePage() {
  const navigate = useNavigate()
  const [history, setHistory] = useState(getHistory)
  const recentAnalyses = history.slice(0, 5)

  const viewAnalysis = (item) => {
    navigate('/results', {
      state: {
        results: item.results,
        analysisContext: item.analysisContext
      }
    })
  }

  const deleteAnalysis = (id) => {
    if (removeAnalysis(id)) setHistory(getHistory())
  }

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
          <button
            className="show-all-button"
            type="button"
            onClick={() => navigate('/history')}
          >
            Show all →
          </button>
        </div>

        <div className="recent-list">
          {recentAnalyses.length > 0
            ? recentAnalyses.map((item) => (
              <AnalysisHistoryRow
                item={item}
                key={item.id}
                onView={viewAnalysis}
                onDelete={deleteAnalysis}
              />
            ))
            : (
              <div className="recent-empty">
                <p>Your successful analyses will appear here.</p>
              </div>
            )}
        </div>

      </section>
    </div>
  )
}

export default HomePage
