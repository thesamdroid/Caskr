import { useState, useEffect, useCallback } from 'react';
import {
  PricingPageData,
  PromoCodeValidationResult,
  PromoCodeApplicationResult,
} from '../types/pricing';

const CACHE_KEY = 'caskr_pricing_data';
const CACHE_EXPIRY_KEY = 'caskr_pricing_data_expiry';
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes
const PROMO_CODE_KEY = 'caskr_promo_code';

interface UsePricingDataOptions {
  promoCode?: string;
}

interface UsePricingDataReturn {
  data: PricingPageData | null;
  loading: boolean;
  error: string | null;
  promoValidation: PromoCodeValidationResult | null;
  promoApplications: Map<number, PromoCodeApplicationResult>;
  validatePromoCode: (code: string) => Promise<PromoCodeValidationResult>;
  applyPromoCode: (code: string, tierId: number) => Promise<PromoCodeApplicationResult>;
  clearPromoCode: () => void;
  refetch: () => Promise<void>;
}

function getFromCache(): PricingPageData | null {
  try {
    const expiry = localStorage.getItem(CACHE_EXPIRY_KEY);
    if (expiry && Date.now() < parseInt(expiry, 10)) {
      const cached = localStorage.getItem(CACHE_KEY);
      if (cached) {
        return JSON.parse(cached);
      }
    }
  } catch {
    // Ignore cache errors
  }
  return null;
}

function saveToCache(data: PricingPageData): void {
  try {
    localStorage.setItem(CACHE_KEY, JSON.stringify(data));
    localStorage.setItem(CACHE_EXPIRY_KEY, String(Date.now() + CACHE_DURATION));
  } catch {
    // Ignore cache errors
  }
}

function getStoredPromoCode(): string | null {
  try {
    return localStorage.getItem(PROMO_CODE_KEY);
  } catch {
    return null;
  }
}

function savePromoCode(code: string): void {
  try {
    localStorage.setItem(PROMO_CODE_KEY, code);
  } catch {
    // Ignore storage errors
  }
}

function clearStoredPromoCode(): void {
  try {
    localStorage.removeItem(PROMO_CODE_KEY);
  } catch {
    // Ignore storage errors
  }
}

async function readErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const body = await response.clone().json();
    if (typeof body?.message === 'string' && body.message.trim().length > 0) {
      return body.message;
    }
    if (typeof body?.error === 'string' && body.error.trim().length > 0) {
      return body.error;
    }
  } catch {
    // Ignore JSON parse errors
  }

  try {
    const text = await response.clone().text();
    if (text?.trim()) {
      return text.trim();
    }
  } catch {
    // Ignore plain text parse errors
  }

  return fallback;
}

function getPromoFromUrl(): string | null {
  if (typeof window === 'undefined') return null;
  const params = new URLSearchParams(window.location.search);
  return params.get('promo');
}

export function usePricingData(options: UsePricingDataOptions = {}): UsePricingDataReturn {
  const initialCachedData = getFromCache();
  const [data, setData] = useState<PricingPageData | null>(() => initialCachedData);
  const [loading, setLoading] = useState<boolean>(!initialCachedData);
  const [error, setError] = useState<string | null>(null);
  const [promoValidation, setPromoValidation] = useState<PromoCodeValidationResult | null>(null);
  const [promoApplications, setPromoApplications] = useState<Map<number, PromoCodeApplicationResult>>(
    new Map()
  );

  // In-memory cache for deduplication
  const [inMemoryData, setInMemoryData] = useState<PricingPageData | null>(null);

  const fetchPricingData = useCallback(async (useCache = true) => {
    // Check in-memory cache first
    if (useCache && inMemoryData) {
      setData(inMemoryData);
      return;
    }

    // Check localStorage cache
    if (useCache) {
      const cached = getFromCache();
      if (cached) {
        setData(cached);
        setInMemoryData(cached);
        setLoading(false);
        return;
      }
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/public/pricing');
      if (!response.ok) {
        const message = await readErrorMessage(response, 'Failed to fetch pricing data');
        throw new Error(message);
      }
      const pricingData: PricingPageData = await response.json();
      setData(pricingData);
      setInMemoryData(pricingData);
      saveToCache(pricingData);
      setError(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'An error occurred while loading pricing';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [inMemoryData]);

  const validatePromoCode = useCallback(async (code: string): Promise<PromoCodeValidationResult> => {
    try {
      const response = await fetch('/api/public/pricing/validate-promo', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code }),
      });

      if (!response.ok) {
        const message = await readErrorMessage(response, 'Failed to validate promo code');
        throw new Error(message);
      }

      const result: PromoCodeValidationResult = await response.json();
      setPromoValidation(result);

      if (result.isValid) {
        savePromoCode(code);
        // Update URL with promo code
        const url = new URL(window.location.href);
        url.searchParams.set('promo', code);
        window.history.replaceState({}, '', url.toString());
      }

      return result;
    } catch (err) {
      const errorResult: PromoCodeValidationResult = {
        isValid: false,
        errorMessage: err instanceof Error ? err.message : 'Validation failed',
      };
      setPromoValidation(errorResult);
      return errorResult;
    }
  }, []);

  const applyPromoCode = useCallback(
    async (code: string, tierId: number): Promise<PromoCodeApplicationResult> => {
      try {
        const response = await fetch('/api/public/pricing/apply-promo', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ code, tierId }),
        });

        if (!response.ok) {
          const message = await readErrorMessage(response, 'Failed to apply promo code');
          throw new Error(message);
        }

        const result: PromoCodeApplicationResult = await response.json();

        if (result.success) {
          setPromoApplications((prev) => {
            const newMap = new Map(prev);
            newMap.set(tierId, result);
            return newMap;
          });
        }

        return result;
      } catch (err) {
        const errorResult: PromoCodeApplicationResult = {
          success: false,
          errorMessage: err instanceof Error ? err.message : 'Application failed',
        };
        setPromoApplications((prev) => {
          const next = new Map(prev);
          next.delete(tierId);
          return next;
        });
        return errorResult;
      }
    },
    []
  );

  const clearPromoCode = useCallback(() => {
    clearStoredPromoCode();
    setPromoValidation(null);
    setPromoApplications(new Map());

    // Remove from URL
    const url = new URL(window.location.href);
    url.searchParams.delete('promo');
    window.history.replaceState({}, '', url.toString());
  }, []);

  const refetch = useCallback(async () => {
    await fetchPricingData(false);
  }, [fetchPricingData]);

  // Initial fetch
  useEffect(() => {
    fetchPricingData();
  }, [fetchPricingData]);

  // Auto-validate promo code from URL or props or localStorage
  useEffect(() => {
    const promoCode = options.promoCode || getPromoFromUrl() || getStoredPromoCode();
    if (promoCode && !promoValidation) {
      validatePromoCode(promoCode);
    }
  }, [options.promoCode, promoValidation, validatePromoCode]);

  return {
    data,
    loading,
    error,
    promoValidation,
    promoApplications,
    validatePromoCode,
    applyPromoCode,
    clearPromoCode,
    refetch,
  };
}

export default usePricingData;
