import { createRoot } from 'react-dom/client'
import { ProfileProvider } from './context/ProfileContext.jsx'
import { AuthProvider } from './context/AuthContext.jsx'
import App from './App.jsx'

createRoot(document.getElementById('root')).render(
  <AuthProvider>
    <ProfileProvider>
      <App />
    </ProfileProvider>
  </AuthProvider>,
)
