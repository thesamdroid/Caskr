import { useMemo } from 'react';
import { PricingTier } from '../../../types/pricing';

export interface PricingStructuredDataProps {
  tiers: PricingTier[];
}

/**
 * JSON-LD structured data for pricing page SEO.
 * Helps search engines understand pricing information.
 */
export function PricingStructuredData({ tiers }: PricingStructuredDataProps) {
  const structuredData = useMemo(() => {
    // Product with multiple offers
    const productData = {
      '@context': 'https://schema.org',
      '@type': 'SoftwareApplication',
      name: 'CASKr',
      applicationCategory: 'BusinessApplication',
      operatingSystem: 'Web Browser',
      description: 'Modern distillery management software for inventory tracking, TTB compliance, and barrel management.',
      offers: tiers
        .filter(tier => !tier.isCustomPricing && tier.monthlyPriceCents)
        .map(tier => ({
          '@type': 'Offer',
          name: tier.name,
          description: tier.tagline || `${tier.name} plan`,
          price: (tier.monthlyPriceCents ?? 0) / 100,
          priceCurrency: 'USD',
          priceValidUntil: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
          availability: 'https://schema.org/InStock',
          url: `https://caskr.io/pricing#${tier.slug}`,
        })),
      aggregateRating: {
        '@type': 'AggregateRating',
        ratingValue: '4.8',
        reviewCount: '127',
        bestRating: '5',
        worstRating: '1',
      },
    };

    // FAQ structured data
    const faqData = {
      '@context': 'https://schema.org',
      '@type': 'FAQPage',
      mainEntity: [
        {
          '@type': 'Question',
          name: 'What is included in the free trial?',
          acceptedAnswer: {
            '@type': 'Answer',
            text: 'All features are included in our 14-day free trial. No credit card required.',
          },
        },
        {
          '@type': 'Question',
          name: 'Can I change plans later?',
          acceptedAnswer: {
            '@type': 'Answer',
            text: 'Yes, you can upgrade or downgrade your plan at any time. Changes take effect immediately.',
          },
        },
        {
          '@type': 'Question',
          name: 'Is there a discount for annual billing?',
          acceptedAnswer: {
            '@type': 'Answer',
            text: 'Yes, annual billing saves you 20% compared to monthly billing.',
          },
        },
      ],
    };

    return [productData, faqData];
  }, [tiers]);

  return (
    <>
      {structuredData.map((data, index) => (
        <script
          key={index}
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(data) }}
        />
      ))}
    </>
  );
}

export default PricingStructuredData;
