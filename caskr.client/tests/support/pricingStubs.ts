import type { Page } from '@playwright/test';

export interface PricingTierStub {
  id: number;
  name: string;
  slug: string;
  tagline?: string;
  monthlyPriceCents?: number;
  annualPriceCents?: number;
  annualDiscountPercent: number;
  isPopular: boolean;
  isCustomPricing: boolean;
  ctaText?: string;
  ctaUrl?: string;
  sortOrder: number;
  features: PricingFeatureStub[];
}

export interface PricingFeatureStub {
  featureId: number;
  name: string;
  description?: string;
  category?: string;
  isIncluded: boolean;
  limitValue?: string;
  limitDescription?: string;
  sortOrder: number;
}

export interface PricingFaqStub {
  id: number;
  question: string;
  answer: string;
  sortOrder: number;
}

export interface PromoValidationStub {
  isValid: boolean;
  code?: string;
  discountDescription?: string;
  errorMessage?: string;
  discountType?: 'percentage' | 'fixedAmount' | 'freeMonths';
  discountValue?: number;
  applicableTierIds?: number[];
}

export interface PricingStubOptions {
  tiers?: PricingTierStub[];
  featuresByCategory?: Array<{
    category: string;
    features: Array<{
      id: number;
      name: string;
      description?: string;
      category?: string;
      sortOrder: number;
    }>;
  }>;
  faqs?: PricingFaqStub[];
  pricingResponses?: Array<{ status: number; body?: unknown }>;
  promoValidation?: PromoValidationStub;
  promoValidationDelay?: number;
  promoApplication?: {
    success: boolean;
    discountedMonthlyPriceCents?: number;
    discountedAnnualPriceCents?: number;
    originalMonthlyPriceCents?: number;
    originalAnnualPriceCents?: number;
  };
}

export const defaultPricingTiers: PricingTierStub[] = [
  {
    id: 1,
    name: 'Starter',
    slug: 'starter',
    tagline: 'Perfect for small distilleries',
    monthlyPriceCents: 9900,
    annualPriceCents: 95040,
    annualDiscountPercent: 20,
    isPopular: false,
    isCustomPricing: false,
    ctaText: 'Get Started',
    sortOrder: 1,
    features: [
      { featureId: 1, name: 'Barrel Management', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Basic Reporting', isIncluded: true, sortOrder: 2 },
      { featureId: 3, name: 'Up to 100 barrels', isIncluded: true, limitValue: '100', sortOrder: 3 },
      { featureId: 4, name: 'TTB Compliance', isIncluded: false, sortOrder: 4 },
    ],
  },
  {
    id: 2,
    name: 'Professional',
    slug: 'professional',
    tagline: 'For growing distilleries',
    monthlyPriceCents: 29900,
    annualPriceCents: 287040,
    annualDiscountPercent: 20,
    isPopular: true,
    isCustomPricing: false,
    ctaText: 'Start Free Trial',
    sortOrder: 2,
    features: [
      { featureId: 1, name: 'Barrel Management', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Advanced Reporting', isIncluded: true, sortOrder: 2 },
      { featureId: 3, name: 'Up to 500 barrels', isIncluded: true, limitValue: '500', sortOrder: 3 },
      { featureId: 4, name: 'TTB Compliance', isIncluded: true, sortOrder: 4 },
    ],
  },
  {
    id: 3,
    name: 'Business',
    slug: 'business',
    tagline: 'For established operations',
    monthlyPriceCents: 59900,
    annualPriceCents: 574080,
    annualDiscountPercent: 20,
    isPopular: false,
    isCustomPricing: false,
    ctaText: 'Start Free Trial',
    sortOrder: 3,
    features: [
      { featureId: 1, name: 'Barrel Management', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Advanced Reporting', isIncluded: true, sortOrder: 2 },
      { featureId: 3, name: 'Unlimited barrels', isIncluded: true, limitValue: 'Unlimited', sortOrder: 3 },
      { featureId: 4, name: 'TTB Compliance', isIncluded: true, sortOrder: 4 },
    ],
  },
  {
    id: 4,
    name: 'Enterprise',
    slug: 'enterprise',
    tagline: 'Custom solutions for large operations',
    isPopular: false,
    isCustomPricing: true,
    annualDiscountPercent: 0,
    ctaText: 'Contact Sales',
    sortOrder: 4,
    features: [
      { featureId: 1, name: 'Barrel Management', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Advanced Reporting', isIncluded: true, sortOrder: 2 },
      { featureId: 3, name: 'Unlimited barrels', isIncluded: true, limitValue: 'Unlimited', sortOrder: 3 },
      { featureId: 4, name: 'TTB Compliance', isIncluded: true, sortOrder: 4 },
      { featureId: 5, name: 'Dedicated Support', isIncluded: true, sortOrder: 5 },
    ],
  },
];

export const defaultPricingFaqs: PricingFaqStub[] = [
  {
    id: 1,
    question: 'What is included in the free trial?',
    answer: 'All features are included in our **14-day free trial**. No credit card required.',
    sortOrder: 1,
  },
  {
    id: 2,
    question: 'Can I change plans later?',
    answer: 'Yes, you can upgrade or downgrade your plan at any time. Changes take effect immediately.',
    sortOrder: 2,
  },
  {
    id: 3,
    question: 'Is there a discount for annual billing?',
    answer: 'Yes, annual billing saves you **20%** compared to monthly billing.',
    sortOrder: 3,
  },
];

export const defaultFeaturesByCategory = [
  {
    category: 'Core Features',
    features: [
      { id: 1, name: 'Barrel Management', description: 'Track all your barrels', sortOrder: 1 },
      { id: 2, name: 'Reporting', description: 'Generate reports', sortOrder: 2 },
      { id: 3, name: 'Barrel Limits', description: 'Maximum barrels you can track', sortOrder: 3 },
    ],
  },
  {
    category: 'Compliance',
    features: [
      { id: 4, name: 'TTB Compliance', description: 'Automated TTB reporting', sortOrder: 1 },
      { id: 5, name: 'Dedicated Support', description: 'Priority customer support', sortOrder: 2 },
    ],
  },
  {
    category: 'Security',
    features: [
      { id: 6, name: 'SSO Integration', description: 'Single sign-on support', sortOrder: 1 },
      { id: 7, name: 'Audit Logs', description: 'Complete audit trail', sortOrder: 2 },
    ],
  },
];

// Extended FAQs for testing
export const extendedPricingFaqs: PricingFaqStub[] = [
  {
    id: 1,
    question: 'What is included in the free trial?',
    answer: 'All features are included in our **14-day free trial**. No credit card required.',
    sortOrder: 1,
  },
  {
    id: 2,
    question: 'Can I change plans later?',
    answer: 'Yes, you can upgrade or downgrade your plan at any time. Changes take effect immediately.',
    sortOrder: 2,
  },
  {
    id: 3,
    question: 'Is there a discount for annual billing?',
    answer: 'Yes, annual billing saves you **20%** compared to monthly billing.',
    sortOrder: 3,
  },
  {
    id: 4,
    question: 'How secure is my data?',
    answer: 'We use **SOC 2 Type II** certified infrastructure with encryption at rest and in transit.',
    sortOrder: 4,
  },
  {
    id: 5,
    question: 'Can I export my data?',
    answer: 'Yes, you can export all your data at any time in CSV, Excel, or JSON format.',
    sortOrder: 5,
  },
];

export const stubPricingData = async (page: Page, options: PricingStubOptions = {}) => {
  const tiers = options.tiers ?? defaultPricingTiers;
  const featuresByCategory = options.featuresByCategory ?? defaultFeaturesByCategory;
  const faqs = options.faqs ?? defaultPricingFaqs;

  let pricingRequestCount = 0;

  await page.route('**/api/public/pricing', async (route) => {
    const definedResponse = options.pricingResponses?.[pricingRequestCount];
    pricingRequestCount += 1;

    if (definedResponse) {
      await route.fulfill({
        status: definedResponse.status,
        contentType: 'application/json',
        body: JSON.stringify(definedResponse.body ?? { tiers, featuresByCategory, faqs, generatedAt: new Date().toISOString() }),
      });
    } else {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          tiers,
          featuresByCategory,
          faqs,
          generatedAt: new Date().toISOString(),
        }),
      });
    }
  });

  await page.route('**/api/public/pricing/validate-promo', async (route) => {
    // Support delay for testing loading states
    if (options.promoValidationDelay) {
      await new Promise(resolve => setTimeout(resolve, options.promoValidationDelay));
    }

    const validation = options.promoValidation ?? { isValid: false, errorMessage: 'Invalid promo code' };
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(validation),
    });
  });

  await page.route('**/api/public/pricing/apply-promo', async (route) => {
    const body = route.request().postDataJSON?.();
    const tierId = body?.tierId ?? 1;
    const tier = tiers.find(t => t.id === tierId);

    const application = options.promoApplication ?? (options.promoValidation?.isValid ? {
      success: true,
      code: body?.code,
      tierId,
      discountType: options.promoValidation.discountType ?? 'percentage',
      discountValue: options.promoValidation.discountValue ?? 20,
      originalMonthlyPriceCents: tier?.monthlyPriceCents,
      discountedMonthlyPriceCents: tier?.monthlyPriceCents
        ? Math.round(tier.monthlyPriceCents * 0.8)
        : undefined,
      originalAnnualPriceCents: tier?.annualPriceCents,
      discountedAnnualPriceCents: tier?.annualPriceCents
        ? Math.round(tier.annualPriceCents * 0.8)
        : undefined,
    } : { success: false });

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(application),
    });
  });

  // Mock analytics endpoint
  await page.route('**/api/analytics/**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true }),
    });
  });
};
