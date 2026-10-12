
import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import IngredientCard from '../components/IngredientCard'
import UnknownIngredientCard from '../components/UnknownIngredientCard'
import {
  getProfileSignals,
  getUvOverview
} from '../utils/formulaInterpretation'
import { getAnalysisCoverage } from '../utils/analysisMetrics'

const getFlags = (ingredient, type) =>
  ingredient.conditionFlags?.filter((flag) => flag.flagType === type) ?? []

const uniqueIngredients = (ingredients) =>
  ingredients.filter((ingredient, index, all) =>
    all.findIndex(
      (other) => other.inciName === ingredient.inciName
    ) === index
  )

const formatConditionName = (condition) =>
  condition.replace(/([a-z])([A-Z])/g, '$1 $2')

const formatSkinHairSummary = (skinType, hairCondition) => {
  const parts = []

  if (skinType) parts.push(`Skin: ${skinType}`)
  if (hairCondition) parts.push(`Hair: ${hairCondition}`)

  return parts.length > 0 ? parts.join(', ') : 'Not specified'
}

function ProfileIngredient({ ingredient, flagType }) {
  const flags = flagType
    ? getFlags(ingredient, flagType)
    : ingredient.conditionFlags ?? []

  return (
    <article className="profile-ingredient">
      <strong>{ingredient.inciName}</strong>

      {flags.map((flag, index) => (
        <p key={`${ingredient.inciName}-${flag.condition}-${index}`}>
          <span>{formatConditionName(flag.condition)}</span>
          {' - '}
          {flag.flagType === 'Beneficial'
            ? 'Its broader cosmetic category is marked as potentially relevant in the profile rule set. This does not prove that the ingredient or finished product benefits the condition.'
            : flag.notes}

          {flag.evidenceSource && (
            <small className="rule-source">
              Rule source: {flag.evidenceSource}.
            </small>
          )}
        </p>
      ))}

      {!flagType && flags.length === 0 && (
        <p>
          A regulatory restriction or condition is recorded for this ingredient.
        </p>
      )}
    </article>
  )
}

function ResultsPage() {
  const { state } = useLocation()
  const navigate = useNavigate()

  const results = state?.results
  const context = state?.analysisContext ?? {}

  const [pubChemDescriptions, setPubChemDescriptions] = useState({})

  // Load additional chemical descriptions from PubChem.
  useEffect(() => {
    const ingredients = [
      ...new Set(
        (results?.results ?? [])
          .map((item) => item.inciName?.trim())
          .filter(Boolean)
      )
    ]

    if (ingredients.length === 0) {
      setPubChemDescriptions({})
      return
    }

    const controller = new AbortController()
    const API_URL = import.meta.env.VITE_API_URL || '/api'

    // Clear descriptions from any previous analysis.
    setPubChemDescriptions({})

    async function loadPubChemDescriptions() {
      try {
        const response = await fetch(`${API_URL}/pubchem/batch`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(ingredients),
          signal: controller.signal
        })

        if (!response.ok) {
          throw new Error(
            `PubChem request failed: ${response.status}`
          )
        }

        const data = await response.json()
        const descriptionsByIngredient = {}

        for (const item of data) {
          if (
            item.status === 'DescriptionFound' &&
            item.descriptions?.length > 0
          ) {
            const ingredientKey = item.ingredient
              .trim()
              .toUpperCase()

            descriptionsByIngredient[ingredientKey] =
              item.descriptions
          }
        }

        if (!controller.signal.aborted) {
          setPubChemDescriptions(descriptionsByIngredient)
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          console.error(
            'PubChem descriptions unavailable:',
            error
          )
        }
      }
    }

    loadPubChemDescriptions()

    return () => controller.abort()
  }, [results])

  if (!results) {
    return (
      <div>
        <p>No results found.</p>
        <button
          type="button"
          className="secondary-button"
          onClick={() => navigate('/')}
        >
          Go Home
        </button>
      </div>
    )
  }

  const openProfile = () =>
    navigate('/profile', {
      state: {
        returnToResults: {
          results,
          analysisContext: context
        }
      }
    })

  const knownIngredients = results.results ?? []
  const unknownIngredients = results.unknownIngredients ?? []

  const grouped = {
    Red: [],
    Amber: [],
    Green: [],
    Grey: []
  }

  knownIngredients.forEach((item) => {
    const rating = grouped[item.safetyRating]
      ? item.safetyRating
      : 'Grey'

    grouped[rating].push(item)
  })

  const sortByInputOrder = (left, right) => {
    const leftOrder =
      Number.isInteger(left.inputOrder) && left.inputOrder >= 0
        ? left.inputOrder
        : Number.MAX_SAFE_INTEGER

    const rightOrder =
      Number.isInteger(right.inputOrder) && right.inputOrder >= 0
        ? right.inputOrder
        : Number.MAX_SAFE_INTEGER

    return leftOrder - rightOrder
  }

  const orderedKnownIngredients = [...knownIngredients]
    .sort(sortByInputOrder)

  const orderedUnknownIngredients = [...unknownIngredients]
    .sort(sortByInputOrder)

  const {
    knownCount,
    unknownCount,
    totalCount,
    coverage
  } = getAnalysisCoverage(results)

  const uncertainInput =
    totalCount > 0 && unknownCount / totalCount >= 0.25

  const isBarcodeAnalysis =
    context.source === 'Open Beauty Facts'

  const hasEditableSourceIngredients =
    Array.isArray(context.sourceIngredients) &&
    context.sourceIngredients.length > 0

  const canReviewSourceIngredients =
    isBarcodeAnalysis || hasEditableSourceIngredients

  const profileSignals = getProfileSignals(knownIngredients)
  const uvOverview = getUvOverview(knownIngredients)
  const selectedConditions = context.selectedConditions ?? []

  const regulatoryOnly = uniqueIngredients(
    profileSignals.regulatory.filter(
      (ingredient) =>
        !profileSignals.avoid.some(
          (avoidIngredient) =>
            avoidIngredient.inciName === ingredient.inciName
        )
    )
  )

  const openManualInput = () =>
    navigate('/manual', {
      state: {
        initialIngredients: context.sourceIngredients,
        draftSource: context.source,
        productName: context.productName,
        barcode: context.barcode,
        reviewResult: {
          results,
          analysisContext: context
        }
      }
    })

  const openEmptyManualInput = () => navigate('/manual')

  return (
    <div className="results-page">
      <div className="results-header">
        <div>
          <p className="eyebrow">
            Ingredient analysis
          </p>

          <h1>
            {context.productName || 'Your formula overview'}
          </h1>

          <div className="results-context">
            <span>
              Source: {context.source || 'Ingredient analysis'}
            </span>

            {context.barcode && (
              <span>Barcode: {context.barcode}</span>
            )}
          </div>
        </div>

        <button
          className="secondary-button"
          onClick={() => navigate('/')}
        >
          Analyse another formula
        </button>
      </div>

      <div className="results-layout">
        <aside className="results-sidebar">
          {results.aiSummary && (
            <section className="insight-card results-summary">
              <h2 className="section-label">
                AI formula interpretation
              </h2>

              <p>{results.aiSummary}</p>

              <small>
                * Generated from verified ingredient matches and
                profile flags. It supports understanding,
                but is not a medical diagnosis - for a detailed
                assessment, consult a dermatologist.
              </small>
            </section>
          )}

          <section className="summary-grid results-meta">
            <article className="summary-card">
              <h2 className="section-label">
                Data coverage
              </h2>

              <strong>{coverage}%</strong>

              <p>
                {knownCount} verified · {unknownCount} not matched
                {' · '}
                {totalCount} total
              </p>
            </article>

            <article className="summary-card">
              <h2 className="section-label">
                Personalisation
              </h2>

              <strong className="personalisation-value">
                {selectedConditions.length > 0
                  ? selectedConditions
                      .map(formatConditionName)
                      .join(', ')
                  : formatSkinHairSummary(
                      context.skinType,
                      context.hairCondition
                    )}
              </strong>

              <p>
                {selectedConditions.length > 0
                  ? 'These profile conditions were considered for this analysis.'
                  : 'No specific skin or hair conditions were selected for this analysis.'}
              </p>

              <button
                type="button"
                className="text-button"
                onClick={openProfile}
              >
                Edit profile
              </button>
            </article>
          </section>
        </aside>

        <div className="results-content">
          {grouped.Red.length > 0 && (
            <section
              className="results-warning results-warning--red"
              role="alert"
            >
              <strong>Review before use</strong>

              <p>
                {grouped.Red.length} ingredient
                {grouped.Red.length === 1 ? '' : 's'}
                {' '}with a red regulatory rating:{' '}
                {grouped.Red
                  .map((item) => item.inciName)
                  .join(', ')}.
              </p>
            </section>
          )}

          {grouped.Amber.length > 0 && (
            <section
              className="results-warning results-warning--amber"
              role="alert"
            >
              <strong>Use with caution</strong>

              <p>
                {grouped.Amber.length} ingredient
                {grouped.Amber.length === 1 ? '' : 's'}
                {' '}
                {grouped.Amber.length === 1
                  ? 'requires'
                  : 'require'}
                {' '}caution:{' '}
                {grouped.Amber
                  .map((item) => item.inciName)
                  .join(', ')}.
              </p>
            </section>
          )}

          {unknownCount > 0 && (
            <section
              className="quality-banner results-alert"
              role="alert"
            >
              <div>
                <strong>
                  Some ingredients could not be checked
                </strong>

                <p>
                  {unknownCount} of {totalCount} entries were not
                  found in the verified database.

                  {context.source === 'Open Beauty Facts'
                    ? ' The barcode data may be incomplete or incorrectly split, so please compare it with the product label.'
                    : ' Please check spelling and separators, or compare the list with the product label.'}
                </p>
              </div>

              {canReviewSourceIngredients && (
                <button
                  type="button"
                  className="secondary-button"
                  onClick={
                    hasEditableSourceIngredients
                      ? openManualInput
                      : openEmptyManualInput
                  }
                >
                  {hasEditableSourceIngredients
                    ? 'Edit in manual input'
                    : 'Enter ingredients manually'}
                </button>
              )}
            </section>
          )}

          {selectedConditions.length > 0 && (
            <section className="results-section">
              <h2>What matters for your profile</h2>

              <div className="profile-grid">
                <article className="profile-card concern">
                  <h3 className="section-label">
                    Ingredients that may not suit your skin
                  </h3>

                  {profileSignals.avoid.length > 0
                    ? profileSignals.avoid.map((ingredient) => (
                        <ProfileIngredient
                          key={`avoid-${ingredient.inciName}`}
                          ingredient={ingredient}
                          flagType="Avoid"
                        />
                      ))
                    : (
                      <p>
                        No verified “Avoid” flags were found
                        for your selected conditions.
                      </p>
                    )}
                </article>

                <article className="profile-card supportive">
                  <h3 className="section-label">
                    How these ingredients may benefit your skin
                  </h3>

                  {profileSignals.beneficial.length > 0
                    ? profileSignals.beneficial.map((ingredient) => (
                        <ProfileIngredient
                          key={`beneficial-${ingredient.inciName}`}
                          ingredient={ingredient}
                          flagType="Beneficial"
                        />
                      ))
                    : (
                      <p>
                        No specifically beneficial profile
                        matches were identified.
                      </p>
                    )}
                </article>
              </div>

              {regulatoryOnly.length > 0 && (
                <div className="regulatory-note">
                  <h3>Regulatory notes</h3>

                  <p>
                    These ingredients have recorded regulatory
                    use conditions or restrictions. This does
                    not automatically mean that the finished
                    product is unsafe; applicability may depend
                    on product type and concentration.
                  </p>

                  <ul className="regulatory-ingredient-list">
                    {regulatoryOnly.map((ingredient) => (
                      <li
                        className="ingredient-chip ingredient-chip--caution"
                        key={`regulatory-${ingredient.inciName}`}
                      >
                        {ingredient.inciName}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </section>
          )}

          {uvOverview.filters.length > 0 && (
            <section className="results-section uv-overview">
              <span className="section-label">
                Confirmed UV filters
              </span>

              <h2>
                {uvOverview.systemType} filter system
              </h2>

              <p>
                “Mineral/inorganic” filters are often called
                physical filters, while organic filters are often
                called chemical filters. Both work mainly by
                absorbing UV; mineral filters can also scatter
                and reflect some light. The type alone does not
                make a filter safer or better, and an ingredient
                list cannot confirm the product’s SPF or level
                of protection.
              </p>

              <div className="uv-filter-list">
                {uvOverview.filters.map((ingredient) => (
                  <span key={`uv-${ingredient.inciName}`}>
                    <strong>{ingredient.inciName}</strong>
                    {' - '}
                    {ingredient.uvFilterType}
                  </span>
                ))}
              </div>
            </section>
          )}

          <section className="data-confidence">
            <div>
              <span className="section-label">
                About these data
              </span>

              <h2>
                {uncertainInput
                  ? 'Use this result with caution'
                  : 'Most ingredients were successfully identified'}
              </h2>

              <p>
                Source: {context.source || 'Ingredient analysis'}.
                {' '}{knownCount} of {totalCount} entries were matched
                to verified database records. Unmatched entries
                are preserved below and are not silently corrected
                or used as verified facts in the AI interpretation.
              </p>
            </div>

            {canReviewSourceIngredients && (
              <button
                type="button"
                className="secondary-button"
                onClick={
                  hasEditableSourceIngredients
                    ? openManualInput
                    : openEmptyManualInput
                }
              >
                {hasEditableSourceIngredients
                  ? 'Edit in manual input'
                  : 'Check with manual input'}
              </button>
            )}
          </section>

          <details className="full-analysis" open>
            <summary>
              Show the full technical ingredient list ({totalCount})
            </summary>

            <p className="technical-list-note">
              Identified ingredients are shown in label order.
              Repeated entries are shown once. Ingredients above
              1% are generally listed in descending concentration;
              ingredients at or below 1% may appear in any order.
              Exact concentrations are not available. Listed
              functions describe recorded cosmetic uses; they are
              not a safety rating or proof of a skin benefit.
            </p>

            <div className="rating-section">
              {orderedKnownIngredients.map((item, index) => (
                <IngredientCard
                  key={`known-${item.inciName}-${index}`}
                  ingredient={item}
                  chemicalDescriptions={
                    pubChemDescriptions[
                      item.inciName?.trim().toUpperCase()
                    ] ?? []
                  }
                />
              ))}
            </div>

            {orderedUnknownIngredients.length > 0 && (
              <div className="rating-section">
                <h3>
                  Not matched to the database (
                  {orderedUnknownIngredients.length})
                </h3>

                {orderedUnknownIngredients.map((item, index) => (
                  <UnknownIngredientCard
                    key={`unknown-${item.name}-${index}`}
                    name={item.name}
                    aiLabel={item.aiLabel}
                    confidence={item.confidence}
                    classificationStatus={
                      item.classificationStatus
                    }
                  />
                ))}
              </div>
            )}
          </details>
        </div>
      </div>
    </div>
  )
}

export default ResultsPage
