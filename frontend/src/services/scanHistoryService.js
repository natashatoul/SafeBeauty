import apiClient from './apiClient'

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
    const response = await apiClient.get('/scanhistory')
    return response.data
  } catch (error) {
    if (error.response?.status === 401) {
      window.dispatchEvent(new Event('safebeauty:unauthorized'))
    }
    return null
  }
}

export const saveAnalysis = async ({ results, analysisContext = {} }) => {
  try {
    const payload = stripPersonalisation({ results, analysisContext })
    const response = await apiClient.post('/scanhistory', payload)

    return {
      id: response.data.id,
      createdAt: response.data.createdAt,
      results,
      analysisContext: { ...analysisContext, historyId: response.data.id }
    }
  } catch (error) {
    if (error.response?.status === 401) {
      window.dispatchEvent(new Event('safebeauty:unauthorized'))
    }
    return null
  }
}

export const updateAnalysis = async (id, { results, analysisContext = {} }) => {
  try {
    const payload = stripPersonalisation({ results, analysisContext })
    const response = await apiClient.put(`/scanhistory/${id}`, payload)

    return {
      id: response.data.id,
      createdAt: response.data.createdAt,
      results,
      analysisContext: { ...analysisContext, historyId: id }
    }
  } catch (error) {
    if (error.response?.status === 401) {
      window.dispatchEvent(new Event('safebeauty:unauthorized'))
    }
    return null
  }
}

export const removeAnalysis = async (id) => {
  try {
    await apiClient.delete(`/scanhistory/${id}`)
    return true
  } catch (error) {
    if (error.response?.status === 401) {
      window.dispatchEvent(new Event('safebeauty:unauthorized'))
    }
    return false
  }
}

export const clearHistory = async () => {
  try {
    await apiClient.delete('/scanhistory')
    return true
  } catch (error) {
    if (error.response?.status === 401) {
      window.dispatchEvent(new Event('safebeauty:unauthorized'))
    }
    return false
  }
}
