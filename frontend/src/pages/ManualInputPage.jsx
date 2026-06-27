import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { analyseIngredients } from '../services/ingredientService'

function ManualInputPage() {
  const [text, setText] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  const handleAnalyse = async () => {
    const ingredients = text.split(',').map(i => i.trim()).filter(i => i !== '')
    setLoading(true)
    try {
      const results = await analyseIngredients(ingredients)
      navigate('/results', { state: { results } })
    } catch (error) {
      alert('Something went wrong. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <h2>Enter Ingredients</h2>
      <p>Paste the ingredient list from your product, separated by commas.</p>
      <textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder="e.g. Aqua, Glycerin, Niacinamide..."
        rows={6}
        cols={50}
      />
      <br />
      <button onClick={handleAnalyse} disabled={loading || text.trim() === ''}>
        {loading ? 'Analysing...' : 'Analyse'}
      </button>
    </div>
  )
}

export default ManualInputPage