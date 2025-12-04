import { useNavigate } from 'react-router-dom'
import { useAppSelector } from '../../hooks'

function SignupComplete() {
  const navigate = useNavigate()
  const { formData } = useAppSelector(state => state.signup)

  const handleGetStarted = () => {
    navigate('/dashboard?onboarding=true')
  }

  return (
    <div className="signup-complete">
      <div className="success-icon">&#x1F389;</div>
      <h1>Welcome to Caskr!</h1>
      <p className="welcome-message">
        Congratulations, {formData.firstName}! Your account has been created
        and your workspace is ready.
      </p>

      <div className="next-steps">
        <h3>What's Next?</h3>
        <ul className="steps-list">
          <li>
            <span className="step-number">1</span>
            <span className="step-text">Complete your distillery profile</span>
          </li>
          <li>
            <span className="step-number">2</span>
            <span className="step-text">Connect your accounting integrations</span>
          </li>
          <li>
            <span className="step-number">3</span>
            <span className="step-text">Import your barrel inventory</span>
          </li>
          <li>
            <span className="step-number">4</span>
            <span className="step-text">Invite your team members</span>
          </li>
        </ul>
      </div>

      <div className="email-notice">
        <p>
          We've sent a welcome email to <strong>{formData.email}</strong> with
          your login details and a getting started guide.
        </p>
      </div>

      <button
        type="button"
        className="btn btn-primary btn-lg btn-block"
        onClick={handleGetStarted}
      >
        Go to Dashboard
      </button>

      <p className="support-notice">
        Need help getting started?{' '}
        <a href="/support">Contact our support team</a>
      </p>
    </div>
  )
}

export default SignupComplete
