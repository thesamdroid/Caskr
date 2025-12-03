/**
 * SiteSwitcher Component
 *
 * Allows users to switch between desktop and mobile site versions.
 * Supports multiple variants: banner, compact, modal, toast.
 */

import React, { useState, useCallback } from 'react'
import { useSiteSwitcher, type SiteVersion } from './useSiteSwitcher'
import styles from './SiteSwitcher.module.css'

export type SiteSwitcherVariant = 'banner' | 'compact' | 'modal' | 'toast'

export interface SiteSwitcherProps {
  /** Visual variant */
  variant?: SiteSwitcherVariant
  /** Force show regardless of auto-detection */
  forceShow?: boolean
  /** Callback when user switches site */
  onSwitch?: (version: SiteVersion) => void
  /** Callback when user dismisses */
  onDismiss?: () => void
  /** Additional class name */
  className?: string
  /** Position for banner variant */
  position?: 'top' | 'bottom'
}

/**
 * Site Switcher Component
 */
export function SiteSwitcher({
  variant = 'banner',
  forceShow = false,
  onSwitch,
  onDismiss,
  className,
  position = 'bottom',
}: SiteSwitcherProps): React.ReactElement | null {
  const {
    currentSite,
    detectedDevice,
    isMismatch,
    preference,
    shouldShowBanner,
    isLoading,
    switchSite,
    dismissBanner,
    dismissBannerPermanently,
  } = useSiteSwitcher()

  const [rememberChoice, setRememberChoice] = useState(true)
  const [showModal, setShowModal] = useState(false)

  // Handle switch
  const handleSwitch = useCallback(
    async (version: SiteVersion) => {
      await switchSite(version, rememberChoice)
      onSwitch?.(version)
    },
    [switchSite, rememberChoice, onSwitch]
  )

  // Handle dismiss
  const handleDismiss = useCallback(() => {
    dismissBanner()
    onDismiss?.()
  }, [dismissBanner, onDismiss])

  // Handle don't ask again
  const handleDontAskAgain = useCallback(() => {
    dismissBannerPermanently()
    onDismiss?.()
  }, [dismissBannerPermanently, onDismiss])

  // Don't render if not needed
  if (!forceShow && !shouldShowBanner) {
    // Render compact variant if preference is set
    if (variant === 'compact' && preference !== 'auto') {
      return (
        <button
          className={`${styles.compactButton} ${className || ''}`}
          onClick={() => setShowModal(true)}
          aria-label="Switch site version"
        >
          {currentSite === 'mobile' ? <MobileIcon /> : <DesktopIcon />}
        </button>
      )
    }
    return null
  }

  const targetSite = currentSite === 'mobile' ? 'desktop' : 'mobile'
  const deviceMessage =
    detectedDevice === 'mobile'
      ? "We detected you're on a mobile device"
      : detectedDevice === 'desktop'
        ? "We detected you're on a desktop"
        : "You're viewing the " + currentSite + ' site'

  // Banner variant
  if (variant === 'banner') {
    return (
      <div
        className={`${styles.banner} ${styles[position]} ${className || ''}`}
        role="region"
        aria-label="Site version switcher"
      >
        <div className={styles.bannerContent}>
          <div className={styles.bannerIcon}>
            {detectedDevice === 'mobile' ? <MobileIcon /> : <DesktopIcon />}
          </div>

          <div className={styles.bannerText}>
            <p className={styles.bannerMessage}>{deviceMessage}</p>
            {isMismatch && (
              <p className={styles.bannerSuggestion}>
                Would you like to switch to the {targetSite} site?
              </p>
            )}
          </div>
        </div>

        <div className={styles.bannerActions}>
          <label className={styles.rememberCheckbox}>
            <input
              type="checkbox"
              checked={rememberChoice}
              onChange={(e) => setRememberChoice(e.target.checked)}
            />
            <span>Remember my choice</span>
          </label>

          <div className={styles.bannerButtons}>
            <button
              className={styles.switchButton}
              onClick={() => handleSwitch(targetSite)}
              disabled={isLoading}
            >
              Switch to {targetSite}
            </button>
            <button
              className={styles.stayButton}
              onClick={handleDismiss}
            >
              Stay here
            </button>
          </div>
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

  // Toast variant
  if (variant === 'toast') {
    return (
      <div
        className={`${styles.toast} ${className || ''}`}
        role="alert"
      >
        <div className={styles.toastContent}>
          <span className={styles.toastMessage}>
            {isMismatch ? `Try the ${targetSite} site?` : deviceMessage}
          </span>
          <div className={styles.toastActions}>
            <button
              className={styles.toastSwitch}
              onClick={() => handleSwitch(targetSite)}
              disabled={isLoading}
            >
              Switch
            </button>
            <button
              className={styles.toastDismiss}
              onClick={handleDismiss}
              aria-label="Dismiss"
            >
              <CloseIcon />
            </button>
          </div>
        </div>
      </div>
    )
  }

  // Compact variant button that opens modal
  if (variant === 'compact') {
    return (
      <>
        <button
          className={`${styles.compactButton} ${styles.compactButtonActive} ${className || ''}`}
          onClick={() => setShowModal(true)}
          aria-label="Switch site version"
        >
          {currentSite === 'mobile' ? <MobileIcon /> : <DesktopIcon />}
          <span className={styles.compactBadge} />
        </button>

        {showModal && (
          <SiteSwitcherModal
            currentSite={currentSite}
            targetSite={targetSite}
            deviceMessage={deviceMessage}
            rememberChoice={rememberChoice}
            setRememberChoice={setRememberChoice}
            isLoading={isLoading}
            onSwitch={handleSwitch}
            onDismiss={() => {
              setShowModal(false)
              handleDismiss()
            }}
            onDontAskAgain={() => {
              setShowModal(false)
              handleDontAskAgain()
            }}
            onClose={() => setShowModal(false)}
          />
        )}
      </>
    )
  }

  // Modal variant
  if (variant === 'modal') {
    return (
      <SiteSwitcherModal
        currentSite={currentSite}
        targetSite={targetSite}
        deviceMessage={deviceMessage}
        rememberChoice={rememberChoice}
        setRememberChoice={setRememberChoice}
        isLoading={isLoading}
        onSwitch={handleSwitch}
        onDismiss={handleDismiss}
        onDontAskAgain={handleDontAskAgain}
        onClose={handleDismiss}
        className={className}
      />
    )
  }

  return null
}

// Modal subcomponent
interface SiteSwitcherModalProps {
  currentSite: SiteVersion
  targetSite: SiteVersion
  deviceMessage: string
  rememberChoice: boolean
  setRememberChoice: (value: boolean) => void
  isLoading: boolean
  onSwitch: (version: SiteVersion) => void
  onDismiss: () => void
  onDontAskAgain: () => void
  onClose: () => void
  className?: string
}

function SiteSwitcherModal({
  currentSite,
  targetSite: _targetSite,
  deviceMessage,
  rememberChoice,
  setRememberChoice,
  isLoading,
  onSwitch,
  onDismiss,
  onDontAskAgain,
  onClose,
  className,
}: SiteSwitcherModalProps) {
  // _targetSite is available for future use (e.g., highlighting recommended option)
  return (
    <div
      className={`${styles.overlay} ${className || ''}`}
      role="dialog"
      aria-modal="true"
      aria-labelledby="site-switcher-title"
    >
      <div className={styles.modal}>
        <div className={styles.modalHeader}>
          <h2 id="site-switcher-title" className={styles.modalTitle}>
            Choose Site Version
          </h2>
          <button
            className={styles.closeButton}
            onClick={onClose}
            aria-label="Close"
          >
            <CloseIcon />
          </button>
        </div>

        <div className={styles.modalContent}>
          <p className={styles.modalMessage}>{deviceMessage}</p>

          <div className={styles.siteOptions}>
            <button
              className={`${styles.siteOption} ${currentSite === 'desktop' ? styles.siteOptionActive : ''}`}
              onClick={() => onSwitch('desktop')}
              disabled={isLoading}
            >
              <DesktopIcon />
              <span>Desktop Site</span>
              {currentSite === 'desktop' && <span className={styles.currentBadge}>Current</span>}
            </button>

            <button
              className={`${styles.siteOption} ${currentSite === 'mobile' ? styles.siteOptionActive : ''}`}
              onClick={() => onSwitch('mobile')}
              disabled={isLoading}
            >
              <MobileIcon />
              <span>Mobile Site</span>
              {currentSite === 'mobile' && <span className={styles.currentBadge}>Current</span>}
            </button>
          </div>

          <label className={styles.rememberCheckbox}>
            <input
              type="checkbox"
              checked={rememberChoice}
              onChange={(e) => setRememberChoice(e.target.checked)}
            />
            <span>Remember my choice</span>
          </label>
        </div>

        <div className={styles.modalFooter}>
          <button className={styles.stayButton} onClick={onDismiss}>
            Keep current
          </button>
          <button className={styles.dontAskButton} onClick={onDontAskAgain}>
            Don't ask again
          </button>
        </div>
      </div>
    </div>
  )
}

// Icons
function MobileIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="5" y="2" width="14" height="20" rx="2" ry="2" />
      <line x1="12" y1="18" x2="12.01" y2="18" />
    </svg>
  )
}

function DesktopIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
      <line x1="8" y1="21" x2="16" y2="21" />
      <line x1="12" y1="17" x2="12" y2="21" />
    </svg>
  )
}

function CloseIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="6" x2="6" y2="18" />
      <line x1="6" y1="6" x2="18" y2="18" />
    </svg>
  )
}

export default SiteSwitcher
