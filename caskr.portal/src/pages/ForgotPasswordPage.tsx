import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { forgotPassword, clearError } from '../features/authSlice'

function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const dispatch = useAppDispatch()
  const { isLoading, error } = useAppSelector(state => state.auth)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(clearError())

    try {
      await dispatch(forgotPassword(email)).unwrap()
      setSubmitted(true)
    } catch {
      // Error handled by slice
    }
  }

  if (submitted) {
    return (
      <div className="auth-page">
        <div className="auth-container">
          <div className="auth-header">
            <div className="auth-logo success-icon">&#x2709;</div>
            <h1>Check Your Email</h1>
            <p>
              If an account exists for <strong>{email}</strong>, we've sent a password reset link.
            </p>
          </div>

          <div className="verification-instructions">
            <p>Click the link in the email to reset your password.</p>
            <p className="text-muted">
              Didn't receive the email? Check your spam folder or try again.
            </p>
          </div>

          <button
            onClick={() => setSubmitted(false)}
            className="btn btn-secondary btn-block"
          >
            Try Another Email
          </button>

          <div className="auth-links">
            <Link to="/login" className="auth-link">
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-header">
          <div className="auth-logo">&#x1F511;</div>
          <h1>Forgot Your Password?</h1>
          <p>Enter your email and we'll send you a reset link.</p>
        </div>

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="email">Email Address</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              autoComplete="email"
            />
          </div>

          {error && (
            <div className="form-error" role="alert">
              {error}
            </div>
          )}

          <button type="submit" className="btn btn-primary btn-block" disabled={isLoading}>
            {isLoading ? 'Sending...' : 'Send Reset Link'}
          </button>
        </form>

        <div className="auth-links">
          <Link to="/login" className="auth-link">
            Back to Login
          </Link>
        </div>
      </div>
    </div>
  )
}

export default ForgotPasswordPage
