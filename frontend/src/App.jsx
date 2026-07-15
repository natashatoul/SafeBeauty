import { BrowserRouter, Link, Routes, Route } from 'react-router-dom'
import HomePage from './pages/HomePage'
import ScanPage from './pages/ScanPage'
import ManualInputPage from './pages/ManualInputPage'
import ResultsPage from './pages/ResultsPage'
import ProfilePage from './pages/ProfilePage'
import HistoryPage from './pages/HistoryPage'
import Navbar from './components/Navbar'
import desktopLogo from './assets/Logo_desktop.svg'
import './App.css'

function App() {
  return (
    <BrowserRouter basename="/SafeBeauty">
      <div className="app-shell">
        <Navbar />
        <main className="app-main">
          <Link to="/" className="mobile-brand" aria-label="SafeBeauty home">
            <img src={desktopLogo} alt="SafeBeauty" />
          </Link>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/scan" element={<ScanPage />} />
            <Route path="/manual" element={<ManualInputPage />} />
            <Route path="/results" element={<ResultsPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/history" element={<HistoryPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}

export default App
