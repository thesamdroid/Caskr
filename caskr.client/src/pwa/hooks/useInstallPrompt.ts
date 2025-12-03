/**
 * useInstallPrompt Hook
 *
 * Manages PWA install prompt state and behavior.
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { trackInstallEvent, detectPlatform } from '../analytics'
import { isRunningAsPWA } from '../serviceWorkerRegistration'

// Storage keys
const DISMISS_KEY = 'caskr_install_dismissed_at'
const DONT_ASK_KEY = 'caskr_install_dont_ask'
const VISIT_COUNT_KEY = 'caskr_visit_count'
const TASK_COUNT_KEY = 'caskr_task_completed_count'

// Configuration
const DISMISS_TIMEOUT_DAYS = 7
const REQUIRED_VISITS = 2
const REQUIRED_TASKS = 3

// BeforeInstallPromptEvent type
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

export interface InstallPromptState {
  /** Whether the install prompt can be shown */
  canShow: boolean
  /** Whether the device is iOS */
  isIOS: boolean
  /** Whether the device is Android */
  isAndroid: boolean
  /** Whether the app is already installed */
  isInstalled: boolean
  /** Whether on desktop */
  isDesktop: boolean
  /** Whether user has permanently dismissed */
  isDontAskAgain: boolean
  /** Whether engagement criteria is met */
  isEngagementMet: boolean
  /** Current visit count */
  visitCount: number
  /** Current task completion count */
  taskCount: number
  /** Trigger the native install prompt (Android/Chrome) */
  promptInstall: () => Promise<boolean>
  /** Dismiss the prompt temporarily */
  dismiss: () => void
  /** Permanently dismiss the prompt */
  dontAskAgain: () => void
  /** Mark a task as completed (for engagement tracking) */
  markTaskCompleted: () => void
  /** Reset all dismiss state (for testing) */
  reset: () => void
}

/**
 * Hook for managing PWA install prompt
 */
export function useInstallPrompt(): InstallPromptState {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null)
  const [isInstalled, setIsInstalled] = useState(false)
  const [isDontAskAgain, setIsDontAskAgain] = useState(false)
  const [isDismissed, setIsDismissed] = useState(false)
  const [visitCount, setVisitCount] = useState(0)
  const [taskCount, setTaskCount] = useState(0)
  const promptEventFiredRef = useRef(false)

  const platform = detectPlatform()
  const isIOS = platform === 'ios'
  const isAndroid = platform === 'android'
  const isDesktop = platform === 'desktop'

  // Check if engagement criteria is met
  const isEngagementMet = visitCount >= REQUIRED_VISITS || taskCount >= REQUIRED_TASKS

  // Initialize state from localStorage
  useEffect(() => {
    // Check if already installed
    setIsInstalled(isRunningAsPWA())

    // Check "don't ask again"
    const dontAsk = localStorage.getItem(DONT_ASK_KEY)
    setIsDontAskAgain(dontAsk === 'true')

    // Check temporary dismissal
    const dismissedAt = localStorage.getItem(DISMISS_KEY)
    if (dismissedAt) {
      const dismissDate = new Date(dismissedAt)
      const now = new Date()
      const daysSinceDismiss = (now.getTime() - dismissDate.getTime()) / (1000 * 60 * 60 * 24)
      setIsDismissed(daysSinceDismiss < DISMISS_TIMEOUT_DAYS)
    }

    // Load visit count
    const storedVisits = parseInt(localStorage.getItem(VISIT_COUNT_KEY) || '0', 10)
    const newVisitCount = storedVisits + 1
    localStorage.setItem(VISIT_COUNT_KEY, newVisitCount.toString())
    setVisitCount(newVisitCount)

    // Load task count
    const storedTasks = parseInt(localStorage.getItem(TASK_COUNT_KEY) || '0', 10)
    setTaskCount(storedTasks)
  }, [])

  // Listen for beforeinstallprompt event
  useEffect(() => {
    const handleBeforeInstallPrompt = (e: Event) => {
      e.preventDefault()
      setDeferredPrompt(e as BeforeInstallPromptEvent)

      if (!promptEventFiredRef.current) {
        promptEventFiredRef.current = true
        trackInstallEvent('beforeinstallprompt_fired')
      }
    }

    const handleAppInstalled = () => {
      setIsInstalled(true)
      setDeferredPrompt(null)
      trackInstallEvent('appinstalled')
    }

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    window.addEventListener('appinstalled', handleAppInstalled)

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
      window.removeEventListener('appinstalled', handleAppInstalled)
    }
  }, [])

  // Trigger native install prompt
  const promptInstall = useCallback(async (): Promise<boolean> => {
    if (!deferredPrompt) {
      return false
    }

    trackInstallEvent('install_clicked')

    try {
      await deferredPrompt.prompt()
      const { outcome } = await deferredPrompt.userChoice

      if (outcome === 'accepted') {
        setDeferredPrompt(null)
        return true
      } else {
        trackInstallEvent('prompt_dismissed', { outcome })
        return false
      }
    } catch (error) {
      console.error('[useInstallPrompt] Error prompting install:', error)
      return false
    }
  }, [deferredPrompt])

  // Dismiss temporarily
  const dismiss = useCallback(() => {
    localStorage.setItem(DISMISS_KEY, new Date().toISOString())
    setIsDismissed(true)
    trackInstallEvent('prompt_dismissed')
  }, [])

  // Permanently dismiss
  const dontAskAgain = useCallback(() => {
    localStorage.setItem(DONT_ASK_KEY, 'true')
    setIsDontAskAgain(true)
    trackInstallEvent('dont_ask_again')
  }, [])

  // Mark task completed
  const markTaskCompleted = useCallback(() => {
    const newCount = taskCount + 1
    localStorage.setItem(TASK_COUNT_KEY, newCount.toString())
    setTaskCount(newCount)
  }, [taskCount])

  // Reset state (for testing)
  const reset = useCallback(() => {
    localStorage.removeItem(DISMISS_KEY)
    localStorage.removeItem(DONT_ASK_KEY)
    localStorage.removeItem(VISIT_COUNT_KEY)
    localStorage.removeItem(TASK_COUNT_KEY)
    setIsDismissed(false)
    setIsDontAskAgain(false)
    setVisitCount(0)
    setTaskCount(0)
    promptEventFiredRef.current = false
  }, [])

  // Determine if we can show the prompt
  const canShow =
    !isInstalled && // Not already installed
    !isDesktop && // Not on desktop
    !isDontAskAgain && // User hasn't permanently dismissed
    !isDismissed && // User hasn't temporarily dismissed
    isEngagementMet && // Engagement criteria met
    (isIOS || isAndroid || !!deferredPrompt) // Has platform support

  return {
    canShow,
    isIOS,
    isAndroid,
    isInstalled,
    isDesktop,
    isDontAskAgain,
    isEngagementMet,
    visitCount,
    taskCount,
    promptInstall,
    dismiss,
    dontAskAgain,
    markTaskCompleted,
    reset,
  }
}

export default useInstallPrompt
