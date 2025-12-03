import { useMemo, useRef, useEffect, useState } from 'react';
import { AnimatedPrice } from '../../../components/pricing/AnimatedPrice';
import { SavingsBadge } from '../../../components/pricing/SavingsBadge';
import { PricingTier, BillingPeriod, PriceCalculation, PromoCodeApplicationResult } from '../../../types/pricing';
import { calculatePrices } from '../../../hooks/useBillingPeriod';
import './PricingCard.css';

export interface PricingCardProps {
  tier: PricingTier;
  billingPeriod: BillingPeriod;
  isHighlighted?: boolean;
  animationDelay?: number;
  promoApplication?: PromoCodeApplicationResult;
}

/**
 * Individual pricing card displaying tier information.
 */
export function PricingCard({
  tier,
  billingPeriod,
  isHighlighted = false,
  animationDelay = 0,
  promoApplication,
}: PricingCardProps) {
  const [previousPrice, setPreviousPrice] = useState<number | undefined>(undefined);
  const lastPriceRef = useRef<number | undefined>(undefined);

  // Get promo-adjusted prices if available
  const promoMonthly = promoApplication?.discountedMonthlyPriceCents;
  const promoAnnual = promoApplication?.discountedAnnualPriceCents;

  // Calculate price based on billing period
  const priceCalc: PriceCalculation = useMemo(() => {
    return calculatePrices(tier, billingPeriod, promoMonthly, promoAnnual);
  }, [tier, billingPeriod, promoMonthly, promoAnnual]);

  // Track previous price for animation
  useEffect(() => {
    if (lastPriceRef.current !== undefined && lastPriceRef.current !== priceCalc.displayPrice) {
      setPreviousPrice(lastPriceRef.current);
    }
    lastPriceRef.current = priceCalc.displayPrice;
  }, [priceCalc.displayPrice]);

  // Check if promo applies to this tier
  const hasPromoDiscount = promoApplication?.success && promoApplication.tierId === tier.id;
  const originalPrice = hasPromoDiscount
    ? billingPeriod === 'monthly'
      ? promoApplication.originalMonthlyPriceCents
      : Math.round((promoApplication.originalAnnualPriceCents ?? 0) / 12)
    : undefined;

  return (
    <article
      className={`pricing-card ${tier.isPopular ? 'pricing-card-popular' : ''} ${isHighlighted ? 'pricing-card-highlighted' : ''}`}
      style={{ animationDelay: `${animationDelay}ms` }}
      aria-labelledby={`tier-${tier.slug}-name`}
    >
      {tier.isPopular && (
        <div className="pricing-card-badge">
          Most Popular
        </div>
      )}

      <header className="pricing-card-header">
        <h3 id={`tier-${tier.slug}-name`} className="pricing-card-name">
          {tier.name}
        </h3>
        {tier.tagline && (
          <p className="pricing-card-tagline">{tier.tagline}</p>
        )}
      </header>

      <div className="pricing-card-price">
        {tier.isCustomPricing ? (
          <div className="pricing-card-custom">
            <span className="pricing-card-custom-text">Custom</span>
            <span className="pricing-card-custom-subtext">Contact us for pricing</span>
          </div>
        ) : (
          <>
            <AnimatedPrice
              amount={priceCalc.displayPrice}
              previousAmount={previousPrice}
              period={priceCalc.period}
              showOriginal={!!hasPromoDiscount}
              originalAmount={originalPrice}
              className="price-center"
            />
            {billingPeriod === 'annual' && priceCalc.savings && priceCalc.savings > 0 && (
              <SavingsBadge
                savingsAmount={priceCalc.savings}
                isVisible={billingPeriod === 'annual'}
              />
            )}
          </>
        )}
      </div>

      <ul className="pricing-card-features" aria-label={`${tier.name} features`}>
        {tier.features.slice(0, 8).map((feature) => (
          <li
            key={feature.featureId}
            className={`pricing-card-feature ${feature.isIncluded ? 'included' : 'excluded'}`}
          >
            <span className="pricing-card-feature-icon" aria-hidden="true">
              {feature.isIncluded ? (
                <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                  <path
                    d="M13.5 4.5L6 12L2.5 8.5"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              ) : (
                <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                  <path
                    d="M4 4L12 12M12 4L4 12"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                  />
                </svg>
              )}
            </span>
            <span className="pricing-card-feature-text">
              {feature.limitValue ? (
                <><strong>{feature.limitValue}</strong> {feature.name}</>
              ) : (
                feature.name
              )}
            </span>
          </li>
        ))}
      </ul>

      <footer className="pricing-card-footer">
        <a
          href={tier.ctaUrl || (tier.isCustomPricing ? '/contact-sales' : '/signup')}
          className={`pricing-card-cta ${tier.isPopular ? 'button-primary' : 'button-secondary'}`}
        >
          {tier.ctaText || (tier.isCustomPricing ? 'Contact Sales' : 'Get Started')}
        </a>
      </footer>
    </article>
  );
}

export default PricingCard;
