import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || '/api'

export const analyseIngredients = async (ingredientNames) => {
  const response = await axios.post(`${API_URL}/ingredients/analyse`, {
    ingredients: ingredientNames
  })
  return response.data
}

