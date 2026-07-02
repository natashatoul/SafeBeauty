import { createRoot } from 'react-dom/client'
import { ProfileProvider } from './context/ProfileContext.jsx'
import './index.css'
import App from './App.jsx'

createRoot(document.getElementById('root')).render(
  <ProfileProvider>
    <App />
  </ProfileProvider>,
)
