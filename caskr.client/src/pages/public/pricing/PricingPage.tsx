import { useEffect, useMemo } from 'react';
import { usePricingData } from '../../../hooks/usePricingData';
import { useBillingPeriod } from '../../../hooks/useBillingPeriod';
import { PricingHero } from './PricingHero';
import { PricingCards } from './PricingCards';
import { FeatureComparisonTable } from './FeatureComparisonTable';
import { PricingFaq } from './PricingFaq';
import { PromoCodeInput } from './PromoCodeInput';
import { PricingCta } from './PricingCta';
import { PricingFooter } from './PricingFooter';
import { PricingStructuredData } from './PricingStructuredData';
import './PricingPage.css';

/**
 * Public pricing page.
 * No authentication required.
 */
export function PricingPage() {
  const {
    data,
    loading,
    error,
    promoValidation,
    promoApplications,
    validatePromoCode,
    clearPromoCode,
  } = usePricingData();

  const {
    billingPeriod,
    setBillingPeriod,
  } = useBillingPeriod();

  // Get max annual discount for the toggle display
  const maxAnnualDiscount = useMemo(() => {
    if (!data?.tiers) return 20; // Default fallback
    const discounts = data.tiers
      .filter(t => t.annualDiscountPercent > 0)
      .map(t => t.annualDiscountPercent);
    return discounts.length > 0 ? Math.max(...discounts) : 20;
  }, [data?.tiers]);

  // Set page title and meta tags
  useEffect(() => {
    document.title = 'Pricing - CASKr | Distillery Management Software';

    // Update meta description
    const metaDescription = document.querySelector('meta[name="description"]');
    if (metaDescription) {
      metaDescription.setAttribute(
        'content',
        'Simple, transparent pricing for CASKr distillery management software. Start free, scale as you grow. No hidden fees.'
      );
    }

    // Update Open Graph tags
    const ogTitle = document.querySelector('meta[property="og:title"]');
    if (ogTitle) {
      ogTitle.setAttribute('content', 'Pricing - CASKr | Distillery Management Software');
    }

    const ogDescription = document.querySelector('meta[property="og:description"]');
    if (ogDescription) {
      ogDescription.setAttribute(
        'content',
        'Simple, transparent pricing for CASKr distillery management software. Start free, scale as you grow.'
      );
    }
  }, []);

  // Handle loading state with skeleton
  if (loading && !data) {
    return (
      <main className="pricing-page" aria-busy="true" aria-label="Loading pricing information">
        <PricingHero
          billingPeriod={billingPeriod}
          onBillingChange={setBillingPeriod}
          annualDiscountPercent={maxAnnualDiscount}
        />
        <PricingCards
          tiers={[]}
          billingPeriod={billingPeriod}
          loading={true}
        />
      </main>
    );
  }

  // Handle error state
  if (error && !data) {
    return (
      <main className="pricing-page pricing-page-error">
        <div className="pricing-error-container" role="alert">
          <h1>Unable to load pricing</h1>
          <p>We're having trouble loading our pricing information. Please try again later.</p>
          <button
            type="button"
            className="button-primary"
            onClick={() => window.location.reload()}
          >
            Retry
          </button>
        </div>
      </main>
    );
  }

  return (
    <main className="pricing-page">
      {/* Structured data for SEO */}
      {data && <PricingStructuredData tiers={data.tiers} />}

      {/* Hero section with billing toggle */}
      <PricingHero
        billingPeriod={billingPeriod}
        onBillingChange={setBillingPeriod}
        annualDiscountPercent={maxAnnualDiscount}
      />

      {/* Promo code input */}
      <PromoCodeInput
        onValidate={validatePromoCode}
        onClear={clearPromoCode}
        currentValidation={promoValidation}
      />

      {/* Pricing cards */}
      {data && (
        <PricingCards
          tiers={data.tiers}
          billingPeriod={billingPeriod}
          promoApplications={promoApplications}
        />
      )}

      {/* Feature comparison table */}
      {data && data.featuresByCategory.length > 0 && (
        <FeatureComparisonTable
          tiers={data.tiers}
          featuresByCategory={data.featuresByCategory}
        />
      )}

      {/* FAQ section */}
      {data && data.faqs.length > 0 && (
        <PricingFaq faqs={data.faqs} />
      )}

      {/* Final CTA */}
      <PricingCta />

      {/* Footer */}
      <PricingFooter />

      {/* Mobile sticky button */}
      <MobileStickyButton />
    </main>
  );
}

/**
 * Sticky "View Plans" button for mobile
 */
function MobileStickyButton() {
  const handleClick = () => {
    const cardsSection = document.querySelector('.pricing-cards-section');
    cardsSection?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <div className="pricing-mobile-sticky">
      <button
        type="button"
        className="button-primary pricing-mobile-cta"
        onClick={handleClick}
      >
        View Plans
      </button>
    </div>
  );
}

export default PricingPage;
