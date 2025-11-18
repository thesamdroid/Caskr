import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authorizedFetch } from '../api/authorizedFetch'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const response = await authorizedFetch('api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    })
    if (!response.ok) {
      setMessage('Login failed')
      return
    }
    const data = await response.json()
    localStorage.setItem('token', data.token)
    setPassword('')
    navigate('/')
  }

  return (
    <div className="main-content" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: 'calc(100vh - 160px)' }}>
      <section
        className="content-section"
        aria-labelledby="login-title"
        style={{ maxWidth: '500px', width: '100%' }}
      >
        <div className="section-header" style={{ textAlign: 'center', marginBottom: 'var(--space-xl)' }}>
          <div className="barrel-icon" style={{ width: '64px', height: '64px', margin: '0 auto var(--space-lg)', fontSize: '32px' }} aria-hidden="true" />
          <h1 id="login-title" className="section-title" style={{ marginBottom: 'var(--space-sm)' }}>
            Welcome to CASKr
          </h1>
          <p className="section-subtitle">Sign in to your account to continue</p>
        </div>

        <form onSubmit={handleSubmit} aria-label="Login form">
          <div className="form-group">
            <label htmlFor="email">Email Address</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              aria-required="true"
              autoComplete="email"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="Enter your password"
              required
              aria-required="true"
              autoComplete="current-password"
            />
          </div>

          {message && (
            <div
              className="form-error"
              role="alert"
              aria-live="polite"
              style={{ marginBottom: 'var(--space-lg)', padding: 'var(--space-md)', background: 'var(--error-bg)', border: '1px solid var(--error-border)', borderRadius: 'var(--radius-md)' }}
            >
              {message}
            </div>
          )}

          <button type="submit" className="button-primary w-full button-lg">
            Sign In
          </button>
        </form>
      </section>
    </div>
  )
}

export default LoginPage
