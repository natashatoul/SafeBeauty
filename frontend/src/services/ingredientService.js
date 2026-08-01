import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || '/api'

// this funct returns ingredients from frontend to backend
// async - operation that takes time to complete
export const analyseIngredients = async (
  ingredientNames,
  userConditions = [],
  presentationContext = {}
) => {
  const response = await axios.post(`${API_URL}/ingredients/analyse`, {
    ingredients: ingredientNames,
    userConditions,
    ageGroup: presentationContext.ageGroup || null,
    gender: presentationContext.gender || null
  })
  return response.data
}

