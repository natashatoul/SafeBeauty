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

function formatConditionName(condition = '') {
  return condition.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function getProfileRuleExplanation(flag) {
  if (flag.flagType === 'Beneficial') {
    return 'Its broader cosmetic category is marked as potentially relevant in the profile rule set. This does not prove that the ingredient or finished product benefits the condition.'
  }

  if (flag.flagType === 'Avoid') {
    return 'Its broader cosmetic category is flagged as a potential concern for this profile.'
  }

  return 'Its broader cosmetic category has a caution rule for this profile.'
}

// IngredientCard - UI component it is a pattern of a card and ingredient - 
// it/s object with data(name. rating. categoria)
function IngredientCard({ ingredient }) { // it is a component and it is like custom HTML. ingredient - it is a prop
    const displayFunctions = getDisplayFunctions(ingredient.function)
    const displayCategories = getDisplayCategories(ingredient.category)
    const showRatingLabel = ingredient.safetyRating && ingredient.safetyRating !== 'Grey'
    const ratingTone = ['Green', 'Amber', 'Red'].includes(ingredient.safetyRating)
      ? ingredient.safetyRating.toLowerCase()
      : 'grey'

    return (
      <article className={`technical-ingredient technical-ingredient--${ratingTone}`}>
        <div className="technical-ingredient-heading">
            <strong>{ingredient.inciName}</strong>
          {showRatingLabel && <span>{ingredient.safetyRating}</span>}
        </div>
        {(displayFunctions || displayCategories) && (
          <div className="technical-ingredient-details">
            {displayFunctions && <p>Most relevant listed functions: {displayFunctions}</p>}
            {displayCategories && <p>Category: {displayCategories}</p>}
          </div>
        )}
        {ingredient.conditionFlags?.length > 0 && (
          <ul className="technical-flags">
            {ingredient.conditionFlags.map((flag, index) => (
              <li key={`${flag.condition}-${flag.flagType}-${index}`}>
                <strong>{formatConditionName(flag.condition)}: {flag.flagType}</strong>
                <p>{getProfileRuleExplanation(flag)}</p>
                {flag.evidenceSource && (
                  <small className="rule-source">Rule source: {flag.evidenceSource}</small>
                )}
              </li>
            ))}
          </ul>
        )}
      </article>
    )
}

export default IngredientCard
