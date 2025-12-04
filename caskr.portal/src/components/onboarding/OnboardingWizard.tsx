import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useAppSelector } from '../../hooks'
import * as signupApi from '../../api/signupApi'
import type { OnboardingStep, TeamInvite } from '../../types/signup'

interface OnboardingState {
  currentStep: OnboardingStep
  completedSteps: OnboardingStep[]
  isLoading: boolean
  error: string | null
}

function OnboardingWizard() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const user = useAppSelector(state => state.auth.user)
  const [showWizard, setShowWizard] = useState(false)

  const [state, setState] = useState<OnboardingState>({
    currentStep: 'profile',
    completedSteps: [],
    isLoading: false,
    error: null
  })

  // Profile form data
  const [profileData, setProfileData] = useState({
    name: '',
    address: '',
    city: '',
    state: '',
    postalCode: '',
    dspNumber: '',
    ttbPermitId: ''
  })

  // Team invites
  const [teamInvites, setTeamInvites] = useState<TeamInvite[]>([
    { email: '', role: 'viewer' }
  ])

  // Check if onboarding is needed
  useEffect(() => {
    if (searchParams.get('onboarding') === 'true' && user) {
      setShowWizard(true)
    }
  }, [searchParams, user])

  // Load onboarding progress
  useEffect(() => {
    if (user && showWizard) {
      const loadProgress = async () => {
        try {
          const progress = await signupApi.getOnboardingProgress(user.id)
          setState(prev => ({
            ...prev,
            completedSteps: progress.completedSteps as OnboardingStep[]
          }))
        } catch {
          // Start fresh if can't load progress
        }
      }
      loadProgress()
    }
  }, [user, showWizard])

  const steps: { id: OnboardingStep; label: string }[] = [
    { id: 'profile', label: 'Distillery Profile' },
    { id: 'integrations', label: 'Connect Integrations' },
    { id: 'import', label: 'Import Data' },
    { id: 'team', label: 'Invite Team' },
    { id: 'tour', label: 'Quick Tour' }
  ]

  const currentStepIndex = steps.findIndex(s => s.id === state.currentStep)

  const handleCompleteStep = async () => {
    if (!user) return

    setState(prev => ({ ...prev, isLoading: true, error: null }))
    try {
      await signupApi.completeOnboardingStep(user.id, state.currentStep)

      const newCompleted = [...state.completedSteps, state.currentStep]
      const nextStepIndex = currentStepIndex + 1

      if (nextStepIndex < steps.length) {
        setState(prev => ({
          ...prev,
          completedSteps: newCompleted,
          currentStep: steps[nextStepIndex].id,
          isLoading: false
        }))
      } else {
        setState(prev => ({
          ...prev,
          completedSteps: newCompleted,
          currentStep: 'complete',
          isLoading: false
        }))
        setShowWizard(false)
        navigate('/dashboard')
      }
    } catch (error) {
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Failed to save progress'
      }))
    }
  }

  const handleSkip = () => {
    const nextStepIndex = currentStepIndex + 1
    if (nextStepIndex < steps.length) {
      setState(prev => ({
        ...prev,
        currentStep: steps[nextStepIndex].id
      }))
    } else {
      setShowWizard(false)
      navigate('/dashboard')
    }
  }

  const handleClose = () => {
    setShowWizard(false)
    navigate('/dashboard')
  }

  const handleSaveProfile = async () => {
    if (!user) return
    setState(prev => ({ ...prev, isLoading: true }))
    try {
      await signupApi.saveDistilleryProfile(user.id, profileData)
      await handleCompleteStep()
    } catch (error) {
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Failed to save profile'
      }))
    }
  }

  const handleInviteTeam = async () => {
    if (!user) return
    const validInvites = teamInvites.filter(i => i.email.trim())
    if (validInvites.length === 0) {
      await handleCompleteStep()
      return
    }

    setState(prev => ({ ...prev, isLoading: true }))
    try {
      await signupApi.inviteTeamMembers(user.id, validInvites)
      await handleCompleteStep()
    } catch (error) {
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Failed to send invites'
      }))
    }
  }

  const addTeamInvite = () => {
    setTeamInvites(prev => [...prev, { email: '', role: 'viewer' }])
  }

  const updateTeamInvite = (index: number, field: keyof TeamInvite, value: string) => {
    setTeamInvites(prev => {
      const newInvites = [...prev]
      newInvites[index] = { ...newInvites[index], [field]: value }
      return newInvites
    })
  }

  const removeTeamInvite = (index: number) => {
    setTeamInvites(prev => prev.filter((_, i) => i !== index))
  }

  if (!showWizard) return null

  return (
    <div className="onboarding-overlay">
      <div className="onboarding-wizard">
        <div className="wizard-header">
          <h2>Complete Your Setup</h2>
          <button type="button" className="wizard-close" onClick={handleClose}>
            &times;
          </button>
        </div>

        <div className="wizard-progress">
          {steps.map((step, index) => (
            <div
              key={step.id}
              className={`wizard-step ${
                state.completedSteps.includes(step.id)
                  ? 'completed'
                  : step.id === state.currentStep
                  ? 'active'
                  : ''
              }`}
            >
              <div className="step-indicator">
                {state.completedSteps.includes(step.id) ? '&#x2713;' : index + 1}
              </div>
              <span className="step-label">{step.label}</span>
            </div>
          ))}
        </div>

        {state.error && (
          <div className="wizard-error">{state.error}</div>
        )}

        <div className="wizard-content">
          {/* Step 1: Profile */}
          {state.currentStep === 'profile' && (
            <div className="wizard-step-content">
              <h3>Distillery Profile</h3>
              <p>Tell us about your distillery</p>

              <div className="form-group">
                <label>Distillery Name</label>
                <input
                  type="text"
                  value={profileData.name}
                  onChange={e => setProfileData(prev => ({ ...prev, name: e.target.value }))}
                  placeholder="Kentucky Spirits Distillery"
                />
              </div>

              <div className="form-group">
                <label>Address</label>
                <input
                  type="text"
                  value={profileData.address}
                  onChange={e => setProfileData(prev => ({ ...prev, address: e.target.value }))}
                  placeholder="123 Bourbon Lane"
                />
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>City</label>
                  <input
                    type="text"
                    value={profileData.city}
                    onChange={e => setProfileData(prev => ({ ...prev, city: e.target.value }))}
                  />
                </div>
                <div className="form-group">
                  <label>State</label>
                  <input
                    type="text"
                    value={profileData.state}
                    onChange={e => setProfileData(prev => ({ ...prev, state: e.target.value }))}
                  />
                </div>
                <div className="form-group">
                  <label>ZIP</label>
                  <input
                    type="text"
                    value={profileData.postalCode}
                    onChange={e => setProfileData(prev => ({ ...prev, postalCode: e.target.value }))}
                  />
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>DSP Number (optional)</label>
                  <input
                    type="text"
                    value={profileData.dspNumber}
                    onChange={e => setProfileData(prev => ({ ...prev, dspNumber: e.target.value }))}
                    placeholder="DSP-KY-12345"
                  />
                </div>
                <div className="form-group">
                  <label>TTB Permit ID (optional)</label>
                  <input
                    type="text"
                    value={profileData.ttbPermitId}
                    onChange={e => setProfileData(prev => ({ ...prev, ttbPermitId: e.target.value }))}
                  />
                </div>
              </div>

              <div className="wizard-actions">
                <button type="button" className="btn btn-secondary" onClick={handleSkip}>
                  Skip for now
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleSaveProfile}
                  disabled={state.isLoading}
                >
                  {state.isLoading ? 'Saving...' : 'Continue'}
                </button>
              </div>
            </div>
          )}

          {/* Step 2: Integrations */}
          {state.currentStep === 'integrations' && (
            <div className="wizard-step-content">
              <h3>Connect Integrations</h3>
              <p>Link your accounting and banking systems (optional)</p>

              <div className="integration-options">
                <div className="integration-card">
                  <div className="integration-icon">QB</div>
                  <div className="integration-info">
                    <h4>QuickBooks</h4>
                    <p>Sync your financial data automatically</p>
                  </div>
                  <button type="button" className="btn btn-secondary btn-small">
                    Connect
                  </button>
                </div>

                <div className="integration-card">
                  <div className="integration-icon">Bank</div>
                  <div className="integration-info">
                    <h4>Bank Feeds</h4>
                    <p>Import transactions from your bank</p>
                  </div>
                  <button type="button" className="btn btn-secondary btn-small">
                    Connect
                  </button>
                </div>
              </div>

              <div className="wizard-actions">
                <button type="button" className="btn btn-secondary" onClick={handleSkip}>
                  Skip for now
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleCompleteStep}
                  disabled={state.isLoading}
                >
                  Continue
                </button>
              </div>
            </div>
          )}

          {/* Step 3: Import Data */}
          {state.currentStep === 'import' && (
            <div className="wizard-step-content">
              <h3>Import Your Barrels</h3>
              <p>Add your existing barrel inventory</p>

              <div className="import-options">
                <div className="import-option">
                  <input type="radio" name="import" id="import-csv" />
                  <label htmlFor="import-csv">
                    <strong>Import from CSV</strong>
                    <span>Upload a spreadsheet of your barrels</span>
                  </label>
                </div>
                <div className="import-option">
                  <input type="radio" name="import" id="import-manual" />
                  <label htmlFor="import-manual">
                    <strong>Add Manually</strong>
                    <span>Enter barrels one by one</span>
                  </label>
                </div>
                <div className="import-option">
                  <input type="radio" name="import" id="import-skip" defaultChecked />
                  <label htmlFor="import-skip">
                    <strong>Start Fresh</strong>
                    <span>I'll add barrels later</span>
                  </label>
                </div>
              </div>

              <div className="wizard-actions">
                <button type="button" className="btn btn-secondary" onClick={handleSkip}>
                  Skip for now
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleCompleteStep}
                  disabled={state.isLoading}
                >
                  Continue
                </button>
              </div>
            </div>
          )}

          {/* Step 4: Team */}
          {state.currentStep === 'team' && (
            <div className="wizard-step-content">
              <h3>Invite Your Team</h3>
              <p>Add team members to collaborate (optional)</p>

              <div className="team-invites">
                {teamInvites.map((invite, index) => (
                  <div key={index} className="invite-row">
                    <input
                      type="email"
                      value={invite.email}
                      onChange={e => updateTeamInvite(index, 'email', e.target.value)}
                      placeholder="colleague@distillery.com"
                    />
                    <select
                      value={invite.role}
                      onChange={e => updateTeamInvite(index, 'role', e.target.value as TeamInvite['role'])}
                    >
                      <option value="admin">Admin</option>
                      <option value="manager">Manager</option>
                      <option value="viewer">Viewer</option>
                    </select>
                    {teamInvites.length > 1 && (
                      <button
                        type="button"
                        className="btn-icon"
                        onClick={() => removeTeamInvite(index)}
                      >
                        &times;
                      </button>
                    )}
                  </div>
                ))}
                <button type="button" className="btn btn-secondary btn-small" onClick={addTeamInvite}>
                  + Add Another
                </button>
              </div>

              <div className="wizard-actions">
                <button type="button" className="btn btn-secondary" onClick={handleSkip}>
                  Skip for now
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleInviteTeam}
                  disabled={state.isLoading}
                >
                  {state.isLoading ? 'Sending...' : 'Send Invites'}
                </button>
              </div>
            </div>
          )}

          {/* Step 5: Tour */}
          {state.currentStep === 'tour' && (
            <div className="wizard-step-content">
              <h3>Quick Tour</h3>
              <p>Learn the basics of Caskr</p>

              <div className="tour-highlights">
                <div className="tour-item">
                  <div className="tour-icon">&#x1F4CA;</div>
                  <h4>Dashboard</h4>
                  <p>See your inventory at a glance</p>
                </div>
                <div className="tour-item">
                  <div className="tour-icon">&#x1F6E2;</div>
                  <h4>Barrels</h4>
                  <p>Track every barrel's journey</p>
                </div>
                <div className="tour-item">
                  <div className="tour-icon">&#x1F4DD;</div>
                  <h4>Reports</h4>
                  <p>Generate TTB-compliant reports</p>
                </div>
              </div>

              <div className="wizard-actions">
                <button
                  type="button"
                  className="btn btn-primary btn-lg"
                  onClick={handleCompleteStep}
                  disabled={state.isLoading}
                >
                  Get Started!
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

export default OnboardingWizard
