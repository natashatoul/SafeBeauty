import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AnalysisHistoryRow from '../components/AnalysisHistoryRow'
import { clearHistory, getHistory, removeAnalysis } from '../utils/analysisHistory'

function HistoryPage() {
  const navigate = useNavigate()
  const [history, setHistory] = useState(getHistory)

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

  const deleteAllAnalyses = () => {
    const confirmed = window.confirm('Delete all saved analyses from this browser?')
    if (confirmed && clearHistory()) setHistory([])
  }

  return (
    <div className="history-page">
      <header className="history-header">
        <div>
          <h1>Analysis history</h1>
          <p>Return to previous formula results saved on this device.</p>
        </div>
        {history.length > 0 && (
          <button
            className="secondary-button history-clear-button"
            type="button"
            onClick={deleteAllAnalyses}
          >
            Clear history
          </button>
        )}
      </header>

      <p className="history-storage-note">
        Analysis history is stored only in this browser and is never sent to our servers.
      </p>

      {history.length > 0 ? (
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
    </div>
  )
}

export default HistoryPage
