import { createRoot } from 'react-dom/client'
import { ProfileProvider } from './context/ProfileContext.jsx'
import App from './App.jsx'

createRoot(document.getElementById('root')).render(
  <ProfileProvider>
    <App />
  </ProfileProvider>,
)
