/**
 * useSiteSwitcher Hook
 *
 * Manages site version preference between desktop and mobile.
 */

import { useState, useEffect, useCallback } from 'react'

// Storage keys
const PREFERENCE_KEY = 'caskr_site_preference'
const DISMISS_KEY = 'caskr_site_banner_dismissed'
const DISMISS_TIMEOUT_DAYS = 7

// API endpoint
const API_ENDPOINT = '/api/mobile/preference'

export type SiteVersion = 'desktop' | 'mobile' | 'auto'

export interface SiteSwitcherState {
  /** Current site version */
  currentSite: SiteVersion
  /** Detected device type */
  detectedDevice: 'mobile' | 'desktop' | 'tablet' | 'unknown'
  /** Whether site matches detected device */
  isMismatch: boolean
  /** User's saved preference */
  preference: SiteVersion
  /** Whether banner should be shown */
  shouldShowBanner: boolean
  /** Whether loading state */
  isLoading: boolean
  /** Error if any */
  error: string | null
  /** Switch to a different site version */
  switchSite: (version: SiteVersion, remember?: boolean) => Promise<void>
  /** Dismiss the banner temporarily */
  dismissBanner: () => void
  /** Dismiss the banner permanently (don't ask again) */
  dismissBannerPermanently: () => void
  /** Clear preference (revert to auto) */
  clearPreference: () => Promise<void>
  /** Refresh state */
  refresh: () => Promise<void>
}

/**
 * Detect if current site is mobile based on URL or config
 */
function detectCurrentSite(): SiteVersion {
  const hostname = window.location.hostname
  // Check if on mobile subdomain
  if (hostname.startsWith('m.') || hostname.includes('mobile')) {
    return 'mobile'
  }
  return 'desktop'
}

/**
 * Detect device type from user agent
 */
function detectDeviceType(): 'mobile' | 'desktop' | 'tablet' | 'unknown' {
  const ua = navigator.userAgent.toLowerCase()

  // Check for tablets first (they often include mobile in UA)
  if (/ipad|tablet|(android(?!.*mobile))/i.test(ua)) {
    return 'tablet'
  }

  // Check for mobile
  if (/iphone|ipod|android.*mobile|windows phone|blackberry|opera mini|iemobile/i.test(ua)) {
    return 'mobile'
  }

  // Check for desktop indicators
  if (/windows|macintosh|linux/i.test(ua) && !/mobile/i.test(ua)) {
    return 'desktop'
  }

  return 'unknown'
}

/**
 * Check if banner was recently dismissed
 */
function wasBannerDismissed(): boolean {
  const dismissedAt = localStorage.getItem(DISMISS_KEY)
  if (!dismissedAt) return false

  const dismissDate = new Date(dismissedAt)
  const now = new Date()
  const daysSinceDismiss = (now.getTime() - dismissDate.getTime()) / (1000 * 60 * 60 * 24)

  return daysSinceDismiss < DISMISS_TIMEOUT_DAYS
}

/**
 * Hook for managing site switching
 */
export function useSiteSwitcher(): SiteSwitcherState {
  const [currentSite] = useState<SiteVersion>(() => detectCurrentSite())
  const [detectedDevice] = useState<'mobile' | 'desktop' | 'tablet' | 'unknown'>(() =>
    detectDeviceType()
  )
  const [preference, setPreference] = useState<SiteVersion>('auto')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isBannerDismissed, setIsBannerDismissed] = useState(() => wasBannerDismissed())

  // Check if detected device is known (not unknown)
  const isKnownDevice = detectedDevice !== 'unknown' && detectedDevice !== 'tablet'

  // Calculate mismatch (only for mobile/desktop, not tablet or unknown)
  const isMismatch = isKnownDevice && (
    (detectedDevice === 'mobile' && currentSite === 'desktop') ||
    (detectedDevice === 'desktop' && currentSite === 'mobile')
  )

  // Determine if banner should show
  const shouldShowBanner =
    !isBannerDismissed &&
    preference === 'auto' &&
    isMismatch

  // Load preference from API/localStorage
  const loadPreference = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      // Try API first
      const response = await fetch(API_ENDPOINT)
      if (response.ok) {
        const data = await response.json()
        const pref = data.preferredSite === 1 ? 'desktop' :
          data.preferredSite === 2 ? 'mobile' : 'auto'
        setPreference(pref)
        localStorage.setItem(PREFERENCE_KEY, pref)
      } else {
        // Fall back to localStorage
        const stored = localStorage.getItem(PREFERENCE_KEY) as SiteVersion | null
        if (stored) {
          setPreference(stored)
        }
      }
    } catch {
      // Fall back to localStorage
      const stored = localStorage.getItem(PREFERENCE_KEY) as SiteVersion | null
      if (stored) {
        setPreference(stored)
      }
    } finally {
      setIsLoading(false)
    }
  }, [])

  // Initial load
  useEffect(() => {
    loadPreference()
  }, [loadPreference])

  // Listen for storage events (sync across tabs)
  useEffect(() => {
    const handleStorage = (e: StorageEvent) => {
      if (e.key === PREFERENCE_KEY && e.newValue) {
        setPreference(e.newValue as SiteVersion)
      }
      if (e.key === DISMISS_KEY) {
        setIsBannerDismissed(wasBannerDismissed())
      }
    }

    window.addEventListener('storage', handleStorage)
    return () => window.removeEventListener('storage', handleStorage)
  }, [])

  // Listen for logout event to clear preference
  useEffect(() => {
    const handleLogout = () => {
      localStorage.removeItem(PREFERENCE_KEY)
      setPreference('auto')
    }

    window.addEventListener('caskr-logout', handleLogout)
    return () => window.removeEventListener('caskr-logout', handleLogout)
  }, [])

  // Switch site version
  const switchSite = useCallback(
    async (version: SiteVersion, remember = true) => {
      setIsLoading(true)
      setError(null)

      try {
        if (remember) {
          // Save to API
          try {
            const preferredSite = version === 'desktop' ? 'desktop' :
              version === 'mobile' ? 'mobile' : 'auto'

            const response = await fetch(API_ENDPOINT, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ preferredSite }),
            })

            if (!response.ok) {
              throw new Error('API save failed')
            }
          } catch {
            // Save to localStorage as fallback
            localStorage.setItem(PREFERENCE_KEY, version)
          }

          setPreference(version)
          localStorage.setItem(PREFERENCE_KEY, version)
        }

        // Redirect if needed
        if (version !== currentSite && version !== 'auto') {
          const currentUrl = new URL(window.location.href)

          if (version === 'mobile') {
            // Redirect to mobile site
            if (!currentUrl.hostname.startsWith('m.')) {
              currentUrl.hostname = `m.${currentUrl.hostname}`
              window.location.href = currentUrl.toString()
            }
          } else if (version === 'desktop') {
            // Redirect to desktop site
            if (currentUrl.hostname.startsWith('m.')) {
              currentUrl.hostname = currentUrl.hostname.replace(/^m\./, '')
              window.location.href = currentUrl.toString()
            }
          }
        }
      } catch (err) {
        setError('Failed to save preference')
        console.error('[SiteSwitcher] Error:', err)
      } finally {
        setIsLoading(false)
      }
    },
    [currentSite]
  )

  // Dismiss banner temporarily
  const dismissBanner = useCallback(() => {
    localStorage.setItem(DISMISS_KEY, new Date().toISOString())
    setIsBannerDismissed(true)
  }, [])

  // Dismiss banner permanently
  const dismissBannerPermanently = useCallback(() => {
    // Set a far future date
    const farFuture = new Date()
    farFuture.setFullYear(farFuture.getFullYear() + 10)
    localStorage.setItem(DISMISS_KEY, farFuture.toISOString())
    setIsBannerDismissed(true)
  }, [])

  // Clear preference
  const clearPreference = useCallback(async () => {
    setIsLoading(true)

    try {
      await fetch(API_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ preferredSite: 'auto' }),
      })
    } catch {
      // Ignore API errors
    }

    localStorage.removeItem(PREFERENCE_KEY)
    localStorage.removeItem(DISMISS_KEY)
    setPreference('auto')
    setIsBannerDismissed(false)
    setIsLoading(false)
  }, [])

  return {
    currentSite,
    detectedDevice,
    isMismatch,
    preference,
    shouldShowBanner,
    isLoading,
    error,
    switchSite,
    dismissBanner,
    dismissBannerPermanently,
    clearPreference,
    refresh: loadPreference,
  }
}

export default useSiteSwitcher
