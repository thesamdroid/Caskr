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
    tagline: 'For emerging craft distilleries',
    monthlyPriceCents: 69900,
    annualPriceCents: 671040,
    annualDiscountPercent: 20,
    isPopular: false,
    isCustomPricing: false,
    ctaText: 'Start Free Trial',
    ctaUrl: '/signup?plan=starter',
    sortOrder: 1,
    features: [
      { featureId: 1, name: 'Barrel Inventory', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Barrel Limit', isIncluded: true, limitValue: '500', sortOrder: 2 },
      { featureId: 3, name: 'Users', isIncluded: true, limitValue: '3', sortOrder: 3 },
      { featureId: 4, name: 'Locations', isIncluded: true, limitValue: '1', sortOrder: 4 },
      { featureId: 5, name: 'TTB Compliance', isIncluded: true, sortOrder: 5 },
      { featureId: 9, name: 'QuickBooks Integration', isIncluded: true, sortOrder: 6 },
      { featureId: 12, name: 'Standard Reports', isIncluded: true, limitValue: '15', sortOrder: 7 },
      { featureId: 13, name: 'Custom Report Builder', isIncluded: false, sortOrder: 8 },
      { featureId: 15, name: 'Investor Portal', isIncluded: false, sortOrder: 9 },
      { featureId: 25, name: 'Support Response', isIncluded: true, limitValue: '48h', sortOrder: 10 },
    ],
  },
  {
    id: 2,
    name: 'Growth',
    slug: 'growth',
    tagline: 'For growing craft distilleries',
    monthlyPriceCents: 169900,
    annualPriceCents: 1631040,
    annualDiscountPercent: 20,
    isPopular: true,
    isCustomPricing: false,
    ctaText: 'Start Free Trial',
    ctaUrl: '/signup?plan=growth',
    sortOrder: 2,
    features: [
      { featureId: 1, name: 'Barrel Inventory', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Barrel Limit', isIncluded: true, limitValue: '2,500', sortOrder: 2 },
      { featureId: 3, name: 'Users', isIncluded: true, limitValue: '10', sortOrder: 3 },
      { featureId: 4, name: 'Locations', isIncluded: true, limitValue: '2', sortOrder: 4 },
      { featureId: 5, name: 'TTB Compliance', isIncluded: true, sortOrder: 5 },
      { featureId: 9, name: 'QuickBooks Integration', isIncluded: true, sortOrder: 6 },
      { featureId: 12, name: 'Standard Reports', isIncluded: true, limitValue: '30+', sortOrder: 7 },
      { featureId: 13, name: 'Custom Report Builder', isIncluded: true, sortOrder: 8 },
      { featureId: 15, name: 'Investor Portal', isIncluded: true, sortOrder: 9 },
      { featureId: 16, name: 'Investor Limit', isIncluded: true, limitValue: '50', sortOrder: 10 },
      { featureId: 22, name: 'Webhooks', isIncluded: true, sortOrder: 11 },
      { featureId: 25, name: 'Support Response', isIncluded: true, limitValue: '24h', sortOrder: 12 },
    ],
  },
  {
    id: 3,
    name: 'Professional',
    slug: 'professional',
    tagline: 'For established multi-location distilleries',
    monthlyPriceCents: 299900,
    annualPriceCents: 2879040,
    annualDiscountPercent: 20,
    isPopular: false,
    isCustomPricing: false,
    ctaText: 'Start Free Trial',
    ctaUrl: '/signup?plan=professional',
    sortOrder: 3,
    features: [
      { featureId: 1, name: 'Barrel Inventory', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Barrel Limit', isIncluded: true, limitValue: '10,000', sortOrder: 2 },
      { featureId: 3, name: 'Users', isIncluded: true, limitValue: '25', sortOrder: 3 },
      { featureId: 4, name: 'Locations', isIncluded: true, limitValue: '5', sortOrder: 4 },
      { featureId: 5, name: 'TTB Compliance', isIncluded: true, sortOrder: 5 },
      { featureId: 9, name: 'QuickBooks Integration', isIncluded: true, sortOrder: 6 },
      { featureId: 12, name: 'Standard Reports', isIncluded: true, limitValue: '30+', sortOrder: 7 },
      { featureId: 13, name: 'Custom Report Builder', isIncluded: true, sortOrder: 8 },
      { featureId: 15, name: 'Investor Portal', isIncluded: true, sortOrder: 9 },
      { featureId: 16, name: 'Investor Limit', isIncluded: true, limitValue: '200', sortOrder: 10 },
      { featureId: 22, name: 'Webhooks', isIncluded: true, sortOrder: 11 },
      { featureId: 24, name: 'Production Planning', isIncluded: true, sortOrder: 12 },
      { featureId: 25, name: 'Support Response', isIncluded: true, limitValue: '4h', sortOrder: 13 },
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
    ctaUrl: '/contact?plan=enterprise',
    sortOrder: 4,
    features: [
      { featureId: 1, name: 'Barrel Inventory', isIncluded: true, sortOrder: 1 },
      { featureId: 2, name: 'Barrel Limit', isIncluded: true, limitValue: 'Unlimited', sortOrder: 2 },
      { featureId: 3, name: 'Users', isIncluded: true, limitValue: 'Unlimited', sortOrder: 3 },
      { featureId: 4, name: 'Locations', isIncluded: true, limitValue: 'Unlimited', sortOrder: 4 },
      { featureId: 5, name: 'TTB Compliance', isIncluded: true, sortOrder: 5 },
      { featureId: 9, name: 'QuickBooks Integration', isIncluded: true, sortOrder: 6 },
      { featureId: 12, name: 'Standard Reports', isIncluded: true, limitValue: '30+', sortOrder: 7 },
      { featureId: 13, name: 'Custom Report Builder', isIncluded: true, sortOrder: 8 },
      { featureId: 15, name: 'Investor Portal', isIncluded: true, sortOrder: 9 },
      { featureId: 16, name: 'Investor Limit', isIncluded: true, limitValue: 'Unlimited', sortOrder: 10 },
      { featureId: 22, name: 'Webhooks', isIncluded: true, sortOrder: 11 },
      { featureId: 24, name: 'Production Planning', isIncluded: true, sortOrder: 12 },
      { featureId: 25, name: 'Support Response', isIncluded: true, limitValue: '1h', sortOrder: 13 },
      { featureId: 27, name: 'Dedicated Account Manager', isIncluded: true, sortOrder: 14 },
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
    category: 'Core',
    features: [
      { id: 1, name: 'Barrel Inventory', description: 'Complete barrel lifecycle tracking', sortOrder: 1 },
      { id: 2, name: 'Barrel Limit', description: 'Maximum barrels you can track', sortOrder: 2 },
      { id: 3, name: 'Users', description: 'Team member accounts', sortOrder: 3 },
      { id: 4, name: 'Locations', description: 'Warehouse/facility locations', sortOrder: 4 },
    ],
  },
  {
    category: 'Compliance',
    features: [
      { id: 5, name: 'TTB Compliance', description: 'Full TTB form automation (5110.28, 5110.40, 5100.16)', sortOrder: 1 },
      { id: 6, name: 'Gauge Records', description: 'Temperature-corrected proof gallon calculations', sortOrder: 2 },
      { id: 7, name: 'Excise Tax Calculation', description: 'Federal excise tax with reduced rate tracking', sortOrder: 3 },
      { id: 8, name: 'Audit Trail', description: '31+ field comprehensive audit logging', sortOrder: 4 },
    ],
  },
  {
    category: 'Financial',
    features: [
      { id: 9, name: 'QuickBooks Integration', description: 'Bi-directional sync with QuickBooks Online', sortOrder: 1 },
      { id: 10, name: 'Invoice Sync', description: 'Automatic invoice creation in QuickBooks', sortOrder: 2 },
      { id: 11, name: 'COGS Tracking', description: 'Cost of goods sold calculation and journal entries', sortOrder: 3 },
    ],
  },
  {
    category: 'Reporting',
    features: [
      { id: 12, name: 'Standard Reports', description: 'Pre-built financial, inventory, and compliance reports', sortOrder: 1 },
      { id: 13, name: 'Custom Report Builder', description: 'Drag-and-drop report creation with 20+ tables', sortOrder: 2 },
      { id: 14, name: 'Export Options', description: 'Export to CSV and PDF', sortOrder: 3 },
    ],
  },
  {
    category: 'Portal',
    features: [
      { id: 15, name: 'Investor Portal', description: 'Customer-facing portal for cask ownership', sortOrder: 1 },
      { id: 16, name: 'Investor Limit', description: 'Maximum investor accounts', sortOrder: 2 },
      { id: 17, name: 'Document Management', description: 'Ownership certificates, photos, invoices', sortOrder: 3 },
      { id: 18, name: 'Maturation Tracking', description: 'Age and progress tracking for investors', sortOrder: 4 },
    ],
  },
  {
    category: 'Operations',
    features: [
      { id: 19, name: 'Mobile Access', description: 'PWA mobile experience', sortOrder: 1 },
      { id: 20, name: 'Barcode Scanning', description: 'Web-based QR and barcode scanning (5 formats)', sortOrder: 2 },
      { id: 21, name: 'Offline Support', description: 'Work offline with automatic sync', sortOrder: 3 },
    ],
  },
  {
    category: 'Integration',
    features: [
      { id: 22, name: 'Webhooks', description: '12 event types for integrations', sortOrder: 1 },
      { id: 23, name: 'API Access', description: 'Full REST API with documentation', sortOrder: 2 },
    ],
  },
  {
    category: 'Production',
    features: [
      { id: 24, name: 'Production Planning', description: 'Scheduling and capacity management', sortOrder: 1 },
    ],
  },
  {
    category: 'Support',
    features: [
      { id: 25, name: 'Support Response', description: 'Support response time SLA', sortOrder: 1 },
      { id: 26, name: 'Onboarding', description: 'Implementation assistance', sortOrder: 2 },
      { id: 27, name: 'Dedicated Account Manager', description: 'Named account manager', sortOrder: 3 },
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
