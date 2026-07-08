import { useLocation, useNavigate } from 'react-router-dom'
// Reusable card component — replaces the inline ingredient markup
// that used to be written directly here (avoids duplicating the same JSX in two places).
// It has its own internal color dictionary, so the old `ratingColor` object
// that used to live in this file is no longer needed and has been removed.
import IngredientCard from '../components/IngredientCard'
// Same idea, but for ingredients the app couldn't find in the database —
// replaces the inline <div> that used to be written directly in the map() below.
import UnknownIngredientCard from '../components/UnknownIngredientCard'

// The order in which safety-rating sections appear on the page.
// Red first (prohibited — most important), then Amber (caution),
// then Green (regulator-approved), then Grey (no regulatory data).
// This array drives BOTH the order and the section titles below,
// so changing the order here changes the whole page — one place to edit.
const RATING_ORDER = ['Red', 'Amber', 'Green', 'Grey']

// Human-readable titles shown as the heading of each section.
// Keys match the safetyRating strings that come back from the API.
const RATING_TITLES = {
  Red: 'Red — avoid',
  Amber: 'Amber — caution',
  Green: 'Green — regulator-approved',
  Grey: 'Grey — no regulatory rating'
}

function ResultsPage() {
  // HOOK: useLocation()
  // This hook reads information about the current "page" (route) the user is on.
  // One piece of that information is `state` — data that was passed along
  // when navigate() was called to bring the user to this page.
  // Destructuring { state } immediately pulls out just that one field.
  const { state } = useLocation()

  // HOOK: useNavigate()
  // This hook gives us a function called `navigate`.
  // Calling navigate('/some-path') moves the user to another page
  // WITHOUT a full browser reload (this is how React apps handle page changes).
  const navigate = useNavigate()

  // Optional chaining (?.): if `state` exists, get its `results` field.
  // If `state` is undefined (e.g. user opened this URL directly, with no navigation history),
  // this safely returns undefined instead of throwing an error.
  const results = state?.results

  // Guard clause: if there's no data to show, stop here and render a fallback UI.
  // This is an early return — if this condition is true, none of the code below runs.
  if (!results) {
    return (
      <div>
        <p>No results found.</p>
        {/* Clicking this button calls navigate('/'), sending the user back to the home page */}
        <button onClick={() => navigate('/')}>Go Home</button>
      </div>
    )
  }

  // GROUPING STEP
  // Before rendering, we sort the flat list of ingredients into buckets
  // keyed by their safetyRating. This is what turns the plain list into
  // colour-grouped sections.
  //
  // We start with an empty bucket for every known rating so the order is
  // guaranteed and predictable:  { Red: [], Amber: [], Green: [], Grey: [] }
  const grouped = { Red: [], Amber: [], Green: [], Grey: [] }

  // Loop over every analysed ingredient and drop it into the right bucket.
  results.results.forEach((item) => {
    // If the rating is one we know about, push into that bucket.
    // If somehow an unexpected rating arrives, fall back to Grey so
    // nothing silently disappears from the page.
    if (grouped[item.safetyRating]) {
      grouped[item.safetyRating].push(item)
    } else {
      grouped.Grey.push(item)
    }
  })

  return (
    <div>
      <h2>Analysis Results</h2>
      {results.aiSummary && (
        <div style={{background: '#f5f5f5', borderLeft: '4px solid #2196f3', padding: '12px', marginBottom: '16px'}}>
          <strong>Product Insight</strong>
          <p>{results.aiSummary}</p>
        </div>  
      )}

      {/* SECTION RENDERING
          Instead of one flat .map() over all ingredients, we now loop over
          RATING_ORDER (Red, Amber, Green, Grey). For each rating we take its
          bucket from `grouped` and render a titled section — but only if that
          bucket actually has ingredients in it (so empty colours don't show
          an empty heading). */}
      {RATING_ORDER.map((rating) => {
        // Pull this rating's ingredients out of the grouped object.
        const items = grouped[rating]

        // If there are none of this colour, render nothing for this section.
        // Returning null from inside .map() means "skip this one".
        if (items.length === 0) return null

        return (
          <div key={rating} style={{marginBottom: '10px'}}>
            {/* Section heading, e.g. "Amber — caution".
                The count in brackets tells the user how many are in this group. */}
            <h3>{RATING_TITLES[rating]} ({items.length})</h3>

            {/* Now the familiar card loop, but only over THIS colour's items.
                We build a stable key from the ingredient name plus its index
                within this section, so React can track each card reliably. */}
            {items.map((item, index) => (
              <IngredientCard key={`${rating}-${item.inciName}-${index}`} ingredient={item} />
            ))}
          </div>
        )
      })}

      {/* Conditional rendering: only show this whole section if there's at least one unknown ingredient */}
      {results.unknownIngredients.length > 0 && (
        <div>
          <h3>Unknown Ingredients</h3>
          {/* Same pattern as above: loop through the array and render one
              UnknownIngredientCard per name, instead of writing the same
              <div> markup inline for every item */}
          {results.unknownIngredients.map((item, i) => (
            <UnknownIngredientCard key={i} name={item.name} aiLabel={item.aiLabel} confidence={item.confidence} />
          ))}
        </div>
      )}

      {/* Same navigate() pattern as above — sends the user back to the home page */}
      <button onClick={() => navigate('/')}>Analyse Another Product</button>
    </div>
  )
}

export default ResultsPage