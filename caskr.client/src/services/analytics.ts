/**
 * Analytics Service
 *
 * Provides a unified interface for tracking analytics events across multiple providers.
 * Supports Google Analytics 4, Segment (optional), custom backend, and console logging.
 */

// Event property types
export type EventProperties = Record<string, string | number | boolean | undefined | null>;

/**
 * Analytics provider interface
 */
export interface AnalyticsProvider {
  name: string;
  track(event: string, properties?: EventProperties): void;
  page(name: string, properties?: EventProperties): void;
  identify(userId: string, traits?: EventProperties): void;
  isEnabled(): boolean;
}

/**
 * Analytics service configuration
 */
export interface AnalyticsConfig {
  /** Google Analytics 4 Measurement ID */
  ga4MeasurementId?: string;
  /** Segment Write Key */
  segmentWriteKey?: string;
  /** Custom backend endpoint for analytics */
  backendEndpoint?: string;
  /** Enable console logging in development */
  enableConsoleLogging?: boolean;
  /** Respect Do Not Track header */
  respectDoNotTrack?: boolean;
  /** Enable analytics (can be toggled for consent) */
  enabled?: boolean;
}

/**
 * Check if Do Not Track is enabled
 */
function isDoNotTrackEnabled(): boolean {
  if (typeof navigator === 'undefined') return false;
  const dnt = navigator.doNotTrack || (window as any).doNotTrack || (navigator as any).msDoNotTrack;
  return dnt === '1' || dnt === 'yes' || dnt === true;
}

/**
 * Google Analytics 4 Provider
 */
class GA4Provider implements AnalyticsProvider {
  name = 'Google Analytics 4';
  private measurementId: string;
  private isLoaded = false;

  constructor(measurementId: string) {
    this.measurementId = measurementId;
    this.loadScript();
  }

  private loadScript() {
    if (typeof window === 'undefined' || this.isLoaded) return;

    // Check if GA is already loaded
    if ((window as any).gtag) {
      this.isLoaded = true;
      return;
    }

    // Load gtag.js
    const script = document.createElement('script');
    script.async = true;
    script.src = `https://www.googletagmanager.com/gtag/js?id=${this.measurementId}`;
    document.head.appendChild(script);

    // Initialize gtag
    (window as any).dataLayer = (window as any).dataLayer || [];
    (window as any).gtag = function gtag() {
      (window as any).dataLayer.push(arguments);
    };
    (window as any).gtag('js', new Date());
    (window as any).gtag('config', this.measurementId, {
      anonymize_ip: true, // GDPR compliance
      send_page_view: false, // Manual page tracking
    });

    this.isLoaded = true;
  }

  isEnabled(): boolean {
    return this.isLoaded && typeof (window as any).gtag === 'function';
  }

  track(event: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    (window as any).gtag('event', event, properties);
  }

  page(name: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    (window as any).gtag('event', 'page_view', {
      page_title: name,
      ...properties,
    });
  }

  identify(userId: string, traits?: EventProperties): void {
    if (!this.isEnabled()) return;
    (window as any).gtag('config', this.measurementId, {
      user_id: userId,
      ...traits,
    });
  }
}

/**
 * Segment Provider (optional)
 */
class SegmentProvider implements AnalyticsProvider {
  name = 'Segment';
  private writeKey: string;
  private isLoaded = false;

  constructor(writeKey: string) {
    this.writeKey = writeKey;
    this.loadScript();
  }

  private loadScript() {
    if (typeof window === 'undefined' || this.isLoaded) return;

    // Check if Segment is already loaded
    if ((window as any).analytics) {
      this.isLoaded = true;
      return;
    }

    // Segment analytics.js snippet (simplified)
    const analytics = (window as any).analytics = (window as any).analytics || [];
    if (!analytics.initialize) {
      if (analytics.invoked) return;
      analytics.invoked = true;
      analytics.methods = ['trackSubmit', 'trackClick', 'trackLink', 'trackForm', 'pageview', 'identify', 'reset', 'group', 'track', 'ready', 'alias', 'debug', 'page', 'once', 'off', 'on'];
      analytics.factory = function(method: string) {
        return function() {
          const args = Array.prototype.slice.call(arguments);
          args.unshift(method);
          analytics.push(args);
          return analytics;
        };
      };
      for (let i = 0; i < analytics.methods.length; i++) {
        const method = analytics.methods[i];
        analytics[method] = analytics.factory(method);
      }
      analytics.load = function(key: string) {
        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.async = true;
        script.src = `https://cdn.segment.com/analytics.js/v1/${key}/analytics.min.js`;
        const first = document.getElementsByTagName('script')[0];
        first?.parentNode?.insertBefore(script, first);
      };
      analytics.SNIPPET_VERSION = '4.1.0';
      analytics.load(this.writeKey);
    }

    this.isLoaded = true;
  }

  isEnabled(): boolean {
    return this.isLoaded && typeof (window as any).analytics?.track === 'function';
  }

  track(event: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    (window as any).analytics.track(event, properties);
  }

  page(name: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    (window as any).analytics.page(name, properties);
  }

  identify(userId: string, traits?: EventProperties): void {
    if (!this.isEnabled()) return;
    (window as any).analytics.identify(userId, traits);
  }
}

/**
 * Custom Backend Provider
 */
class BackendProvider implements AnalyticsProvider {
  name = 'Custom Backend';
  private endpoint: string;
  private sessionId: string;

  constructor(endpoint: string) {
    this.endpoint = endpoint;
    this.sessionId = this.getOrCreateSessionId();
  }

  private getOrCreateSessionId(): string {
    if (typeof sessionStorage === 'undefined') return '';

    let sessionId = sessionStorage.getItem('analytics_session_id');
    if (!sessionId) {
      sessionId = `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
      sessionStorage.setItem('analytics_session_id', sessionId);
    }
    return sessionId;
  }

  isEnabled(): boolean {
    return !!this.endpoint;
  }

  private async send(type: string, data: EventProperties): Promise<void> {
    try {
      await fetch(this.endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          type,
          timestamp: new Date().toISOString(),
          sessionId: this.sessionId,
          url: typeof window !== 'undefined' ? window.location.href : '',
          userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : '',
          ...data,
        }),
      });
    } catch {
      // Silently fail - analytics shouldn't break the app
    }
  }

  track(event: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    this.send('track', { event, properties });
  }

  page(name: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    this.send('page', { name, properties });
  }

  identify(userId: string, traits?: EventProperties): void {
    if (!this.isEnabled()) return;
    this.send('identify', { userId, traits });
  }
}

/**
 * Console Logger Provider (development)
 */
class ConsoleProvider implements AnalyticsProvider {
  name = 'Console';
  private isDev: boolean;

  constructor() {
    this.isDev = typeof process !== 'undefined' && process.env?.NODE_ENV === 'development';
  }

  isEnabled(): boolean {
    return this.isDev;
  }

  track(event: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    console.log('[Analytics] Track:', event, properties);
  }

  page(name: string, properties?: EventProperties): void {
    if (!this.isEnabled()) return;
    console.log('[Analytics] Page:', name, properties);
  }

  identify(userId: string, traits?: EventProperties): void {
    if (!this.isEnabled()) return;
    console.log('[Analytics] Identify:', userId, traits);
  }
}

/**
 * Main Analytics Service
 */
class AnalyticsService {
  private providers: AnalyticsProvider[] = [];
  private config: AnalyticsConfig;
  private consentGiven = false;
  private defaultProperties: EventProperties = {};

  constructor(config: AnalyticsConfig = {}) {
    this.config = {
      respectDoNotTrack: true,
      enableConsoleLogging: true,
      enabled: true,
      ...config,
    };

    this.initialize();
  }

  private initialize() {
    // Check for DNT
    if (this.config.respectDoNotTrack && isDoNotTrackEnabled()) {
      console.log('[Analytics] Do Not Track is enabled, analytics disabled');
      return;
    }

    // Add console provider in development
    if (this.config.enableConsoleLogging) {
      this.providers.push(new ConsoleProvider());
    }

    // Add GA4 provider
    if (this.config.ga4MeasurementId) {
      this.providers.push(new GA4Provider(this.config.ga4MeasurementId));
    }

    // Add Segment provider
    if (this.config.segmentWriteKey) {
      this.providers.push(new SegmentProvider(this.config.segmentWriteKey));
    }

    // Add backend provider
    if (this.config.backendEndpoint) {
      this.providers.push(new BackendProvider(this.config.backendEndpoint));
    }
  }

  /**
   * Set default properties to include with all events
   */
  setDefaultProperties(properties: EventProperties): void {
    this.defaultProperties = { ...this.defaultProperties, ...properties };
  }

  /**
   * Set consent status (for GDPR)
   */
  setConsent(hasConsent: boolean): void {
    this.consentGiven = hasConsent;
  }

  /**
   * Check if analytics can be used
   */
  private canTrack(): boolean {
    if (!this.config.enabled) return false;
    if (this.config.respectDoNotTrack && isDoNotTrackEnabled()) return false;
    // In production, require consent. In dev, allow without consent for testing.
    const isDev = typeof process !== 'undefined' && process.env?.NODE_ENV === 'development';
    if (!isDev && !this.consentGiven) return false;
    return true;
  }

  /**
   * Track an event
   */
  track(event: string, properties?: EventProperties): void {
    if (!this.canTrack()) return;

    const mergedProperties = {
      ...this.defaultProperties,
      ...properties,
      timestamp: new Date().toISOString(),
    };

    for (const provider of this.providers) {
      try {
        provider.track(event, mergedProperties);
      } catch (error) {
        console.error(`[Analytics] Error tracking with ${provider.name}:`, error);
      }
    }
  }

  /**
   * Track a page view
   */
  page(name: string, properties?: EventProperties): void {
    if (!this.canTrack()) return;

    const mergedProperties = {
      ...this.defaultProperties,
      ...properties,
      url: typeof window !== 'undefined' ? window.location.href : '',
      referrer: typeof document !== 'undefined' ? document.referrer : '',
    };

    for (const provider of this.providers) {
      try {
        provider.page(name, mergedProperties);
      } catch (error) {
        console.error(`[Analytics] Error tracking page with ${provider.name}:`, error);
      }
    }
  }

  /**
   * Identify a user
   */
  identify(userId: string, traits?: EventProperties): void {
    if (!this.canTrack()) return;

    // Never include PII in traits
    const sanitizedTraits = { ...traits };

    for (const provider of this.providers) {
      try {
        provider.identify(userId, sanitizedTraits);
      } catch (error) {
        console.error(`[Analytics] Error identifying with ${provider.name}:`, error);
      }
    }
  }

  /**
   * Add a custom provider
   */
  addProvider(provider: AnalyticsProvider): void {
    this.providers.push(provider);
  }

  /**
   * Get list of active providers
   */
  getProviders(): string[] {
    return this.providers.filter(p => p.isEnabled()).map(p => p.name);
  }
}

// Singleton instance
let analyticsInstance: AnalyticsService | null = null;

/**
 * Initialize the analytics service
 */
export function initializeAnalytics(config: AnalyticsConfig = {}): AnalyticsService {
  if (!analyticsInstance) {
    analyticsInstance = new AnalyticsService(config);
  }
  return analyticsInstance;
}

/**
 * Get the analytics service instance
 */
export function getAnalytics(): AnalyticsService {
  if (!analyticsInstance) {
    analyticsInstance = new AnalyticsService();
  }
  return analyticsInstance;
}

/**
 * Convenience function to track an event
 */
export function track(event: string, properties?: EventProperties): void {
  getAnalytics().track(event, properties);
}

/**
 * Convenience function to track a page view
 */
export function page(name: string, properties?: EventProperties): void {
  getAnalytics().page(name, properties);
}

/**
 * Convenience function to identify a user
 */
export function identify(userId: string, traits?: EventProperties): void {
  getAnalytics().identify(userId, traits);
}

export default AnalyticsService;
