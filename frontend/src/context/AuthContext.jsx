import { createContext, useContext, useEffect, useState } from 'react'
import { getSession, logoutUser } from '../services/authService'

const AuthContext = createContext()

export function AuthProvider({ children }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isAuthReady, setIsAuthReady] = useState(false)

  useEffect(() => {
    const handleUnauthorized = () => {
      setIsAuthenticated(false)
    }

    window.addEventListener('safebeauty:unauthorized', handleUnauthorized)

    getSession()
      .then(() => setIsAuthenticated(true))
      .catch(() => setIsAuthenticated(false))
      .finally(() => setIsAuthReady(true))

    return () => {
      window.removeEventListener('safebeauty:unauthorized', handleUnauthorized)
    }
  }, [])

  const login = () => {
    setIsAuthenticated(true)
  }

  const logout = async () => {
    try {
      await logoutUser()
    } finally {
      setIsAuthenticated(false)
    }
  }
  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        isAuthReady,
        login,
        logout
      }}
    >
      {isAuthReady ? (
        children
      ) : (
        <div className="auth-loading-screen" role="status" aria-live="polite">
          Checking your session...
        </div>
      )}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  return useContext(AuthContext)
}
