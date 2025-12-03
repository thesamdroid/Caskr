export type SitePreference = 'auto' | 'desktop' | 'mobile'

export type DeviceType = 'Desktop' | 'Mobile' | 'Tablet' | 'Bot' | 'Unknown'

export interface DeviceDetectionResponse {
  deviceType: DeviceType
  deviceName: string
  browser: string
  operatingSystem: string
  isMobile: boolean
  isTablet: boolean
  isBot: boolean
  hasTouchCapability: boolean
  recommendedSite: 'desktop' | 'mobile'
  userPreference?: SitePreferenceResponse
}

export interface SitePreferenceResponse {
  preferredSite: SitePreference
  lastDetectedDevice: string | null
  updatedAt: string | null
}

export interface SaveSitePreferenceRequest {
  preferredSite: SitePreference
  screenWidth?: number
  screenHeight?: number
}

export interface MobileDetectionState {
  isLoading: boolean
  error: string | null
  detection: DeviceDetectionResponse | null
  preference: SitePreference
  screenWidth: number
  screenHeight: number
  isMobileDevice: boolean
  isTabletDevice: boolean
  recommendedSite: 'desktop' | 'mobile'
  shouldRedirect: boolean
}
