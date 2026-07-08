import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { analyseIngredients } from '../services/ingredientService'
import { useProfile } from '../context/ProfileContext'

function ManualInputPage() {
  // HOOK: useState('')
  // Creates a piece of state called "text", starting as an empty string.
  // useState returns an array with exactly 2 items:
  //   1) the current value (text)
  //   2) a function to update that value (setText)
  // Whenever setText() is called, React re-renders this component
  // so the screen reflects the new value.
  const [text, setText] = useState('')

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

  // This function runs when the user clicks the "Analyse" button.
  // It's async because it needs to wait for a network request to finish.
  const handleAnalyse = async () => {
    // Turn the raw text into a clean array of ingredient names:
    // 1. split(',')   -> break the string into pieces wherever there's a comma
    // 2. map(trim)    -> remove extra spaces from each piece
    // 3. filter(...)  -> drop any empty strings (e.g. from a trailing comma)
    const ingredients = text.split(',').map(i => i.trim()).filter(i => i !== '')

    // Update the "loading" state to true.
    // This triggers a re-render, so the UI can show "Analysing..." on the button.
    setLoading(true)

    try {
      // Call the service function and WAIT (await) for the result.
      // This is likely a network request to a server or API.
      const results = await analyseIngredients(ingredients, profile.conditions ?? [])

      // Move to the "/results" route, carrying the results data along.
      // { state: { results } } attaches this data to the navigation —
      // the next page can read it back out via useLocation().
      navigate('/results', { state: { results } })
    } catch (error) {
      // If analyseIngredients() throws an error (e.g. network failure),
      // this block runs instead of crashing the app.
      alert('Something went wrong. Please try again.')
    } finally {
      // This runs no matter what — whether it succeeded or failed.
      // Reset "loading" back to false, since the analysis attempt is over.
      setLoading(false)
    }
  }

  return (
    <div>
      <h2>Enter Ingredients</h2>
      <p>Paste the ingredient list from your product, separated by commas.</p>

      <textarea
        // "Controlled" textarea: its visible content is always
        // exactly whatever is stored in the "text" state.
        value={text}
        // Every time the user types a character, this fires.
        // e.target.value is the textarea's current content.
        // setText(...) updates the state, which re-renders the textarea
        // with the new value.
        onChange={(e) => setText(e.target.value)}
        placeholder="e.g. Aqua, Glycerin, Niacinamide..."
        rows={6}
        cols={50}
      />
      <br />

      <button
        onClick={handleAnalyse}
        // Button is disabled (greyed out, unclickable) if EITHER:
        // - loading is true (an analysis is already in progress), OR
        // - the trimmed text is empty (nothing to analyse)
        disabled={loading || text.trim() === ''}
      >
        {/* Ternary: if loading is true show "Analysing...", otherwise show "Analyse" */}
        {loading ? 'Analysing...' : 'Analyse'}
      </button>
    </div>
  )
}

export default ManualInputPage
