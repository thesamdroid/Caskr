/**
 * Install Analytics Module
 *
 * Tracks PWA installation-related events for analytics and user behavior understanding.
 */

export type InstallEvent =
  | 'beforeinstallprompt_fired'
  | 'prompt_shown'
  | 'prompt_shown_ios'
  | 'install_clicked'
  | 'appinstalled'
  | 'prompt_dismissed'
  | 'dont_ask_again'
  | 'ios_instructions_viewed'
  | 'install_banner_shown'
  | 'install_banner_dismissed'

export interface InstallEventData {
  event: InstallEvent
  timestamp: string
  platform: 'ios' | 'android' | 'desktop' | 'unknown'
  userAgent: string
  referrer?: string
  sessionId?: string
  engagementScore?: number
  outcome?: 'accepted' | 'dismissed' | 'timeout'
}

// Storage key for install analytics
const ANALYTICS_STORAGE_KEY = 'caskr_install_analytics'
const SESSION_ID_KEY = 'caskr_session_id'

/**
 * Generate a unique session ID
 */
function generateSessionId(): string {
  return `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`
}

/**
 * Get or create session ID
 */
function getSessionId(): string {
  let sessionId = sessionStorage.getItem(SESSION_ID_KEY)
  if (!sessionId) {
    sessionId = generateSessionId()
    sessionStorage.setItem(SESSION_ID_KEY, sessionId)
  }
  return sessionId
}

/**
 * Detect platform from user agent
 */
export function detectPlatform(): 'ios' | 'android' | 'desktop' | 'unknown' {
  const ua = navigator.userAgent.toLowerCase()

  if (/iphone|ipad|ipod/.test(ua)) {
    return 'ios'
  }
  if (/android/.test(ua)) {
    return 'android'
  }
  if (/windows|mac|linux/.test(ua) && !/mobile/.test(ua)) {
    return 'desktop'
  }
  return 'unknown'
}

/**
 * Track an install-related event
 */
export function trackInstallEvent(
  event: InstallEvent,
  additionalData?: Partial<InstallEventData>
): void {
  const eventData: InstallEventData = {
    event,
    timestamp: new Date().toISOString(),
    platform: detectPlatform(),
    userAgent: navigator.userAgent,
    referrer: document.referrer || undefined,
    sessionId: getSessionId(),
    ...additionalData,
  }

  // Store locally
  storeEventLocally(eventData)

  // Send to analytics backend (if available)
  sendToAnalytics(eventData)

  // Log in development
  if (process.env.NODE_ENV === 'development') {
    console.log('[InstallAnalytics]', event, eventData)
  }
}

/**
 * Store event in localStorage for later retrieval
 */
function storeEventLocally(eventData: InstallEventData): void {
  try {
    const stored = localStorage.getItem(ANALYTICS_STORAGE_KEY)
    const events: InstallEventData[] = stored ? JSON.parse(stored) : []

    events.push(eventData)

    // Keep only last 100 events
    const trimmed = events.slice(-100)
    localStorage.setItem(ANALYTICS_STORAGE_KEY, JSON.stringify(trimmed))
  } catch (error) {
    console.error('[InstallAnalytics] Failed to store event:', error)
  }
}

/**
 * Send event to analytics backend
 */
async function sendToAnalytics(eventData: InstallEventData): Promise<void> {
  try {
    // Post to analytics endpoint if available
    const response = await fetch('/api/analytics/install', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(eventData),
    })

    if (!response.ok && response.status !== 404) {
      console.warn('[InstallAnalytics] Failed to send to backend:', response.status)
    }
  } catch {
    // Silently fail - analytics shouldn't break the app
  }
}

/**
 * Get all stored analytics events
 */
export function getStoredEvents(): InstallEventData[] {
  try {
    const stored = localStorage.getItem(ANALYTICS_STORAGE_KEY)
    return stored ? JSON.parse(stored) : []
  } catch {
    return []
  }
}

/**
 * Clear stored analytics events
 */
export function clearStoredEvents(): void {
  localStorage.removeItem(ANALYTICS_STORAGE_KEY)
}

/**
 * Get install funnel summary
 */
export function getInstallFunnelSummary(): Record<InstallEvent, number> {
  const events = getStoredEvents()
  const summary: Record<InstallEvent, number> = {
    beforeinstallprompt_fired: 0,
    prompt_shown: 0,
    prompt_shown_ios: 0,
    install_clicked: 0,
    appinstalled: 0,
    prompt_dismissed: 0,
    dont_ask_again: 0,
    ios_instructions_viewed: 0,
    install_banner_shown: 0,
    install_banner_dismissed: 0,
  }

  for (const event of events) {
    summary[event.event]++
  }

  return summary
}

/**
 * Calculate conversion rate
 */
export function getConversionRate(): number {
  const summary = getInstallFunnelSummary()
  const prompted = summary.prompt_shown + summary.prompt_shown_ios
  const installed = summary.appinstalled

  if (prompted === 0) return 0
  return (installed / prompted) * 100
}
