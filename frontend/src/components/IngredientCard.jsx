import { getPlainLanguageFunctionDescription } from '../utils/cosmeticFunctionDescriptions'


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

function getDisplayFunctions(functions = [], legacyFunctionText = '') {
  const normalizedFunctions = Array.isArray(functions) ? functions : []
  const availableFunctions = normalizedFunctions.length > 0
    ? normalizedFunctions
    : legacyFunctionText
      .split(',')
      .map((name) => ({ name: name.trim(), description: '' }))
      .filter((item) => item.name)

  if (availableFunctions.length === 0) return []

  const filtered = availableFunctions.filter((item) => !LOW_CONTEXT_FUNCTIONS.has(item.name.toUpperCase()))
  const usefulFunctions = filtered.length > 0 ? filtered : availableFunctions

  return [...usefulFunctions]
    .sort((a, b) => {
      const aIndex = FUNCTION_PRIORITY.indexOf(a.name.toUpperCase())
      const bIndex = FUNCTION_PRIORITY.indexOf(b.name.toUpperCase())
      return (aIndex === -1 ? 999 : aIndex) - (bIndex === -1 ? 999 : bIndex)
    })
    .slice(0, 3)
}

function getPurposeLabel({ supportsSkin, supportsFormula }) {
  if (supportsSkin && supportsFormula) return 'Supports both the skin and the formula'
  if (supportsSkin) return 'Supports the skin'
  if (supportsFormula) return 'Supports the formula'
  return 'General cosmetic function'
}

function positionFunctionTooltip(detailsElement) {
  window.requestAnimationFrame(() => {
    const tooltip = detailsElement.querySelector('.function-info-tooltip')
    if (!tooltip || window.getComputedStyle(tooltip).display === 'none') return

    const viewportPadding = 12

    tooltip.style.left = '0px'
    tooltip.style.right = 'auto'
    tooltip.style.top = 'auto'
    tooltip.style.bottom = 'calc(100% + 8px)'

    const initialRect = tooltip.getBoundingClientRect()
    const maximumLeft = Math.max(
      viewportPadding,
      window.innerWidth - initialRect.width - viewportPadding
    )
    const clampedLeft = Math.min(
      Math.max(initialRect.left, viewportPadding),
      maximumLeft
    )

    tooltip.style.left = `${clampedLeft - initialRect.left}px`

    if (tooltip.getBoundingClientRect().top < viewportPadding) {
      tooltip.style.top = 'calc(100% + 8px)'
      tooltip.style.bottom = 'auto'
    }
  })
}

function IngredientCard({ ingredient, chemicalDescriptions = [] }) {
    const displayFunctions = getDisplayFunctions(ingredient.functions, ingredient.function)
    const ratingTone = ['Green', 'Amber', 'Red'].includes(ingredient.safetyRating)
      ? ingredient.safetyRating.toLowerCase()
      : 'grey'

    return (
      <article className={`technical-ingredient technical-ingredient--${ratingTone}`}>
        <div className="technical-ingredient-heading">
          <strong>{ingredient.inciName}</strong>
        </div>
        {displayFunctions.length > 0 && (
          <div className="technical-ingredient-details">
            <div className="technical-functions">
              {displayFunctions.map((cosmeticFunction) => {
                const plainDescription = getPlainLanguageFunctionDescription(cosmeticFunction.name)

                return (
                  <span className="technical-function" key={cosmeticFunction.name}>
                    <strong>{cosmeticFunction.name}</strong>
                    {cosmeticFunction.description && (
                      <details
                        className="function-info"
                        onToggle={(event) => positionFunctionTooltip(event.currentTarget)}
                        onMouseEnter={(event) => positionFunctionTooltip(event.currentTarget)}
                        onFocus={(event) => positionFunctionTooltip(event.currentTarget)}
                      >
                        <summary aria-label={`Show description for ${cosmeticFunction.name}`}>
                          <span aria-hidden="true">i</span>
                        </summary>
                        <div className="function-info-tooltip" role="tooltip">
                          <button
                            type="button"
                            className="function-info-close"
                            aria-label={`Close description for ${cosmeticFunction.name}`}
                            onClick={(event) => event.currentTarget.closest('details')?.removeAttribute('open')}
                          >
                            <span aria-hidden="true">×</span>
                          </button>
                          <strong className="function-purpose">
                            {getPurposeLabel(cosmeticFunction)}
                          </strong>
                          {plainDescription && (
                            <div className="function-plain-language">
                              <span className="function-description-label">
                                {cosmeticFunction.name}
                              </span>
                              <p>{plainDescription}</p>
                            </div>
                          )}
                          <div className="function-official-description">
                            <span className="function-description-label">
                              CosIng definition
                            </span>
                            <p>{cosmeticFunction.description}</p>
                          </div>
                        </div>
                      </details>
                    )}
                  </span>
                )
              })}
            </div>
          </div>
      )}

      {chemicalDescriptions.length > 0 && (
        <div className="technical-ingredient-details technical-ingredient-details--chemical">
          <div className="technical-functions">
            <span className="technical-function">
              <strong>ADDITIONAL CHEMICAL INFORMATION</strong>

              <details
                className="function-info"
                onToggle={(event) =>
                  positionFunctionTooltip(event.currentTarget)
                }
                onMouseEnter={(event) =>
                  positionFunctionTooltip(event.currentTarget)
                }
                onFocus={(event) =>
                  positionFunctionTooltip(event.currentTarget)
                }
              >
                <summary
                  aria-label={`Show chemical information for ${ingredient.inciName}`}
                >
                  <span aria-hidden="true">i</span>
                </summary>

                <div className="function-info-tooltip" role="tooltip">
                  <button
                    type="button"
                    className="function-info-close"
                    aria-label="Close chemical information"
                    onClick={(event) =>
                      event.currentTarget
                        .closest('details')
                        ?.removeAttribute('open')
                    }
                  >
                    <span aria-hidden="true">×</span>
                  </button>

                  <strong className="function-purpose">
                    Additional chemical information
                  </strong>

                  {chemicalDescriptions.map((entry, index) => (
                    <div
                      className="function-official-description"
                      key={`${entry.cid ?? 'unknown'}-${index}`}
                    >
                      <span className="function-description-label">
                        {entry.title || ingredient.inciName}
                      </span>

                      <p>{entry.description}</p>

                      <small>
                        Source: {entry.sourceName || 'PubChem'}

                        {entry.cid && (
                          <>
                            {' · '}
                            <a
                              href={`https://pubchem.ncbi.nlm.nih.gov/compound/${entry.cid}`}
                              target="_blank"
                              rel="noopener noreferrer"
                            >
                              View on PubChem ↗
                            </a>
                          </>
                        )}
                      </small>
                    </div>
                  ))}
                </div>
              </details>
            </span>
          </div>
        </div>
      )}

    </article>
  )
}

export default IngredientCard
