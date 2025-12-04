/**
 * useAnalytics Hook
 *
 * React hook for tracking analytics events with context support.
 */

import { useCallback, useContext, useEffect, useRef } from 'react';
import { AnalyticsContext } from '../components/analytics/AnalyticsProvider';
import { getAnalytics, EventProperties } from '../services/analytics';

interface UseAnalyticsOptions {
  /** Default properties to include with all events */
  defaultProperties?: EventProperties;
  /** Auto-track page view on mount */
  trackPageViewOnMount?: boolean;
  /** Page name for auto-tracking */
  pageName?: string;
}

interface UseAnalyticsReturn {
  /** Track an event */
  track: (event: string, properties?: EventProperties) => void;
  /** Track a page view */
  trackPageView: (name?: string, properties?: EventProperties) => void;
  /** Identify a user */
  identify: (userId: string, traits?: EventProperties) => void;
  /** Check if analytics has consent */
  hasConsent: boolean;
  /** Set consent status */
  setConsent: (consent: boolean) => void;
}

/**
 * Hook for tracking analytics events
 */
export function useAnalytics(options: UseAnalyticsOptions = {}): UseAnalyticsReturn {
  const {
    defaultProperties = {},
    trackPageViewOnMount = false,
    pageName,
  } = options;

  // Try to get context, but allow usage without provider
  let contextValue: { hasConsent: boolean; setConsent: (consent: boolean) => void } | null = null;
  try {
    contextValue = useContext(AnalyticsContext);
  } catch {
    // Context not available, use defaults
  }

  const hasConsent = contextValue?.hasConsent ?? false;
  const setConsent = contextValue?.setConsent ?? (() => {});
  const hasTrackedPageView = useRef(false);

  /**
   * Track an event with merged properties
   */
  const track = useCallback(
    (event: string, properties?: EventProperties) => {
      const analytics = getAnalytics();
      analytics.track(event, {
        ...defaultProperties,
        ...properties,
      });
    },
    [defaultProperties]
  );

  /**
   * Track a page view
   */
  const trackPageView = useCallback(
    (name?: string, properties?: EventProperties) => {
      const analytics = getAnalytics();
      const pageToTrack = name ?? pageName ?? document.title;
      analytics.page(pageToTrack, {
        ...defaultProperties,
        ...properties,
      });
    },
    [defaultProperties, pageName]
  );

  /**
   * Identify a user
   */
  const identify = useCallback(
    (userId: string, traits?: EventProperties) => {
      const analytics = getAnalytics();
      analytics.identify(userId, traits);
    },
    []
  );

  // Auto-track page view on mount
  useEffect(() => {
    if (trackPageViewOnMount && !hasTrackedPageView.current) {
      hasTrackedPageView.current = true;
      trackPageView();
    }
  }, [trackPageViewOnMount, trackPageView]);

  return {
    track,
    trackPageView,
    identify,
    hasConsent,
    setConsent,
  };
}

/**
 * Hook for tracking time on page
 */
export function useTimeOnPage(pageName: string) {
  const startTimeRef = useRef<number>(Date.now());
  const totalTimeRef = useRef<number>(0);
  const isVisibleRef = useRef<boolean>(true);
  const hasTrackedRef = useRef<boolean>(false);
  const { track } = useAnalytics();

  useEffect(() => {
    // Track time when visibility changes
    const handleVisibilityChange = () => {
      if (document.hidden) {
        // Page is hidden, add elapsed time
        totalTimeRef.current += Date.now() - startTimeRef.current;
        isVisibleRef.current = false;
      } else {
        // Page is visible again, restart timer
        startTimeRef.current = Date.now();
        isVisibleRef.current = true;
      }
    };

    // Track after 30 seconds
    const thirtySecondTimer = setTimeout(() => {
      if (!hasTrackedRef.current) {
        const currentTime = isVisibleRef.current
          ? totalTimeRef.current + (Date.now() - startTimeRef.current)
          : totalTimeRef.current;

        track('time_on_page', {
          page: pageName,
          duration_ms: currentTime,
          milestone: '30s',
        });
        hasTrackedRef.current = true;
      }
    }, 30000);

    document.addEventListener('visibilitychange', handleVisibilityChange);

    // Track on unmount
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      clearTimeout(thirtySecondTimer);

      const finalTime = isVisibleRef.current
        ? totalTimeRef.current + (Date.now() - startTimeRef.current)
        : totalTimeRef.current;

      track('time_on_page_final', {
        page: pageName,
        duration_ms: finalTime,
      });
    };
  }, [pageName, track]);
}

/**
 * Hook for tracking scroll depth
 */
export function useScrollDepth(pageName: string) {
  const milestonesTracked = useRef<Set<number>>(new Set());
  const { track } = useAnalytics();

  useEffect(() => {
    const milestones = [25, 50, 75, 100];

    const handleScroll = () => {
      const scrollTop = window.scrollY;
      const docHeight = document.documentElement.scrollHeight - window.innerHeight;
      const scrollPercent = docHeight > 0 ? Math.round((scrollTop / docHeight) * 100) : 0;

      for (const milestone of milestones) {
        if (scrollPercent >= milestone && !milestonesTracked.current.has(milestone)) {
          milestonesTracked.current.add(milestone);
          track('scroll_depth', {
            page: pageName,
            depth: milestone,
          });
        }
      }
    };

    // Throttle scroll handler
    let ticking = false;
    const throttledHandler = () => {
      if (!ticking) {
        window.requestAnimationFrame(() => {
          handleScroll();
          ticking = false;
        });
        ticking = true;
      }
    };

    window.addEventListener('scroll', throttledHandler, { passive: true });

    return () => {
      window.removeEventListener('scroll', throttledHandler);
    };
  }, [pageName, track]);
}

/**
 * Hook for tracking hover duration
 */
export function useHoverTracking<T extends HTMLElement>(
  event: string,
  properties: EventProperties = {}
) {
  const elementRef = useRef<T>(null);
  const hoverStartRef = useRef<number | null>(null);
  const { track } = useAnalytics();

  useEffect(() => {
    const element = elementRef.current;
    if (!element) return;

    const handleMouseEnter = () => {
      hoverStartRef.current = Date.now();
    };

    const handleMouseLeave = () => {
      if (hoverStartRef.current) {
        const duration = Date.now() - hoverStartRef.current;
        // Only track if hover was significant (> 500ms)
        if (duration > 500) {
          track(event, {
            ...properties,
            duration_ms: duration,
          });
        }
        hoverStartRef.current = null;
      }
    };

    element.addEventListener('mouseenter', handleMouseEnter);
    element.addEventListener('mouseleave', handleMouseLeave);

    return () => {
      element.removeEventListener('mouseenter', handleMouseEnter);
      element.removeEventListener('mouseleave', handleMouseLeave);
    };
  }, [event, properties, track]);

  return elementRef;
}

export default useAnalytics;
