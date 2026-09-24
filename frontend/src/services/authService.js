import apiClient from './apiClient'

export const registerUser = async (email, password) => {
  const response = await apiClient.post('/account/register', { email, password })
  return response.data
}

export const loginUser = async (email, password) => {
  await apiClient.post('/account/login', { email, password })
}

export const getSession = async () => {
  await apiClient.get('/account/session')
}

export const logoutUser = async () => {
  await apiClient.post('/account/logout')
}
