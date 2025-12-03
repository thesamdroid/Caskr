import { useState } from 'react'
import { Link, useSearchParams, useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { resetPassword, clearError } from '../features/authSlice'

function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const { isLoading, error } = useAppSelector(state => state.auth)

  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [validationError, setValidationError] = useState('')
  const [success, setSuccess] = useState(false)

  const token = searchParams.get('token')

  const validatePassword = (pwd: string): boolean => {
    const hasUppercase = /[A-Z]/.test(pwd)
    const hasLowercase = /[a-z]/.test(pwd)
    const hasNumber = /\d/.test(pwd)
    const hasMinLength = pwd.length >= 8
    return hasUppercase && hasLowercase && hasNumber && hasMinLength
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    dispatch(clearError())
    setValidationError('')

    if (!token) {
      setValidationError('Invalid reset link. Please request a new one.')
      return
    }

    if (password !== confirmPassword) {
      setValidationError('Passwords do not match')
      return
    }

    if (!validatePassword(password)) {
      setValidationError(
        'Password must be at least 8 characters and contain uppercase, lowercase, and a number'
      )
      return
    }

    try {
      await dispatch(resetPassword({ token, newPassword: password })).unwrap()
      setSuccess(true)
    } catch {
      // Error handled by slice
    }
  }

  if (!token) {
    return (
      <div className="auth-page">
        <div className="auth-container">
          <div className="auth-header">
            <div className="auth-logo error-icon">&#x2717;</div>
            <h1>Invalid Reset Link</h1>
            <p>This password reset link is invalid or has expired.</p>
          </div>

          <Link to="/forgot-password" className="btn btn-primary btn-block">
            Request New Reset Link
          </Link>

          <div className="auth-links">
            <Link to="/login" className="auth-link">
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    )
  }

  if (success) {
    return (
      <div className="auth-page">
        <div className="auth-container">
          <div className="auth-header">
            <div className="auth-logo success-icon">&#x2713;</div>
            <h1>Password Reset Successful</h1>
            <p>Your password has been updated successfully.</p>
          </div>

          <button
            onClick={() => navigate('/login')}
            className="btn btn-primary btn-block"
          >
            Sign In with New Password
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-header">
          <div className="auth-logo">&#x1F511;</div>
          <h1>Reset Your Password</h1>
          <p>Enter your new password below.</p>
        </div>

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="password">New Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="Min. 8 characters"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">Confirm New Password</label>
            <input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              placeholder="Confirm password"
              required
            />
          </div>

          <small className="form-hint">
            Password must contain at least 8 characters, one uppercase letter, one lowercase letter, and one number.
          </small>

          {(error || validationError) && (
            <div className="form-error" role="alert">
              {validationError || error}
            </div>
          )}

          <button type="submit" className="btn btn-primary btn-block" disabled={isLoading}>
            {isLoading ? 'Resetting...' : 'Reset Password'}
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

export default ResetPasswordPage
