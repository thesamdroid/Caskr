import { useState, useEffect, useRef } from 'react'
import { useAppDispatch, useAppSelector } from '../../hooks'
import { verifyCode, sendVerification, setStep } from '../../features/signupSlice'

function VerificationStep() {
  const dispatch = useAppDispatch()
  const { verification, isLoading, formData } = useAppSelector(state => state.signup)
  const [code, setCode] = useState(['', '', '', '', '', ''])
  const [canResend, setCanResend] = useState(false)
  const [resendCountdown, setResendCountdown] = useState(60)
  const inputRefs = useRef<(HTMLInputElement | null)[]>([])

  // Send initial verification code
  useEffect(() => {
    if (formData.email) {
      dispatch(sendVerification(formData.email))
    }
  }, [dispatch, formData.email])

  // Resend countdown timer
  useEffect(() => {
    if (resendCountdown > 0) {
      const timer = setTimeout(() => setResendCountdown(prev => prev - 1), 1000)
      return () => clearTimeout(timer)
    } else {
      setCanResend(true)
    }
  }, [resendCountdown])

  const handleCodeChange = (index: number, value: string) => {
    if (!/^\d*$/.test(value)) return

    const newCode = [...code]
    newCode[index] = value.slice(-1)
    setCode(newCode)

    // Auto-focus next input
    if (value && index < 5) {
      inputRefs.current[index + 1]?.focus()
    }

    // Auto-submit when all digits entered
    if (newCode.every(digit => digit !== '') && index === 5) {
      handleVerify(newCode.join(''))
    }
  }

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === 'Backspace' && !code[index] && index > 0) {
      inputRefs.current[index - 1]?.focus()
    }
  }

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault()
    const pastedData = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6)
    const newCode = [...code]
    for (let i = 0; i < pastedData.length; i++) {
      newCode[i] = pastedData[i]
    }
    setCode(newCode)
    if (pastedData.length === 6) {
      handleVerify(pastedData)
    }
  }

  const handleVerify = (codeString?: string) => {
    const fullCode = codeString || code.join('')
    if (fullCode.length !== 6) return
    dispatch(verifyCode({ email: formData.email, code: fullCode }))
  }

  const handleResend = () => {
    if (!canResend) return
    dispatch(sendVerification(formData.email))
    setCanResend(false)
    setResendCountdown(60)
    setCode(['', '', '', '', '', ''])
    inputRefs.current[0]?.focus()
  }

  const maskedEmail = formData.email.replace(
    /^(.{2})(.*)(@.*)$/,
    (_, start, middle, domain) => `${start}${'*'.repeat(Math.min(middle.length, 5))}${domain}`
  )

  if (verification.isLocked) {
    return (
      <div className="verification-step">
        <div className="verification-locked">
          <div className="locked-icon">&#x1F512;</div>
          <h2>Account Locked</h2>
          <p>
            Too many failed verification attempts. We've sent an email to{' '}
            <strong>{maskedEmail}</strong> with instructions to unlock your account.
          </p>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => dispatch(setStep('register'))}
          >
            Start Over
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="verification-step">
      <div className="verification-icon">&#x2709;</div>
      <h2>Check Your Email</h2>
      <p className="verification-message">
        We sent a 6-digit verification code to <strong>{maskedEmail}</strong>
      </p>
      <p className="verification-hint">
        Enter the code below to verify your email address
      </p>

      <div className="code-input-group" onPaste={handlePaste}>
        {code.map((digit, index) => (
          <input
            key={index}
            ref={el => (inputRefs.current[index] = el)}
            type="text"
            inputMode="numeric"
            pattern="\d*"
            maxLength={1}
            value={digit}
            onChange={e => handleCodeChange(index, e.target.value)}
            onKeyDown={e => handleKeyDown(index, e)}
            className="code-input"
            autoFocus={index === 0}
            disabled={isLoading}
          />
        ))}
      </div>

      <p className="code-expiry">Code expires in 10 minutes</p>

      <button
        type="button"
        className="btn btn-primary btn-block"
        onClick={() => handleVerify()}
        disabled={isLoading || code.some(d => !d)}
      >
        {isLoading ? 'Verifying...' : 'Verify Email'}
      </button>

      <div className="resend-section">
        <p>Didn't receive the code?</p>
        {canResend ? (
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleResend}
            disabled={isLoading || verification.codesSentCount >= 3}
          >
            Resend Code
          </button>
        ) : (
          <span className="resend-countdown">
            Resend available in {resendCountdown}s
          </span>
        )}
        {verification.codesSentCount >= 3 && (
          <p className="resend-limit">Maximum resend attempts reached</p>
        )}
      </div>

      <div className="verification-alternative">
        <p>Or use the magic link sent to your email</p>
      </div>

      <button
        type="button"
        className="btn-link"
        onClick={() => dispatch(setStep('register'))}
      >
        Use a different email
      </button>
    </div>
  )
}

export default VerificationStep
