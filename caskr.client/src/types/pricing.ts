// Pricing types matching backend DTOs

export type BillingPeriod = 'monthly' | 'annual';

export interface PricingTierFeature {
  featureId: number;
  name: string;
  description?: string;
  category?: string;
  isIncluded: boolean;
  limitValue?: string;
  limitDescription?: string;
  sortOrder: number;
}

export interface PricingTier {
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
  features: PricingTierFeature[];
  monthlyPriceFormatted?: string;
  annualPriceFormatted?: string;
  annualSavingsMessage?: string;
}

export interface PricingFeature {
  id: number;
  name: string;
  description?: string;
  category?: string;
  sortOrder: number;
}

export interface PricingFeatureCategory {
  category: string;
  features: PricingFeature[];
}

export interface PricingFaq {
  id: number;
  question: string;
  answer: string;
  sortOrder: number;
}

export interface PricingPageData {
  tiers: PricingTier[];
  featuresByCategory: PricingFeatureCategory[];
  faqs: PricingFaq[];
  generatedAt: string;
}

export type DiscountType = 'percentage' | 'fixedAmount' | 'freeMonths';

export interface PromoCodeValidationResult {
  isValid: boolean;
  errorMessage?: string;
  code?: string;
  description?: string;
  discountType?: DiscountType;
  discountValue?: number;
  applicableTierIds?: number[];
  discountDescription?: string;
}

export interface PromoCodeApplicationResult {
  success: boolean;
  errorMessage?: string;
  code?: string;
  tierId?: number;
  originalMonthlyPriceCents?: number;
  discountedMonthlyPriceCents?: number;
  originalAnnualPriceCents?: number;
  discountedAnnualPriceCents?: number;
  freeMonths?: number;
  discountType?: DiscountType;
  discountValue?: number;
  originalMonthlyPriceFormatted?: string;
  discountedMonthlyPriceFormatted?: string;
  originalAnnualPriceFormatted?: string;
  discountedAnnualPriceFormatted?: string;
}

export interface PriceCalculation {
  displayPrice: number;
  period: string;
  savings: number | null;
  annualTotal?: number;
}
