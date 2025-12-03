import { useState, useCallback, useMemo, useEffect } from 'react';
import { BillingPeriod, PricingTier, PriceCalculation } from '../types/pricing';

const STORAGE_KEY = 'caskr_billing_period';

interface UseBillingPeriodOptions {
  defaultPeriod?: BillingPeriod;
  persistToStorage?: boolean;
}

interface UseBillingPeriodReturn {
  billingPeriod: BillingPeriod;
  setBillingPeriod: (period: BillingPeriod) => void;
  toggleBillingPeriod: () => void;
  isAnnual: boolean;
  calculatePrice: (tier: PricingTier, promoDiscountedMonthly?: number, promoDiscountedAnnual?: number) => PriceCalculation;
  calculateSavings: (tier: PricingTier) => number | null;
}

function getStoredPeriod(): BillingPeriod | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'monthly' || stored === 'annual') {
      return stored;
    }
  } catch {
    // Ignore storage errors
  }
  return null;
}

function savePeriod(period: BillingPeriod): void {
  try {
    localStorage.setItem(STORAGE_KEY, period);
  } catch {
    // Ignore storage errors
  }
}

/**
 * Calculate prices for a tier based on billing period
 */
export function calculatePrices(
  tier: PricingTier,
  period: BillingPeriod,
  promoDiscountedMonthly?: number,
  promoDiscountedAnnual?: number
): PriceCalculation {
  if (tier.isCustomPricing) {
    return {
      displayPrice: 0,
      period: '',
      savings: null,
    };
  }

  if (period === 'monthly') {
    const displayPrice = promoDiscountedMonthly ?? tier.monthlyPriceCents ?? 0;
    return {
      displayPrice,
      period: '/month',
      savings: null,
    };
  }

  // Annual billing
  const annualTotal = promoDiscountedAnnual ?? tier.annualPriceCents ?? 0;
  const annualMonthly = Math.round(annualTotal / 12);
  const monthlyPrice = promoDiscountedMonthly ?? tier.monthlyPriceCents ?? 0;
  const monthlyCost = monthlyPrice * 12;
  const annualSavings = monthlyCost - annualTotal;

  return {
    displayPrice: annualMonthly, // show as monthly equivalent
    period: '/month, billed annually',
    savings: annualSavings > 0 ? annualSavings : null,
    annualTotal,
  };
}

/**
 * Hook for managing billing period state
 */
export function useBillingPeriod(options: UseBillingPeriodOptions = {}): UseBillingPeriodReturn {
  const { defaultPeriod = 'monthly', persistToStorage = true } = options;

  const [billingPeriod, setBillingPeriodState] = useState<BillingPeriod>(() => {
    if (persistToStorage) {
      const stored = getStoredPeriod();
      if (stored) return stored;
    }
    return defaultPeriod;
  });

  const setBillingPeriod = useCallback(
    (period: BillingPeriod) => {
      setBillingPeriodState(period);
      if (persistToStorage) {
        savePeriod(period);
      }
    },
    [persistToStorage]
  );

  const toggleBillingPeriod = useCallback(() => {
    setBillingPeriod(billingPeriod === 'monthly' ? 'annual' : 'monthly');
  }, [billingPeriod, setBillingPeriod]);

  const isAnnual = useMemo(() => billingPeriod === 'annual', [billingPeriod]);

  const calculatePrice = useCallback(
    (tier: PricingTier, promoDiscountedMonthly?: number, promoDiscountedAnnual?: number) => {
      return calculatePrices(tier, billingPeriod, promoDiscountedMonthly, promoDiscountedAnnual);
    },
    [billingPeriod]
  );

  const calculateSavings = useCallback(
    (tier: PricingTier): number | null => {
      if (tier.isCustomPricing || !tier.monthlyPriceCents || !tier.annualPriceCents) {
        return null;
      }
      const monthlyCost = tier.monthlyPriceCents * 12;
      const savings = monthlyCost - tier.annualPriceCents;
      return savings > 0 ? savings : null;
    },
    []
  );

  // Sync with localStorage changes from other tabs
  useEffect(() => {
    if (!persistToStorage) return;

    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY && e.newValue) {
        if (e.newValue === 'monthly' || e.newValue === 'annual') {
          setBillingPeriodState(e.newValue);
        }
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, [persistToStorage]);

  return {
    billingPeriod,
    setBillingPeriod,
    toggleBillingPeriod,
    isAnnual,
    calculatePrice,
    calculateSavings,
  };
}

export default useBillingPeriod;
