import { BillingToggle } from '../../../components/pricing/BillingToggle';
import { BillingPeriod } from '../../../types/pricing';
import './PricingHero.css';

export interface PricingHeroProps {
  billingPeriod: BillingPeriod;
  onBillingChange: (period: BillingPeriod) => void;
  annualDiscountPercent: number;
}

/**
 * Hero section for the pricing page.
 * Displays headline, subheadline, and billing toggle.
 */
export function PricingHero({
  billingPeriod,
  onBillingChange,
  annualDiscountPercent,
}: PricingHeroProps) {
  return (
    <section className="pricing-hero" aria-labelledby="pricing-headline">
      <div className="pricing-hero-content">
        <h1 id="pricing-headline" className="pricing-hero-headline">
          Simple, Transparent Pricing
        </h1>
        <p className="pricing-hero-subheadline">
          Start free, scale as you grow. No hidden fees.
        </p>
        <div className="pricing-hero-toggle">
          <BillingToggle
            value={billingPeriod}
            onChange={onBillingChange}
            annualDiscountPercent={annualDiscountPercent}
          />
        </div>
      </div>
    </section>
  );
}

export default PricingHero;
