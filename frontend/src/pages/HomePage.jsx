import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AnalysisHistoryRow from '../components/AnalysisHistoryRow'
import { useAuth } from '../context/AuthContext'
import { getHistory, removeAnalysis } from '../services/scanHistoryService'

function HomePage() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [history, setHistory] = useState([])
  const recentAnalyses = history.slice(0, 5)

    useEffect(() => {
    if (!isAuthenticated) {
      setHistory([])
      return
    }

    getHistory().then(setHistory)
  }, [isAuthenticated])

  const viewAnalysis = (item) => {
    navigate('/results', {
      state: {
        results: item.results,
        analysisContext: item.analysisContext
      }
    })
  }

  const deleteAnalysis = async (id) => {
    if (await removeAnalysis(id)) setHistory(await getHistory())
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
        {isAuthenticated ? (
          <>
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
        </>
        ) : (
          <div className="profile-account-section">
            <p>Log in to see your recent analyses and sync them across devices.</p>
            <div className="profile-account-actions">
              <button type="button" className="primary-button" onClick={() => navigate('/login')}>
                Log in
              </button>
              <button type="button" className="secondary-button" onClick={() => navigate('/register')}>
                Create account
              </button>
            </div>
          </div>
        )}

      </section>
    </div>
  )
}

export default HomePage
