const getSourceEntry = (item, sourceEntries, fallbackProperty) => {
  const inputOrder = item?.inputOrder
  if (Number.isInteger(inputOrder) && inputOrder >= 0) {
    const sourceEntry = sourceEntries[inputOrder]?.trim()
    if (sourceEntry) return sourceEntry
  }

  return item?.[fallbackProperty]?.trim() ?? ''
}

const toReviewEntries = (items, sourceEntries, fallbackProperty) => (items ?? [])
  .map((item) => getSourceEntry(item, sourceEntries, fallbackProperty))
  .filter(Boolean)

export const getIngredientReviewGroups = (sourceEntries = [], analysis = {}) => ({
  matched: toReviewEntries(analysis.results, sourceEntries, 'inciName'),
  needsChecking: toReviewEntries(analysis.unknownIngredients, sourceEntries, 'name')
})
