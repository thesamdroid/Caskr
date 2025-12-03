import { useMemo } from 'react'
import { useSafeArea } from './useSafeArea'

/**
 * Base height of the bottom navigation bar
 */
const BASE_NAV_HEIGHT = 56

/**
 * Hook to calculate the total bottom navigation height including safe area
 */
export function useBottomNavHeight(): {
  baseHeight: number
  totalHeight: number
  safeAreaBottom: number
} {
  const safeArea = useSafeArea()

  return useMemo(() => ({
    baseHeight: BASE_NAV_HEIGHT,
    totalHeight: BASE_NAV_HEIGHT + safeArea.bottom,
    safeAreaBottom: safeArea.bottom
  }), [safeArea.bottom])
}

export default useBottomNavHeight
