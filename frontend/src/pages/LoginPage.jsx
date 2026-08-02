import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { loginUser } from '../services/authService'
import { useAuth } from '../context/AuthContext'

function LoginPage() {
  const [email, setEmail] = useState('') // user do
  const [password, setPassword] = useState('') // user do
  const [loading, setLoading] = useState(false) // status true / false
  const [error, setError] = useState('') // text of error like wrong password

  const navigate = useNavigate() // hook from library react-router-dom for navigation "/." home fer exam
  const { login } = useAuth() // hook from AuthContext we take only login func to save token and status log in
  
  
  // this func call when click on the button log in
  const handleSubmit = async (event) => {
    event.preventDefault() // by default browser reload the page, but this comand cancel standart behaviour of browser
    setLoading(true) // on mode loading and
    setError('') // clean previous errors

    try {
        // if server answer success we have token and pass this token to the var function login(data.token)
      const data = await loginUser(email, password)
      login(data.token) // global function
      navigate('/') // redirect to the home page
    } catch {
      // 401 from the backend, network failure — same message either way,
      // so a failed login doesn't reveal which part was wrong.
      setError('Invalid email or password. Please try again.')
    } finally {
      setLoading(false) // off loading mode
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <h1>Welcome back</h1>
        <p>Log in to see your saved profile and scan history.</p>

        <form onSubmit={handleSubmit}>
          <label htmlFor="login-email">Email</label>
          <input // linked to the useState
            id="login-email"
            type="email"
            value={email} // prop value from useState
            // when user type letter 
            onChange={(event) => setEmail(event.target.value)} 
            placeholder="you@example.com"
            required
          />

          <label htmlFor="login-password">Password</label>
          <input
            id="login-password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />

          {error && <p className="login-error" role="alert">{error}</p>} {/* if in error field there is text - show it, if not - nothing show */}

          <button type="submit" className="primary-button" disabled={loading}>
            {loading ? 'Logging in...' : 'Log in'}
          </button>
        </form>

        <div className="login-divider"><span>or</span></div>

        <button type="button" className="secondary-button" onClick={() => navigate('/')}>
          Continue as guest
        </button>

        <p className="login-footer-link">
          New here? <Link to="/register">Create an account</Link>
        </p>
      </div>
    </div>
  )
}

export default LoginPage
