import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { login } from '../features/authSlice'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')
  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const isLoading = useAppSelector(state => state.auth.isLoading)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setMessage('')

    try {
      await dispatch(login({ email, password })).unwrap()
      setPassword('')
      navigate('/')
    } catch (error) {
      const errorMessage = typeof error === 'string' ? error : 'Login failed'
      setMessage(errorMessage)
    }
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

          <button type="submit" className="button-primary w-full button-lg" disabled={isLoading}>
            Sign In
          </button>
        </form>
      </section>
    </div>
  )
}

export default LoginPage
