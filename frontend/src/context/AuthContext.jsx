import { createContext, useContext, useEffect, useState } from 'react'

// can subscribe to via useAuth(), instead of passing the token down
// through props at every level.
// it is a global storage and here will be data user logged in or no
const AuthContext = createContext()

const TOKEN_STORAGE_KEY = 'safebeauty_token'

function loadSavedToken() {
  try {
    const savedToken = localStorage.getItem(TOKEN_STORAGE_KEY)
    if (!savedToken) return null

    const payload = JSON.parse(atob(savedToken.split('.')[1]))
    if (typeof payload.exp === 'number' && payload.exp * 1000 <= Date.now()) {
      localStorage.removeItem(TOKEN_STORAGE_KEY)
      return null
    }

    return savedToken
  } catch {
    // Some browsers throw if localStorage is blocked (private mode,
    // strict cookie settings). Treat that the same as "not logged in"
    // rather than crashing the whole app.
    return null
  }
}

// this component wrap all application to avoid prop drilling
export function AuthProvider({ children }) {
  // token is either a JWT string, or null when the user isn't logged in.
  const [token, setToken] = useState(loadSavedToken)

  useEffect(() => {
    const handleUnauthorized = () => {
      setToken(null)
      localStorage.removeItem(TOKEN_STORAGE_KEY)
    }

    window.addEventListener('safebeauty:unauthorized', handleUnauthorized)
    return () => window.removeEventListener('safebeauty:unauthorized', handleUnauthorized)
  }, [])

  // Called after a successful login response from the backend.
  const login = (newToken) => {
    setToken(newToken)
    localStorage.setItem(TOKEN_STORAGE_KEY, newToken)
  }

  const logout = () => {
    setToken(null)
    localStorage.removeItem(TOKEN_STORAGE_KEY)
  }


  // value it is a prop to all childrens component of app

  return (
    <AuthContext.Provider
      value={{
        token,
        isAuthenticated: !!token,
        login,
        logout
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

// it is a custom hook that helps any componebts on any level take access to 
// const { user, login } = useAuth()
// without parants
// useContext it is a standart hook from React library
export function useAuth() {
  return useContext(AuthContext)
}

// if don't use this hook we need
// 1. Import system hook React
// import { useContext } from 'react'; 
// // 2. Impport object of context from provider file
// import { AuthContext } from './AuthProvider'; 

// export function Header() {
//   // 3. Give context to hook
//   const auth = useContext(AuthContext); 

//   return (
//     <header>
//       {/* 4. Call to data through point: auth.isAuthenticated */}
//       {auth.isAuthenticated ? (
//         <button onClick={auth.logout}>Log out</button>
//       ) : (
//         <button>Log in</button>
//       )}
//     </header>
//   );
// }
