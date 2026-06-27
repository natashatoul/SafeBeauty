import { useLocation, useNavigate } from 'react-router-dom'

const ratingColor = {
  Green: 'green',
  Amber: 'orange',
  Red: 'red',
  Grey: 'grey'
}

function ResultsPage() {
  const { state } = useLocation()
  const navigate = useNavigate()
  const results = state?.results

  if (!results) {
    return (
      <div>
        <p>No results found.</p>
        <button onClick={() => navigate('/')}>Go Home</button>
      </div>
    )
  }

  return (
    <div>
      <h2>Analysis Results</h2>

      {results.results.map((item, index) => (
        <div key={index} style={{ borderLeft: `4px solid ${ratingColor[item.safetyRating] || 'grey'}`, padding: '8px', marginBottom: '8px' }}>
          <strong>{item.inciName}</strong>
          <span> — {item.safetyRating}</span>
          {item.function && <p>Function: {item.function}</p>}
          {item.category && <p>Category: {item.category}</p>}
        </div>
      ))}

      {results.unknownIngredients.length > 0 && (
        <div>
          <h3>Unknown Ingredients</h3>
          {results.unknownIngredients.map((name, i) => (
            <div key={i} style={{ borderLeft: '4px solid grey', padding: '8px', marginBottom: '8px' }}>
              <strong>{name}</strong> — Not found in database
            </div>
          ))}
        </div>
      )}

      <button onClick={() => navigate('/')}>Analyse Another Product</button>
    </div>
  )
}

export default ResultsPage