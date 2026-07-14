import { useLayoutEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { analyseIngredients } from '../services/ingredientService'
import { useProfile } from '../context/ProfileContext'
import {
  hasLikelyMissingIngredientSeparators,
  parseIngredientList
} from '../utils/ingredientParsing'
import { saveAnalysis } from '../utils/analysisHistory'

function ManualInputPage() {
  // HOOK: useState('')
  // Creates a piece of state called "text", starting as an empty string.
  // useState returns an array with exactly 2 items:
  //   1) the current value (text)
  //   2) a function to update that value (setText)
  // Whenever setText() is called, React re-renders this component
  // so the screen reflects the new value.
  const [text, setText] = useState('')
  const textareaRef = useRef(null)

  // HOOK: useState(false)
  // A second, separate piece of state — completely independent from "text".
  // Starts as false. Used as a flag: "is the analysis currently running?"
  const [loading, setLoading] = useState(false)

  // HOOK: useNavigate()
  // Gives us a function to move the user to a different route/page
  // without a full page reload.
  const navigate = useNavigate()

  // Read the locally saved profile so selected conditions can personalise
  // backend condition flags.
  const { profile } = useProfile()
  const detectedIngredients = parseIngredientList(text)
  const willInferIngredientBoundaries = hasLikelyMissingIngredientSeparators(
    text,
    detectedIngredients
  )

  useLayoutEffect(() => {
    const textarea = textareaRef.current
    if (!textarea) return

    textarea.style.height = 'auto'
    textarea.style.height = `${textarea.scrollHeight}px`
  }, [text])

  // This function runs when the user clicks the "Analyse" button.
  // It's async because it needs to wait for a network request to finish.
  const handleAnalyse = async () => {
    // Turn common label formats (commas, bullets, semicolons or lines)
    // into a clean array while preserving slashes inside valid INCI names.
    const ingredients = detectedIngredients

    // Update the "loading" state to true.
    // This triggers a re-render, so the UI can show "Analysing..." on the button.
    setLoading(true)

    try {
      // Call the service function and WAIT (await) for the result.
      // This is likely a network request to a server or API.
      const results = await analyseIngredients(ingredients, profile.conditions ?? [], {
        ageGroup: profile.ageGroup,
        gender: profile.gender
      })
      const analysisContext = {
        source: 'Manual input',
        selectedConditions: profile.conditions ?? [],
        ageGroup: profile.ageGroup,
        gender: profile.gender
      }

      // History is a convenience feature: a storage failure must not block the
      // result page, so saveAnalysis returns null instead of throwing.
      const savedAnalysis = saveAnalysis({ results, analysisContext })

      // Move to the "/results" route, carrying the results data along.
      // { state: { results } } attaches this data to the navigation —
      // the next page can read it back out via useLocation().
      navigate('/results', {
        state: {
          results,
          analysisContext: savedAnalysis?.analysisContext ?? analysisContext
        }
      })
    } catch {
      // If analyseIngredients() throws an error (e.g. network failure),
      // this block runs instead of crashing the app.
      alert('Something went wrong. Please try again.')
    } finally {
      // This runs no matter what — whether it succeeded or failed.
      // Reset "loading" back to false, since the analysis attempt is over.
      setLoading(false)
    }
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    await handleAnalyse()
  }

  return (
    <div className="manual-page">
      <header className="manual-page-header">
        <h1>Enter Ingredients</h1>
        <p>Paste the ingredient list separated by commas, bullets, semicolons or new lines.</p>
      </header>

      <form className="manual-form-card" onSubmit={handleSubmit}>
        <label className="manual-input-label" htmlFor="ingredient-list">Ingredient list</label>
        <textarea
          ref={textareaRef}
          id="ingredient-list"
          className="manual-textarea"
          value={text}
          onChange={(event) => setText(event.target.value)}
          placeholder="e.g. Aqua, Glycerin, Niacinamide..."
          rows={8}
        />

        <div className="manual-form-actions">
          {text.trim() !== '' && (
            <p className="ingredient-count" role="status">
              {willInferIngredientBoundaries
                ? 'Ingredient boundaries will be detected during analysis'
                : `${detectedIngredients.length} ingredient${detectedIngredients.length === 1 ? '' : 's'} detected`}
            </p>
          )}
          <button
            type="submit"
            className="primary-button"
            disabled={loading || text.trim() === ''}
          >
            {loading ? 'Analysing...' : 'Analyse ingredients'}
          </button>
        </div>
      </form>
    </div>
  )
}

export default ManualInputPage
