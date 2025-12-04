/**
 * Promo code price calculation utilities
 *
 * Calculates discounted prices based on promo code type and value.
 */

import { DiscountType, BillingPeriod } from '../types/pricing';

/**
 * Validated promo code information
 */
export interface ValidatedPromo {
  code: string;
  discountType: DiscountType;
  discountValue: number;
  description: string;
  appliesTo: number[] | null; // tier IDs or null for all
}

/**
 * Result of a promo price calculation
 */
export interface PromoCalculationResult {
  finalPrice: number;
  savings: number;
  discountPercent?: number;
  freeMonths?: number;
}

/**
 * Calculate the discounted price based on promo code type
 *
 * @param basePriceCents - The original price in cents
 * @param promo - The validated promo code details
 * @returns Object with finalPrice and savings in cents
 */
export function calculatePromoPrice(
  basePriceCents: number,
  promo: ValidatedPromo
): PromoCalculationResult {
  switch (promo.discountType) {
    case 'percentage': {
      const discount = Math.round(basePriceCents * (promo.discountValue / 100));
      return {
        finalPrice: basePriceCents - discount,
        savings: discount,
        discountPercent: promo.discountValue,
      };
    }
    case 'fixedAmount': {
      const savings = Math.min(promo.discountValue, basePriceCents);
      return {
        finalPrice: Math.max(0, basePriceCents - promo.discountValue),
        savings,
      };
    }
    case 'freeMonths': {
      // Free months only applies to annual billing
      // discountValue represents number of free months
      return {
        finalPrice: basePriceCents,
        savings: 0,
        freeMonths: promo.discountValue,
      };
    }
    default:
      return {
        finalPrice: basePriceCents,
        savings: 0,
      };
  }
}

/**
 * Calculate annual price with free months applied
 *
 * @param monthlyPriceCents - Monthly price in cents
 * @param freeMonths - Number of free months
 * @returns Annual total with free months discount
 */
export function calculateAnnualWithFreeMonths(
  monthlyPriceCents: number,
  freeMonths: number
): PromoCalculationResult {
  const paidMonths = Math.max(0, 12 - freeMonths);
  const finalPrice = monthlyPriceCents * paidMonths;
  const savings = monthlyPriceCents * freeMonths;

  return {
    finalPrice,
    savings,
    freeMonths,
  };
}

/**
 * Check if a promo code applies to a specific tier
 *
 * @param promo - The validated promo
 * @param tierId - The tier ID to check
 * @returns true if promo applies to this tier
 */
export function promoAppliesToTier(promo: ValidatedPromo, tierId: number): boolean {
  // null means applies to all tiers
  if (promo.appliesTo === null) {
    return true;
  }
  return promo.appliesTo.includes(tierId);
}

/**
 * Format discount for display
 *
 * @param promo - The validated promo
 * @returns Human-readable discount string
 */
export function formatDiscount(promo: ValidatedPromo): string {
  switch (promo.discountType) {
    case 'percentage':
      return `${promo.discountValue}% off`;
    case 'fixedAmount':
      return `Save $${(promo.discountValue / 100).toFixed(2)}`;
    case 'freeMonths':
      return promo.discountValue === 1
        ? '1 month free'
        : `${promo.discountValue} months free`;
    default:
      return promo.description;
  }
}

/**
 * Calculate both monthly and annual prices with promo applied
 */
export function calculatePromoPrices(
  monthlyPriceCents: number,
  annualPriceCents: number,
  promo: ValidatedPromo,
  billingPeriod: BillingPeriod
): {
  displayPrice: number;
  originalPrice: number;
  savings: number;
  period: string;
} {
  if (billingPeriod === 'monthly') {
    if (promo.discountType === 'freeMonths') {
      // Free months don't apply to monthly billing
      return {
        displayPrice: monthlyPriceCents,
        originalPrice: monthlyPriceCents,
        savings: 0,
        period: '/month',
      };
    }

    const result = calculatePromoPrice(monthlyPriceCents, promo);
    return {
      displayPrice: result.finalPrice,
      originalPrice: monthlyPriceCents,
      savings: result.savings,
      period: '/month',
    };
  }

  // Annual billing
  if (promo.discountType === 'freeMonths') {
    const monthlyEquiv = Math.round(annualPriceCents / 12);
    const result = calculateAnnualWithFreeMonths(monthlyEquiv, promo.discountValue);
    return {
      displayPrice: Math.round(result.finalPrice / 12), // show as monthly equivalent
      originalPrice: Math.round(annualPriceCents / 12),
      savings: result.savings,
      period: '/month, billed annually',
    };
  }

  const result = calculatePromoPrice(annualPriceCents, promo);
  return {
    displayPrice: Math.round(result.finalPrice / 12), // show as monthly equivalent
    originalPrice: Math.round(annualPriceCents / 12),
    savings: result.savings,
    period: '/month, billed annually',
  };
}

export default calculatePromoPrice;
