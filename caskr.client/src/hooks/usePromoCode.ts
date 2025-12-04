/**
 * usePromoCode Hook
 *
 * Manages promo code state with URL parameter sync, localStorage persistence,
 * and API validation integration.
 */

import { useState, useCallback, useEffect, useRef } from 'react';
import { PromoCodeValidationResult, DiscountType } from '../types/pricing';
import { ValidatedPromo } from '../utils/calculatePromoPrice';

// Storage keys
const PROMO_STORAGE_KEY = 'caskr_promo_code';
const PROMO_DATA_KEY = 'caskr_promo_data';
const PROMO_EXPIRY_KEY = 'caskr_promo_expiry';

// Promo data expires after 24 hours
const PROMO_EXPIRY_DURATION = 24 * 60 * 60 * 1000;

/**
 * Error types for promo code validation
 */
export type PromoErrorType = 'invalid' | 'expired' | 'max_redemptions' | 'network' | 'unknown';

/**
 * Promo code validation response
 */
export interface PromoValidationResponse {
  valid: boolean;
  promo?: ValidatedPromo;
  error?: string;
  errorType?: PromoErrorType;
}

interface UsePromoCodeOptions {
  initialCode?: string;
  autoValidateOnMount?: boolean;
  syncWithUrl?: boolean;
  persistToStorage?: boolean;
}

interface UsePromoCodeReturn {
  /** The currently validated promo (null if none) */
  promo: ValidatedPromo | null;
  /** The raw validation result from API */
  validationResult: PromoCodeValidationResult | null;
  /** Whether validation is in progress */
  isValidating: boolean;
  /** Error message if validation failed */
  error: string | null;
  /** Error type for specific error handling */
  errorType: PromoErrorType | null;
  /** Validate a promo code */
  validateCode: (code: string) => Promise<PromoValidationResponse>;
  /** Apply a validated promo */
  applyPromo: (promo: ValidatedPromo) => void;
  /** Remove the current promo */
  removePromo: () => void;
  /** Get the current code from URL or storage */
  getCurrentCode: () => string | null;
}

/**
 * Get promo code from URL parameter
 */
function getPromoFromUrl(): string | null {
  if (typeof window === 'undefined') return null;
  const params = new URLSearchParams(window.location.search);
  return params.get('promo');
}

/**
 * Update URL with promo code
 */
function updateUrlWithPromo(code: string | null): void {
  if (typeof window === 'undefined') return;

  const url = new URL(window.location.href);
  if (code) {
    url.searchParams.set('promo', code);
  } else {
    url.searchParams.delete('promo');
  }
  window.history.replaceState({}, '', url.toString());
}

/**
 * Get stored promo code from localStorage
 */
function getStoredPromoCode(): string | null {
  try {
    return localStorage.getItem(PROMO_STORAGE_KEY);
  } catch {
    return null;
  }
}

/**
 * Get stored promo data from localStorage
 */
function getStoredPromoData(): ValidatedPromo | null {
  try {
    const expiry = localStorage.getItem(PROMO_EXPIRY_KEY);
    if (expiry && Date.now() > parseInt(expiry, 10)) {
      // Promo data has expired
      clearStoredPromo();
      return null;
    }

    const data = localStorage.getItem(PROMO_DATA_KEY);
    if (data) {
      return JSON.parse(data);
    }
  } catch {
    // Ignore parse errors
  }
  return null;
}

/**
 * Store promo code and data
 */
function storePromo(code: string, promo: ValidatedPromo): void {
  try {
    localStorage.setItem(PROMO_STORAGE_KEY, code);
    localStorage.setItem(PROMO_DATA_KEY, JSON.stringify(promo));
    localStorage.setItem(PROMO_EXPIRY_KEY, String(Date.now() + PROMO_EXPIRY_DURATION));
  } catch {
    // Ignore storage errors
  }
}

/**
 * Clear stored promo data
 */
function clearStoredPromo(): void {
  try {
    localStorage.removeItem(PROMO_STORAGE_KEY);
    localStorage.removeItem(PROMO_DATA_KEY);
    localStorage.removeItem(PROMO_EXPIRY_KEY);
  } catch {
    // Ignore storage errors
  }
}

/**
 * Map API error message to error type
 */
function getErrorType(errorMessage?: string): PromoErrorType {
  if (!errorMessage) return 'unknown';

  const message = errorMessage.toLowerCase();
  if (message.includes('invalid') || message.includes('not valid')) return 'invalid';
  if (message.includes('expired')) return 'expired';
  if (message.includes('redemption') || message.includes('no longer available')) return 'max_redemptions';
  if (message.includes('network') || message.includes('connection')) return 'network';

  return 'unknown';
}

/**
 * Get user-friendly error message
 */
function getUserFriendlyError(errorType: PromoErrorType): string {
  switch (errorType) {
    case 'invalid':
      return 'This promo code is not valid';
    case 'expired':
      return 'This promo code has expired';
    case 'max_redemptions':
      return 'This promo code is no longer available';
    case 'network':
      return 'Could not validate code. Please try again.';
    default:
      return 'Unable to validate promo code';
  }
}

/**
 * Validate a promo code against the API
 */
async function validatePromoCodeApi(code: string): Promise<PromoValidationResponse> {
  try {
    const response = await fetch('/api/public/pricing/validate-promo', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code }),
    });

    if (!response.ok) {
      const errorType: PromoErrorType = response.status >= 500 ? 'network' : 'invalid';
      return {
        valid: false,
        error: getUserFriendlyError(errorType),
        errorType,
      };
    }

    const result: PromoCodeValidationResult = await response.json();

    if (result.isValid && result.code) {
      const promo: ValidatedPromo = {
        code: result.code,
        discountType: result.discountType ?? 'percentage',
        discountValue: result.discountValue ?? 0,
        description: result.discountDescription ?? result.description ?? '',
        appliesTo: result.applicableTierIds ?? null,
      };

      return {
        valid: true,
        promo,
      };
    }

    const errorType = getErrorType(result.errorMessage);
    return {
      valid: false,
      error: result.errorMessage ?? getUserFriendlyError(errorType),
      errorType,
    };
  } catch (error) {
    return {
      valid: false,
      error: getUserFriendlyError('network'),
      errorType: 'network',
    };
  }
}

/**
 * Hook for managing promo code state
 */
export function usePromoCode(options: UsePromoCodeOptions = {}): UsePromoCodeReturn {
  const {
    initialCode,
    autoValidateOnMount = true,
    syncWithUrl = true,
    persistToStorage = true,
  } = options;

  const [promo, setPromo] = useState<ValidatedPromo | null>(() => {
    // Try to restore from storage on initial load
    if (persistToStorage) {
      return getStoredPromoData();
    }
    return null;
  });

  const [validationResult, setValidationResult] = useState<PromoCodeValidationResult | null>(null);
  const [isValidating, setIsValidating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [errorType, setErrorType] = useState<PromoErrorType | null>(null);

  // Track if we've already validated on mount
  const hasValidatedOnMount = useRef(false);

  /**
   * Get the current code from URL, props, or storage
   */
  const getCurrentCode = useCallback((): string | null => {
    // Priority: URL > initialCode prop > localStorage
    const urlCode = getPromoFromUrl();
    if (urlCode) return urlCode;

    if (initialCode) return initialCode;

    if (persistToStorage) {
      return getStoredPromoCode();
    }

    return null;
  }, [initialCode, persistToStorage]);

  /**
   * Validate a promo code
   */
  const validateCode = useCallback(
    async (code: string): Promise<PromoValidationResponse> => {
      setIsValidating(true);
      setError(null);
      setErrorType(null);

      const result = await validatePromoCodeApi(code);

      if (result.valid && result.promo) {
        setPromo(result.promo);
        setValidationResult({
          isValid: true,
          code: result.promo.code,
          discountType: result.promo.discountType,
          discountValue: result.promo.discountValue,
          discountDescription: result.promo.description,
          applicableTierIds: result.promo.appliesTo ?? undefined,
        });

        // Persist to storage
        if (persistToStorage) {
          storePromo(code, result.promo);
        }

        // Update URL
        if (syncWithUrl) {
          updateUrlWithPromo(code);
        }
      } else {
        setError(result.error ?? 'Validation failed');
        setErrorType(result.errorType ?? 'unknown');
        setValidationResult({
          isValid: false,
          errorMessage: result.error,
        });
      }

      setIsValidating(false);
      return result;
    },
    [persistToStorage, syncWithUrl]
  );

  /**
   * Apply a pre-validated promo
   */
  const applyPromo = useCallback(
    (validatedPromo: ValidatedPromo) => {
      setPromo(validatedPromo);
      setError(null);
      setErrorType(null);
      setValidationResult({
        isValid: true,
        code: validatedPromo.code,
        discountType: validatedPromo.discountType,
        discountValue: validatedPromo.discountValue,
        discountDescription: validatedPromo.description,
        applicableTierIds: validatedPromo.appliesTo ?? undefined,
      });

      if (persistToStorage) {
        storePromo(validatedPromo.code, validatedPromo);
      }

      if (syncWithUrl) {
        updateUrlWithPromo(validatedPromo.code);
      }
    },
    [persistToStorage, syncWithUrl]
  );

  /**
   * Remove the current promo
   */
  const removePromo = useCallback(() => {
    setPromo(null);
    setValidationResult(null);
    setError(null);
    setErrorType(null);

    if (persistToStorage) {
      clearStoredPromo();
    }

    if (syncWithUrl) {
      updateUrlWithPromo(null);
    }
  }, [persistToStorage, syncWithUrl]);

  // Auto-validate promo code from URL/props/storage on mount
  useEffect(() => {
    if (!autoValidateOnMount || hasValidatedOnMount.current) return;

    const code = getCurrentCode();
    if (code && !promo) {
      hasValidatedOnMount.current = true;
      validateCode(code);
    }
  }, [autoValidateOnMount, getCurrentCode, promo, validateCode]);

  return {
    promo,
    validationResult,
    isValidating,
    error,
    errorType,
    validateCode,
    applyPromo,
    removePromo,
    getCurrentCode,
  };
}

export default usePromoCode;
