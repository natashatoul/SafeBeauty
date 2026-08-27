import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || '/api'
const TOKEN_STORAGE_KEY = 'safebeauty_token'

const getToken = () => {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY)
  } catch {
    return null
  }
}

const authHeaders = () => {
  const token = getToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

// selectedConditions and conditionFlags are health-adjacent data (GDPR
// Article 9). They stay in the browser and are used to compute this one
// analysis, but are stripped out of what actually gets persisted server-side.
const stripPersonalisation = ({ results, analysisContext }) => {
  const { selectedConditions, ...restContext } = analysisContext

  return {
    results: {
      ...results,
      results: (results?.results ?? []).map(({ conditionFlags, ...rest }) => rest)
    },
    analysisContext: restContext
  }
}

export const getHistory = async () => {
  try {
    const response = await axios.get(`${API_URL}/scanhistory`, { headers: authHeaders() })
    return response.data
  } catch {
    return null
  }
}

export const saveAnalysis = async ({ results, analysisContext = {} }) => {
  try {
    const payload = stripPersonalisation({ results, analysisContext })
    const response = await axios.post(`${API_URL}/scanhistory`, payload, { headers: authHeaders() })

    return {
      id: response.data.id,
      createdAt: response.data.createdAt,
      results,
      analysisContext: { ...analysisContext, historyId: response.data.id }
    }
  } catch {
    return null
  }
}

export const updateAnalysis = async (id, { results, analysisContext = {} }) => {
  try {
    const payload = stripPersonalisation({ results, analysisContext })
    const response = await axios.put(`${API_URL}/scanhistory/${id}`, payload, { headers: authHeaders() })

    return {
      id: response.data.id,
      createdAt: response.data.createdAt,
      results,
      analysisContext: { ...analysisContext, historyId: id }
    }
  } catch {
    return null
  }
}

export const removeAnalysis = async (id) => {
  try {
    await axios.delete(`${API_URL}/scanhistory/${id}`, { headers: authHeaders() })
    return true
  } catch {
    return false
  }
}

export const clearHistory = async () => {
  try {
    await axios.delete(`${API_URL}/scanhistory`, { headers: authHeaders() })
    return true
  } catch {
    return false
  }
}
