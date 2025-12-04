import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../hooks'
import { updateFormData, register, checkEmail } from '../../features/signupSlice'
import { calculatePasswordStrength, validatePassword, isValidEmail } from '../../types/signup'

function RegistrationStep() {
  const dispatch = useAppDispatch()
  const { formData, isLoading } = useAppSelector(state => state.signup)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [emailChecking, setEmailChecking] = useState(false)
  const [emailAvailable, setEmailAvailable] = useState<boolean | null>(null)

  const passwordStrength = calculatePasswordStrength(formData.password)

  // Check email availability with debounce
  useEffect(() => {
    if (!formData.email || !isValidEmail(formData.email)) {
      setEmailAvailable(null)
      return
    }

    const timer = setTimeout(async () => {
      setEmailChecking(true)
      try {
        await dispatch(checkEmail(formData.email)).unwrap()
        setEmailAvailable(true)
        setErrors(prev => {
          const { email, ...rest } = prev
          return rest
        })
      } catch (error) {
        setEmailAvailable(false)
        setErrors(prev => ({
          ...prev,
          email: error as string
        }))
      } finally {
        setEmailChecking(false)
      }
    }, 500)

    return () => clearTimeout(timer)
  }, [formData.email, dispatch])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.email) {
      newErrors.email = 'Email is required'
    } else if (!isValidEmail(formData.email)) {
      newErrors.email = 'Please enter a valid email address'
    } else if (emailAvailable === false) {
      newErrors.email = 'An account with this email already exists'
    }

    const passwordValidation = validatePassword(formData.password)
    if (!passwordValidation.valid) {
      newErrors.password = passwordValidation.errors[0]
    }

    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match'
    }

    if (!formData.firstName.trim()) {
      newErrors.firstName = 'First name is required'
    }

    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Last name is required'
    }

    if (!formData.distilleryName.trim()) {
      newErrors.distilleryName = 'Distillery name is required'
    }

    if (!formData.agreedToTerms) {
      newErrors.agreedToTerms = 'You must agree to the terms'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validateForm()) return
    dispatch(register(formData))
  }

  const handleChange = (field: keyof typeof formData, value: string | boolean) => {
    dispatch(updateFormData({ [field]: value }))
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => {
        const { [field]: _, ...rest } = prev
        return rest
      })
    }
  }

  const getStrengthClass = () => {
    switch (passwordStrength.score) {
      case 0:
      case 1:
        return 'strength-weak'
      case 2:
        return 'strength-fair'
      case 3:
        return 'strength-strong'
      case 4:
        return 'strength-very-strong'
      default:
        return ''
    }
  }

  return (
    <form onSubmit={handleSubmit} className="registration-form">
      <h2>Create Your Account</h2>
      <p className="form-subtitle">Start managing your distillery with Caskr</p>

      <div className="form-row">
        <div className="form-group">
          <label htmlFor="firstName">First Name *</label>
          <input
            id="firstName"
            type="text"
            value={formData.firstName}
            onChange={e => handleChange('firstName', e.target.value)}
            placeholder="John"
            autoComplete="given-name"
          />
          {errors.firstName && <span className="form-error-text">{errors.firstName}</span>}
        </div>

        <div className="form-group">
          <label htmlFor="lastName">Last Name *</label>
          <input
            id="lastName"
            type="text"
            value={formData.lastName}
            onChange={e => handleChange('lastName', e.target.value)}
            placeholder="Doe"
            autoComplete="family-name"
          />
          {errors.lastName && <span className="form-error-text">{errors.lastName}</span>}
        </div>
      </div>

      <div className="form-group">
        <label htmlFor="distilleryName">Distillery Name *</label>
        <input
          id="distilleryName"
          type="text"
          value={formData.distilleryName}
          onChange={e => handleChange('distilleryName', e.target.value)}
          placeholder="Kentucky Spirits Distillery"
          autoComplete="organization"
        />
        {errors.distilleryName && <span className="form-error-text">{errors.distilleryName}</span>}
      </div>

      <div className="form-group">
        <label htmlFor="email">Email Address *</label>
        <div className="input-with-status">
          <input
            id="email"
            type="email"
            value={formData.email}
            onChange={e => handleChange('email', e.target.value)}
            placeholder="john@distillery.com"
            autoComplete="email"
            className={emailAvailable === false ? 'input-error' : emailAvailable ? 'input-success' : ''}
          />
          {emailChecking && <span className="input-status checking">Checking...</span>}
          {!emailChecking && emailAvailable === true && (
            <span className="input-status available">&#x2713;</span>
          )}
        </div>
        {errors.email && <span className="form-error-text">{errors.email}</span>}
      </div>

      <div className="form-group">
        <label htmlFor="password">Password *</label>
        <input
          id="password"
          type="password"
          value={formData.password}
          onChange={e => handleChange('password', e.target.value)}
          placeholder="Create a strong password"
          autoComplete="new-password"
        />
        {formData.password && (
          <div className="password-strength">
            <div className={`strength-bar ${getStrengthClass()}`}>
              <div
                className="strength-fill"
                style={{ width: `${(passwordStrength.score + 1) * 20}%` }}
              />
            </div>
            <span className={`strength-label ${getStrengthClass()}`}>
              {passwordStrength.label}
            </span>
          </div>
        )}
        {errors.password && <span className="form-error-text">{errors.password}</span>}
        <div className="password-requirements">
          <small>
            At least 8 characters, one uppercase, one lowercase, one number
          </small>
        </div>
      </div>

      <div className="form-group">
        <label htmlFor="confirmPassword">Confirm Password *</label>
        <input
          id="confirmPassword"
          type="password"
          value={formData.confirmPassword}
          onChange={e => handleChange('confirmPassword', e.target.value)}
          placeholder="Confirm your password"
          autoComplete="new-password"
        />
        {errors.confirmPassword && <span className="form-error-text">{errors.confirmPassword}</span>}
      </div>

      <div className="form-group">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={formData.agreedToTerms}
            onChange={e => handleChange('agreedToTerms', e.target.checked)}
          />
          <span>
            I agree to the{' '}
            <a href="/terms" target="_blank" rel="noopener noreferrer">
              Terms of Service
            </a>{' '}
            and{' '}
            <a href="/privacy" target="_blank" rel="noopener noreferrer">
              Privacy Policy
            </a>
          </span>
        </label>
        {errors.agreedToTerms && <span className="form-error-text">{errors.agreedToTerms}</span>}
      </div>

      {/* Honeypot field for bot prevention */}
      <input
        type="text"
        name="website"
        style={{ display: 'none' }}
        tabIndex={-1}
        autoComplete="off"
      />

      <button
        type="submit"
        className="btn btn-primary btn-block btn-lg"
        disabled={isLoading || emailChecking || emailAvailable === false}
      >
        {isLoading ? 'Creating Account...' : 'Continue'}
      </button>
    </form>
  )
}

export default RegistrationStep
