import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || '/api'

export const registerUser = async (email, password) => {
  const response = await axios.post(`${API_URL}/account/register`, { email, password })
  return response.data
}

export const loginUser = async (email, password) => {
  const response = await axios.post(`${API_URL}/account/login`, { email, password })
  return response.data // { token }
}

// axios.post(url, body) - sent POST-request with body (convert to JSON)
// returns Promise, await make this promis to real answer
// response.data - it is a body from server (after return Ok(...))
// not all HTTP response. only data
// registerUser sent {email, password} to /account/regester and get back "User regestered successfully"
// loginUser get back Token: ... 
