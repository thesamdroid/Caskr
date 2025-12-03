import { useEffect, useState } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { verifyEmail } from '../features/authSlice'

function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const dispatch = useAppDispatch()
  const { isLoading, error } = useAppSelector(state => state.auth)
  const [verificationStatus, setVerificationStatus] = useState<'pending' | 'success' | 'error'>('pending')

  const token = searchParams.get('token')
  const registeredEmail = location.state?.email
  const justRegistered = location.state?.registered

  useEffect(() => {
    if (token) {
      dispatch(verifyEmail(token))
        .unwrap()
        .then(() => setVerificationStatus('success'))
        .catch(() => setVerificationStatus('error'))
    }
  }, [token, dispatch])

  // Just registered - show instructions
  if (justRegistered && !token) {
    return (
      <div className="auth-page">
        <div className="auth-container">
          <div className="auth-header">
            <div className="auth-logo success-icon">&#x2709;</div>
            <h1>Check Your Email</h1>
            <p>
              We've sent a verification link to{' '}
              <strong>{registeredEmail || 'your email address'}</strong>
            </p>
          </div>

          <div className="verification-instructions">
            <p>Please check your inbox and click the verification link to activate your account.</p>
            <p className="text-muted">
              If you don't see the email, check your spam folder or{' '}
              <Link to="/register">try registering again</Link>.
            </p>
          </div>

          <div className="auth-links">
            <Link to="/login" className="auth-link">
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    )
  }

  // Verifying token
  if (token && isLoading) {
    return (
      <div className="auth-page">
        <div className="auth-container">
          <div className="auth-header">
            <div className="auth-logo loading-icon">&#x23F3;</div>
            <h1>Verifying Your Email</h1>
            <p>Please wait...</p>
          </div>
        </div>
      </div>
    )
  }

  // Verification success
  if (verificationStatus === 'success') {
    return (
      <div className="auth-page">
        <div className="auth-container">
          <div className="auth-header">
            <div className="auth-logo success-icon">&#x2713;</div>
            <h1>Email Verified!</h1>
            <p>Your account has been verified successfully.</p>
          </div>

          <Link to="/login" className="btn btn-primary btn-block">
            Sign In to Your Account
          </Link>
        </div>
      </div>
    )
  }

  // Verification error
  if (verificationStatus === 'error' || error) {
    return (
      <div className="auth-page">
        <div className="auth-container">
          <div className="auth-header">
            <div className="auth-logo error-icon">&#x2717;</div>
            <h1>Verification Failed</h1>
            <p>{error || 'Invalid or expired verification link.'}</p>
          </div>

          <div className="auth-links">
            <Link to="/register" className="auth-link">
              Register Again
            </Link>
            <span className="auth-divider">|</span>
            <Link to="/login" className="auth-link">
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    )
  }

  // Default - no token and not just registered
  return (
    <div className="auth-page">
      <div className="auth-container">
        <div className="auth-header">
          <div className="auth-logo">&#x2709;</div>
          <h1>Verify Your Email</h1>
          <p>Please check your email for a verification link.</p>
        </div>

        <div className="auth-links">
          <Link to="/login" className="auth-link">
            Back to Login
          </Link>
        </div>
      </div>
    </div>
  )
}

export default VerifyEmailPage
