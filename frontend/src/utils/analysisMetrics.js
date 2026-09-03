export const getAnalysisCoverage = (results) => {
  const knownCount = results?.results?.length ?? 0
  const unknownCount = results?.unknownIngredients?.length ?? 0
  const totalCount = knownCount + unknownCount

  return {
    knownCount,
    unknownCount,
    totalCount,
    coverage: totalCount > 0 ? Math.round((knownCount / totalCount) * 100) : 0
  }
}