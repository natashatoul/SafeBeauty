import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AnalysisHistoryRow from '../components/AnalysisHistoryRow'
import { useAuth } from '../context/AuthContext'
import { clearHistory, getHistory, removeAnalysis } from '../services/scanHistoryService'

function HistoryPage() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [history, setHistory] = useState([])
  const [loadError, setLoadError] = useState(false)
  const [actionError, setActionError] = useState('')


  useEffect(() => {
    if (!isAuthenticated) {
      setHistory([])
      setLoadError(false)
      return
    }

    getHistory().then((items) => {
      if (items === null) {
        setLoadError(true)
      } else {
        setHistory(items)
        setLoadError(false)
      }
    })
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
  const ok = await removeAnalysis(id)
  if (!ok) {
    setActionError('Could not delete this entry. Please try again.')
    return
  }

  const items = await getHistory()
  if (items === null) {
    setActionError('Deleted, but could not refresh the list. Please reload the page.')
  } else {
    setHistory(items)
    setActionError('')
  }
}


  const deleteAllAnalyses = async () => {
    const confirmed = window.confirm('Delete all saved analyses?')
    if (!confirmed) return

    const ok = await clearHistory()
    if (ok) {
      setHistory([])
      setActionError('')
    } else {
      setActionError('Could not clear history. Please try again.')
    }
  }
  


  return (
    <div className="history-page">
      <header className="history-header">
        <div>
          <h1>Analysis history</h1>
          <p>Return to previous formula results saved to your account.</p>
        </div>
        {isAuthenticated && history.length > 0 && (
          <button
            className="secondary-button history-clear-button"
            type="button"
            onClick={deleteAllAnalyses}
          >
            Clear history
          </button>
        )}
            </header>

      {actionError && (
        <p className="history-action-error" role="alert">{actionError}</p>
      )}

      {isAuthenticated ? (

                <>
          {loadError ? (
            <section className="history-empty">
              <h2>Could not load your history</h2>
              <p>Something went wrong while fetching your saved analyses. Please try again later.</p>
            </section>
          ) : history.length > 0 ? (
            <div className="history-list">
              {history.map((item) => (
                <AnalysisHistoryRow
                  item={item}
                  key={item.id}
                  onView={viewAnalysis}
                  onDelete={deleteAnalysis}
                />
              ))}
            </div>
          ) : (
            <section className="history-empty">
              <h2>No saved analyses yet</h2>
              <p>Your successful barcode scans and manual ingredient analyses will appear here.</p>
              <button type="button" onClick={() => navigate('/manual')}>
                Analyse ingredients
              </button>
            </section>
          )}


          <p className="history-storage-note">
            * Saved to your account. Skin and hair conditions used for personalisation are not included in the saved record.
          </p>
        </>
      ) : (
        <div className="profile-account-section">
          <p>Log in to view your saved analyses and sync them across devices.</p>
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
    </div>
  )
}

export default HistoryPage
