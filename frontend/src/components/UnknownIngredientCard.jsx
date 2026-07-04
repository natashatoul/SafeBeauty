// UnknownIngredientCard - a small reusable component for ingredients
// that were not found in the database. Unlike IngredientCard, it takes
// just a plain string (the ingredient name), not a full ingredient object,
// because there's no safety data to show for these.
function UnknownIngredientCard({ name, aiLabel, confidence }) {
  return (
    <div style={{ borderLeft: '4px solid grey', padding: '8px', marginBottom: '8px' }}>
      <strong>{name}</strong> 
      <p>Not found in database</p> 
      <p>AI analysis: {aiLabel} ({Math.round(confidence * 100)}%)</p>
      <p style={{ fontSize: '0.8em', color: 'grey'}}>
        This is an AI estimate, not a verified safety assessment.</p>
    </div>
  )
}

export default UnknownIngredientCard