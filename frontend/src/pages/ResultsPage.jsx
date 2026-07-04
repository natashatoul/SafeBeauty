import { useLocation, useNavigate } from 'react-router-dom'
// Reusable card component — replaces the inline ingredient markup
// that used to be written directly here (avoids duplicating the same JSX in two places).
// It has its own internal color dictionary, so the old `ratingColor` object
// that used to live in this file is no longer needed and has been removed.
import IngredientCard from '../components/IngredientCard'
// Same idea, but for ingredients the app couldn't find in the database —
// replaces the inline <div> that used to be written directly in the map() below.
import UnknownIngredientCard from '../components/UnknownIngredientCard'

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

  return (
    <div>
      <h2>Analysis Results</h2>

      {/* .map() loops through each analyzed ingredient in results.results.
          Each one is rendered by IngredientCard, which handles its own
          layout, border color, and conditional fields (function, category, flags) —
          this used to be written out inline here, but that duplicated the
          exact same markup already present in IngredientCard.jsx */}
      {results.results.map((item, index) => (
        <IngredientCard key={index} ingredient={item} />
      ))}

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