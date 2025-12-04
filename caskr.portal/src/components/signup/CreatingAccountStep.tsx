function CreatingAccountStep() {
  return (
    <div className="creating-account-step">
      <div className="creating-animation">
        <div className="loading-spinner large" />
      </div>
      <h2>Creating Your Account</h2>
      <p>Please wait while we set up your distillery workspace...</p>
      <div className="creation-steps">
        <div className="creation-step completed">
          <span className="step-icon">&#x2713;</span>
          Setting up your organization
        </div>
        <div className="creation-step completed">
          <span className="step-icon">&#x2713;</span>
          Configuring your subscription
        </div>
        <div className="creation-step active">
          <span className="step-icon loading">&#x25CF;</span>
          Generating API keys
        </div>
        <div className="creation-step">
          <span className="step-icon">&#x25CB;</span>
          Sending welcome email
        </div>
      </div>
    </div>
  )
}

export default CreatingAccountStep
