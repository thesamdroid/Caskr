import { useEffect, useMemo, useCallback } from 'react';
import { usePricingData } from '../../../hooks/usePricingData';
import { useBillingPeriod } from '../../../hooks/useBillingPeriod';
import { useAnalytics, useTimeOnPage, useScrollDepth } from '../../../hooks/useAnalytics';
import { TrackVisibility } from '../../../components/analytics';
import { PricingHero } from './PricingHero';
import { PricingCards } from './PricingCards';
import { FeatureComparisonTable } from './FeatureComparisonTable';
import { PricingFaq } from './PricingFaq';
import { PromoCodeInput } from './PromoCodeInput';
import { PricingCta } from './PricingCta';
import { PricingFooter } from './PricingFooter';
import { PricingStructuredData } from './PricingStructuredData';
import { ValidatedPromo } from '../../../utils/calculatePromoPrice';
import './PricingPage.css';

/**
 * Get URL parameters for analytics
 */
function getUrlParams(): Record<string, string | undefined> {
  if (typeof window === 'undefined') return {};

  const params = new URLSearchParams(window.location.search);
  return {
    utm_source: params.get('utm_source') ?? undefined,
    utm_medium: params.get('utm_medium') ?? undefined,
    utm_campaign: params.get('utm_campaign') ?? undefined,
    promo_code: params.get('promo') ?? undefined,
  };
}

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
    refetch,
  } = usePricingData();

  const {
    billingPeriod,
    setBillingPeriod,
  } = useBillingPeriod();

  const { track, trackPageView } = useAnalytics();

  // Track time on page
  useTimeOnPage('pricing_page');

  // Track scroll depth
  useScrollDepth('pricing_page');

  // Get max annual discount for the toggle display
  const maxAnnualDiscount = useMemo(() => {
    if (!data?.tiers) return 20; // Default fallback
    const discounts = data.tiers
      .filter(t => t.annualDiscountPercent > 0)
      .map(t => t.annualDiscountPercent);
    return discounts.length > 0 ? Math.max(...discounts) : 20;
  }, [data?.tiers]);

  // Track page view on mount
  useEffect(() => {
    const urlParams = getUrlParams();
    trackPageView('pricing_page', {
      source: document.referrer || undefined,
      ...urlParams,
    });
  }, [trackPageView]);

  // Track billing toggle changes
  const handleBillingChange = useCallback(
    (period: 'monthly' | 'annual') => {
      track('billing_toggle_changed', {
        from: billingPeriod,
        to: period,
      });
      setBillingPeriod(period);
    },
    [billingPeriod, setBillingPeriod, track]
  );

  // Analytics callback for PromoCodeInput
  const handlePromoAnalyticsEvent = useCallback(
    (event: string, eventData?: Record<string, unknown>) => {
      track(event, eventData);
    },
    [track]
  );

  // Handle promo code apply
  const handlePromoApply = useCallback(
    (promo: ValidatedPromo) => {
      // The actual promo application is handled by usePricingData
      // Just ensure validation is triggered
      if (promo.code) {
        validatePromoCode(promo.code);
      }
    },
    [validatePromoCode]
  );

  // Handle promo code remove
  const handlePromoRemove = useCallback(() => {
    clearPromoCode();
  }, [clearPromoCode]);

  const handleRetry = useCallback(() => {
    track('pricing_retry_clicked', { error });
    refetch();
  }, [error, refetch, track]);

  // Handle FAQ open
  const handleFaqOpen = useCallback(
    (questionId: string, questionText: string) => {
      track('faq_item_opened', {
        question_id: questionId,
        question_text: questionText.substring(0, 100), // Truncate for analytics
      });
    },
    [track]
  );

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
          onBillingChange={handleBillingChange}
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
          <p className="pricing-error-message">{error}</p>
          <button
            type="button"
            className="button-primary"
            onClick={handleRetry}
          >
            Retry
          </button>
        </div>
      </main>
    );
  }

  // Get initial promo code from URL
  const initialPromoCode = typeof window !== 'undefined'
    ? new URLSearchParams(window.location.search).get('promo') ?? undefined
    : undefined;

  return (
    <main className="pricing-page">
      {/* Structured data for SEO */}
      {data && <PricingStructuredData tiers={data.tiers} />}

      {/* Hero section with billing toggle */}
      <PricingHero
        billingPeriod={billingPeriod}
        onBillingChange={handleBillingChange}
        annualDiscountPercent={maxAnnualDiscount}
      />

      {/* Promo code input */}
      <PromoCodeInput
        onApply={handlePromoApply}
        onRemove={handlePromoRemove}
        onValidate={validatePromoCode}
        onClear={clearPromoCode}
        currentValidation={promoValidation}
        initialCode={initialPromoCode}
        onAnalyticsEvent={handlePromoAnalyticsEvent}
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
        <TrackVisibility
          event="feature_comparison_viewed"
          properties={{ section: 'feature_comparison' }}
        >
          <FeatureComparisonTable
            tiers={data.tiers}
            featuresByCategory={data.featuresByCategory}
          />
        </TrackVisibility>
      )}

      {/* FAQ section */}
      {data && data.faqs.length > 0 && (
        <PricingFaq faqs={data.faqs} onFaqOpen={handleFaqOpen} />
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
  const { track } = useAnalytics();

  const handleClick = () => {
    track('mobile_sticky_cta_clicked', {});
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
