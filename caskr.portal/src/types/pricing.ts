// Pricing Types for Admin Dashboard

export interface PricingTier {
  id: number
  name: string
  slug: string
  tagline?: string
  monthlyPriceCents?: number
  annualPriceCents?: number
  annualDiscountPercent: number
  isPopular: boolean
  isCustomPricing: boolean
  ctaText?: string
  ctaUrl?: string
  sortOrder: number
  isActive: boolean
  createdAt: string
  updatedAt: string
  tierFeatures?: PricingTierFeature[]
}

export interface PricingFeature {
  id: number
  name: string
  description?: string
  category?: string
  icon?: string
  tooltipText?: string
  sortOrder: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface PricingTierFeature {
  id: number
  tierId: number
  featureId: number
  isIncluded: boolean
  limitValue?: number
  limitDescription?: string
  feature?: PricingFeature
}

export interface PricingFaq {
  id: number
  question: string
  answer: string
  category?: string
  sortOrder: number
  isActive: boolean
  publishDate?: string
  unpublishDate?: string
  createdAt: string
  updatedAt: string
}

export type DiscountType = 'Percentage' | 'FixedAmount' | 'FreeMonths'

export interface PricingPromotion {
  id: number
  code: string
  description?: string
  discountType: DiscountType
  discountValue: number
  appliesToTiersJson?: string
  validFrom?: string
  validUntil?: string
  maxRedemptions?: number
  currentRedemptions: number
  minimumMonths?: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface PricingAuditLog {
  id: number
  userId: number
  userName?: string
  action: string
  entityType: string
  entityId?: number
  oldValue?: string
  newValue?: string
  reason?: string
  ipAddress?: string
  createdAt: string
}

export interface PricingPageData {
  tiers: PricingTierDto[]
  featuresByCategory: PricingFeatureCategoryDto[]
  faqs: PricingFaqDto[]
  generatedAt: string
}

export interface PricingTierDto {
  id: number
  name: string
  slug: string
  tagline?: string
  monthlyPriceCents?: number
  annualPriceCents?: number
  annualDiscountPercent: number
  isPopular: boolean
  isCustomPricing: boolean
  ctaText?: string
  ctaUrl?: string
  sortOrder: number
  features: PricingTierFeatureDto[]
}

export interface PricingTierFeatureDto {
  featureId: number
  name: string
  description?: string
  category?: string
  isIncluded: boolean
  limitValue?: number
  limitDescription?: string
  sortOrder: number
}

export interface PricingFeatureCategoryDto {
  category: string
  features: PricingFeatureDto[]
}

export interface PricingFeatureDto {
  id: number
  name: string
  description?: string
  category?: string
  sortOrder: number
}

export interface PricingFaqDto {
  id: number
  question: string
  answer: string
  sortOrder: number
}

// Form types for creating/editing
export interface TierFormData {
  name: string
  slug: string
  tagline?: string
  monthlyPriceCents?: number
  annualPriceCents?: number
  annualDiscountPercent: number
  isPopular: boolean
  isCustomPricing: boolean
  ctaText?: string
  ctaUrl?: string
  sortOrder: number
  isActive: boolean
}

export interface FeatureFormData {
  name: string
  description?: string
  category?: string
  icon?: string
  tooltipText?: string
  sortOrder: number
  isActive: boolean
}

export interface FaqFormData {
  question: string
  answer: string
  category?: string
  sortOrder: number
  isActive: boolean
  publishDate?: string
  unpublishDate?: string
}

export interface PromotionFormData {
  code: string
  description?: string
  discountType: DiscountType
  discountValue: number
  appliesToTiersJson?: string
  validFrom?: string
  validUntil?: string
  maxRedemptions?: number
  minimumMonths?: number
  isActive: boolean
}

// Feature categories
export const FEATURE_CATEGORIES = [
  'General',
  'Compliance',
  'Reporting',
  'Integrations',
  'Support'
] as const

export type FeatureCategory = (typeof FEATURE_CATEGORIES)[number]

// Available icons for features
export const FEATURE_ICONS = [
  'check',
  'barrel',
  'chart',
  'document',
  'users',
  'lock',
  'cloud',
  'api',
  'support',
  'star'
] as const

export type FeatureIcon = (typeof FEATURE_ICONS)[number]

// Promo status helpers
export type PromoStatus = 'active' | 'scheduled' | 'expired' | 'inactive'

export function getPromoStatus(promo: PricingPromotion): PromoStatus {
  if (!promo.isActive) return 'inactive'
  const now = new Date()
  if (promo.validFrom && new Date(promo.validFrom) > now) return 'scheduled'
  if (promo.validUntil && new Date(promo.validUntil) < now) return 'expired'
  if (promo.maxRedemptions && promo.currentRedemptions >= promo.maxRedemptions) return 'expired'
  return 'active'
}
