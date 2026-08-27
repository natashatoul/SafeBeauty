import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { registerUser } from '../services/authService'

function RegisterPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [registered, setRegistered] = useState(false) // we need it to change screens

  const navigate = useNavigate()

  // sent data to backend
  const handleSubmit = async (event) => {
    event.preventDefault()
    setLoading(true)
    setError('')

    try {
      await registerUser(email, password)
      setRegistered(true)
    } catch (err) {
      if (!err.response) {
        setError('Could not reach the server. Please check your connection and try again.')
      } else {
        const backendMessage = err.response.data?.[0]?.description
        setError(backendMessage || 'Could not create an account. Please check your details and try again.')
      }
     } finally {
      setLoading(false)
    }
  }



    // After a successful register, the backend sends a verification email —
    // show that instead of logging the user in immediately.
    if (registered) {
      return (
        <div className="login-page">
          <div className="login-card">
            <h1>Check your email</h1>
            <p>We sent a verification link to {email}. Confirm it, then log in.</p>
            <button type="button" className="primary-button" onClick={() => navigate('/login')}>
              Go to login
            </button>
          </div>
        </div>
      )
    }

    return (
      <div className="login-page">
        <div className="login-card">
          <h1>Create an account</h1>
          <p>Sign up to save your profile and scan history.</p>

          <form onSubmit={handleSubmit}>
            <label htmlFor="register-email">Email</label>
            <input
              id="register-email"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="you@example.com"
              required
            />

            <label htmlFor="register-password">Password</label>
            <input
              id="register-password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />

            {error && <p className="login-error" role="alert">{error}</p>}

            <button type="submit" className="primary-button" disabled={loading}>
              {loading ? 'Creating account...' : 'Create account'}
            </button>
          </form>

          <p className="login-footer-link">
            Already have an account? <Link to="/login">Log in</Link>
          </p>
        </div>
      </div>
    )
  }

  export default RegisterPage
