import { useLocation, useNavigate } from 'react-router-dom'
import IngredientCard from '../components/IngredientCard'
import UnknownIngredientCard from '../components/UnknownIngredientCard'

const RATING_ORDER = ['Red', 'Amber', 'Green', 'Grey']

const RATING_TITLES = {
  Red: 'Red — avoid',
  Amber: 'Amber — caution',
  Green: 'Green — regulator-approved',
  Grey: 'Grey — no regulatory rating'
}

const IMPORTANT_CATEGORY_EXCLUSIONS = new Set(['EU Glossary Ingredient'])

const FORMULA_ROLES = [
  {
    title: 'Hydration helpers',
    technical: 'Humectants',
    description: 'Help attract and hold water in the skin.',
    match: (item) => hasCategory(item, 'Humectants')
  },
  {
    title: 'Skin-softening ingredients',
    technical: 'Emollients',
    description: 'Help soften and smooth the skin surface.',
    match: (item) => hasCategory(item, 'Emollients')
  },
  {
    title: 'UV protection filters',
    technical: 'UV Filter',
    description: 'Filter UV radiation and are commonly used in SPF products.',
    match: (item) => hasCategory(item, 'UV Filter')
  },
  {
    title: 'Preservation system',
    technical: 'Preservatives',
    description: 'Help keep the product stable and reduce microbial growth.',
    match: (item) => hasCategory(item, 'Preservative')
  },
  {
    title: 'Fragrance',
    technical: 'Fragrance',
    description: 'Added for scent; may matter for fragrance-sensitive users.',
    match: (item) => hasCategory(item, 'Fragrance')
  },
  {
    title: 'Regulatory notes',
    technical: 'Restricted substances',
    description: 'Ingredients with specific regulatory conditions or concentration limits.',
    match: (item) => hasCategory(item, 'Restricted Substance') || item.safetyRating === 'Red'
  }
]

function splitCategories(categoryText = '') {
  return categoryText
    .split(',')
    .map((category) => category.trim())
    .filter(Boolean)
}

function countBy(items, getKey) {
  return items.reduce((counts, item) => {
    const key = getKey(item)
    if (!key) return counts
    counts[key] = (counts[key] || 0) + 1
    return counts
  }, {})
}

function getTopCategories(items) {
  const counts = {}

  items.forEach((item) => {
    splitCategories(item.category)
      .filter((category) => !IMPORTANT_CATEGORY_EXCLUSIONS.has(category))
      .forEach((category) => {
        counts[category] = (counts[category] || 0) + 1
      })
  })

  return Object.entries(counts)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 6)
}

function getFormulaRoles(items) {
  return FORMULA_ROLES
    .map((role) => ({
      ...role,
      ingredients: items.filter(role.match)
    }))
    .filter((role) => role.ingredients.length > 0)
}

function hasCategory(item, categoryName) {
  return splitCategories(item.category).includes(categoryName)
}

function detectProductSignals(items) {
  const uvFilters = items.filter((item) => hasCategory(item, 'UV Filter'))
  const humectants = items.filter((item) => hasCategory(item, 'Humectants'))
  const emollients = items.filter((item) => hasCategory(item, 'Emollients'))
  const preservatives = items.filter((item) => hasCategory(item, 'Preservative'))
  const fragrance = items.filter((item) => hasCategory(item, 'Fragrance'))

  const signals = []

  if (uvFilters.length >= 3) {
    signals.push({
      title: 'Likely sunscreen / SPF product',
      text: `${uvFilters.length} UV filters were identified, so the ingredient profile strongly suggests sun protection.`
    })
  }

  if (humectants.length > 0) {
    signals.push({
      title: 'Hydration support',
      text: `${humectants.length} humectant ingredient(s) may help attract or hold water in the formula.`
    })
  }

  if (emollients.length > 0) {
    signals.push({
      title: 'Skin-conditioning base',
      text: `${emollients.length} emollient ingredient(s) suggest a smoothing or moisturising cosmetic base.`
    })
  }

  if (preservatives.length > 0) {
    signals.push({
      title: 'Preserved formula',
      text: `${preservatives.length} preservative ingredient(s) were found.`
    })
  }

  if (fragrance.length > 0) {
    signals.push({
      title: 'Fragrance present',
      text: 'Fragrance/parfum appears in the ingredient list and may matter for sensitive profiles.'
    })
  }

  return signals.length > 0
    ? signals
    : [{ title: 'General cosmetic formula', text: 'No strong product-type signal was detected from the available categories.' }]
}

function getPriorityIngredients(grouped, unknownIngredients) {
  const avoidFlagged = [...grouped.Red, ...grouped.Amber, ...grouped.Grey, ...grouped.Green]
    .filter((item) => item.conditionFlags?.some((flag) => flag.flagType === 'Avoid'))

  const regulatoryConcerns = [...grouped.Red, ...grouped.Amber]

  return [
    ...avoidFlagged,
    ...regulatoryConcerns
  ]
    .filter((item, index, all) => all.findIndex((other) => other.inciName === item.inciName) === index)
    .slice(0, 5)
    .concat(unknownIngredients.slice(0, 2).map((item) => ({
      inciName: item.name,
      safetyRating: 'Unknown',
      category: 'Not found in database',
      function: item.aiLabel ? `AI estimate: ${item.aiLabel}` : '',
      conditionFlags: []
    })))
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

  const grouped = { Red: [], Amber: [], Green: [], Grey: [] }

  results.results.forEach((item) => {
    if (grouped[item.safetyRating]) {
      grouped[item.safetyRating].push(item)
    } else {
      grouped.Grey.push(item)
    }
  })

  const ratingCounts = countBy(results.results, (item) => item.safetyRating)
  const totalKnown = results.results.length
  const totalUnknown = results.unknownIngredients.length
  const totalIngredients = totalKnown + totalUnknown
  const productSignals = detectProductSignals(results.results)
  const priorityIngredients = getPriorityIngredients(grouped, results.unknownIngredients)
  const formulaRoles = getFormulaRoles(results.results)

  return (
    <div className="results-page">
      <div className="results-header">
        <div>
          <p className="eyebrow">Analysis Results</p>
          <h1>Product overview</h1>
        </div>
        <button className="secondary-button" onClick={() => navigate('/')}>
          Analyse another product
        </button>
      </div>

      {results.aiSummary && (
        <section className="insight-card">
          <span className="section-label">AI summary</span>
          <p>{results.aiSummary}</p>
        </section>
      )}

      <section className="summary-grid">
        <article className="summary-card">
          <span className="section-label">Ingredient coverage</span>
          <strong>{totalIngredients}</strong>
          <p>{totalKnown} found in database · {totalUnknown} unknown</p>
        </article>

        <article className="summary-card">
          <span className="section-label">Ratings</span>
          <div className="rating-pills">
            {RATING_ORDER.map((rating) => (
              <span key={rating} className={`rating-pill ${rating.toLowerCase()}`}>
                {rating} {ratingCounts[rating] || 0}
              </span>
            ))}
          </div>
        </article>
      </section>

      <section className="results-section">
        <h2>What this formula seems to be doing</h2>
        <div className="signal-grid">
          {productSignals.map((signal) => (
            <article className="signal-card" key={signal.title}>
              <h3>{signal.title}</h3>
              <p>{signal.text}</p>
            </article>
          ))}
        </div>
      </section>

      {formulaRoles.length > 0 && (
        <section className="results-section">
          <h2>Formula roles</h2>
          <div className="role-grid">
            {formulaRoles.map((role) => (
              <article className="role-card" key={role.title}>
                <div>
                  <h3>{role.title}</h3>
                  <span>{role.technical}</span>
                  <p>{role.description}</p>
                </div>
                <ul>
                  {role.ingredients.map((ingredient) => (
                    <li key={`${role.title}-${ingredient.inciName}`}>{ingredient.inciName}</li>
                  ))}
                </ul>
              </article>
            ))}
          </div>
        </section>
      )}

      {priorityIngredients.length > 0 && (
        <section className="results-section">
          <h2>Key items to notice</h2>
          <div className="priority-list">
            {priorityIngredients.map((item, index) => (
              <div className="priority-card" key={`${item.inciName}-${index}`}>
                <strong>{item.inciName}</strong>
                <span>{item.safetyRating}</span>
                {item.category && <p>{item.category}</p>}
                {item.conditionFlags?.length > 0 && (
                  <p>{item.conditionFlags[0].flagType}: {item.conditionFlags[0].notes}</p>
                )}
              </div>
            ))}
          </div>
        </section>
      )}

      <details className="full-analysis">
        <summary>Show full ingredient breakdown</summary>

        {RATING_ORDER.map((rating) => {
          const items = grouped[rating]
          if (items.length === 0) return null

          return (
            <div key={rating} className="rating-section">
              <h3>{RATING_TITLES[rating]} ({items.length})</h3>
              {items.map((item, index) => (
                <IngredientCard key={`${rating}-${item.inciName}-${index}`} ingredient={item} />
              ))}
            </div>
          )
        })}

        {results.unknownIngredients.length > 0 && (
          <div className="rating-section">
            <h3>Unknown ingredients ({results.unknownIngredients.length})</h3>
            {results.unknownIngredients.map((item, i) => (
              <UnknownIngredientCard
                key={i}
                name={item.name}
                aiLabel={item.aiLabel}
                confidence={item.confidence}
              />
            ))}
          </div>
        )}
      </details>
    </div>
  )
}

export default ResultsPage
