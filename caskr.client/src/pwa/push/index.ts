export {
  isPushSupported,
  getNotificationPermission,
  requestNotificationPermission,
  subscribeToPush,
  unsubscribeFromPush,
  isSubscribed,
  getCurrentSubscription,
  getUserSubscriptions,
  removeSubscription,
  getNotificationPreferences,
  updateNotificationPreferences,
  sendTestNotification,
} from './pushSubscription'

export type {
  PushSubscriptionData,
  NotificationPreferences,
} from './pushSubscription'

export { usePushNotifications } from './usePushNotifications'
export type { PushNotificationState } from './usePushNotifications'
