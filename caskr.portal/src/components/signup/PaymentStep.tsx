import { useState } from 'react'
import { useAppDispatch, useAppSelector } from '../../hooks'
import { createSubscription, completeSetup, setStep, setError } from '../../features/signupSlice'

// Note: In production, this would use Stripe Elements
// For this implementation, we'll create a mock payment form

function PaymentStep() {
  const dispatch = useAppDispatch()
  const { userId, planSelection, isLoading } = useAppSelector(state => state.signup)

  const [formData, setFormData] = useState({
    cardNumber: '',
    cardExpiry: '',
    cardCvc: '',
    cardholderName: '',
    billingAddress: {
      line1: '',
      line2: '',
      city: '',
      state: '',
      postalCode: '',
      country: 'US'
    }
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  const formatCardNumber = (value: string) => {
    const cleaned = value.replace(/\D/g, '').slice(0, 16)
    const groups = cleaned.match(/.{1,4}/g)
    return groups ? groups.join(' ') : cleaned
  }

  const formatExpiry = (value: string) => {
    const cleaned = value.replace(/\D/g, '').slice(0, 4)
    if (cleaned.length > 2) {
      return `${cleaned.slice(0, 2)}/${cleaned.slice(2)}`
    }
    return cleaned
  }

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    const cardNumber = formData.cardNumber.replace(/\s/g, '')
    if (!cardNumber || cardNumber.length < 13) {
      newErrors.cardNumber = 'Please enter a valid card number'
    }

    if (!formData.cardExpiry || formData.cardExpiry.length < 5) {
      newErrors.cardExpiry = 'Please enter a valid expiry date'
    } else {
      const [month, year] = formData.cardExpiry.split('/')
      const now = new Date()
      const expiryDate = new Date(2000 + parseInt(year), parseInt(month) - 1)
      if (expiryDate < now) {
        newErrors.cardExpiry = 'Card has expired'
      }
    }

    if (!formData.cardCvc || formData.cardCvc.length < 3) {
      newErrors.cardCvc = 'Please enter a valid CVC'
    }

    if (!formData.cardholderName.trim()) {
      newErrors.cardholderName = 'Cardholder name is required'
    }

    if (!formData.billingAddress.line1.trim()) {
      newErrors.line1 = 'Address is required'
    }

    if (!formData.billingAddress.city.trim()) {
      newErrors.city = 'City is required'
    }

    if (!formData.billingAddress.state.trim()) {
      newErrors.state = 'State is required'
    }

    if (!formData.billingAddress.postalCode.trim()) {
      newErrors.postalCode = 'ZIP code is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validateForm() || !userId || !planSelection.selectedTierId) return

    try {
      // In production, this would create a Stripe payment method first
      // const paymentMethod = await stripe.createPaymentMethod(...)
      const mockPaymentMethodId = 'pm_mock_' + Date.now()

      await dispatch(createSubscription({
        userId,
        tierId: planSelection.selectedTierId,
        billingCycle: planSelection.billingCycle,
        promoCode: planSelection.promoCode || undefined,
        paymentMethodId: mockPaymentMethodId,
        billingAddress: formData.billingAddress
      })).unwrap()

      // Complete account setup
      await dispatch(completeSetup(userId)).unwrap()
    } catch (error) {
      dispatch(setError(error instanceof Error ? error.message : 'Payment failed'))
    }
  }

  const handleChange = (field: string, value: string) => {
    if (field.startsWith('billingAddress.')) {
      const addressField = field.replace('billingAddress.', '')
      setFormData(prev => ({
        ...prev,
        billingAddress: { ...prev.billingAddress, [addressField]: value }
      }))
    } else {
      setFormData(prev => ({ ...prev, [field]: value }))
    }
    if (errors[field]) {
      setErrors(prev => {
        const { [field]: _, ...rest } = prev
        return rest
      })
    }
  }

  const acceptedCards = ['Visa', 'Mastercard', 'Amex', 'Discover']

  return (
    <form onSubmit={handleSubmit} className="payment-step">
      <h2>Payment Details</h2>
      <p className="form-subtitle">Secure payment processing by Stripe</p>

      {/* Order Summary */}
      <div className="order-summary">
        <h3>Order Summary</h3>
        <div className="summary-row">
          <span>Plan</span>
          <span>
            {planSelection.billingCycle === 'annual' ? 'Annual' : 'Monthly'}
          </span>
        </div>
        {planSelection.promoCode && (
          <div className="summary-row discount">
            <span>Promo: {planSelection.promoCode}</span>
            <span>-20%</span>
          </div>
        )}
        <div className="summary-row total">
          <span>Total Today</span>
          <span>
            ${planSelection.discountedPrice
              ? (planSelection.discountedPrice / 100).toFixed(2)
              : '0.00'}
          </span>
        </div>
        <p className="trial-reminder">
          14-day free trial included. You won't be charged today.
        </p>
      </div>

      {/* Card Information */}
      <div className="form-section">
        <h3>Card Information</h3>
        <div className="accepted-cards">
          {acceptedCards.map(card => (
            <span key={card} className="card-brand">{card}</span>
          ))}
        </div>

        <div className="form-group">
          <label htmlFor="cardNumber">Card Number</label>
          <input
            id="cardNumber"
            type="text"
            inputMode="numeric"
            value={formData.cardNumber}
            onChange={e => handleChange('cardNumber', formatCardNumber(e.target.value))}
            placeholder="1234 5678 9012 3456"
            autoComplete="cc-number"
          />
          {errors.cardNumber && <span className="form-error-text">{errors.cardNumber}</span>}
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="cardExpiry">Expiry Date</label>
            <input
              id="cardExpiry"
              type="text"
              inputMode="numeric"
              value={formData.cardExpiry}
              onChange={e => handleChange('cardExpiry', formatExpiry(e.target.value))}
              placeholder="MM/YY"
              autoComplete="cc-exp"
            />
            {errors.cardExpiry && <span className="form-error-text">{errors.cardExpiry}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="cardCvc">CVC</label>
            <input
              id="cardCvc"
              type="text"
              inputMode="numeric"
              maxLength={4}
              value={formData.cardCvc}
              onChange={e => handleChange('cardCvc', e.target.value.replace(/\D/g, ''))}
              placeholder="123"
              autoComplete="cc-csc"
            />
            {errors.cardCvc && <span className="form-error-text">{errors.cardCvc}</span>}
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="cardholderName">Cardholder Name</label>
          <input
            id="cardholderName"
            type="text"
            value={formData.cardholderName}
            onChange={e => handleChange('cardholderName', e.target.value)}
            placeholder="John Doe"
            autoComplete="cc-name"
          />
          {errors.cardholderName && <span className="form-error-text">{errors.cardholderName}</span>}
        </div>
      </div>

      {/* Billing Address */}
      <div className="form-section">
        <h3>Billing Address</h3>

        <div className="form-group">
          <label htmlFor="line1">Street Address</label>
          <input
            id="line1"
            type="text"
            value={formData.billingAddress.line1}
            onChange={e => handleChange('billingAddress.line1', e.target.value)}
            placeholder="123 Main St"
            autoComplete="address-line1"
          />
          {errors.line1 && <span className="form-error-text">{errors.line1}</span>}
        </div>

        <div className="form-group">
          <label htmlFor="line2">Apt, Suite, etc. (optional)</label>
          <input
            id="line2"
            type="text"
            value={formData.billingAddress.line2}
            onChange={e => handleChange('billingAddress.line2', e.target.value)}
            placeholder="Apt 4B"
            autoComplete="address-line2"
          />
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="city">City</label>
            <input
              id="city"
              type="text"
              value={formData.billingAddress.city}
              onChange={e => handleChange('billingAddress.city', e.target.value)}
              placeholder="Louisville"
              autoComplete="address-level2"
            />
            {errors.city && <span className="form-error-text">{errors.city}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="state">State</label>
            <input
              id="state"
              type="text"
              value={formData.billingAddress.state}
              onChange={e => handleChange('billingAddress.state', e.target.value)}
              placeholder="KY"
              autoComplete="address-level1"
            />
            {errors.state && <span className="form-error-text">{errors.state}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="postalCode">ZIP Code</label>
            <input
              id="postalCode"
              type="text"
              value={formData.billingAddress.postalCode}
              onChange={e => handleChange('billingAddress.postalCode', e.target.value)}
              placeholder="40202"
              autoComplete="postal-code"
            />
            {errors.postalCode && <span className="form-error-text">{errors.postalCode}</span>}
          </div>
        </div>
      </div>

      <div className="payment-actions">
        <button
          type="button"
          className="btn btn-secondary"
          onClick={() => dispatch(setStep('plan'))}
          disabled={isLoading}
        >
          Back
        </button>
        <button
          type="submit"
          className="btn btn-primary btn-lg"
          disabled={isLoading}
        >
          {isLoading ? 'Processing...' : 'Start Free Trial'}
        </button>
      </div>

      <div className="security-notice">
        <span className="lock-icon">&#x1F512;</span>
        Your payment information is encrypted and secure
      </div>
    </form>
  )
}

export default PaymentStep
