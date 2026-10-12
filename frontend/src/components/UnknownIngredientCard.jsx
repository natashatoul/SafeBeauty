// UnknownIngredientCard - a small reusable component for ingredients
// that were not found in the database. Unlike IngredientCard, it takes
// just a plain string (the ingredient name), not a full ingredient object,
// because there's no safety data to show for these.
function UnknownIngredientCard({ name, aiLabel, confidence, classificationStatus }) {
  const hasAiEstimate = classificationStatus === 'Classified'
    || (!classificationStatus && aiLabel && aiLabel !== 'Unknown' && Number.isFinite(confidence))
  const aiClassificationSkipped = classificationStatus === 'SkippedLimit'

  return (
    <article className="technical-ingredient technical-ingredient--grey">
      <div className="technical-ingredient-heading">
        <strong>{name}</strong>
        <span>Not verified</span>
      </div>
      {aiClassificationSkipped ? (
        <div className="technical-ingredient-details">
          <p>Not classified by AI</p>
          <small className="technical-ingredient-note">
            The source contains too many unmatched entries, so SafeBeauty avoids guessing.
          </small>
        </div>
      ) : hasAiEstimate ? (
        <div className="technical-ingredient-details">
          <p>AI estimate: {aiLabel} ({Math.round(confidence * 100)}%)</p>
          <small className="technical-ingredient-note">
            This is an AI estimate, not a verified safety assessment.
          </small>
        </div>
      ) : (
        <div className="technical-ingredient-details">
          <p>AI classification unavailable</p>
          <small className="technical-ingredient-note">
            SafeBeauty could not obtain an AI estimate. This ingredient remains unverified.
          </small>
        </div>
      )}
    </article>
  )
}

export default UnknownIngredientCard
