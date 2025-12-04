import { useState, useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../hooks'
import { setPlanSelection, setStep } from '../../features/signupSlice'
import type { PricingTier } from '../../types/pricing'

function PlanSelectionStep() {
  const dispatch = useAppDispatch()
  const { planSelection } = useAppSelector(state => state.signup)
  const [tiers, setTiers] = useState<PricingTier[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [promoCode, setPromoCode] = useState(planSelection.promoCode || '')
  const [promoValidating, setPromoValidating] = useState(false)
  const [promoValid, setPromoValid] = useState<boolean | null>(null)
  const [promoError, setPromoError] = useState<string | null>(null)

  // Fetch pricing tiers
  useEffect(() => {
    const fetchTiers = async () => {
      try {
        const response = await fetch('/api/public/pricing/tiers')
        if (response.ok) {
          const data = await response.json()
          setTiers(data)
          // Pre-select popular tier if none selected
          if (!planSelection.selectedTierId) {
            const popularTier = data.find((t: PricingTier) => t.isPopular)
            if (popularTier) {
              dispatch(setPlanSelection({ selectedTierId: popularTier.id }))
            }
          }
        }
      } catch (error) {
        console.error('Failed to fetch tiers:', error)
      } finally {
        setIsLoading(false)
      }
    }
    fetchTiers()
  }, [dispatch, planSelection.selectedTierId])

  const handlePromoValidate = async () => {
    if (!promoCode.trim()) return

    setPromoValidating(true)
    setPromoError(null)
    try {
      const response = await fetch('/api/public/pricing/validate-promo', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          code: promoCode,
          tierId: planSelection.selectedTierId
        })
      })
      const result = await response.json()
      if (result.isValid) {
        setPromoValid(true)
        dispatch(setPlanSelection({
          promoCode,
          discountedPrice: result.discountedPriceCents
        }))
      } else {
        setPromoValid(false)
        setPromoError(result.message || 'Invalid promo code')
      }
    } catch {
      setPromoError('Failed to validate promo code')
    } finally {
      setPromoValidating(false)
    }
  }

  const formatPrice = (cents?: number) => {
    if (cents === undefined || cents === null) return 'Custom'
    return `$${(cents / 100).toFixed(0)}`
  }

  const getMonthlyPrice = (tier: PricingTier): number => {
    if (planSelection.billingCycle === 'annual') {
      return tier.annualPriceCents ? tier.annualPriceCents / 12 : 0
    }
    return tier.monthlyPriceCents || 0
  }

  const handleContinue = () => {
    if (!planSelection.selectedTierId) return
    dispatch(setStep('payment'))
  }

  if (isLoading) {
    return (
      <div className="plan-selection-step">
        <div className="loading-state">
          <div className="loading-spinner" />
          <p>Loading plans...</p>
        </div>
      </div>
    )
  }

  const selectedTier = tiers.find(t => t.id === planSelection.selectedTierId)

  return (
    <div className="plan-selection-step">
      <h2>Choose Your Plan</h2>
      <p className="form-subtitle">Select the plan that best fits your distillery</p>

      {/* Billing Cycle Toggle */}
      <div className="billing-toggle">
        <button
          type="button"
          className={`toggle-option ${planSelection.billingCycle === 'monthly' ? 'active' : ''}`}
          onClick={() => dispatch(setPlanSelection({ billingCycle: 'monthly' }))}
        >
          Monthly
        </button>
        <button
          type="button"
          className={`toggle-option ${planSelection.billingCycle === 'annual' ? 'active' : ''}`}
          onClick={() => dispatch(setPlanSelection({ billingCycle: 'annual' }))}
        >
          Annual
          <span className="savings-badge">Save 20%</span>
        </button>
      </div>

      {/* Plan Cards */}
      <div className="plan-cards">
        {tiers.filter(t => t.isActive && !t.isCustomPricing).map(tier => (
          <div
            key={tier.id}
            className={`plan-card ${planSelection.selectedTierId === tier.id ? 'selected' : ''} ${tier.isPopular ? 'popular' : ''}`}
            onClick={() => dispatch(setPlanSelection({ selectedTierId: tier.id }))}
          >
            {tier.isPopular && <div className="popular-badge">Most Popular</div>}
            <h3 className="plan-name">{tier.name}</h3>
            <p className="plan-tagline">{tier.tagline}</p>
            <div className="plan-price">
              <span className="price-amount">
                {formatPrice(getMonthlyPrice(tier))}
              </span>
              <span className="price-period">/month</span>
            </div>
            {planSelection.billingCycle === 'annual' && tier.annualDiscountPercent > 0 && (
              <div className="annual-savings">
                Billed annually (save {tier.annualDiscountPercent}%)
              </div>
            )}
            <div className="plan-features">
              {tier.tierFeatures?.slice(0, 5).map((tf, idx) => (
                <div key={idx} className="feature-item">
                  <span className="feature-check">&#x2713;</span>
                  {tf.limitDescription || tf.feature?.name}
                </div>
              ))}
            </div>
            <button
              type="button"
              className={`btn ${planSelection.selectedTierId === tier.id ? 'btn-primary' : 'btn-secondary'} btn-block`}
            >
              {planSelection.selectedTierId === tier.id ? 'Selected' : 'Select Plan'}
            </button>
          </div>
        ))}
      </div>

      {/* Enterprise Option */}
      <div className="enterprise-option">
        <p>
          Need more? <a href="/contact">Contact us</a> for Enterprise pricing
        </p>
      </div>

      {/* Promo Code */}
      <div className="promo-section">
        <div className="promo-input-group">
          <input
            type="text"
            value={promoCode}
            onChange={e => {
              setPromoCode(e.target.value.toUpperCase())
              setPromoValid(null)
              setPromoError(null)
            }}
            placeholder="Promo code"
            className={promoValid === true ? 'input-success' : promoValid === false ? 'input-error' : ''}
          />
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handlePromoValidate}
            disabled={promoValidating || !promoCode.trim()}
          >
            {promoValidating ? 'Validating...' : 'Apply'}
          </button>
        </div>
        {promoValid && (
          <p className="promo-success">Promo code applied!</p>
        )}
        {promoError && (
          <p className="promo-error">{promoError}</p>
        )}
      </div>

      {/* Summary */}
      {selectedTier && (
        <div className="plan-summary">
          <div className="summary-row">
            <span>Selected Plan</span>
            <span>{selectedTier.name}</span>
          </div>
          <div className="summary-row">
            <span>Billing</span>
            <span>{planSelection.billingCycle === 'annual' ? 'Annual' : 'Monthly'}</span>
          </div>
          {promoValid && planSelection.discountedPrice && (
            <>
              <div className="summary-row strikethrough">
                <span>Original Price</span>
                <span>{formatPrice(getMonthlyPrice(selectedTier))}/mo</span>
              </div>
              <div className="summary-row discount">
                <span>Discounted Price</span>
                <span>{formatPrice(planSelection.discountedPrice / 12)}/mo</span>
              </div>
            </>
          )}
          <div className="summary-row total">
            <span>Total</span>
            <span>
              {formatPrice(
                planSelection.discountedPrice ||
                (planSelection.billingCycle === 'annual'
                  ? selectedTier.annualPriceCents
                  : selectedTier.monthlyPriceCents)
              )}
              {planSelection.billingCycle === 'annual' ? '/year' : '/mo'}
            </span>
          </div>
        </div>
      )}

      <button
        type="button"
        className="btn btn-primary btn-block btn-lg"
        onClick={handleContinue}
        disabled={!planSelection.selectedTierId}
      >
        Continue to Payment
      </button>

      <p className="trial-note">
        Start with a 14-day free trial. Cancel anytime.
      </p>
    </div>
  )
}

export default PlanSelectionStep
