const FUNCTION_PRIORITY = [
  'UV FILTER',
  'PRESERVATIVE',
  'HUMECTANT',
  'SKIN CONDITIONING - EMOLLIENT',
  'SKIN CONDITIONING - HUMECTANT',
  'SKIN CONDITIONING',
  'SKIN PROTECTING',
  'ANTIOXIDANT',
  'SOOTHING',
  'CLEANSING',
  'SURFACTANT - CLEANSING',
  'SURFACTANT - EMULSIFYING',
  'EMULSION STABILISING',
  'CHELATING',
  'BUFFERING',
  'SOLVENT',
  'VISCOSITY CONTROLLING'
]

const LOW_CONTEXT_FUNCTIONS = new Set([
  'HAIR CONDITIONING',
  'ORAL CARE',
  'PERFUMING',
  'FRAGRANCE',
  'DENATURANT'
])

const LOW_CONTEXT_CATEGORIES = new Set([
  'EU Glossary Ingredient'
])

function getDisplayFunctions(functionText = '') {
  const functions = functionText
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)

  if (functions.length === 0) return ''

  const filtered = functions.filter((item) => !LOW_CONTEXT_FUNCTIONS.has(item.toUpperCase()))
  const usefulFunctions = filtered.length > 0 ? filtered : functions

  return usefulFunctions
    .sort((a, b) => {
      const aIndex = FUNCTION_PRIORITY.indexOf(a.toUpperCase())
      const bIndex = FUNCTION_PRIORITY.indexOf(b.toUpperCase())
      return (aIndex === -1 ? 999 : aIndex) - (bIndex === -1 ? 999 : bIndex)
    })
    .slice(0, 3)
    .join(', ')
}

function getDisplayCategories(categoryText = '') {
  return categoryText
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)
    .filter((item) => !LOW_CONTEXT_CATEGORIES.has(item))
    .join(', ')
}

// IngredientCard - UI component it is a pattern of a card and ingredient - 
// it/s object with data(name. rating. categoria)
function IngredientCard({ ingredient }) { // it is a component and it is like custom HTML. ingredient - it is a props
    const borderColor = {
        Green: '#4caf50',
        Amber: '#ff9800',
        Red: '#f44336',
        Grey: '#9e9e9e'
    };

    const color = borderColor[ingredient.safetyRating] || '#9e9e9e';
    const displayFunctions = getDisplayFunctions(ingredient.function)
    const displayCategories = getDisplayCategories(ingredient.category)
    const showRatingLabel = ingredient.safetyRating && ingredient.safetyRating !== 'Grey'

    return (
        <div style={{ borderLeft: `4px solid ${color}`, padding: '5px', marginBottom: '10px', paddingLeft: '20px' }}>
            <strong>{ingredient.inciName}</strong>
            {showRatingLabel && <span> — {ingredient.safetyRating}</span>}
            {displayFunctions && <p>Most relevant listed functions: {displayFunctions}</p>}
            {displayCategories && <p>Category: {displayCategories}</p>}
            {ingredient.conditionFlags?.length > 0 && (
                // .map() goes through each item in the conditionFlags array (this list already exists inside ingredient)
                // for each flag, it creates one <li> and shows 3 of its fields: condition, flagType, notes
                // i is just the index number of the flag (0, 1, 2...), React needs it for the key, to track list items
                // color has nothing to do with this line — color is only used once, for the border of the whole card
                <ul>
                    {ingredient.conditionFlags.map((flag, i) => (
                        <li key={i}>{flag.condition}: {flag.flagType} — {flag.notes}</li>
                    ))}
                </ul>
            )}
        </div>
    );
}

export default IngredientCard
