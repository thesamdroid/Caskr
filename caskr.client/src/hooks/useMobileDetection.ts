import { useState, useEffect, useCallback, useRef } from 'react'
import {
  DeviceDetectionResponse,
  SitePreference,
  SaveSitePreferenceRequest,
  SitePreferenceResponse,
  MobileDetectionState
} from '../types/mobile'

// Constants
const MOBILE_BREAKPOINT = 768
const LOCAL_STORAGE_KEY = 'caskr_site_preference'
const DETECTION_CACHE_KEY = 'caskr_device_detection'
const CACHE_DURATION_MS = 30 * 60 * 1000 // 30 minutes

// Mobile domain configuration
const MOBILE_DOMAIN = 'm.caskr.co'
const DESKTOP_DOMAIN = 'caskr.co'

interface CachedDetection {
  data: DeviceDetectionResponse
  timestamp: number
}

interface UseMobileDetectionOptions {
  autoRedirect?: boolean
  redirectDelay?: number
  onRedirect?: (targetSite: 'desktop' | 'mobile') => void
}

interface UseMobileDetectionReturn extends MobileDetectionState {
  setPreference: (preference: SitePreference) => Promise<void>
  refresh: () => Promise<void>
  clearCache: () => void
}

/**
 * Hook for detecting mobile devices and managing site preference
 */
export function useMobileDetection(
  options: UseMobileDetectionOptions = {}
): UseMobileDetectionReturn {
  const { autoRedirect = false, redirectDelay = 0, onRedirect } = options

  const [state, setState] = useState<MobileDetectionState>({
    isLoading: true,
    error: null,
    detection: null,
    preference: 'auto',
    screenWidth: typeof window !== 'undefined' ? window.innerWidth : 1920,
    screenHeight: typeof window !== 'undefined' ? window.innerHeight : 1080,
    isMobileDevice: false,
    isTabletDevice: false,
    recommendedSite: 'desktop',
    shouldRedirect: false
  })

  const redirectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const hasFetchedRef = useRef(false)

  /**
   * Get cached detection result
   */
  const getCachedDetection = useCallback((): DeviceDetectionResponse | null => {
    try {
      const cached = sessionStorage.getItem(DETECTION_CACHE_KEY)
      if (cached) {
        const parsed: CachedDetection = JSON.parse(cached)
        if (Date.now() - parsed.timestamp < CACHE_DURATION_MS) {
          return parsed.data
        }
      }
    } catch {
      // Ignore cache errors
    }
    return null
  }, [])

  /**
   * Cache detection result
   */
  const cacheDetection = useCallback((data: DeviceDetectionResponse) => {
    try {
      const cached: CachedDetection = {
        data,
        timestamp: Date.now()
      }
      sessionStorage.setItem(DETECTION_CACHE_KEY, JSON.stringify(cached))
    } catch {
      // Ignore cache errors
    }
  }, [])

  /**
   * Get stored preference from localStorage
   */
  const getStoredPreference = useCallback((): SitePreference => {
    try {
      const stored = localStorage.getItem(LOCAL_STORAGE_KEY)
      if (stored === 'auto' || stored === 'desktop' || stored === 'mobile') {
        return stored
      }
    } catch {
      // Ignore localStorage errors
    }
    return 'auto'
  }, [])

  /**
   * Store preference in localStorage
   */
  const storePreference = useCallback((preference: SitePreference) => {
    try {
      localStorage.setItem(LOCAL_STORAGE_KEY, preference)
    } catch {
      // Ignore localStorage errors
    }
  }, [])

  /**
   * Client-side detection fallback
   */
  const detectClientSide = useCallback((): Partial<DeviceDetectionResponse> => {
    const userAgent = navigator.userAgent
    const isMobile = /Mobile|iP(hone|od)|Android.*Mobile|Windows Phone|BlackBerry/i.test(userAgent)
    const isTablet = /iPad|Android(?!.*Mobile)|Tablet|Kindle|Silk/i.test(userAgent)
    const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0
    const screenWidth = window.innerWidth

    let recommendedSite: 'desktop' | 'mobile' = 'desktop'
    if (isMobile || isTablet || screenWidth < MOBILE_BREAKPOINT) {
      recommendedSite = 'mobile'
    }

    return {
      isMobile,
      isTablet,
      hasTouchCapability: isTouchDevice,
      recommendedSite
    }
  }, [])

  /**
   * Fetch detection from API
   */
  const fetchDetection = useCallback(async (): Promise<DeviceDetectionResponse | null> => {
    try {
      const screenWidth = window.innerWidth
      const response = await fetch(`/api/mobile/detect?screenWidth=${screenWidth}`)

      if (!response.ok) {
        throw new Error(`API returned ${response.status}`)
      }

      const data: DeviceDetectionResponse = await response.json()
      cacheDetection(data)
      return data
    } catch (error) {
      console.error('[useMobileDetection] API fetch failed:', error)
      return null
    }
  }, [cacheDetection])

  /**
   * Determine if redirect should happen
   */
  const shouldRedirectToSite = useCallback(
    (
      detection: DeviceDetectionResponse | null,
      preference: SitePreference,
      currentHost: string
    ): { shouldRedirect: boolean; targetSite: 'desktop' | 'mobile' } => {
      const isOnMobileSite = currentHost.includes(MOBILE_DOMAIN)
      const isOnDesktopSite = currentHost.includes(DESKTOP_DOMAIN) && !isOnMobileSite

      // If we can't determine current site, don't redirect
      if (!isOnMobileSite && !isOnDesktopSite) {
        return { shouldRedirect: false, targetSite: 'desktop' }
      }

      // Determine target site based on preference or detection
      let targetSite: 'desktop' | 'mobile' = 'desktop'

      if (preference === 'mobile') {
        targetSite = 'mobile'
      } else if (preference === 'desktop') {
        targetSite = 'desktop'
      } else if (detection) {
        // Auto mode: use detection
        if (detection.isBot) {
          targetSite = 'desktop' // Don't redirect bots
          return { shouldRedirect: false, targetSite }
        }
        targetSite = detection.recommendedSite
      }

      // Determine if redirect is needed
      const shouldRedirect =
        (isOnDesktopSite && targetSite === 'mobile') ||
        (isOnMobileSite && targetSite === 'desktop')

      return { shouldRedirect, targetSite }
    },
    []
  )

  /**
   * Perform redirect
   */
  const performRedirect = useCallback(
    (targetSite: 'desktop' | 'mobile') => {
      const currentUrl = new URL(window.location.href)
      const targetHost = targetSite === 'mobile' ? MOBILE_DOMAIN : DESKTOP_DOMAIN

      // Only redirect if host needs to change
      if (!currentUrl.host.includes(targetHost)) {
        const newUrl = `${currentUrl.protocol}//${targetHost}${currentUrl.pathname}${currentUrl.search}`

        if (onRedirect) {
          onRedirect(targetSite)
        }

        window.location.href = newUrl
      }
    },
    [onRedirect]
  )

  /**
   * Initialize detection
   */
  const initialize = useCallback(async () => {
    if (hasFetchedRef.current) return
    hasFetchedRef.current = true

    const screenWidth = window.innerWidth
    const screenHeight = window.innerHeight
    const storedPreference = getStoredPreference()

    // Check for cached detection first
    let detection = getCachedDetection()

    if (!detection) {
      // Fetch from API
      detection = await fetchDetection()
    }

    // If API failed, use client-side detection
    if (!detection) {
      const clientDetection = detectClientSide()
      detection = {
        deviceType: clientDetection.isMobile ? 'Mobile' : clientDetection.isTablet ? 'Tablet' : 'Desktop',
        deviceName: 'Unknown',
        browser: 'Unknown',
        operatingSystem: 'Unknown',
        isMobile: clientDetection.isMobile ?? false,
        isTablet: clientDetection.isTablet ?? false,
        isBot: false,
        hasTouchCapability: clientDetection.hasTouchCapability ?? false,
        recommendedSite: clientDetection.recommendedSite ?? 'desktop'
      }
    }

    // Use server preference if available, otherwise localStorage
    const preference = detection.userPreference?.preferredSite ?? storedPreference

    // Determine if redirect should happen
    const { shouldRedirect, targetSite } = shouldRedirectToSite(
      detection,
      preference,
      window.location.host
    )

    setState({
      isLoading: false,
      error: null,
      detection,
      preference,
      screenWidth,
      screenHeight,
      isMobileDevice: detection.isMobile,
      isTabletDevice: detection.isTablet,
      recommendedSite: detection.recommendedSite,
      shouldRedirect
    })

    // Auto-redirect if enabled
    if (autoRedirect && shouldRedirect) {
      if (redirectDelay > 0) {
        redirectTimeoutRef.current = setTimeout(() => {
          performRedirect(targetSite)
        }, redirectDelay)
      } else {
        performRedirect(targetSite)
      }
    }
  }, [
    getStoredPreference,
    getCachedDetection,
    fetchDetection,
    detectClientSide,
    shouldRedirectToSite,
    autoRedirect,
    redirectDelay,
    performRedirect
  ])

  /**
   * Set user preference
   */
  const setPreference = useCallback(
    async (preference: SitePreference) => {
      try {
        // Store locally first for immediate effect
        storePreference(preference)

        setState((prev) => ({
          ...prev,
          preference
        }))

        // Send to API
        const request: SaveSitePreferenceRequest = {
          preferredSite: preference,
          screenWidth: window.innerWidth,
          screenHeight: window.innerHeight
        }

        const response = await fetch('/api/mobile/preference', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(request)
        })

        if (!response.ok) {
          console.warn('[useMobileDetection] Failed to save preference to server')
        } else {
          const data: SitePreferenceResponse = await response.json()
          console.log('[useMobileDetection] Preference saved:', data)
        }
      } catch (error) {
        console.error('[useMobileDetection] Error setting preference:', error)
        // Don't throw - localStorage fallback is sufficient
      }
    },
    [storePreference]
  )

  /**
   * Refresh detection
   */
  const refresh = useCallback(async () => {
    hasFetchedRef.current = false
    setState((prev) => ({ ...prev, isLoading: true, error: null }))

    // Clear cache before refetching
    try {
      sessionStorage.removeItem(DETECTION_CACHE_KEY)
    } catch {
      // Ignore
    }

    await initialize()
  }, [initialize])

  /**
   * Clear all cached data
   */
  const clearCache = useCallback(() => {
    try {
      sessionStorage.removeItem(DETECTION_CACHE_KEY)
      localStorage.removeItem(LOCAL_STORAGE_KEY)
    } catch {
      // Ignore errors
    }
  }, [])

  // Initialize on mount
  useEffect(() => {
    initialize()

    return () => {
      if (redirectTimeoutRef.current) {
        clearTimeout(redirectTimeoutRef.current)
      }
    }
  }, [initialize])

  // Listen for screen resize
  useEffect(() => {
    const handleResize = () => {
      const screenWidth = window.innerWidth
      const screenHeight = window.innerHeight
      const isMobileSize = screenWidth < MOBILE_BREAKPOINT

      setState((prev) => ({
        ...prev,
        screenWidth,
        screenHeight,
        // Update recommended site if in auto mode and crossing breakpoint
        recommendedSite:
          prev.preference === 'auto'
            ? isMobileSize || prev.isMobileDevice || prev.isTabletDevice
              ? 'mobile'
              : 'desktop'
            : prev.recommendedSite
      }))
    }

    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  return {
    ...state,
    setPreference,
    refresh,
    clearCache
  }
}

export default useMobileDetection
