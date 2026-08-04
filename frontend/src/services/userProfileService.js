import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || '/api'
const TOKEN_STORAGE_KEY = 'safebeauty_token'
// Bearer token from localstorage
const getToken = () => {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY)
  } catch {
    return null
  }
}
// hheaders are Authorization: `Bearer ${token}`
const authHeaders = () => {
  const token = getToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export const getProfile = async () => {
  try {
    const response = await axios.get(`${API_URL}/userprofile`, { headers: authHeaders() })
    return response.data
  } catch {
    return null
  }
}

export const saveProfile = async (profile) => {
  try {
    const response = await axios.put(`${API_URL}/userprofile`, profile, { headers: authHeaders() })
    return response.data
  } catch {
    return null
  }
}
