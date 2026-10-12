import apiClient from './apiClient'

// this funct returns ingredients from frontend to backend
// async - operation that takes time to complete
export const analyseIngredients = async (
  ingredientNames,
  userConditions = [],
  presentationContext = {}
) => {
  const response = await apiClient.post('/ingredients/analyse', {
    ingredients: ingredientNames,
    userConditions,
    ageGroup: presentationContext.ageGroup || null,
    gender: presentationContext.gender || null
  })
  return response.data
}
