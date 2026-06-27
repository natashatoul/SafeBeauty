import axios from 'axios'

const API_URL = 'http://localhost:5166/api'

export const analyseIngredients = async (ingredientNames) => {
  const response = await axios.post(`${API_URL}/ingredients/analyse`, {
    ingredients: ingredientNames
  })
  return response.data
}

