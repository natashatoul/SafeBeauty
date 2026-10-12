import { useLayoutEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { analyseIngredients } from '../services/ingredientService'
import { useProfile } from '../context/ProfileContext'
import {
  hasLikelyMissingIngredientSeparators,
  parseIngredientList
} from '../utils/ingredientParsing'
import { getIngredientReviewGroups } from '../utils/ingredientReview'
import { saveAnalysis } from '../services/scanHistoryService'

function ManualInputPage() {
  const { state } = useLocation()
  const initialIngredients = Array.isArray(state?.initialIngredients)
    ? state.initialIngredients
    : parseIngredientList(state?.initialText ?? '')
  const initialText = initialIngredients.length > 0
    ? initialIngredients.join(',\n')
    : state?.initialText ?? ''
  const isBarcodeDraft = state?.draftSource === 'Open Beauty Facts'
  const lowCoverageReview = state?.lowCoverageReview
  const reviewResult = state?.reviewResult ?? state?.originalBarcodeResult

  // HOOK: useState('')
  // Creates a piece of state called "text", starting as an empty string.
  // useState returns an array with exactly 2 items:
  //   1) the current value (text)
  //   2) a function to update that value (setText)
  // Whenever setText() is called, React re-renders this component
  // so the screen reflects the new value.
  const [text, setText] = useState(initialText)
  const textareaRef = useRef(null)

  // HOOK: useState(false)
  // A second, separate piece of state — completely independent from "text".
  // Starts as false. Used as a flag: "is the analysis currently running?"
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')


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
  const reviewGroups = reviewResult?.results
    ? getIngredientReviewGroups(initialIngredients, reviewResult.results)
    : { matched: [], needsChecking: [] }

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
    setError('')

    try {
      // Call the service function and WAIT (await) for the result.
      // This is likely a network request to a server or API.
      const results = await analyseIngredients(ingredients, profile.conditions ?? [], {
        ageGroup: profile.ageGroup,
        gender: profile.gender
      })
      const analysisContext = {
        source: isBarcodeDraft ? 'Manual input (edited barcode list)' : 'Manual input',
        productName: state?.productName,
        barcode: state?.barcode,
        // Retain exactly what the user reviewed so the result can be reopened
        // and corrected again without retyping the package label.
        sourceIngredients: ingredients,
        selectedConditions: profile.conditions ?? [],
        ageGroup: profile.ageGroup,
        gender: profile.gender,
        skinType: profile.skinType,
        hairCondition: profile.hairCondition,
      }

      // History is a convenience feature: a storage failure must not block the
      // result page, so saveAnalysis returns null instead of throwing.
      const savedAnalysis = await saveAnalysis({ results, analysisContext })

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
      setError('Something went wrong. Please try again.')
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

  const viewBarcodeResultAnyway = async () => {
    if (!reviewResult?.results) return

    const savedAnalysis = await saveAnalysis({
      results: reviewResult.results,
      analysisContext: reviewResult.analysisContext ?? {}
    })

    navigate('/results', {
      state: {
        results: reviewResult.results,
        analysisContext: savedAnalysis?.analysisContext
          ?? reviewResult.analysisContext
          ?? {}
      }
    })
  }

  return (
    <div className="manual-page">
      <header className="manual-page-header">
        <h1>{isBarcodeDraft ? 'Edit Barcode Ingredients' : 'Enter Ingredients'}</h1>
        <p>
          {isBarcodeDraft
            ? 'These entries came from Open Beauty Facts. Edit spelling, separators, or split fragments before analysing again.'
            : 'Paste the ingredient list separated by commas, bullets, semicolons or new lines.'}
        </p>
      </header>

      {isBarcodeDraft && lowCoverageReview && (
        <section className="manual-review-warning" role="alert">
          <div>
            <strong>Check the barcode ingredient list</strong>
            <p>
              Only {lowCoverageReview.knownCount} of {lowCoverageReview.totalCount} barcode entries
              were matched to the verified database. The source may contain misspelled or incorrectly
              split ingredients, so review the list before analysing again.
            </p>
          </div>
          {reviewResult?.results && (
            <button type="button" className="secondary-button" onClick={viewBarcodeResultAnyway}>
              View barcode result anyway
            </button>
          )}
        </section>
      )}

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

        {reviewResult?.results && (
          <section className="barcode-draft-review" aria-label="Previous ingredient match review">
            <article className="barcode-draft-column barcode-draft-column--warning">
              <h3>Not matched ({reviewGroups.needsChecking.length})</h3>
              <p>These entries were not matched in the previous analysis. Check them against the product label.</p>
              <p className="barcode-draft-tip">
                Correct spelling and join broken fragments into full INCI names.
                If a word is split across two lines on the packaging with a hyphen, remove that hyphen:
                for example, PROPY- + LENE GLYCOL should become PROPYLENE GLYCOL.
                Avoid deleting entries unless you can confirm they are not ingredients.
              </p>
              {reviewGroups.needsChecking.length > 0 ? (
                <div className="barcode-draft-chip-list">
                  {reviewGroups.needsChecking.map((entry, index) => (
                    <span key={`needs-checking-${entry}-${index}`}>{entry}</span>
                  ))}
                </div>
              ) : (
                <p className="barcode-draft-empty">No unmatched entries in the current draft.</p>
              )}
            </article>

            <article className="barcode-draft-column">
              <h3>Matched ({reviewGroups.matched.length})</h3>
              <p>These entries were recognised or normalised in the previous analysis.</p>
              {reviewGroups.matched.length > 0 ? (
                <div className="barcode-draft-chip-list">
                  {reviewGroups.matched.slice(0, 16).map((entry, index) => (
                    <span key={`matched-${entry}-${index}`}>{entry}</span>
                  ))}
                  {reviewGroups.matched.length > 16 && (
                    <span>+{reviewGroups.matched.length - 16} more</span>
                  )}
                </div>
              ) : (
                <p className="barcode-draft-empty">No matched entries yet.</p>
              )}
            </article>
          </section>
        )}

        <div className="manual-form-actions">
          {error && (
            <p className="manual-form-error" role="alert">{error}</p>
          )}

          {text.trim() !== '' && (
            <p className="ingredient-count" role="status">
              {willInferIngredientBoundaries
                ? 'This list may have missing separators. SafeBeauty will try to split it automatically.'
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
