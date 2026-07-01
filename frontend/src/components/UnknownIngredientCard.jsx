// UnknownIngredientCard - a small reusable component for ingredients
// that were not found in the database. Unlike IngredientCard, it takes
// just a plain string (the ingredient name), not a full ingredient object,
// because there's no safety data to show for these.
function UnknownIngredientCard({ name }) {
  return (
    <div style={{ borderLeft: '4px solid grey', padding: '8px', marginBottom: '8px' }}>
      <strong>{name}</strong> — Not found in database
    </div>
  )
}

export default UnknownIngredientCard