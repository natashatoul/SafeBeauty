import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || '/api'

const apiClient = axios.create({
  baseURL: API_URL,
  withCredentials: true
})

const stateChangingMethods = new Set(['post', 'put', 'patch', 'delete'])

apiClient.interceptors.request.use((config) => {
  if (stateChangingMethods.has(config.method?.toLowerCase())) {
    config.headers['X-CSRF-Protection'] = '1'
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      window.dispatchEvent(new Event('safebeauty:unauthorized'))
    }

    return Promise.reject(error)
  }
)

export default apiClient
