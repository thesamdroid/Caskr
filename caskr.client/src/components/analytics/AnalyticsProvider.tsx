/**
 * Analytics Provider Component
 *
 * Provides analytics context to the React component tree.
 * Handles consent management and analytics initialization.
 */

import { createContext, useState, useEffect, useCallback, useMemo, ReactNode } from 'react';
import { initializeAnalytics, AnalyticsConfig, getAnalytics } from '../../services/analytics';

/**
 * Analytics context type
 */
export interface AnalyticsContextType {
  /** Whether user has given consent for analytics */
  hasConsent: boolean;
  /** Set consent status */
  setConsent: (consent: boolean) => void;
  /** Whether analytics is initialized */
  isInitialized: boolean;
}

/**
 * Analytics context
 */
export const AnalyticsContext = createContext<AnalyticsContextType>({
  hasConsent: false,
  setConsent: () => {},
  isInitialized: false,
});

/**
 * Storage key for consent
 */
const CONSENT_STORAGE_KEY = 'caskr_analytics_consent';

/**
 * Get stored consent status
 */
function getStoredConsent(): boolean | null {
  try {
    const stored = localStorage.getItem(CONSENT_STORAGE_KEY);
    if (stored === 'true') return true;
    if (stored === 'false') return false;
  } catch {
    // Ignore storage errors
  }
  return null;
}

/**
 * Store consent status
 */
function storeConsent(consent: boolean): void {
  try {
    localStorage.setItem(CONSENT_STORAGE_KEY, String(consent));
  } catch {
    // Ignore storage errors
  }
}

/**
 * Props for AnalyticsProvider
 */
export interface AnalyticsProviderProps {
  /** Child components */
  children: ReactNode;
  /** Analytics configuration */
  config?: AnalyticsConfig;
  /** Default consent status (for regions where consent is implied) */
  defaultConsent?: boolean;
  /** Whether to require explicit consent before tracking */
  requireExplicitConsent?: boolean;
}

/**
 * Analytics Provider component
 *
 * Wraps the application and provides analytics context.
 */
export function AnalyticsProvider({
  children,
  config = {},
  defaultConsent = false,
  requireExplicitConsent = true,
}: AnalyticsProviderProps) {
  const [hasConsent, setHasConsentState] = useState<boolean>(() => {
    const stored = getStoredConsent();
    if (stored !== null) return stored;
    return !requireExplicitConsent || defaultConsent;
  });

  const [isInitialized, setIsInitialized] = useState(false);

  // Initialize analytics on mount
  useEffect(() => {
    const analytics = initializeAnalytics(config);
    analytics.setConsent(hasConsent);
    setIsInitialized(true);

    // Set default properties from URL params
    if (typeof window !== 'undefined') {
      const params = new URLSearchParams(window.location.search);
      const utmSource = params.get('utm_source');
      const utmMedium = params.get('utm_medium');
      const utmCampaign = params.get('utm_campaign');
      const promoCode = params.get('promo');

      if (utmSource || utmMedium || utmCampaign || promoCode) {
        analytics.setDefaultProperties({
          utm_source: utmSource ?? undefined,
          utm_medium: utmMedium ?? undefined,
          utm_campaign: utmCampaign ?? undefined,
          promo_code: promoCode ?? undefined,
        });
      }
    }
  }, [config, hasConsent]);

  /**
   * Set consent status
   */
  const setConsent = useCallback((consent: boolean) => {
    setHasConsentState(consent);
    storeConsent(consent);
    getAnalytics().setConsent(consent);
  }, []);

  const contextValue = useMemo(
    () => ({
      hasConsent,
      setConsent,
      isInitialized,
    }),
    [hasConsent, setConsent, isInitialized]
  );

  return (
    <AnalyticsContext.Provider value={contextValue}>
      {children}
    </AnalyticsContext.Provider>
  );
}

export default AnalyticsProvider;
