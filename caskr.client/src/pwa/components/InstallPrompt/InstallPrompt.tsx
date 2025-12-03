/**
 * InstallPrompt Component
 *
 * Shows PWA install prompt with platform-specific UI.
 * - Android/Chrome: Direct install button using beforeinstallprompt
 * - iOS: Manual instructions for Add to Home Screen
 */

import React, { useState, useCallback } from 'react'
import { useInstallPrompt } from '../../hooks/useInstallPrompt'
import { trackInstallEvent } from '../../analytics'
import styles from './InstallPrompt.module.css'

export interface InstallPromptProps {
  /** Callback when installation succeeds */
  onInstalled?: () => void
  /** Callback when prompt is dismissed */
  onDismissed?: () => void
  /** Force show regardless of criteria (for testing) */
  forceShow?: boolean
  /** Custom class name */
  className?: string
}

/**
 * PWA Install Prompt Component
 */
export function InstallPrompt({
  onInstalled,
  onDismissed,
  forceShow = false,
  className,
}: InstallPromptProps): React.ReactElement | null {
  const {
    canShow,
    isIOS,
    isAndroid: _isAndroid, // Available for future Android-specific UI
    promptInstall,
    dismiss,
    dontAskAgain,
  } = useInstallPrompt()

  const [isVisible, setIsVisible] = useState(true)
  const [showIOSInstructions, setShowIOSInstructions] = useState(false)
  const [isInstalling, setIsInstalling] = useState(false)

  // Handle Android install
  const handleInstall = useCallback(async () => {
    setIsInstalling(true)
    const success = await promptInstall()
    setIsInstalling(false)

    if (success) {
      setIsVisible(false)
      onInstalled?.()
    }
  }, [promptInstall, onInstalled])

  // Handle iOS instructions toggle
  const handleShowIOSInstructions = useCallback(() => {
    setShowIOSInstructions(true)
    trackInstallEvent('ios_instructions_viewed')
  }, [])

  // Handle dismiss
  const handleDismiss = useCallback(() => {
    dismiss()
    setIsVisible(false)
    onDismissed?.()
  }, [dismiss, onDismissed])

  // Handle don't ask again
  const handleDontAskAgain = useCallback(() => {
    dontAskAgain()
    setIsVisible(false)
    onDismissed?.()
  }, [dontAskAgain, onDismissed])

  // Track when prompt is shown
  React.useEffect(() => {
    if ((canShow || forceShow) && isVisible) {
      trackInstallEvent(isIOS ? 'prompt_shown_ios' : 'prompt_shown')
    }
  }, [canShow, forceShow, isVisible, isIOS])

  // Don't render if hidden or criteria not met
  if (!isVisible || (!canShow && !forceShow)) {
    return null
  }

  // iOS Instructions Modal
  if (showIOSInstructions && isIOS) {
    return (
      <div className={`${styles.overlay} ${className || ''}`}>
        <div className={styles.modal}>
          <div className={styles.header}>
            <h2 className={styles.title}>Install Caskr</h2>
            <button
              className={styles.closeButton}
              onClick={() => setShowIOSInstructions(false)}
              aria-label="Close"
            >
              <CloseIcon />
            </button>
          </div>

          <div className={styles.instructions}>
            <p className={styles.instructionText}>
              Follow these steps to add Caskr to your home screen:
            </p>

            <ol className={styles.stepsList}>
              <li className={styles.step}>
                <div className={styles.stepIcon}>
                  <ShareIcon />
                </div>
                <div className={styles.stepContent}>
                  <strong>Tap the Share button</strong>
                  <span>at the bottom of your browser</span>
                </div>
              </li>
              <li className={styles.step}>
                <div className={styles.stepIcon}>
                  <PlusSquareIcon />
                </div>
                <div className={styles.stepContent}>
                  <strong>Scroll and tap "Add to Home Screen"</strong>
                  <span>from the share menu options</span>
                </div>
              </li>
              <li className={styles.step}>
                <div className={styles.stepIcon}>
                  <CheckIcon />
                </div>
                <div className={styles.stepContent}>
                  <strong>Tap "Add"</strong>
                  <span>to confirm and install the app</span>
                </div>
              </li>
            </ol>
          </div>

          <div className={styles.actions}>
            <button
              className={styles.primaryButton}
              onClick={() => setShowIOSInstructions(false)}
            >
              Got it!
            </button>
          </div>
        </div>
      </div>
    )
  }

  // Main prompt UI
  return (
    <div className={`${styles.prompt} ${className || ''}`}>
      <div className={styles.content}>
        <div className={styles.iconWrapper}>
          <img
            src="/icons/icon-96x96.png"
            alt="Caskr"
            className={styles.appIcon}
          />
        </div>

        <div className={styles.textContent}>
          <h3 className={styles.promptTitle}>Install Caskr</h3>
          <p className={styles.promptDescription}>
            Add to your home screen for quick access and offline support.
          </p>
        </div>
      </div>

      <div className={styles.buttons}>
        {isIOS ? (
          <button
            className={styles.primaryButton}
            onClick={handleShowIOSInstructions}
          >
            How to Install
          </button>
        ) : (
          <button
            className={styles.primaryButton}
            onClick={handleInstall}
            disabled={isInstalling}
          >
            {isInstalling ? 'Installing...' : 'Install'}
          </button>
        )}

        <button
          className={styles.secondaryButton}
          onClick={handleDismiss}
        >
          Not now
        </button>
      </div>

      <button
        className={styles.dontAskButton}
        onClick={handleDontAskAgain}
      >
        Don't ask again
      </button>
    </div>
  )
}

// Icon Components
function CloseIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1="18" y1="6" x2="6" y2="18" />
      <line x1="6" y1="6" x2="18" y2="18" />
    </svg>
  )
}

function ShareIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M4 12v8a2 2 0 002 2h12a2 2 0 002-2v-8" />
      <polyline points="16,6 12,2 8,6" />
      <line x1="12" y1="2" x2="12" y2="15" />
    </svg>
  )
}

function PlusSquareIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
      <line x1="12" y1="8" x2="12" y2="16" />
      <line x1="8" y1="12" x2="16" y2="12" />
    </svg>
  )
}

function CheckIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <polyline points="20,6 9,17 4,12" />
    </svg>
  )
}

export default InstallPrompt
