import { useLocation, useNavigate } from 'react-router-dom'
import IngredientCard from '../components/IngredientCard'
import UnknownIngredientCard from '../components/UnknownIngredientCard'
import {
  getFormulaGroups,
  getProfileSignals,
  getUvOverview
} from '../utils/formulaInterpretation'

const RATING_ORDER = ['Red', 'Amber', 'Green', 'Grey']

const RATING_TITLES = {
  Red: 'Avoid or prohibited use',
  Amber: 'Conditions or cautions apply',
  Green: 'Permitted use identified',
  Grey: 'No specific restriction identified'
}

const getFlags = (ingredient, type) =>
  ingredient.conditionFlags?.filter((flag) => flag.flagType === type) ?? []

const getIngredientChipTone = (ingredient) => {
  const flags = ingredient.conditionFlags ?? []

  if (ingredient.safetyRating === 'Red') return 'red'
  if (ingredient.safetyRating === 'Amber'
    || flags.some((flag) => flag.flagType === 'Avoid' || flag.flagType === 'Caution')) return 'caution'
  if (flags.some((flag) => flag.flagType === 'Beneficial')) return 'beneficial'

  return 'neutral'
}

const uniqueIngredients = (ingredients) => ingredients.filter((ingredient, index, all) =>
  all.findIndex((other) => other.inciName === ingredient.inciName) === index)

const formatConditionName = (condition) => condition.replace(/([a-z])([A-Z])/g, '$1 $2')

function ProfileIngredient({ ingredient, flagType }) {
  const flags = flagType ? getFlags(ingredient, flagType) : ingredient.conditionFlags ?? []

  return (
    <article className="profile-ingredient">
      <strong>{ingredient.inciName}</strong>
      {flags.map((flag, index) => (
        <p key={`${ingredient.inciName}-${flag.condition}-${index}`}>
          <span>{flag.condition}</span> — {flag.flagType === 'Beneficial'
            ? 'Its broader cosmetic category is marked as potentially relevant in the profile rule set. This does not prove that the ingredient or finished product benefits the condition.'
            : flag.notes}
          {flag.evidenceSource && (
            <small className="rule-source">Rule source: {flag.evidenceSource}.</small>
          )}
        </p>
      ))}
      {!flagType && flags.length === 0 && (
        <p>A regulatory restriction or condition is recorded for this ingredient.</p>
      )}
    </article>
  )
}

function ResultsPage() {
  const { state } = useLocation()
  const navigate = useNavigate()
  const results = state?.results
  const context = state?.analysisContext ?? {}

  if (!results) {
    return (
      <div>
        <p>No results found.</p>
        <button type="button" className="secondary-button" onClick={() => navigate('/')}>Go Home</button>
      </div>
    )
  }

  const openProfile = () => navigate('/profile', {
    state: {
      returnToResults: {
        results,
        analysisContext: context
      }
    }
  })

  const knownIngredients = results.results ?? []
  const unknownIngredients = results.unknownIngredients ?? []
  const grouped = { Red: [], Amber: [], Green: [], Grey: [] }

  knownIngredients.forEach((item) => {
    const rating = grouped[item.safetyRating] ? item.safetyRating : 'Grey'
    grouped[rating].push(item)
  })

  const totalKnown = knownIngredients.length
  const totalUnknown = unknownIngredients.length
  const totalIngredients = totalKnown + totalUnknown
  const coverage = totalIngredients > 0 ? Math.round((totalKnown / totalIngredients) * 100) : 0
  const uncertainInput = totalIngredients > 0 && totalUnknown / totalIngredients >= 0.25
  const formulaGroups = getFormulaGroups(knownIngredients)
  const profileSignals = getProfileSignals(knownIngredients)
  const uvOverview = getUvOverview(knownIngredients)
  const selectedConditions = context.selectedConditions ?? []
  const regulatoryOnly = uniqueIngredients(profileSignals.regulatory.filter((ingredient) =>
    !profileSignals.avoid.some((avoidIngredient) => avoidIngredient.inciName === ingredient.inciName)))

  return (
    <div className="results-page">
      <div className="results-header">
        <div>
          <p className="eyebrow">Ingredient analysis</p>
          <h1>{context.productName || 'Your formula overview'}</h1>
          <div className="results-context">
            <span>Source: {context.source || 'Ingredient analysis'}</span>
            {context.barcode && <span>Barcode: {context.barcode}</span>}
          </div>
        </div>
        <button className="secondary-button" onClick={() => navigate('/')}>
          Analyse another formula
        </button>
      </div>

      {uncertainInput && (
        <section className="quality-banner" role="alert">
          <div>
            <strong>Please check the ingredient list</strong>
            <p>
              {totalUnknown} of {totalIngredients} entries could not be verified. The source may be
              incomplete or contain OCR/parsing errors, so the interpretation may miss important details.
            </p>
          </div>
          {context.source !== 'Manual input' && (
            <button type="button" className="secondary-button" onClick={() => navigate('/manual')}>
              Enter ingredients manually
            </button>
          )}
        </section>
      )}

      {results.aiSummary && (
        <section className="insight-card">
          <h2 className="section-label">AI formula interpretation</h2>
          <p>{results.aiSummary}</p>
          <small>
            * Generated from verified ingredient matches and profile flags. It supports understanding,
            but is not a medical diagnosis or a substitute for professional advice.
          </small>
        </section>
      )}

      <section className="summary-grid">
        <article className="summary-card">
          <h2 className="section-label">Data coverage</h2>
          <strong>{coverage}%</strong>
          <p>{totalKnown} verified · {totalUnknown} not matched · {totalIngredients} total</p>
        </article>
        <article className="summary-card">
          <h2 className="section-label">Personalisation</h2>
          <strong>{selectedConditions.length}</strong>
          <p>
            {selectedConditions.length > 0
              ? `Profile conditions used: ${selectedConditions.map(formatConditionName).join(', ')}`
              : 'No profile conditions were selected for this analysis.'}
          </p>
          <button type="button" className="text-button" onClick={openProfile}>
            {selectedConditions.length > 0 ? 'Edit profile' : 'Set up profile'}
          </button>
        </article>
      </section>

      <section className="results-section">
        <h2>What matters for your profile</h2>
        {selectedConditions.length === 0 ? (
          <div className="empty-guidance">
            <p>Add conditions or preferences to highlight ingredients that may be especially relevant to you.</p>
            <button type="button" className="secondary-button" onClick={openProfile}>
              Set up profile
            </button>
          </div>
        ) : (
          <div className="profile-grid">
            <article className="profile-card concern">
              <h3 className="section-label">Worth avoiding for your profile</h3>
              {profileSignals.avoid.length > 0
                ? profileSignals.avoid.map((ingredient) => (
                  <ProfileIngredient key={`avoid-${ingredient.inciName}`} ingredient={ingredient} flagType="Avoid" />
                ))
                : <p>No verified “Avoid” flags were found for your selected conditions.</p>}
            </article>
            <article className="profile-card supportive">
              <h3 className="section-label">Potentially relevant formula roles</h3>
              {profileSignals.beneficial.length > 0
                ? profileSignals.beneficial.map((ingredient) => (
                  <ProfileIngredient key={`beneficial-${ingredient.inciName}`} ingredient={ingredient} flagType="Beneficial" />
                ))
                : <p>No specifically beneficial profile matches were identified.</p>}
            </article>
          </div>
        )}

        {regulatoryOnly.length > 0 && (
          <div className="regulatory-note">
            <h3>Regulatory notes</h3>
            <p>
              These ingredients have recorded regulatory use conditions or restrictions. This does
              not automatically mean that the finished product is unsafe; applicability may depend
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

      {formulaGroups.length > 0 && (
        <section className="results-section">
          <h2>How the formula is built</h2>
          <p className="section-intro">Ingredients are grouped by a practical role, with the technical term kept for reference.</p>
          <div className="ingredient-flag-legend" aria-label="Ingredient highlight colour key">
            <span className="ingredient-chip ingredient-chip--red">Regulatory concern</span>
            <span className="ingredient-chip ingredient-chip--caution">Avoid or caution</span>
            <span className="ingredient-chip ingredient-chip--beneficial">Profile relevance</span>
            <span className="ingredient-chip ingredient-chip--neutral">No highlighted flag</span>
          </div>
          <div className="formula-grid">
            {formulaGroups.map((group) => (
              <article className="formula-card" key={group.id}>
                <div className="formula-card-heading">
                  <div>
                    <h3>{group.title}</h3>
                    <span>{group.technical}</span>
                  </div>
                  <strong>{group.ingredients.length}</strong>
                </div>
                <p>{group.description}</p>
                <div className="ingredient-chips">
                  {group.ingredients.slice(0, 5).map((ingredient) => (
                    <span
                      className={`ingredient-chip ingredient-chip--${getIngredientChipTone(ingredient)}`}
                      key={`${group.id}-${ingredient.inciName}`}
                    >
                      {ingredient.inciName}
                    </span>
                  ))}
                  {group.ingredients.length > 5 && (
                    <span className="ingredient-chip ingredient-chip--neutral">
                      +{group.ingredients.length - 5} more
                    </span>
                  )}
                </div>
              </article>
            ))}
          </div>
        </section>
      )}

      {uvOverview.filters.length > 0 && (
        <section className="results-section uv-overview">
          <span className="section-label">Confirmed UV filters</span>
          <h2>{uvOverview.systemType} filter system</h2>
          <p>
            “Mineral/inorganic” filters are often called physical filters, while organic filters are
            often called chemical filters. Both work mainly by absorbing UV; mineral filters can also
            scatter and reflect some light. The type alone does not make a filter safer or better, and
            an ingredient list cannot confirm the product’s SPF or level of protection.
          </p>
          <div className="uv-filter-list">
            {uvOverview.filters.map((ingredient) => (
              <span key={`uv-${ingredient.inciName}`}>
                <strong>{ingredient.inciName}</strong> — {ingredient.uvFilterType}
              </span>
            ))}
          </div>
        </section>
      )}

      <section className="data-confidence">
        <div>
          <span className="section-label">About these data</span>
          <h2>{uncertainInput ? 'Use this result with caution' : 'Ingredient coverage is suitable for interpretation'}</h2>
          <p>
            Source: {context.source || 'Ingredient analysis'}. {totalKnown} of {totalIngredients} entries
            were matched to verified database records. Unmatched entries are preserved below and are not
            silently corrected or used as verified facts in the AI interpretation.
          </p>
        </div>
        {context.source !== 'Manual input' && (
          <button type="button" className="secondary-button" onClick={() => navigate('/manual')}>
            Check with manual input
          </button>
        )}
      </section>

      <details className="full-analysis">
        <summary>Show the full technical ingredient list ({totalIngredients})</summary>
        <p className="technical-list-note">
          The complete INCI list is retained for transparency. Ratings describe database and regulatory
          information; they are not a simple good/bad score for the finished product.
        </p>

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

        {unknownIngredients.length > 0 && (
          <div className="rating-section">
            <h3>Not matched to the database ({unknownIngredients.length})</h3>
            {unknownIngredients.map((item, index) => (
              <UnknownIngredientCard
                key={`${item.name}-${index}`}
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
