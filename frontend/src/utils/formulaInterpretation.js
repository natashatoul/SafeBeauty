const splitValues = (text = '') => text
  .split(',')
  .map((value) => value.trim())
  .filter(Boolean)

const hasCategory = (ingredient, category) =>
  splitValues(ingredient.category).includes(category)

export const getProfileSignals = (ingredients) => {
  const avoid = ingredients.filter((item) =>
    item.conditionFlags?.some((flag) => flag.flagType === 'Avoid'))
  const beneficial = ingredients.filter((item) =>
    item.conditionFlags?.some((flag) => flag.flagType === 'Beneficial'))
  const regulatory = ingredients.filter((item) =>
    item.safetyRating === 'Red'
    || item.safetyRating === 'Amber'
    || hasCategory(item, 'Restricted Substance'))

  return { avoid, beneficial, regulatory }
}

export const getUvOverview = (ingredients) => {
  const filters = ingredients.filter((item) => item.isUvFilter)
  const types = [...new Set(filters.map((item) => item.uvFilterType).filter(Boolean))]

  return {
    filters,
    systemType: types.length > 1 ? 'Combination' : types[0] || ''
  }
}
