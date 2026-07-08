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

    return (
        <div style={{ borderLeft: `4px solid ${color}`, padding: '5px', marginBottom: '10px', paddingLeft: '20px' }}>
            <strong>{ingredient.inciName}</strong>
            <span> — {ingredient.safetyRating}</span>
            {ingredient.function && <p>Function: {ingredient.function}</p>}
            {ingredient.category && <p>Category: {ingredient.category}</p>}
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