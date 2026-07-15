const RATING_ORDER = ['Red', 'Amber', 'Green', 'Grey']

const formatAnalysisDate = (createdAt) => {
  const date = new Date(createdAt)
  if (Number.isNaN(date.getTime())) return 'Date unavailable'

  return new Intl.DateTimeFormat('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric'
  }).format(date)
}

function AnalysisHistoryRow({ item, onView, onDelete }) {
  const productName = item.analysisContext?.productName?.trim()
    || 'Manual ingredient analysis'
  const verifiedIngredients = Array.isArray(item.results?.results)
    ? item.results.results
    : []

  // The API keeps verified ingredients in results.results. Building the
  // summary here means both Home and History always show the same counts.
  const ratings = RATING_ORDER
    .map((label) => ({
      label,
      count: verifiedIngredients.filter(
        (ingredient) => ingredient.safetyRating?.toLowerCase() === label.toLowerCase()
      ).length
    }))
    .filter((rating) => rating.count > 0)
  const ratingPlaceholders = Array.from(
    { length: Math.max(0, RATING_ORDER.length - ratings.length) },
    (_, index) => index
  )

  return (
    <article className="recent-row">
      <div className="recent-product">
        <h3>{productName}</h3>
        <p>{formatAnalysisDate(item.createdAt)}</p>
      </div>

      <div className="rating-pills" aria-label={`Rating summary for ${productName}`}>
        {ratings.length > 0 ? (
          <>
            {ratings.map((rating) => (
              <span
                className={`rating-pill ${rating.label.toLowerCase()}`}
                key={rating.label}
                aria-label={`${rating.label} ${rating.count}`}
              >
                <span className="rating-pill-label">{rating.label}</span>
                <span className="rating-pill-count">{rating.count}</span>
              </span>
            ))}
            {ratingPlaceholders.map((index) => (
              <span
                className="rating-pill-placeholder"
                key={`rating-placeholder-${index}`}
                aria-hidden="true"
              />
            ))}
          </>
        ) : (
          <span className="history-no-ratings">No verified ratings</span>
        )}
      </div>

      <div className="history-row-actions">
        <button className="text-button" type="button" onClick={() => onView(item)}>
          View result →
        </button>
        {onDelete && (
          <button
            className="history-delete-button"
            type="button"
            onClick={() => onDelete(item.id)}
            aria-label={`Delete ${productName} from history`}
          >
            Delete
          </button>
        )}
      </div>
    </article>
  )
}

export default AnalysisHistoryRow
