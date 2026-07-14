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
          Scan a barcode or paste a full ingredient list to understand what a formula contains,
          highlight ingredients that may matter for your selected profile, and review the result in
          clear, practical language.
        </p>
      </section>

      <section className="action-grid" aria-labelledby="start-analysis-heading">
        <h2 className="visually-hidden" id="start-analysis-heading">Start a product analysis</h2>
        <article className="action-card">
          <div>
            <h3>Scan a product</h3>
            <p>Use your camera to read barcode instantly.</p>
          </div>
          <button type="button" onClick={() => navigate('/scan')}>
            Open scanner →
          </button>
        </article>

        <article className="action-card">
          <div>
            <h3>Enter ingredients manually</h3>
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
