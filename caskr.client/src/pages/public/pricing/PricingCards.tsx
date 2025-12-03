import { useMemo } from 'react';
import { PricingCard } from './PricingCard';
import { PricingTier, BillingPeriod, PromoCodeApplicationResult } from '../../../types/pricing';
import './PricingCards.css';

export interface PricingCardsProps {
  tiers: PricingTier[];
  billingPeriod: BillingPeriod;
  promoApplications?: Map<number, PromoCodeApplicationResult>;
  loading?: boolean;
}

/**
 * Skeleton loader for pricing cards
 */
function PricingCardSkeleton() {
  return (
    <div className="pricing-card-skeleton" aria-hidden="true">
      <div className="skeleton-header">
        <div className="skeleton-line skeleton-title" />
        <div className="skeleton-line skeleton-subtitle" />
      </div>
      <div className="skeleton-price">
        <div className="skeleton-line skeleton-amount" />
        <div className="skeleton-line skeleton-period" />
      </div>
      <div className="skeleton-features">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="skeleton-line skeleton-feature" />
        ))}
      </div>
      <div className="skeleton-cta">
        <div className="skeleton-line skeleton-button" />
      </div>
    </div>
  );
}

/**
 * Container component for pricing cards.
 * Displays tiers side-by-side on desktop, stacked on mobile.
 */
export function PricingCards({
  tiers,
  billingPeriod,
  promoApplications,
  loading = false,
}: PricingCardsProps) {
  // Sort tiers by sortOrder
  const sortedTiers = useMemo(() => {
    return [...tiers].sort((a, b) => a.sortOrder - b.sortOrder);
  }, [tiers]);

  // Animation stagger delay (50ms between each card)
  const STAGGER_DELAY = 50;

  if (loading) {
    return (
      <section className="pricing-cards-section" aria-label="Loading pricing plans">
        <div className="pricing-cards-grid">
          {[1, 2, 3, 4].map((i) => (
            <PricingCardSkeleton key={i} />
          ))}
        </div>
      </section>
    );
  }

  if (sortedTiers.length === 0) {
    return (
      <section className="pricing-cards-section" aria-label="Pricing plans">
        <div className="pricing-cards-empty">
          <p>No pricing plans available at this time.</p>
        </div>
      </section>
    );
  }

  return (
    <section className="pricing-cards-section" aria-label="Pricing plans">
      <div className="pricing-cards-grid">
        {sortedTiers.map((tier, index) => (
          <PricingCard
            key={tier.id}
            tier={tier}
            billingPeriod={billingPeriod}
            isHighlighted={tier.isPopular}
            animationDelay={index * STAGGER_DELAY}
            promoApplication={promoApplications?.get(tier.id)}
          />
        ))}
      </div>
    </section>
  );
}

export default PricingCards;
