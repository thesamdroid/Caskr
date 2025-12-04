import { useEffect } from 'react'
import { useSearchParams, useNavigate, Link } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../hooks'
import { initFromParams, resetSignup } from '../features/signupSlice'
import RegistrationStep from '../components/signup/RegistrationStep'
import VerificationStep from '../components/signup/VerificationStep'
import PlanSelectionStep from '../components/signup/PlanSelectionStep'
import PaymentStep from '../components/signup/PaymentStep'
import CreatingAccountStep from '../components/signup/CreatingAccountStep'
import SignupComplete from '../components/signup/SignupComplete'

const stepLabels = ['Register', 'Verify', 'Plan', 'Payment', 'Complete']
const stepNumbers: Record<string, number> = {
  register: 1,
  verify: 2,
  plan: 3,
  payment: 4,
  creating: 4,
  complete: 5
}

function SignupPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { currentStep, error } = useAppSelector(state => state.signup)
  const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated)

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate('/dashboard')
    }
  }, [isAuthenticated, navigate])

  // Initialize from URL params
  useEffect(() => {
    const plan = searchParams.get('plan')
    const promo = searchParams.get('promo')
    if (plan || promo) {
      dispatch(initFromParams({ plan: plan || undefined, promo: promo || undefined }))
    }
  }, [dispatch, searchParams])

  // Reset on unmount
  useEffect(() => {
    return () => {
      // Don't reset if completed
      if (currentStep !== 'complete') {
        dispatch(resetSignup())
      }
    }
  }, [dispatch, currentStep])

  const currentStepNumber = stepNumbers[currentStep]

  return (
    <div className="signup-page">
      <div className="signup-container">
        {/* Header */}
        <div className="signup-header">
          <Link to="/" className="signup-logo">
            <span className="logo-icon">&#x2615;</span>
            <span className="logo-text">Caskr</span>
          </Link>
        </div>

        {/* Progress Bar */}
        {currentStep !== 'complete' && (
          <div className="signup-progress">
            <div className="progress-steps">
              {stepLabels.map((label, index) => (
                <div
                  key={label}
                  className={`progress-step ${
                    index + 1 < currentStepNumber
                      ? 'completed'
                      : index + 1 === currentStepNumber
                      ? 'active'
                      : ''
                  }`}
                >
                  <div className="step-circle">{index + 1}</div>
                  <span className="step-label">{label}</span>
                </div>
              ))}
            </div>
            <div className="progress-bar">
              <div
                className="progress-fill"
                style={{ width: `${((currentStepNumber - 1) / (stepLabels.length - 1)) * 100}%` }}
              />
            </div>
          </div>
        )}

        {/* Error Display */}
        {error && (
          <div className="signup-error">
            {error}
          </div>
        )}

        {/* Step Content */}
        <div className="signup-content">
          {currentStep === 'register' && <RegistrationStep />}
          {currentStep === 'verify' && <VerificationStep />}
          {currentStep === 'plan' && <PlanSelectionStep />}
          {currentStep === 'payment' && <PaymentStep />}
          {currentStep === 'creating' && <CreatingAccountStep />}
          {currentStep === 'complete' && <SignupComplete />}
        </div>

        {/* Footer */}
        {currentStep === 'register' && (
          <div className="signup-footer">
            <p>
              Already have an account?{' '}
              <Link to="/login" className="auth-link">
                Sign in
              </Link>
            </p>
          </div>
        )}
      </div>
    </div>
  )
}

export default SignupPage
