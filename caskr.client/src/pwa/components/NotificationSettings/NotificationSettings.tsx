/**
 * NotificationSettings Component
 *
 * UI for managing push notification preferences.
 */

import React, { useState, useCallback } from 'react'
import { usePushNotifications } from '../../push'
import styles from './NotificationSettings.module.css'

export interface NotificationSettingsProps {
  /** Additional class name */
  className?: string
  /** Callback when settings are saved */
  onSaved?: () => void
}

/**
 * Notification Settings Component
 */
export function NotificationSettings({
  className,
  onSaved,
}: NotificationSettingsProps): React.ReactElement {
  const {
    isSupported,
    permission,
    subscribed,
    isLoading,
    error,
    preferences,
    subscriptions,
    requestPermission,
    subscribe,
    unsubscribe,
    updatePreferences,
    removeSubscription,
    sendTest,
  } = usePushNotifications()

  const [isSendingTest, setIsSendingTest] = useState(false)
  const [testSent, setTestSent] = useState(false)

  // Handle master toggle
  const handleMasterToggle = useCallback(async () => {
    if (!preferences) return

    const newEnabled = !preferences.notificationsEnabled
    await updatePreferences({ notificationsEnabled: newEnabled })
    onSaved?.()
  }, [preferences, updatePreferences, onSaved])

  // Handle category toggle
  const handleCategoryToggle = useCallback(
    async (category: 'taskAssignments' | 'taskReminders' | 'complianceAlerts' | 'syncStatus') => {
      if (!preferences) return

      await updatePreferences({ [category]: !preferences[category] })
      onSaved?.()
    },
    [preferences, updatePreferences, onSaved]
  )

  // Handle quiet hours change
  const handleQuietHoursChange = useCallback(
    async (field: 'quietHoursStart' | 'quietHoursEnd', value: string) => {
      await updatePreferences({ [field]: value || undefined })
      onSaved?.()
    },
    [updatePreferences, onSaved]
  )

  // Handle timezone change
  const handleTimezoneChange = useCallback(
    async (timezone: string) => {
      await updatePreferences({ timezone: timezone || undefined })
      onSaved?.()
    },
    [updatePreferences, onSaved]
  )

  // Handle enable notifications
  const handleEnableNotifications = useCallback(async () => {
    if (permission !== 'granted') {
      const perm = await requestPermission()
      if (perm !== 'granted') return
    }
    await subscribe()
  }, [permission, requestPermission, subscribe])

  // Handle disable notifications
  const handleDisableNotifications = useCallback(async () => {
    await unsubscribe()
  }, [unsubscribe])

  // Handle remove subscription
  const handleRemoveSubscription = useCallback(
    async (id: number) => {
      await removeSubscription(id)
    },
    [removeSubscription]
  )

  // Handle send test
  const handleSendTest = useCallback(async () => {
    setIsSendingTest(true)
    setTestSent(false)
    const success = await sendTest()
    setIsSendingTest(false)
    if (success) {
      setTestSent(true)
      setTimeout(() => setTestSent(false), 3000)
    }
  }, [sendTest])

  // Not supported message
  if (!isSupported) {
    return (
      <div className={`${styles.container} ${className || ''}`}>
        <div className={styles.unsupported}>
          <div className={styles.unsupportedIcon}>
            <BellOffIcon />
          </div>
          <h3 className={styles.unsupportedTitle}>Notifications Not Supported</h3>
          <p className={styles.unsupportedText}>
            Push notifications are not available in this browser. Try using a
            modern browser like Chrome, Firefox, or Safari.
          </p>
        </div>
      </div>
    )
  }

  // Permission denied message
  if (permission === 'denied') {
    return (
      <div className={`${styles.container} ${className || ''}`}>
        <div className={styles.denied}>
          <div className={styles.deniedIcon}>
            <BellOffIcon />
          </div>
          <h3 className={styles.deniedTitle}>Notifications Blocked</h3>
          <p className={styles.deniedText}>
            You've blocked notifications for this site. To enable them:
          </p>
          <ol className={styles.deniedSteps}>
            <li>Click the lock icon in your browser's address bar</li>
            <li>Find "Notifications" in the permissions</li>
            <li>Change it from "Block" to "Allow"</li>
            <li>Refresh the page</li>
          </ol>
        </div>
      </div>
    )
  }

  // Not subscribed message
  if (!subscribed) {
    return (
      <div className={`${styles.container} ${className || ''}`}>
        <div className={styles.subscribe}>
          <div className={styles.subscribeIcon}>
            <BellIcon />
          </div>
          <h3 className={styles.subscribeTitle}>Enable Notifications</h3>
          <p className={styles.subscribeText}>
            Get notified about task assignments, compliance deadlines, and more.
          </p>
          <button
            className={styles.enableButton}
            onClick={handleEnableNotifications}
            disabled={isLoading}
          >
            {isLoading ? 'Enabling...' : 'Enable Notifications'}
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className={`${styles.container} ${className || ''}`}>
      {error && <div className={styles.error}>{error}</div>}

      {/* Master Toggle */}
      <div className={styles.section}>
        <div className={styles.toggleRow}>
          <div className={styles.toggleInfo}>
            <span className={styles.toggleLabel}>Push Notifications</span>
            <span className={styles.toggleDescription}>
              Receive notifications on this device
            </span>
          </div>
          <label className={styles.toggle}>
            <input
              type="checkbox"
              checked={preferences?.notificationsEnabled ?? true}
              onChange={handleMasterToggle}
              disabled={isLoading}
            />
            <span className={styles.toggleSlider} />
          </label>
        </div>
      </div>

      {/* Category Toggles */}
      {preferences?.notificationsEnabled && (
        <div className={styles.section}>
          <h4 className={styles.sectionTitle}>Notification Types</h4>

          <div className={styles.toggleRow}>
            <div className={styles.toggleInfo}>
              <span className={styles.toggleLabel}>Task Assignments</span>
              <span className={styles.toggleDescription}>
                When you're assigned a new task
              </span>
            </div>
            <label className={styles.toggle}>
              <input
                type="checkbox"
                checked={preferences.taskAssignments}
                onChange={() => handleCategoryToggle('taskAssignments')}
                disabled={isLoading}
              />
              <span className={styles.toggleSlider} />
            </label>
          </div>

          <div className={styles.toggleRow}>
            <div className={styles.toggleInfo}>
              <span className={styles.toggleLabel}>Task Reminders</span>
              <span className={styles.toggleDescription}>
                When task deadlines are approaching
              </span>
            </div>
            <label className={styles.toggle}>
              <input
                type="checkbox"
                checked={preferences.taskReminders}
                onChange={() => handleCategoryToggle('taskReminders')}
                disabled={isLoading}
              />
              <span className={styles.toggleSlider} />
            </label>
          </div>

          <div className={styles.toggleRow}>
            <div className={styles.toggleInfo}>
              <span className={styles.toggleLabel}>Compliance Alerts</span>
              <span className={styles.toggleDescription}>
                TTB reports and regulatory deadlines
              </span>
            </div>
            <label className={styles.toggle}>
              <input
                type="checkbox"
                checked={preferences.complianceAlerts}
                onChange={() => handleCategoryToggle('complianceAlerts')}
                disabled={isLoading}
              />
              <span className={styles.toggleSlider} />
            </label>
          </div>

          <div className={styles.toggleRow}>
            <div className={styles.toggleInfo}>
              <span className={styles.toggleLabel}>Sync Status</span>
              <span className={styles.toggleDescription}>
                When offline changes are synced
              </span>
            </div>
            <label className={styles.toggle}>
              <input
                type="checkbox"
                checked={preferences.syncStatus}
                onChange={() => handleCategoryToggle('syncStatus')}
                disabled={isLoading}
              />
              <span className={styles.toggleSlider} />
            </label>
          </div>
        </div>
      )}

      {/* Quiet Hours */}
      {preferences?.notificationsEnabled && (
        <div className={styles.section}>
          <h4 className={styles.sectionTitle}>Quiet Hours</h4>
          <p className={styles.sectionDescription}>
            Don't send notifications during these hours
          </p>

          <div className={styles.quietHours}>
            <div className={styles.timeInput}>
              <label>From</label>
              <input
                type="time"
                value={preferences.quietHoursStart || ''}
                onChange={(e) => handleQuietHoursChange('quietHoursStart', e.target.value)}
                disabled={isLoading}
              />
            </div>
            <div className={styles.timeInput}>
              <label>To</label>
              <input
                type="time"
                value={preferences.quietHoursEnd || ''}
                onChange={(e) => handleQuietHoursChange('quietHoursEnd', e.target.value)}
                disabled={isLoading}
              />
            </div>
          </div>

          <div className={styles.timezoneSelect}>
            <label>Timezone</label>
            <select
              value={preferences.timezone || ''}
              onChange={(e) => handleTimezoneChange(e.target.value)}
              disabled={isLoading}
            >
              <option value="">Select timezone</option>
              <option value="America/New_York">Eastern Time</option>
              <option value="America/Chicago">Central Time</option>
              <option value="America/Denver">Mountain Time</option>
              <option value="America/Los_Angeles">Pacific Time</option>
              <option value="America/Anchorage">Alaska Time</option>
              <option value="Pacific/Honolulu">Hawaii Time</option>
              <option value="UTC">UTC</option>
            </select>
          </div>
        </div>
      )}

      {/* Registered Devices */}
      {subscriptions.length > 0 && (
        <div className={styles.section}>
          <h4 className={styles.sectionTitle}>Registered Devices</h4>
          <div className={styles.deviceList}>
            {subscriptions.map((sub) => (
              <div key={sub.id} className={styles.device}>
                <div className={styles.deviceInfo}>
                  <span className={styles.deviceName}>
                    {sub.deviceName || 'Unknown Device'}
                  </span>
                  <span className={styles.deviceDate}>
                    Added {new Date(sub.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <button
                  className={styles.removeButton}
                  onClick={() => handleRemoveSubscription(sub.id)}
                  disabled={isLoading}
                  aria-label="Remove device"
                >
                  <TrashIcon />
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Test & Disable */}
      <div className={styles.section}>
        <div className={styles.actions}>
          <button
            className={styles.testButton}
            onClick={handleSendTest}
            disabled={isSendingTest || isLoading}
          >
            {isSendingTest ? 'Sending...' : testSent ? 'Test Sent!' : 'Send Test Notification'}
          </button>
          <button
            className={styles.disableButton}
            onClick={handleDisableNotifications}
            disabled={isLoading}
          >
            Disable Notifications
          </button>
        </div>
      </div>
    </div>
  )
}

// Icons
function BellIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
      <path d="M13.73 21a2 2 0 0 1-3.46 0" />
    </svg>
  )
}

function BellOffIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      <path d="M18.63 13A17.89 17.89 0 0 1 18 8" />
      <path d="M6.26 6.26A5.86 5.86 0 0 0 6 8c0 7-3 9-3 9h14" />
      <path d="M18 8a6 6 0 0 0-9.33-5" />
      <line x1="1" y1="1" x2="23" y2="23" />
    </svg>
  )
}

function TrashIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="3 6 5 6 21 6" />
      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
    </svg>
  )
}

export default NotificationSettings
