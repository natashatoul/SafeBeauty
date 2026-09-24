import apiClient from './apiClient'

export const getProfile = async () => {
  try {
    const response = await apiClient.get('/userprofile')
    return response.data
  } catch {
    return null
  }
}

export const saveProfile = async (profile) => {
  try {
    const response = await apiClient.put('/userprofile', profile)
    return response.data
  } catch {
    return null
  }
}
