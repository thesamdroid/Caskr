import { useMemo } from 'react';
import { PricingTier, BillingPeriod } from '../../types/pricing';
import './ComparisonTableHeader.css';

export interface ComparisonTableHeaderProps {
  tiers: PricingTier[];
  billingPeriod?: BillingPeriod;
  highlightedTierId?: number | null;
  onTierHighlight?: (tierId: number | null) => void;
}

/**
 * Table header component for feature comparison table.
 * Shows tier names, prices, and "Most Popular" badge.
 * Supports tier highlighting on click.
 */
export function ComparisonTableHeader({
  tiers,
  billingPeriod = 'monthly',
  highlightedTierId,
  onTierHighlight,
}: ComparisonTableHeaderProps) {
  // Sort tiers by sortOrder
  const sortedTiers = useMemo(() => {
    return [...tiers].sort((a, b) => a.sortOrder - b.sortOrder);
  }, [tiers]);

  const formatPrice = (tier: PricingTier): string => {
    if (tier.isCustomPricing) {
      return 'Custom';
    }

    const priceCents = billingPeriod === 'annual'
      ? tier.annualPriceCents
      : tier.monthlyPriceCents;

    if (!priceCents) return 'Free';

    // For annual, show monthly equivalent
    const monthlyEquivalent = billingPeriod === 'annual'
      ? Math.round(priceCents / 12)
      : priceCents;

    return `$${Math.round(monthlyEquivalent / 100)}`;
  };

  const handleTierClick = (tierId: number) => {
    if (onTierHighlight) {
      onTierHighlight(highlightedTierId === tierId ? null : tierId);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent, tierId: number) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleTierClick(tierId);
    }
  };

  return (
    <thead className="comparison-table-thead">
      <tr>
        <th scope="col" className="comparison-th feature-name-col">
          <span className="visually-hidden">Feature</span>
        </th>
        {sortedTiers.map((tier) => {
          const isHighlighted = highlightedTierId === tier.id;
          const isClickable = !!onTierHighlight;

          return (
            <th
              key={tier.id}
              scope="col"
              className={`comparison-th tier-col ${tier.isPopular ? 'popular' : ''} ${isHighlighted ? 'highlighted' : ''}`}
              onClick={isClickable ? () => handleTierClick(tier.id) : undefined}
              onKeyDown={isClickable ? (e) => handleKeyDown(e, tier.id) : undefined}
              tabIndex={isClickable ? 0 : undefined}
              role={isClickable ? 'button' : undefined}
              aria-pressed={isClickable ? isHighlighted : undefined}
            >
              <div className="tier-header-content">
                {tier.isPopular && (
                  <span className="tier-popular-badge" aria-label="Most popular plan">
                    Most Popular
                  </span>
                )}
                <span className="tier-name">{tier.name}</span>
                <span className="tier-price" aria-label={`${formatPrice(tier)} per month`}>
                  {formatPrice(tier)}
                  {!tier.isCustomPricing && tier.monthlyPriceCents && (
                    <span className="tier-price-period">/mo</span>
                  )}
                </span>
                {billingPeriod === 'annual' && tier.annualDiscountPercent > 0 && (
                  <span className="tier-savings">
                    Save {tier.annualDiscountPercent}%
                  </span>
                )}
              </div>
            </th>
          );
        })}
      </tr>
    </thead>
  );
}

export default ComparisonTableHeader;
